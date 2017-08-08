module AvailabilityService.Commands

open AvailabilityService.Types
open AvailabilityService.Contract.Events
open System.Data
open System.Collections.Generic
open Dapper

type TicketFilter = {
    TicketIds : string[]
}

type AvailableTicket = {
    TicketTypeId : string
    RemainingQuantity : int32
}

type AllocatedTicket = { 
    TicketTypeId : string
    TicketId : string
    OrderId : string
    AllocatedAt : System.DateTime
    Price : decimal
}

type CancelledTicket = { 
    TicketTypeId : string
    TicketId : string
    OrderId : string
    CancelledOn : System.DateTime
    Price : decimal
}

type EventTicketType = {
    EventId : string
    TicketTypeId : string
    OriginalQuantity : int32
    RemainingQuantity : int32
}

let ``create event ticket type`` (conn : IDbConnection) (``event id`` : string) (``ticket type id`` : string) (quantity : int32) = async {
    let! result =
        conn.ExecuteAsync(
            """INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES (@EventId, @TicketTypeId, @OriginalQuantity, @RemainingQuantity)""",
            { EventId = ``event id``; TicketTypeId = ``ticket type id``; OriginalQuantity = quantity; RemainingQuantity = quantity }
        ) |> Async.AwaitTask
    
    return ()
}
    
let ``record cancellation`` (conn : IDbConnection) (cancellation : TicketsCancelledEvent) = async {
    let! result = 
        conn.ExecuteAsync(
            """INSERT INTO [CancelledTickets] (TicketTypeId, TicketId, OrderId, CancelledOn, Price) VALUES (@TicketTypeId, @TicketId, @OrderId, @CancelledOn, @Price)""",
            cancellation.Tickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; TicketId = t.TicketId; OrderId = cancellation.OrderId; CancelledOn = cancellation.RequestedAt; Price = t.Price})
        ) |> Async.AwaitTask
    return ()
}

/// Write the provided tickets to the database
/// If not all of the provided tickets can be reserved, then don't reserve any of them and return None.
/// If all of the provided tickets can be reserved, then the active transaction is returned so any further updates can occur within the same transaction scope
let ``reserve tickets`` (conn : IDbConnection) (tickets : IDictionary<string, int32>) = async {
    // We want to lock the records that we read so that we know the available quantity is accurate
    let transaction = conn.BeginTransaction(System.Data.IsolationLevel.RepeatableRead)
    
    // First see how many remaining tickets there are for each of the ticket types we are trying to make
    // reservations for.
    let! availableTickets = async {
        let! result = 
            conn.QueryAsync<AvailableTicket>(
                """SELECT [TicketTypeId], [RemainingQuantity] FROM [EventTickets] WITH (UPDLOCK) WHERE [RemainingQuantity] <> 0 AND [TicketTypeId] IN @TicketIds""", 
                { TicketIds = (tickets.Keys |> Array.ofSeq) }, 
                transaction
            ) |> Async.AwaitTask
        return  result |> Seq.map (fun t -> (t.TicketTypeId, t.RemainingQuantity)) |> dict
    }

    if availableTickets.Count <> tickets.Count then 
        // Not all of the requested ticket types are available, don't book anything and don't return anything
        transaction.Dispose()
        return None
    elif tickets |> Seq.forall (fun t -> availableTickets.[t.Key] >= t.Value) then
        // All of the requested ticket types have remaining quantities that are >= the requested quantities
        // Decreased the remaining quantities for the ticket types we have handled
        let! result = 
            conn.ExecuteAsync(
                """UPDATE [EventTickets] SET RemainingQuantity = @RemainingQuantity WHERE TicketTypeId = @TicketTypeId""", 
                tickets |> Seq.map (fun t -> { AvailableTicket.TicketTypeId = t.Key; RemainingQuantity = availableTickets.[t.Key] - t.Value }),
                transaction
            ) |> Async.AwaitTask
        
        return Some transaction
    else
        // Not all of the requested ticket types are available in the requested quantities. 
        return None
}

/// Writes the provided ticket allocation records to the database as part of a wider transaction scope
let ``record allocations`` (transaction : IDbTransaction) (allocations : TicketsAllocatedEvent) = async {
    let! result = 
        transaction.Connection.ExecuteAsync(
            """INSERT INTO [AllocatedTickets] (TicketTypeId, TicketId, OrderId, AllocatedAt, Price) VALUES (@TicketTypeId, @TicketId, @OrderId, @AllocatedAt, @Price)""",
            allocations.Tickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; TicketId = t.TicketId; OrderId = allocations.OrderId; AllocatedAt = t.AllocatedAt; Price = t.Price}),
            transaction
        ) |> Async.AwaitTask
    return ()
}
