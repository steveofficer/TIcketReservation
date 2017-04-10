module AvailabilityService.Queries

open AvailabilityService.Types.Db
open MongoDB.Driver
open MongoDB.Driver.Core
open MongoDB.Bson

let ``collection name`` = "Availability"

let ``get event ticket availability`` (db : IMongoDatabase) (``event id`` : string) = async {
    // Get access to the collection
    let collection = db.GetCollection<EventAvailability>(``collection name``)
    // Execute the query
    let! result = collection.Find((fun e -> e.Id = ``event id``)).FirstOrDefaultAsync() |> Async.AwaitTask
    // Return the result + the time it was executed at
    return ((if box result = null then None else Some result), System.DateTime.UtcNow) 
}