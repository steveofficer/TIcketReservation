module AvailabilityService.Queries

open AvailabilityService.Types.Db
open AvailabilityService.Contract.Events
open AvailabilityService.Types.Db
open System.Data.SqlClient
open System.Collections.Generic

let ``get event ticket availability`` (conn : SqlConnection) (``event id`` : string) = async {
    use command = conn.CreateCommand()
    command.CommandText <- """SELECT [TicketTypeId], [RemainingQuantity] FROM [EventTickets] WHERE [EventId] = @eventId"""
    command.Parameters.AddWithValue("@eventId", ``event id``) |> ignore
    
    use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask
    
    return 
        [|
            while reader.Read() do
                yield { TicketTypeId = reader.GetString(0); RemainingQuantity = reader.GetInt32(1) }
        |] 
}

let ``find existing allocations`` (conn : SqlConnection) (orderid : string) = async {
    use command = conn.CreateCommand()
    command.CommandText <- """SELECT [TicketTypeId], [TicketId], [Price] FROM [AllocatedTickets] WHERE [OrderId] = @orderid"""
    command.Parameters.AddWithValue("@orderid", orderid) |> ignore
    
    use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask
    
    return [|
        while reader.Read() do
            yield { TicketTypeId = reader.GetString(0); TicketId = reader.GetString(1); Price = reader.GetDecimal(2) }
    |]
}

let ``reserve tickets`` (conn : SqlConnection) (tickets : IDictionary<string, uint32>) = async {
    // We want to lock the records that we read so that we know the available quantity is accurate
    let transaction = conn.BeginTransaction(System.Data.IsolationLevel.RepeatableRead)
    
    use command = conn.CreateCommand()
    command.CommandText <- """SELECT [TicketTypeId], [RemainingQuantity] FROM [EventTickets] WHERE [RemainingQuantity] <> 0 AND [TicketTypeId] IN @ticketIds"""
    command.Parameters.AddWithValue("@ticketIds", tickets.Keys) |> ignore
    
    use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask
    
    let availableTickets = 
        [|
            while reader.Read() do
                yield (reader.GetString(0), reader.GetInt32(1) |> uint32)
        |] 
        |> dict

    if availableTickets.Count <> tickets.Count then 
        // Not all of the requested ticket types are available
        transaction.Dispose()
        return None
    elif tickets |> Seq.forall (fun t -> availableTickets.[t.Key] >= t.Value) then
        let toCommand (t : KeyValuePair<string, uint32>) = 
            let newRemainingQuantity = availableTickets.[t.Key] - t.Value
            sprintf """UPDATE [EventTickets] SET RemainingQuantity = %d WHERE TicketTypeId = '%s';""" newRemainingQuantity t.Key
        use update = conn.CreateCommand()
        update.CommandText <- System.String.Join("\n", tickets |> Seq.map toCommand)
        update.ExecuteNonQueryAsync() |> Async.AwaitTask |> ignore
        return Some transaction
    else
        // Not all of the requested ticket types are available in the requested quantities. 
        return None
}

let ``record allocations`` (transaction : SqlTransaction) (allocations : TicketsAllocatedEvent) = async {
    let insertValues = allocations.Tickets |> Array.map(fun t -> sprintf "(%s, %s, %s, %G)" t.TicketTypeId t.TicketId allocations.OrderId t.Price)
    
    use command = transaction.Connection.CreateCommand()
    command.CommandText <- 
        sprintf """INSERT INTO [AllocatedTickets] (TicketTypeId, TicketId, OrderId, Price) VALUES %s""" (System.String.Join(",", insertValues))
    
    use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask
    
    return ()
}
