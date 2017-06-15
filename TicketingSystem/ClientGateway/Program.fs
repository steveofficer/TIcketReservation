open RabbitMQ.Client
open RabbitMQ.Client.Events
open RabbitMQ.Subscriber
open MongoDB.Driver
open Handlers

[<EntryPoint>]
let main argv =
    // Create the connection to MongoDB
    let settings = System.Configuration.ConfigurationManager.ConnectionStrings
    let mongoClient = MongoDB.Driver.MongoClient(settings.["mongo"].ConnectionString)
    let mongoDb = mongoClient.GetDatabase(System.Configuration.ConfigurationManager.AppSettings.["database"])

    // Create the connection to RabbitMQ
    let rabbitFactory = RabbitMQ.Client.ConnectionFactory(uri = System.Uri(settings.["rabbit"].ConnectionString))
    let connection = rabbitFactory.CreateConnection()

    // Set up the subscribers
    let service = Service(connection, "ClientGateway")
    TicketsAllocationFailedHandler(mongoDb.GetCollection("Gateway")) |> service.``add subscriber``
    TicketsAllocatedHandler(mongoDb.GetCollection("Gateway")) |> service.``add subscriber``
    TicketsCancelledHandler(mongoDb.GetCollection("Gateway")) |> service.``add subscriber``
    
    // Start the service
    service.Start()
    printfn "Waiting for messages..."
    System.Console.ReadLine() |> ignore
    0