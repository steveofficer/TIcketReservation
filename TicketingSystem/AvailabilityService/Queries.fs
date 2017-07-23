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
let ``has existing cancellations`` (conn : IDbConnection) (``cancellation id`` : string) = async {
    // Create the command
    use command = conn.CreateCommand()
    command.CommandText <- """SELECT TicketTypeId, TicketId FROM [CancelledTickets] WHERE [TicketId] IN @cancellationId"""
    
    // Use parameterized SQL to provide the orderId filter to avoid a SQL Injection attack
    command.CreateParameter(ParameterName = "@cancellationId", Value = ``cancellation id``) 
    |> command.Parameters.Add
    |> ignore

    return! async {
        use reader = command.ExecuteReader()
        return [|
            while reader.Read() do
                yield { CancellationInfo.TicketTypeId = reader.GetString(0); TicketId = reader.GetString(1) }
        |]
    }
}

/// Write the provided tickets to the database
/// If not all of the provided tickets can be reserved, then don't reserve any of them and return None.
/// If all of the provided tickets can be reserved, then the active transaction is returned so any further updates can occur within the same transaction scope
let ``reserve tickets`` (conn : IDbConnection) (tickets : IDictionary<string, uint32>) = async {
    // We want to lock the records that we read so that we know the available quantity is accurate
    let transaction = conn.BeginTransaction(System.Data.IsolationLevel.RepeatableRead)
    
    // First see how many remaining tickets there are for each of the ticket types we are trying to make
    // reservations for.
    use command = conn.CreateCommand()
    command.CommandText <- """SELECT [TicketTypeId], [RemainingQuantity] FROM [EventTickets] WHERE [RemainingQuantity] <> 0 AND [TicketTypeId] IN @ticketIds"""
    
    // Use parameterized SQL to provide the orderId filter to avoid a SQL Injection attack
    command.CreateParameter(ParameterName = "@tickets", Value = tickets.Keys)
    |> command.Parameters.Add
    |> ignore
        
    let! availableTickets = async {
        use reader = command.ExecuteReader()
        return [|
            while reader.Read() do
                yield (reader.GetString(0), reader.GetInt32(1) |> uint32)
        |] 
        |> dict
    }
        
    if availableTickets.Count <> tickets.Count then 
        // Not all of the requested ticket types are available, don't book anything and don't return anything
        transaction.Dispose()
        return None
    elif tickets |> Seq.forall (fun t -> availableTickets.[t.Key] >= t.Value) then
        // All of the requested ticket types have remaining quantities that are >= the requested quantities
        let toCommand (t : KeyValuePair<string, uint32>) = 
            let newRemainingQuantity = availableTickets.[t.Key] - t.Value
            sprintf """UPDATE [EventTickets] SET RemainingQuantity = %d WHERE TicketTypeId = '%s';""" newRemainingQuantity t.Key
        
        // Decreased the remaining quantities for the ticket types we have handled
        do! async {
            use update = conn.CreateCommand()
            update.CommandText <- System.String.Join("\n", tickets |> Seq.map toCommand)
            update.ExecuteNonQuery() |> ignore
        }

        return Some transaction
    else
        // Not all of the requested ticket types are available in the requested quantities. 
        return None
}

/// Writes the provided ticket allocation records to the database as part of a wider transaction scope
let ``record allocations`` (transaction : IDbTransaction) (allocations : TicketsAllocatedEvent) = async {
    let insertValues = allocations.Tickets |> Array.map(fun t -> sprintf "(%s, %s, %s, %G)" t.TicketTypeId t.TicketId allocations.OrderId t.Price)
    
    use command = transaction.Connection.CreateCommand()
    command.CommandText <- 
        sprintf """INSERT INTO [AllocatedTickets] (TicketTypeId, TicketId, OrderId, Price) VALUES %s""" (System.String.Join(",", insertValues))
    
    return! async { 
        command.ExecuteNonQuery() |> ignore
        return ()
    }
}
