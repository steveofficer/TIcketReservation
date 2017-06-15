namespace RabbitMQ.Subscriber

open RabbitMQ.Client
open RabbitMQ.Client.Events

/// The base class for RabbitMQ message handlers
[<AbstractClass>]
type MessageHandlerBase() = 
    abstract member Handle : byte[] -> Async<unit>
    abstract MessageType : System.Type with get

/// An implementation base class for RabbitMQ message handlers that deserializes the content from JSON into a message type
[<AbstractClass>]
type MessageHandler<'T>() =
    inherit MessageHandlerBase()
    abstract Handle : 'T -> Async<unit>
    override this.Handle(content : byte[]) = async {
        let message = Newtonsoft.Json.JsonConvert.DeserializeObject<'T>(content |> System.Text.Encoding.UTF8.GetString)
        do! this.Handle(message)
    }
    override this.MessageType with get() = typeof<'T>

[<AbstractClass>]
type PublishingMessageHandler<'T>(publish) =
    inherit MessageHandler<'T>()

/// A helper class that initializes and manages message receipt from RabbitMQ
type Service(connection : RabbitMQ.Client.IConnection, queue_name) =
    let subscription_channel = connection.CreateModel()
    do subscription_channel.QueueDeclare(queue_name, true, false, false) |> ignore
    
    let handlers = System.Collections.Generic.Dictionary<string, MessageHandlerBase>()

    /// Adds a handler that subscribes to events from other services
    member this.``add subscriber`` (handler : MessageHandlerBase)  =
        let exchange = handler.MessageType.FullName
        subscription_channel.ExchangeDeclare(exchange, "fanout", true, false)
        subscription_channel.QueueBind(queue_name, exchange, "")
        handlers.Add(exchange, handler)

    member this.Start() = 
        let eventingConsumer = EventingBasicConsumer(subscription_channel)
        eventingConsumer.Received |> Observable.add(fun msg -> 
            let messageType = msg.BasicProperties.Headers.["EnclosedType"].ToString()
           
            // deliver the message to its corresponding handler
            match handlers.TryGetValue(messageType) with
            | true, handler -> 
                // We found a handler now invoke it (asynchronously)
                async {
                    try 
                        do! handler.Handle(msg.Body)
                        // Mark the message as acknowledged so it gets removed from the queue
                        subscription_channel.BasicAck(msg.DeliveryTag, multiple = false)
                    with error -> 
                        // Something went wrong while handling the message, so requeue it
                        printfn "Error while handling message: %s" error.Message
                        // Mark the message as failed so it gets retried
                        subscription_channel.BasicReject(msg.DeliveryTag, requeue = not msg.Redelivered)
                } |> Async.Start
            | _ -> 
                // We couldn't find a handler for the message. Reject it so we can delete it from the queue
                printfn "Received an unknown message"
                subscription_channel.BasicReject(msg.DeliveryTag, requeue = false)
        )
        subscription_channel.BasicConsume(queue_name, false, eventingConsumer) |> ignore