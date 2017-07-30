module PricingService.Commands

open PricingService.Types.Db
open MongoDB.Driver
open MongoDB.Driver.Core
open MongoDB.Bson

let ``collection name`` = "Pricing"

let ``create event ticket type`` (db : IMongoDatabase) (``event id`` : string) (``ticket type id`` : string) (price : decimal) = async {
    // Get access to the collection
    let collection = db.GetCollection<EventPricing>(``collection name``)
    // Execute the query
    let filter = Builders.Filter.Eq(StringFieldDefinition<EventPricing, string>("_id"), ``event id``)
    let update = Builders<EventPricing>.Update.AddToSet(StringFieldDefinition<EventPricing>("Tickets"), { TicketTypeId = ``ticket type id``; Price = price })
    let! result = collection.UpdateOneAsync(filter, update) |> Async.AwaitTask
    return ()
}