open RabbitMQ.Client
open RabbitMQ.Client.Events
open RabbitMQ.Subscriber
open MongoDB.Driver
open Handlers

open PricingService.Contract.Events

[<EntryPoint>]
let main argv =
    // Map the serializer to the Transaction model
    MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<Model.Transaction>(fun cm -> 
        cm.AutoMap()
        cm.MapMember(fun c -> c.Details).SetSerializer(Serializer.LedgerTransactionSerializer()) |> ignore
    ) |> ignore
    
    // Create the connection to MongoDB
    let settings = System.Configuration.ConfigurationManager.ConnectionStrings
    let mongoClient = MongoClient(settings.["mongo"].ConnectionString)
    let mongoDb = mongoClient.GetDatabase(System.Configuration.ConfigurationManager.AppSettings.["database"])
    let mongoCollection = mongoDb.GetCollection("Ledger")

    // Create the connection to RabbitMQ
    let rabbitFactory = RabbitMQ.Client.ConnectionFactory(uri = System.Uri(settings.["rabbit"].ConnectionString))
    let connection = rabbitFactory.CreateConnection()

    // Set up the subscribers
    let service = Service(connection, "LedgerService")

    TicketsQuotedHandler(mongoCollection) |> service.``add subscriber``
    TicketsAllocatedHandler(mongoCollection) |> service.``add subscriber``
    TicketsCancelledHandler(mongoCollection) |> service.``add subscriber``
    
    // Start the service
    service.Start()

    0