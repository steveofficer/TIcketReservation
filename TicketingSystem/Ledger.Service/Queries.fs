module LedgerService.Queries

open MongoDB.Driver
open MongoDB.Bson
open Types

let ``collection name`` = "Ledger"

let ``get user tickets`` (db : IMongoDatabase) (``user id`` : string) = async {
    // Get access to the collection
    let collection = db.GetCollection<Transaction>(``collection name``)
    // Find all the allocations that haven't already been cancelled
    let! result = collection.Find((fun e -> e.UserId = ``user id``)).ToListAsync() |> Async.AwaitTask
    // Return the result + the time it was accurate at
    return ((if box result = null then None else Some (result.ToArray())), System.DateTime.UtcNow) 
}

let ``already handled`` (db : IMongoDatabase) (``message id`` : System.Guid) = async {
    // Get access to the collection
    let collection = db.GetCollection<Transaction>(``collection name``)
    // Return whether or not any records exist with the corresponding source
    return! collection.Find(fun t -> t.SourceId = ``message id``).AnyAsync() |> Async.AwaitTask
}

let ``get ticket charges`` (db : IMongoDatabase) (``event id`` : string) (``ticket ids`` : string[]) = async {
    // Get access to the collection
    let collection = db.GetCollection<PriceQuery>(``collection name``)
    // Return whether or not any records exist with the corresponding source
    //return! collection.Find(fun t -> t.EventId = ``event id`` & t.).AnyAsync() |> Async.AwaitTask
    return [|0M|]
}