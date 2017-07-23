open Suave
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open System.Net
open System.Data.SqlClient

[<EntryPoint>]
let main argv = 
    // Create the connection to MongoDB
    let settings = System.Configuration.ConfigurationManager.ConnectionStrings
    let mongoClient = MongoDB.Driver.MongoClient(settings.["mongo"].ConnectionString)
    let mongoDb = mongoClient.GetDatabase(System.Configuration.ConfigurationManager.AppSettings.["database"])
    
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
                path "/admin/events" Handlers.``get all events``
                pathScan "/admin/events/%s" Handlers.``get event details``
                pathScan "/admin/events/%s/tickets" Handlers.``get event tickets``
                pathScan "/admin/events/%s/tickets/%s" Handlers.``get event ticket details``
            ]

            PUT >=> choose [
                path "/admin/events" Handlers.``create event``
                pathScan "/admin/events/%s/tickets" Handlers.``create ticket type``
            ]

            POST >=> choose [
                path "/admin/events/%s" ``update event``
                pathScan "/admin/events/%s/tickets/%s" ``update ticket type``
            ]
            
            NOT_FOUND "The requested path is not valid."
        ] >=> Writers.setMimeType "application/json") 
    0