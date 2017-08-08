module ReserveTicketsDatabaseTests
open Xunit
open Dapper

let databaseConnection = "Server=localhost\sql;Initial Catalog=TestEventAvailability;Integrated Security=True;"

type Tests() =
    let connection = new System.Data.SqlClient.SqlConnection(databaseConnection)
    do connection.Open()
    do connection.Execute("DELETE FROM [EventTickets]") |> ignore

    interface System.IDisposable with
        member __.Dispose() = connection.Dispose()

    [<Theory>]
    [<InlineData(10, 5, 1)>]
    [<InlineData(100, 100, 100)>]
    member __.``reserve tickets succeeds when there is sufficient quantity of all tickets`` (qty1 : int32, qty2 : int32, qty3 : int32) = async {
        // Arrange
        
        let tickets = [| ("Ticket1", 10); ("Ticket2", 5); ("Ticket3", 1) |] |> dict
    
        let! _ = connection.ExecuteAsync(sprintf """INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Ticket1', 100, %d)""" qty1) |> Async.AwaitTask
        let! _ = connection.ExecuteAsync(sprintf """INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Ticket2', 100, %d)""" qty2) |> Async.AwaitTask
        let! _ = connection.ExecuteAsync(sprintf """INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Ticket3', 100, %d)""" qty3) |> Async.AwaitTask
    
        // Act
        let! result = AvailabilityService.Commands.``reserve tickets`` connection tickets

        // Assert
        Assert.True(result |> Option.isSome)
    }

    [<Fact>]
    member __.``reserve tickets reduces remaining quantity of requested tickets`` () = async {
        // Arrange
        use connection = new System.Data.SqlClient.SqlConnection(databaseConnection)
        connection.Open()
        let tickets = [| ("Ticket1", 10); ("Ticket2", 5); ("Ticket3", 1) |] |> dict
    
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Ticket1', 100, 11)""") |> Async.AwaitTask
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Ticket2', 100, 100)""") |> Async.AwaitTask
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Ticket3', 100, 1)""") |> Async.AwaitTask
    
        // Act
        let! result = AvailabilityService.Commands.``reserve tickets`` connection tickets

        // Assert
        Assert.True(result |> Option.isSome)
        
        let! remainingQty = connection.QueryAsync<int32>("SELECT [RemainingQuantity] FROM [EventTickets] ORDER BY [TicketTypeId]", transaction = result.Value) |> Async.AwaitTask
        Assert.Equal([| 1; 95; 0 |], remainingQty)
    }

    [<Fact>]
    member __.``reserve tickets fails if a requested ticket does not exist`` () = async {
        // Arrange
        use connection = new System.Data.SqlClient.SqlConnection(databaseConnection)
        connection.Open()
        let tickets = [| ("Ticket1", 10); ("Ticket2", 5); ("Ticket3", 1) |] |> dict
    
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Ticket2', 100, 100)""") |> Async.AwaitTask
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Ticket3', 100, 1)""") |> Async.AwaitTask

        // Act
        let! result = AvailabilityService.Commands.``reserve tickets`` connection tickets

        // Assert
        Assert.True(result |> Option.isNone)
    }

    [<Theory>]
    [<InlineData(0, 0, 0)>]
    [<InlineData(1, 1, 1)>]
    [<InlineData(0, 5, 1)>]
    [<InlineData(20, 5, 0)>]
    [<InlineData(20, 0, 10)>]
    member __.``reserve tickets fails if a requested ticket is not available`` (qty1 : int32, qty2 : int32, qty3 : int32) = async {
        // Arrange
        use connection = new System.Data.SqlClient.SqlConnection(databaseConnection)
        connection.Open()

        let tickets = [| ("Ticket1", 10); ("Ticket2", 5); ("Ticket3", 1) |] |> dict
        
        let! _ = connection.ExecuteAsync(sprintf """INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Ticket1', 100, %d)""" qty1) |> Async.AwaitTask
        let! _ = connection.ExecuteAsync(sprintf """INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Ticket2', 100, %d)""" qty2) |> Async.AwaitTask
        let! _ = connection.ExecuteAsync(sprintf """INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Ticket3', 100, %d)""" qty3) |> Async.AwaitTask
    
        // Act
        let! result = AvailabilityService.Commands.``reserve tickets`` connection tickets

        // Assert
        Assert.True(result |> Option.isNone)
    }
