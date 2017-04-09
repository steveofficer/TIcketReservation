// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open Suave
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors

[<EntryPoint>]
let main argv = 
    // Create the connection to mongo
    let mongoClient = MongoDB.Driver.MongoClient("")
    let mongoDb = mongoClient.GetDatabase("EventPricing")
    
    // Create the handler that manages the request to create a quote for a ticket request
    let quoteTickets = 
        let query = PricingService.Queries.``get ticket prices`` mongoDb
        let publish x = async.Return()
        PricingService.Handlers.``get ticket prices`` query publish
    
    // Create the handler that manages the request to get the list of ticket prices for an event
    let getEventPrices = 
        let query = PricingService.Queries.``get event ticket prices`` mongoDb
        PricingService.Handlers.``get event ticket prices`` query

    // Create the handler that manages the request to get the list of ticket prices for an event
    let getEventAvailability = 
        let query = AvailabilityService.Queries.``get event ticket availability`` mongoDb
        AvailabilityService.Handlers.``get event ticket availability`` query

    // Start the Suave Server so it start listening for requests
    startWebServer 
        defaultConfig 
        (choose [
            GET >=>
                choose [
                    pathScan "/event/%s/pricing" getEventPrices
                    pathScan "/event/%s/availability" getEventAvailability
                ]
            
            POST >=> 
                choose [
                    pathScan "/event/%s/quote" quoteTickets
                ]
                
            NOT_FOUND "The requested path is not valid."
        ])
    0