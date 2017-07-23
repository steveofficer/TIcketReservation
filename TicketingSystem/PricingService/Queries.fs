module PricingService.Queries

open PricingService.Types.Db
open MongoDB.Driver
open MongoDB.Driver.Core
open MongoDB.Bson

let ``collection name`` = "Pricing"

let ``get event ticket prices`` (db : IMongoDatabase) (``event id`` : string) = async {
    // Get access to the collection
    let collection = db.GetCollection<EventPricing>(``collection name``)
    // Execute the query
    let! result = collection.Find((fun e -> e.Id = ``event id``)).FirstOrDefaultAsync() |> Async.AwaitTask
    // Return the result + the time it was accurate at
    return ((if box result = null then None else Some result), System.DateTime.UtcNow) 
}

let ``get ticket prices`` (db : IMongoDatabase) (``event id`` : string) (``ticket type ids`` : Set<string>) = async {
    // Get the event and remember the time the event details was accurate at
    let! (event, asAt) = ``get event ticket prices`` db ``event id`` 
    return
        match event with
        | Some event ->
            // Extract the prices for the ticket of interest and return them as a Dictionary of id to price
            let prices = 
                event.Tickets 
                |> Array.filter (fun t -> ``ticket type ids``.Contains(t.TicketTypeId))
                |> Array.map (fun t -> (t.TicketTypeId, t.Price))
                |> Map.ofArray
            // Return the prices as well as the time they were accurate at
            (Some prices, asAt)
        | None -> (None, asAt)
}