open RabbitMQ.Client
open RabbitMQ.Client.Events
open RabbitMQ.Subscriber
open MongoDB.Driver
open Handlers
open LedgerService.Queries
open LedgerService.Commands
open LedgerService.Types
open PricingService.Contract.Events

[<EntryPoint>]
let main argv =
    // Map the serializer to the Transaction model
    MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<Transaction>(fun cm -> 
        cm.AutoMap()
        cm.MapMember(fun c -> c.Details).SetSerializer(Serializer.LedgerTransactionSerializer()) |> ignore
    ) |> ignore
    
    // Create the connection to MongoDB
    let settings = System.Configuration.ConfigurationManager.ConnectionStrings
    let mongoClient = MongoClient(settings.["mongo"].ConnectionString)
    let mongoDb = mongoClient.GetDatabase(System.Configuration.ConfigurationManager.AppSettings.["database"])

    let ``message handled`` = ``already handled`` mongoDb
    let ``record entry`` = ``record transaction`` mongoDb

    // Create the connection to RabbitMQ
    let rabbitFactory = RabbitMQ.Client.ConnectionFactory(uri = System.Uri(settings.["rabbit"].ConnectionString))
    let connection = rabbitFactory.CreateConnection()

    // Set up the subscribers
    let service = Service(connection, "LedgerService")

    TicketsQuotedHandler(``message handled``, ``record entry``) |> service.``add subscriber``
    TicketsAllocatedHandler(``message handled``, ``record entry``) |> service.``add subscriber``
    TicketsCancelledHandler(``message handled``, ``record entry``) |> service.``add subscriber``
    
    // Start the service
    service.Start()
    printfn "Waiting for messages..."
    0