open Suave
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open System.Net
open System.Data.SqlClient
open AdminService.Types
open AdminService.Queries
open AdminService.Handlers

[<EntryPoint>]
let main argv = 
    let settings = System.Configuration.ConfigurationManager.ConnectionStrings
    
    // Create the connection to MongoDB
    let mongoClient = MongoDB.Driver.MongoClient(settings.["mongo"].ConnectionString)
    let mongoDb = mongoClient.GetDatabase(System.Configuration.ConfigurationManager.AppSettings.["database"])
    
    let ``id gen``() = MongoDB.Bson.ObjectId.GenerateNewId().ToString()

    let findAllEvents() = AdminService.Queries.``get all events`` mongoDb
    
    let getEventDetails = AdminService.Queries.``get event`` mongoDb

    let getEventTicketDetails ``event id`` ``ticket type id`` = async {
        use conn = new SqlConnection(settings.["sql"].ConnectionString)
        do! conn.OpenAsync() |> Async.AwaitTask
        let! description = AdminService.Queries.``get event ticket details`` mongoDb ``event id`` ``ticket type id``
        match description with
        | None -> return None
        | Some description ->
            let! quantity = AvailabilityService.Queries.``get ticket type availability`` conn ``event id`` ``ticket type id`` 
            let! (price, time) = PricingService.Queries.``get ticket price`` mongoDb ``event id`` ``ticket type id`` 
            match price with
            | Some price -> return Some { EventId = ``event id``; Id = ``ticket type id``; Description = description; Quantity = quantity; Price = price }
            | None -> return None
    }

    let createEvent = AdminService.Commands.``create event`` mongoDb

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

    // Start the Suave Server so it start listening for requests
    let port = Sockets.Port.Parse <| argv.[0]
 
    let serverConfig = 
        { defaultConfig with
           bindings = [ HttpBinding.create HTTP IPAddress.Loopback port ]
        }
    startWebServer 
        serverConfig
        (choose [
            GET >=> choose [
                path "/admin/events" >=> (``get all events`` findAllEvents)
                pathScan "/admin/events/%s" (``get event details`` getEventDetails)
                pathScan "/admin/events/%s/tickets/%s" (fun (eventId : string, ticketTypeId : string) -> ``get event ticket details`` getEventTicketDetails eventId ticketTypeId)
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
        ] >=> Writers.setMimeType "application/json") 
    0