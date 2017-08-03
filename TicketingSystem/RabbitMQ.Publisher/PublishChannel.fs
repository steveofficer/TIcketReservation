module RabbitMQ.Publisher
open RabbitMQ.Client
open Newtonsoft.Json
open System.Threading
open System.Collections.Concurrent
open System.Linq

type PublishChannel(applicationName : string, connection : IConnection) =
    // The acks that are still waiting to be confirmed
    let _pendingAcks = new ConcurrentDictionary<uint64, ManualResetEventSlim>()

    let _channel = connection.CreateModel()
    
    // Use Publisher Confirms
    do _channel.ConfirmSelect()

    // Set up the subscribers to the publisher acknowledgments
    let (multiple, single) = _channel.BasicAcks |> Observable.partition (fun x -> x.Multiple)
    
    // Handle multiple acknowledgements
    do multiple |> Observable.add (fun m -> (
        [ _pendingAcks.Keys.First() .. m.DeliveryTag ] 
        |> List.iter 
            (fun x -> 
                match _pendingAcks.TryRemove x with
                | true, handle -> handle.Set() 
                | _ -> ()
            )
    ))
    
    // Handle a single acknowledgement
    do single |> Observable.add (fun s -> (
        match _pendingAcks.TryRemove s.DeliveryTag with
        | true, handle -> handle.Set() 
        | _ -> ()
    ))

    member x.registerEvents(assemblies : string[]) =
        let (|Command|Event|Ignore|) (t : System.Type) = 
            if t.Name.EndsWith("Event") then Event
            elif t.Name.EndsWith("Command") then Command
            else Ignore

        let isExchangeType = function
            | Command -> true
            | Event -> true
            | Ignore -> false
            
        // Loop through all the assemblies, get the event types and create an exchange for each type
        assemblies
        |> Array.map (fun path -> System.Reflection.Assembly.ReflectionOnlyLoad(path))
        |> Array.collect (fun a -> a.GetTypes().Where(isExchangeType).ToArray())
        |> Array.iter (fun t -> _channel.ExchangeDeclare(t.FullName, "fanout", true, false))

    member x.publish (message : obj) = async {
        let properties = _channel.CreateBasicProperties()
        properties.Headers <- [| 
            ("MessageId", System.Guid.NewGuid().ToString() :> obj);
            ("SentAt", System.DateTime.UtcNow.ToString("O") :> obj); 
            ("SentFrom", System.Environment.MachineName :> obj); 
            ("Source", applicationName :> obj);
            ("EnclosedType", message.GetType().FullName :> obj)
        |] |> dict
        
        // Use persistent messages
        properties.Persistent <- true
        properties.ContentType <- "application/json"
        
        let body = JsonConvert.SerializeObject(message) |> System.Text.UTF8Encoding.UTF8.GetBytes
        
        let mutable waitHandle : WaitHandle option = None
        System.Threading.Monitor.Enter(_channel)
        try
            waitHandle <- 
                _pendingAcks.AddOrUpdate(
                    _channel.NextPublishSeqNo, 
                    System.Func<_,_>(fun _ -> new ManualResetEventSlim()), 
                    System.Func<_,_,_>(fun _ x -> x)
                ).WaitHandle |> Some
        
            _channel.BasicPublish(message.GetType().FullName, "", properties, body)
        finally
            System.Threading.Monitor.Exit(_channel)

        let! x = Async.AwaitWaitHandle waitHandle.Value
        waitHandle.Value.Dispose()
        ()
    }
