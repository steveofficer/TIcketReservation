module LedgerService.Queries

open MongoDB.Driver
open MongoDB.Bson
open Types

let ``collection name`` = "Ledger"

let ``get user cancellable tickets`` (db : IMongoDatabase) (``user id`` : string) = async {
    // Get access to the collection
    let collection = db.GetCollection<Transaction>(``collection name``)
    
    // Find all the allocations that haven't already been cancelled
    let! result =
        collection
            .Aggregate()
            // Find all transactions for the requested user
            .Match(fun t -> t.UserId = ``user id``)
            // Exclude Quotations, they aren't relevant for this query
            .Match(JsonFilterDefinition("""{ "Details._t" : { $ne : "Quotation" } }"""))
            // Separate each Ticket into its own entry
            .Unwind(StringFieldDefinition<_>("Details.Tickets"))
            // Create an intermediary type that contains the information we need for the remainder of the query
            .Project(JsonProjectionDefinition<BsonDocument, BsonDocument>("""{ "Type" : "$Details._t", "TicketTypeId" : "$Details.Tickets.TicketTypeId", "TicketId" : "$Details.Tickets.TicketId", "Price" : "$Details.Tickets.Price" }"""))
            // Group each of the records by Ticket Id, so we can see a list of transaction types for each specific ticket
            .Group(JsonProjectionDefinition<BsonDocument, BsonDocument>("""{ _id : "$TicketId", "Transactions" : { $push : { "Type" : "$Type", "TicketTypeId" : "$TicketTypeId", "Price" : "$Price" } } }"""))
            // Filter out any tickets that haven't been cancelled
            .Match(JsonFilterDefinition("""{ "Transactions.Type" : { $ne : "Cancellation" } }"""))
            // Flatten out the structure again
            .Unwind(StringFieldDefinition<_>("Transactions"))
            // Convert the resulting data to a list
            .ToListAsync() |> Async.AwaitTask
        
    let parse (document : BsonDocument) = 
        let transaction = document.["Transactions"]
        { TicketTypeId = transaction.["TicketTypeId"].ToString(); TicketId = document.["_id"].ToString(); Price = transaction.["Price"].ToDecimal() }

    let cancellableTickets = 
        result
        |> Seq.map parse
        |> Seq.toArray

    return (cancellableTickets, System.DateTime.UtcNow)
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