module AvailabilityService.Commands

open AvailabilityService.Types.Db
open AvailabilityService.Contract.Events
open AvailabilityService.Types.Db
open System.Data
open System.Collections.Generic

let ``record cancellation`` (conn : IDbConnection) (cancellation : TicketsCancelledEvent) = async {
    return ()
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
