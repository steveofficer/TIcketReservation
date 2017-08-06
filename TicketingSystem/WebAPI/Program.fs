open Suave
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open System.Net
open System.Data.SqlClient

[<EntryPoint>]
let main argv = 
    let connectionSettings = System.Configuration.ConfigurationManager.ConnectionStrings
    let appSettings = System.Configuration.ConfigurationManager.AppSettings
    
    // Create the connection to MongoDB
    let mongoClient = MongoDB.Driver.MongoClient(connectionSettings.["mongo"].ConnectionString)
    let mongoDb = mongoClient.GetDatabase(appSettings.["database"])
    
    // Create the connection to RabbitMQ
    let rabbitFactory = RabbitMQ.Client.ConnectionFactory(uri = System.Uri(connectionSettings.["rabbit"].ConnectionString))
    let connection = rabbitFactory.CreateConnection()
    let publisher = RabbitMQ.Publisher.PublishChannel("TicketServiceAPI", connection)
    publisher.registerEvents([| "AvailabilityService.Contract"; "PricingService.Contract" |])

    let secureKey = appSettings.["secure_key"]
   
    let ``id gen`` () = MongoDB.Bson.ObjectId.GenerateNewId().ToString()

    // Create the handler that manages the request to create a quote for a ticket request
    let ``generate a quote`` = 
        let query = PricingService.Queries.``get ticket prices`` mongoDb
        PricingService.Handlers.``create quote`` ``id gen`` (Security.``create signature`` secureKey) query publisher.publish
    
    // Create the handler that manages the request to get the list of events
    let ``get events`` = 
        let query() = AdminService.Queries.``get all events`` mongoDb
        AdminService.Web.Handlers.``get all events`` query

    // Create the handler that manages the request to get the list of tickets purchased by a user
    let ``get user tickets`` =  
        let query = LedgerService.Queries.``get user tickets`` mongoDb
        LedgerService.Handlers.``get tickets`` query

    // Create the handler that manages the request to cancel tickets
    let ``cancel tickets`` =  
        let query = LedgerService.Queries.``get ticket charges`` mongoDb
        AvailabilityService.Handlers.``cancel tickets`` ``id gen`` query publisher.publish

    // Create the handler that manages the request to get the list of ticket prices for an event
    let ``get ticket prices for event`` = 
        let query = PricingService.Queries.``get event ticket prices`` mongoDb
        PricingService.Handlers.``get event ticket prices`` query

    // Create the handler that manages the request to get the list of ticket availability for an event
    let ``get ticket availability for event`` = 
        let query event = 
            use connection = new SqlConnection(connectionSettings.["sql"].ConnectionString)
            AvailabilityService.Queries.``get event ticket availability`` connection event

        AvailabilityService.Handlers.``get event ticket availability`` query
    
    // Create the handler that manages the request to confirm an order
    let ``order the tickets`` = AvailabilityService.Handlers.``confirm order`` (Security.``validate signature`` secureKey) publisher.publish

    // Start the Suave Server so it start listening for requests
    let port = Sockets.Port.Parse <| argv.[0]
 
    let serverConfig = 
        { defaultConfig with
           bindings = [ HttpBinding.create HTTP IPAddress.Loopback port ]
        }
    startWebServer 
        serverConfig
        (choose [
            GET >=>
                choose [
                    pathScan "/users/%s/tickets" ``get user tickets``
                    path "/events" >=> ``get events``
                    pathScan "/event/%s/pricing" ``get ticket prices for event``
                    pathScan "/event/%s/availability" ``get ticket availability for event``
                ]
            
            POST >=> 
                choose [
                    pathScan "/event/%s/quote" ``generate a quote``
                    pathScan "/event/%s/order" ``order the tickets``
                ]

            DELETE >=>
                choose [        
                    pathScan "/event/%s/tickets" ``cancel tickets``
                ]

            NOT_FOUND "The requested path is not valid."
        ] >=> Writers.setMimeType "application/json") 
    0