module AvailabilityService.Queries

open AvailabilityService.Types
open AvailabilityService.Contract.Events
open System.Data
open System.Collections.Generic
open Dapper

type OrderIdFilter = {
    OrderId : string
}

type EventIdFilter = {
    EventId : string
}

type TicketTypeFilter = {
    EventId : string
    TicketTypeId : string
}

type TicketFilter = {
    TicketIds : string[]
}

type CancellationIdFilter = {
    CancellationId : string
}

/// Find the remaining quantities for each of the ticket types for the requested event
let ``get event ticket availability`` (conn : IDbConnection) (``event id`` : string) = async {
    // Create the command
    let! ticketInfo = 
        conn.QueryAsync<EventTicketInfo>(
            """SELECT [TicketTypeId], [RemainingQuantity] FROM [EventTickets] WHERE [EventId] = @EventId""",
            { EventIdFilter.EventId = ``event id`` }
        ) |> Async.AwaitTask
    
    return ticketInfo |> Seq.toArray
}

let ``get ticket type availability`` (conn : IDbConnection) (``event id`` : string) (``ticket type id`` : string) = async {
    // Create the command
    return! 
        conn.QueryFirstAsync<int32>(
            """SELECT [RemainingQuantity] FROM [EventTickets] WHERE [EventId] = @EventId AND [TicketTypeId] = @TicketTypeId""",
            { EventId = ``event id``; TicketTypeId = ``ticket type id`` }
        ) |> Async.AwaitTask
}

/// Find any existing allocated tickets for the requested order id
let ``find existing allocations`` (conn : IDbConnection) (``order id`` : string) = async {
    // Create the command
    let! allocations = 
        conn.QueryAsync<AllocationInfo>(
            """SELECT [TicketTypeId], [TicketId], [AllocatedAt], [Price] FROM [AllocatedTickets] WHERE [OrderId] = @OrderId""",
            { OrderId = ``order id``}
        ) |> Async.AwaitTask
    
    return allocations |> Seq.toArray
}

/// Find any existing allocated tickets for the requested order id
let ``cancellation exists`` (conn : IDbConnection) (``cancellation id`` : string) = async {
    // Create the command
    let! count = 
        conn.QueryFirstAsync<int32>(
            """SELECT COUNT (*) FROM [CancelledTickets] WHERE [CancellationId] = @CancellationId""",
            { CancellationId = ``cancellation id`` }
        ) |> Async.AwaitTask

    return count > 0
}

let ``can tickets be cancelled`` (conn : IDbConnection) (``ticket ids`` : string[]) = async {
    // If the tickets have already been cancelled then they cannot be cancelled again. 
    let! count = 
        conn.QueryFirstAsync<int32>(
            """SELECT COUNT (*) FROM [CancelledTickets] WHERE [TicketId] IN @TicketIds""",
            { TicketIds = ``ticket ids`` }
        ) |> Async.AwaitTask

    return count = 0
}