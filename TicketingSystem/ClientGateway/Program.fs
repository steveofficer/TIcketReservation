open RabbitMQ.Client
open RabbitMQ.Client.Events
open RabbitMQ.Subscriber
open MongoDB.Driver
open MongoDB.Bson
open HttpNotification
open Handlers

type Callback = {
    EventId : string
    Url : string
}

[<EntryPoint>]
let main argv =
    // Create the connection to MongoDB
    let settings = System.Configuration.ConfigurationManager.ConnectionStrings
    let mongoClient = MongoDB.Driver.MongoClient(settings.["mongo"].ConnectionString)
    let mongoDb = mongoClient.GetDatabase(System.Configuration.ConfigurationManager.AppSettings.["database"])
    let collection = mongoDb.GetCollection<Callback>("Gateway")

    // Create the connection to RabbitMQ
    let rabbitFactory = RabbitMQ.Client.ConnectionFactory(uri = System.Uri(settings.["rabbit"].ConnectionString))
    let connection = rabbitFactory.CreateConnection()

    // Find the callback
    let findCallback eventId = async {
        let! callback = collection.Find(fun c -> c.EventId = eventId).FirstAsync() |> Async.AwaitTask
        return callback.Url
    }

    // Set up the subscribers
    let service = Service(connection, "ClientGateway")
    TicketsAllocationFailedHandler(``deliver notification``, findCallback) |> service.``add subscriber``
    TicketsAllocatedHandler(``deliver notification``, findCallback) |> service.``add subscriber``
    TicketsCancelledHandler(``deliver notification``, findCallback) |> service.``add subscriber``
    
    // Start the service
    service.Start()
    printfn "Waiting for messages..."
    0