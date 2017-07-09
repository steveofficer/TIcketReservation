open RabbitMQ.Client
open RabbitMQ.Client.Events
open RabbitMQ.Subscriber
open System.Data.SqlClient
open AvailabilityService.Queries

[<EntryPoint>]
let main argv =
    // Create the connection to SQL
    let settings = System.Configuration.ConfigurationManager.ConnectionStrings
    let connectionFactory() = async {
        let conn = new SqlConnection(settings.["sql"].ConnectionString)
        do! conn.OpenAsync() |> Async.AwaitTask
        return conn
    }

    // Create the connection to RabbitMQ
    let rabbitFactory = RabbitMQ.Client.ConnectionFactory(uri = System.Uri(settings.["rabbit"].ConnectionString))
    let connection = rabbitFactory.CreateConnection()

    // Set up the publishers
    let publisher = RabbitMQ.Publisher.PublishChannel("Availability.Booking", connection)
    publisher.registerEvents([| "AvailabilityService.Contract" |])
    
    // Set up the subscribers
    let service = Service(connection, "Availability.Booking")
    AvailabilityBooking.BookTicketsCommandHandler(publisher.publish, connectionFactory, ``find existing allocations``, ``reserve tickets``, ``record allocations``) |> service.``add subscriber``
    
    // Start the service
    service.Start()
    printfn "Waiting for messages..."
    System.Console.ReadLine() |> ignore
    0