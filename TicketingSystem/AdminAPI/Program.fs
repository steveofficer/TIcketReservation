open Suave
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open System.Net
open System.Data.SqlClient
open AdminService.Types
open AdminService.Queries

[<EntryPoint>]
let main argv = 
    // Create the connection to MongoDB
    let settings = System.Configuration.ConfigurationManager.ConnectionStrings
    let mongoClient = MongoDB.Driver.MongoClient(settings.["mongo"].ConnectionString)
    let mongoDb = mongoClient.GetDatabase(System.Configuration.ConfigurationManager.AppSettings.["database"])
    
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
                path "/admin/events" >=> (Handlers.``get all events`` findAllEvents)
                pathScan "/admin/events/%s" (Handlers.``get event details`` getEventDetails)
                pathScan "/admin/events/%s/tickets/%s" (fun (eventId : string, ticketTypeId : string) -> Handlers.``get event ticket details`` getEventTicketDetails eventId ticketTypeId)
            ]

            PUT >=> choose [
                path "/admin/events" >=> Handlers.``create event`` createEvent
                pathScan "/admin/events/%s/tickets" (Handlers.``create ticket type`` createTicketType)
            ]

            POST >=> choose [
                pathScan "/admin/events/%s" (Handlers.``update event`` updateEvent)
                pathScan "/admin/events/%s/tickets/%s" (fun (event, ticketType) -> Handlers.``update ticket type`` updateTicketType event ticketType)
            ]
            
            NOT_FOUND "The requested path is not valid."
        ] >=> Writers.setMimeType "application/json") 
    0