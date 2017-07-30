module AdminService.Queries

open MongoDB.Driver
open AdminService.Types

let ``get all events`` (db : IMongoDatabase) = async {
    let coll = db.GetCollection<EventSummary>("Events")
    let! result = coll.Find(FilterDefinition<EventSummary>.Empty).ToListAsync() |> Async.AwaitTask
    return result.ToArray()
}

let ``get event`` (db : IMongoDatabase) (``event id`` : string) = async {
    let coll = db.GetCollection<EventDetail>("Events")
    let! result = coll.Find(fun e -> e.Id = ``event id``).FirstOrDefaultAsync() |> Async.AwaitTask
    return if box result = null then None else Some result
}

let ``get event ticket details`` (db : IMongoDatabase) (``event id`` : string) (``ticket type id`` : string) = async {
    // First look up the "master" data
    let coll = db.GetCollection<EventDetail>("Events")
    let! result = coll.Find(fun e -> e.Id = ``event id``).FirstOrDefaultAsync() |> Async.AwaitTask
    
    if box result = null 
    then return None 
    else 
        // The Event exists, now find the price and the quantity
        let ticketType = result.Tickets |> Seq.find (fun t -> t.TicketTypeId = ``ticket type id``)
        return Some ticketType.Description
}