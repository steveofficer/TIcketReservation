module AdminService.Queries

open MongoDB.Driver
open AdminService.Types

let ``collection name`` = "Events"

let ``get all events`` (db : IMongoDatabase) = async {
    let coll = db.GetCollection<EventSummary>(``collection name``)
    let! result = coll.Find(FilterDefinition<EventSummary>.Empty).ToListAsync() |> Async.AwaitTask
    return result.ToArray()
}

let ``get event`` (db : IMongoDatabase) (``event id`` : string) = async {
    let coll = db.GetCollection<EventDetail>(``collection name``)
    let! result = coll.Find(fun e -> e.Id = ``event id``).FirstOrDefaultAsync() |> Async.AwaitTask
    return if box result = null then None else Some result
}

let ``get event ticket types`` (db : IMongoDatabase) (``event id`` : string) = async {
    let coll = db.GetCollection<EventDetail>(``collection name``)
    let! result = coll.Find(fun e -> e.Id = ``event id``).FirstOrDefaultAsync() |> Async.AwaitTask
    return ((if box result = null then [||] else result.Tickets), System.DateTime.UtcNow)
}