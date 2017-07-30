module AdminService.Commands

open MongoDB.Driver
open AdminService.Types

let ``create event`` (db : IMongoDatabase) (event : EventDetail) = async {
    return! db.GetCollection<EventDetail>("Events").InsertOneAsync(event) |> Async.AwaitTask
} 

let ``update event`` (db : IMongoDatabase) (event : EventDetail) = async {
    let coll = db.GetCollection<EventDetail>("Events")
    let filter = Builders.Filter.Eq(StringFieldDefinition<EventDetail, string>("_id"), event.Id)
    let! result = coll.ReplaceOneAsync(filter, event) |> Async.AwaitTask
    return ()
} 

let ``create event ticket type`` (db : IMongoDatabase) (``event id`` : string) (``ticket type id`` : string) (description : string) = async {
    let coll = db.GetCollection<EventDetail>("Events")
    let filter = Builders.Filter.Eq(StringFieldDefinition<EventDetail, string>("_id"), ``event id``)
    let update = Builders.Update.AddToSet(StringFieldDefinition<EventDetail>("Tickets"), { TicketTypeId = ``ticket type id``; Description = description })
    let! result = coll.UpdateOneAsync(filter, update) |> Async.AwaitTask
    return ()
}

let ``update event ticket type`` (db : IMongoDatabase) (ticketType : TicketInfo) = async {
    let coll = db.GetCollection<EventDetail>("Events")
    let! event = coll.Find(fun e -> e.Id = ticketType.EventId).FirstOrDefaultAsync() |> Async.AwaitTask
    
    let currentTicketType = event.Tickets |> Seq.find (fun t -> t.TicketTypeId = ticketType.Id)
    currentTicketType.Description <- ticketType.Description
    
    return! ``update event`` db event
}