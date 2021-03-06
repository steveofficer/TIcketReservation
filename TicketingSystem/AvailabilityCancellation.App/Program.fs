﻿open RabbitMQ.Client
open RabbitMQ.Client.Events
open RabbitMQ.Subscriber
open System.Data
open System.Data.SqlClient
open AvailabilityService.Queries
open AvailabilityService.Commands

[<EntryPoint>]
let main argv =
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
    let publisher = RabbitMQ.Publisher.PublishChannel("Availability.Cancellation", connection)
    publisher.registerEvents([| "AvailabilityService.Contract" |])
    
    // Set up the subscribers
    let service = Service(connection, "Availability.Cancellation", prefetch)
    AvailabilityCancellation.CancelTicketsCommandHandler(
        publisher.publish, connectionFactory, ``cancellation exists``, ``can tickets be cancelled``, ``record cancellation``
    ) |> service.``add subscriber``
    
    // Start the service
    service.Start()
    printfn "Waiting for messages..."
    0