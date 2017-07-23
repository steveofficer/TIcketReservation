module AvailabilityService.Queries

open AvailabilityService.Types.Db
open AvailabilityService.Contract.Events
open AvailabilityService.Types.Db
open System.Data
open System.Collections.Generic

/// Find the remaining quantities for each of the ticket types for the requested event
let ``get event ticket availability`` (conn : IDbConnection) (``event id`` : string) = async {
    // Create the command
    use command = conn.CreateCommand()
    command.CommandText <- """SELECT [TicketTypeId], [RemainingQuantity] FROM [EventTickets] WHERE [EventId] = @eventId"""
    
    // Use parameterized SQL to provide the eventId filter to avoid a SQL Injection attack
    command.CreateParameter(ParameterName = "@eventId", Value = ``event id``)
    |> command.Parameters.Add
    |> ignore

    // Run the query asynchronously and return the queried data
    return! async { 
        use reader = command.ExecuteReader() 
        return 
            [|
                while reader.Read() do
                    yield { TicketTypeId = reader.GetString(0); RemainingQuantity = reader.GetInt32(1) }
            |]
    }
}

/// Find any existing allocated tickets for the requested order id
let ``find existing allocations`` (conn : IDbConnection) (``order id`` : string) = async {
    // Create the command
    use command = conn.CreateCommand()
    command.CommandText <- """SELECT [TicketTypeId], [TicketId], [Price] FROM [AllocatedTickets] WHERE [OrderId] = @orderid"""
    
    // Use parameterized SQL to provide the orderId filter to avoid a SQL Injection attack
    command.CreateParameter(ParameterName = "@orderId", Value = ``order id``)
    |> command.Parameters.Add
    |> ignore
    
    return! async {
        use reader = command.ExecuteReader()
        return [|
            while reader.Read() do
                yield { AllocationInfo.TicketTypeId = reader.GetString(0); TicketId = reader.GetString(1); Price = reader.GetDecimal(2) }
        |]
    }
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

