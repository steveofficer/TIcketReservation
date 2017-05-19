module RabbitMQ.Publisher
open RabbitMQ.Client
open Newtonsoft.Json
open System.Threading
open System.Collections.Concurrent
open System.Linq

type PublishChannel(applicationName : string, connection : IConnection) =
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
        // Loop through all the assemblies, get the event types and create an exchange for each type
        assemblies
        |> Array.map (fun path -> System.Reflection.Assembly.ReflectionOnlyLoad(path))
        |> Array.collect (fun a -> a.GetTypes().Where(fun t -> t.Name.EndsWith("Event")).ToArray())
        |> Array.iter (fun t -> _channel.ExchangeDeclare(t.FullName, "fanout", true, false))

    member x.publish (message : obj) = async {
        let properties = _channel.CreateBasicProperties()
        properties.Headers <- [| ("SentAt", System.DateTime.UtcNow :> obj); ("SentFrom", System.Environment.MachineName :> obj); ("Source", applicationName :> obj) |] |> dict
        // Use persistent messages
        properties.Persistent <- true
        properties.ContentType <- "application/json"
        
        let body = JsonConvert.SerializeObject(message) |> System.Text.UTF8Encoding.UTF8.GetBytes
        
        let waitHandle = 
            _pendingAcks.AddOrUpdate(
                _channel.NextPublishSeqNo, 
                System.Func<_,_>(fun _ -> new ManualResetEventSlim()), 
                System.Func<_,_,_>(fun _ x -> x)
            ).WaitHandle
        
        _channel.BasicPublish(message.GetType().FullName, "", properties, body)
        let! x = Async.AwaitWaitHandle waitHandle
        waitHandle.Dispose()
        ()
    }
