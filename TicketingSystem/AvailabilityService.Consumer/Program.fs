open RabbitMQ.Client
open RabbitMQ.Client.Events
open RabbitMQ.Subscriber
open System.Data
open System.Data.SqlClient
open AvailabilityService.Queries
open AvailabilityService.Commands

[<EntryPoint>]
let main argv =
    // Create the connection to SQL
    let settings = System.Configuration.ConfigurationManager.ConnectionStrings
    let connectionFactory() = async {
        let conn = new SqlConnection(settings.["sql"].ConnectionString)
        do! conn.OpenAsync() |> Async.AwaitTask
        return conn :> IDbConnection
    }

    // Get the Prefetch Size to control the number of unacknowledged message this service has at one time
    let prefetch = System.Configuration.ConfigurationManager.AppSettings.["prefetch_count"] |> System.UInt16.Parse 

    // Create the connection to RabbitMQ
    let rabbitFactory = RabbitMQ.Client.ConnectionFactory(uri = System.Uri(settings.["rabbit"].ConnectionString))
    let connection = rabbitFactory.CreateConnection()

    // Set up the publishers
    let publisher = RabbitMQ.Publisher.PublishChannel("Availability.Booking", connection)
    publisher.registerEvents([| "AvailabilityService.Contract" |])
    
    let ``id gen`` () = MongoDB.Bson.ObjectId.GenerateNewId().ToString()

    // Set up the subscribers
    let service = Service(connection, "Availability.Booking", prefetch)
    AvailabilityBooking.BookTicketsCommandHandler(
        publisher.publish, 
        ``id gen``, 
        connectionFactory, 
        ``find existing allocations``, 
        ``reserve tickets``, 
        ``record allocations``
    ) |> service.``add subscriber``
    
    // Start the service
    service.Start()
    printfn "Waiting for messages..."
    0