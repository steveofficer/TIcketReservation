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
    // Create the connection to MongoDB
    let settings = System.Configuration.ConfigurationManager.ConnectionStrings
    let mongoClient = MongoDB.Driver.MongoClient(settings.["mongo"].ConnectionString)
    let mongoDb = mongoClient.GetDatabase(System.Configuration.ConfigurationManager.AppSettings.["database"])
    
    let ``id gen``() = MongoDB.Bson.ObjectId.GenerateNewId().ToString()

    let findAllEvents() = AdminService.Queries.``get all events`` mongoDb
    
    let getEventDetails = AdminService.Queries.``get event`` mongoDb

    let getEventTicketDetails = AdminService.Queries.``get event ticket details`` mongoDb

    let createEvent = AdminService.Commands.``create event``

    let updateEvent = AdminService.Commands.``update event``

    let createTicketType = AdminService.Commands.``create event ticket type``

    let updateTicketType = AdminService.Commands.``update event ticket type``

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