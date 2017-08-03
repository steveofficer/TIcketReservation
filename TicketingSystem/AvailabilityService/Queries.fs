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
    use command = conn.CreateCommand()
    command.CommandText <- """SELECT COUNT (*) FROM [CancelledTickets] WHERE [CancellationId] = @cancellationId"""
    
    // Use parameterized SQL to provide the orderId filter to avoid a SQL Injection attack
    command.CreateParameter(ParameterName = "@cancellationId", Value = ``cancellation id``) 
    |> command.Parameters.Add
    |> ignore

    return! async {
        let count = command.ExecuteScalar()
        return System.Convert.ToInt32(count) > 0
    }
}