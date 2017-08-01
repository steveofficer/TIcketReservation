open Suave
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open System.Net
open System.Data.SqlClient
open AdminService.Types
open AdminService.Queries
open AdminService.Web.Handlers
open AdminService.Web.Types

[<EntryPoint>]
let main argv = 
    let settings = System.Configuration.ConfigurationManager.ConnectionStrings
    
    // Create the connection to MongoDB
    let mongoClient = MongoDB.Driver.MongoClient(settings.["mongo"].ConnectionString)
    let mongoDb = mongoClient.GetDatabase(System.Configuration.ConfigurationManager.AppSettings.["database"])
    
    let connectionFactory() = async {
        let conn = new SqlConnection(settings.["sql"].ConnectionString)
        do! conn.OpenAsync() |> Async.AwaitTask
        return conn :> System.Data.IDbConnection
    }

    let ``id gen``() = MongoDB.Bson.ObjectId.GenerateNewId().ToString()

    let findAllEvents() = AdminService.Queries.``get all events`` mongoDb
    
    let getEventDetails ``event id`` = async {
        let! event = AdminService.Queries.``get event`` mongoDb ``event id``
        match event with
        | None -> return None
        | Some event ->
            return! async {
                use! conn = connectionFactory()
                let! prices = PricingService.Queries.``get event ticket prices`` mongoDb ``event id``
                let! availability = AvailabilityService.Queries.``get event ticket availability`` conn ``event id``
                match (prices, availability) with
                | ((Some prices, _), availability) -> 
                    return Some 
                        {
                            Id = event.Id
                            Name = event.Name
                            Start = event.Start
                            End = event.End
                            Location = event.Location
                            Information = event.Information
                            Tickets = event.Tickets 
                                        |> Array.zip3 prices.Tickets availability 
                                        |> Array.map (fun (price,qty,description) -> { TicketTypeId = description.TicketTypeId; Description = description.Description; Quantity = qty.RemainingQuantity; Price = price.Price })
                        }
                | _ -> return Some 
                        {
                            Id = event.Id
                            Name = event.Name
                            Start = event.Start
                            End = event.End
                            Location = event.Location
                            Information = event.Information
                            Tickets = [||]
                        }
            }
    }

    let createEvent (detail) = async {
        do! AdminService.Commands.``create event`` mongoDb detail
        do! PricingService.Commands.``create event`` mongoDb detail.Id
        return ()
    }

    let updateEvent = AdminService.Commands.``update event`` mongoDb

    let createTicketType ``ticket type`` = async { 
        use conn = new SqlConnection(settings.["sql"].ConnectionString)
        do! conn.OpenAsync() |> Async.AwaitTask

        do! AdminService.Commands.``create event ticket type`` mongoDb ``ticket type``.EventId ``ticket type``.Id ``ticket type``.Description
        do! AvailabilityService.Commands.``create event ticket type`` conn ``ticket type``.EventId ``ticket type``.Id ``ticket type``.Quantity
        do! PricingService.Commands.``create event ticket type`` mongoDb ``ticket type``.EventId ``ticket type``.Id ``ticket type``.Price
        return ()
    }

    let updateTicketType = AdminService.Commands.``update event ticket type`` mongoDb

    let setCORSHeaders =
        Suave.Writers.setHeader  "Access-Control-Allow-Origin" "*"
        >=> Suave.Writers.setHeader "Access-Control-Allow-Headers" "content-type"
        >=> Suave.Writers.addHeader "Access-Control-Allow-Methods" "GET, POST, PUT"

    // Start the Suave Server so it start listening for requests
    let port = Sockets.Port.Parse <| argv.[0]
 
    let serverConfig = 
        { defaultConfig with
           bindings = [ HttpBinding.create HTTP IPAddress.Loopback port ]
        }
    startWebServer 
        serverConfig
        (choose [
            
            OPTIONS >=>
                fun context ->
                    context |> (Suave.Successful.OK """{ "Response": "CORS approved" }""" )
            
            GET >=> choose [
                path "/admin/events" >=> (``get all events`` findAllEvents)
                pathScan "/admin/events/%s" (``get event details`` getEventDetails)
            ]

            PUT >=> choose [
                path "/admin/events" >=> ``create event`` ``id gen`` createEvent
                pathScan "/admin/events/%s/tickets" (``create ticket type`` ``id gen`` createTicketType)
            ]

            POST >=> choose [
                pathScan "/admin/events/%s" (``update event`` updateEvent)
                pathScan "/admin/events/%s/tickets/%s" (fun (event, ticketType) -> ``update ticket type`` updateTicketType event ticketType)
            ]
            
            NOT_FOUND "The requested path is not valid."
        ] >=> Writers.setMimeType "application/json" >=> setCORSHeaders) 
    0