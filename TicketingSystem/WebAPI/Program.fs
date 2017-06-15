open Suave
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open System.Net

[<EntryPoint>]
let main argv = 
    // Create the connection to MongoDB
    let settings = System.Configuration.ConfigurationManager.ConnectionStrings
    let mongoClient = MongoDB.Driver.MongoClient(settings.["mongo"].ConnectionString)
    let mongoDb = mongoClient.GetDatabase(System.Configuration.ConfigurationManager.AppSettings.["database"])
    
    // Create the connection to RabbitMQ
    let rabbitFactory = RabbitMQ.Client.ConnectionFactory(uri = System.Uri(settings.["rabbit"].ConnectionString))
    let connection = rabbitFactory.CreateConnection()
    let publisher = RabbitMQ.Publisher.PublishChannel("TicketServiceAPI", connection)
    publisher.registerEvents([| "AvailabilityService.Contract"; "PricingService.Contract" |])

    // Create the handler that manages the request to create a quote for a ticket request
    let quoteTicketsHandler = 
        let query = PricingService.Queries.``get ticket prices`` mongoDb
        PricingService.Handlers.``create quote`` query publisher.publish
    
    // Create the handler that manages the request to get the list of ticket prices for an event
    let getEventPricesHandler = 
        let query = PricingService.Queries.``get event ticket prices`` mongoDb
        PricingService.Handlers.``get event ticket prices`` query

    // Create the handler that manages the request to get the list of ticket availability for an event
    let getEventAvailabilityHandler = 
        let query = AvailabilityService.Queries.``get event ticket availability`` mongoDb
        AvailabilityService.Handlers.``get event ticket availability`` query
    
    // Create the handler that manages the request to confirm an order
    let orderTicketsHandler = AvailabilityService.Handlers.``confirm order`` publisher.publish

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
                    pathScan "/event/%s/pricing" getEventPricesHandler
                    pathScan "/event/%s/availability" getEventAvailabilityHandler
                ]
            
            POST >=> 
                choose [
                    pathScan "/event/%s/quote" quoteTicketsHandler
                    pathScan "/event/%s/order" orderTicketsHandler
                ]
                
            NOT_FOUND "The requested path is not valid."
        ] >=> Writers.setMimeType "application/json") 
    0