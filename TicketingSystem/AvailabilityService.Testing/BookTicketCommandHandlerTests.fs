module BookTicketCommandHandlerTests
open Xunit
open System.Collections.Generic
open AvailabilityService.Types
open AvailabilityService.Contract.Commands
open AvailabilityService.Contract.Events
open Dapper
open System.Data

let databaseConnection = "Server=localhost\sql;Initial Catalog=TestTicketAvailability;Integrated Security=True;"

type PublishState() = 
    let events = System.Collections.Generic.List<obj>()
    member this.Events = events

    member this.Publish e = async {
        return this.Events.Add e
    }

type Tests() =
    
    let connection = new System.Data.SqlClient.SqlConnection(databaseConnection)
    do connection.Open()
    do connection.Execute("DELETE FROM [AllocatedTickets]") |> ignore
    do connection.Execute("DELETE FROM [CancelledTickets]") |> ignore
    do connection.Execute("DELETE FROM [EventTickets]") |> ignore
    
    let ``connection factory``() = async {
        let connection = new System.Data.SqlClient.SqlConnection(databaseConnection)
        do connection.Open()
        return connection :> IDbConnection
    }

    let publishState = PublishState()
    
    let createHandler() = 
        let generator = 
            let mutable counter = 0
            fun () -> 
                counter <- counter + 1
                sprintf "TestIdentifier-%d" counter
        AvailabilityBooking.BookTicketsCommandHandler(
            publishState.Publish, 
            generator, 
            ``connection factory``, 
            AvailabilityService.Queries.``find existing allocations``, 
            AvailabilityService.Commands.``reserve tickets``,
            AvailabilityService.Commands.``record allocations``
        )

    interface System.IDisposable with
        member __.Dispose() = connection.Dispose()

    [<Fact>]
    member __.``new ticket allocation stores allocated tickets``() =  async {
        // Arrange
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Type1', 100, 100)""") |> Async.AwaitTask
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Type2', 100, 100)""") |> Async.AwaitTask

        let handler = createHandler ()

        let message = 
            {
                BookTicketsCommand.EventId = "Event1"
                UserId = "User1"
                PaymentReference = "REF001"
                OrderId = "ORD2"
                Tickets = [| {  TicketTypeId = "Type1"; Quantity = 3; PriceEach = 15.23M }; {  TicketTypeId = "Type2"; Quantity = 4; PriceEach = 12.10M } |]
            }
        
        // Act
        do! handler.HandleMessage (System.Guid.NewGuid()) (System.DateTime(2017, 01, 25)) message

        // Assert
        let! allocations = connection.QueryAsync<AvailabilityService.Commands.AllocatedTicket>("SELECT * FROM [AllocatedTickets]") |> Async.AwaitTask
        let allocations = allocations |> Array.ofSeq

        Assert.Equal(7, allocations.Length)
        Assert.True(allocations |> Array.forall (fun t -> t.TicketId.StartsWith("TestIdentifier")))
        Assert.True(allocations |> Array.forall (fun t -> t.OrderId = message.OrderId))

        let (type1, type2) = allocations |> Array.partition (fun t -> t.TicketTypeId = "Type1")
        
        Assert.Equal(3, type1.Length)
        Assert.True(type1 |> Seq.forall (fun t -> t.Price = 15.23M))
        
        Assert.Equal(4, type2.Length)
        Assert.True(type2 |> Seq.forall (fun t -> t.Price = 12.1M))
        
        return ()
    }

    [<Fact>]
    member __.``new ticket allocation publishes allocation event``() = async {
        // Arrange
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Type1', 100, 100)""") |> Async.AwaitTask
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Type2', 100, 100)""") |> Async.AwaitTask

        let handler = createHandler ()

        let message = 
            {
                BookTicketsCommand.EventId = "Event1"
                UserId = "User1"
                PaymentReference = "REF001"
                OrderId = "ORD2"
                Tickets = [| {  TicketTypeId = "Type1"; Quantity = 3; PriceEach = 15.23M }; {  TicketTypeId = "Type2"; Quantity = 4; PriceEach = 12.10M } |]
            }
        
        let sentAt = System.DateTime(2017, 08, 10, 10, 15, 38)

        // Act
        do! handler.HandleMessage (System.Guid.NewGuid()) sentAt message
        
        // Assert
        Assert.Equal(1, publishState.Events.Count)
        let event = Assert.IsType<TicketsAllocatedEvent>(publishState.Events.[0])
        
        Assert.Equal(message.EventId, event.EventId)
        Assert.Equal(message.OrderId, event.OrderId)
        Assert.Equal(message.PaymentReference, event.PaymentReference)
        Assert.Equal(sentAt, event.RequestedAt)
        Assert.Equal(message.UserId, event.UserId)
        Assert.Equal(94.09M, event.TotalPrice)
        Assert.Equal(7, event.Tickets.Length)
        
        Assert.True(event.Tickets |> Array.forall (fun t -> t.AllocatedAt >= System.DateTime.UtcNow.AddSeconds(-30.) && t.AllocatedAt <= System.DateTime.UtcNow.AddSeconds(30.)))

        let (type1, type2) = event.Tickets |> Array.partition (fun t -> t.TicketTypeId = "Type1")
        
        Assert.Equal(3, type1.Length)
        Assert.True(type1 |> Seq.forall (fun t -> t.Price = 15.23M))
        
        Assert.Equal(4, type2.Length)
        Assert.True(type2 |> Seq.forall (fun t -> t.Price = 12.1M))
        
        return ()
    }

    [<Fact>]
    member __.``failed allocation publishes failed event``() = async {
        // Arrange
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Type1', 100, 2)""") |> Async.AwaitTask
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Type2', 100, 100)""") |> Async.AwaitTask

        let handler = createHandler ()

        let message = 
            {
                BookTicketsCommand.EventId = "Event1"
                UserId = "User1"
                PaymentReference = "REF001"
                OrderId = "ORD2"
                Tickets = [| {  TicketTypeId = "Type1"; Quantity = 3; PriceEach = 15.23M }; {  TicketTypeId = "Type2"; Quantity = 4; PriceEach = 12.10M } |]
            }
        
        let sentAt = System.DateTime(2017, 08, 10, 10, 15, 38)

        // Act
        do! handler.HandleMessage (System.Guid.NewGuid()) sentAt message
        
        // Assert
        Assert.Equal(1, publishState.Events.Count)
        let event = Assert.IsType<TicketsAllocationFailedEvent>(publishState.Events.[0])
        
        Assert.Equal(message.EventId, event.EventId)
        Assert.Equal(message.OrderId, event.OrderId)
        Assert.Equal(message.PaymentReference, event.PaymentReference)
        Assert.Equal(sentAt, event.RequestedAt)
        Assert.InRange(event.FailedAt, System.DateTime.UtcNow.AddSeconds(-30.), System.DateTime.UtcNow.AddSeconds(30.))
        Assert.Equal(message.UserId, event.UserId)
        Assert.Equal(2, event.Tickets.Length)
        Assert.Equal("The tickets were not available", event.Reason)

        let (type1, type2) = event.Tickets |> Array.partition (fun t -> t.TicketTypeId = "Type1")
        
        Assert.Equal(1, type1.Length)
        Assert.Equal(3, type1.[0].Quantity)
        
        Assert.Equal(1, type2.Length)
        Assert.Equal(4, type2.[0].Quantity)
        
        return ()
    }

    [<Fact>]
    member __.``failed allocation doesn't store allocation``() = async {
        // Arrange
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Type1', 100, 2)""") |> Async.AwaitTask
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Type2', 100, 100)""") |> Async.AwaitTask

        let handler = createHandler ()

        let message = 
            {
                BookTicketsCommand.EventId = "Event1"
                UserId = "User1"
                PaymentReference = "REF001"
                OrderId = "ORD2"
                Tickets = [| {  TicketTypeId = "Type1"; Quantity = 3; PriceEach = 15.23M }; {  TicketTypeId = "Type2"; Quantity = 4; PriceEach = 12.10M } |]
            }
        
        // Act
        do! handler.HandleMessage (System.Guid.NewGuid()) (System.DateTime.UtcNow) message
        
        // Assert
        let! allocatedTicketCount = connection.QueryFirstAsync<int32>("SELECT COUNT(*) FROM [AllocatedTickets]") |> Async.AwaitTask
        Assert.Equal(0, allocatedTicketCount)
        return ()
    }

    [<Fact>]
    member __.``existing allocation doesn't reallocate tickets``() = async {
        // Arrange
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Type1', 100, 2)""") |> Async.AwaitTask
        let! _ = connection.ExecuteAsync("""INSERT INTO [EventTickets] (EventId, TicketTypeId, OriginalQuantity, RemainingQuantity) VALUES ('Event1', 'Type2', 100, 100)""") |> Async.AwaitTask
        let! _ = connection.ExecuteAsync("""INSERT INTO [AllocatedTickets] (TicketTypeId, TicketId, OrderId, AllocatedAt, Price) VALUES ('Type1', '1', 'ORD2', '2017-01-01', 12)""") |> Async.AwaitTask
        let! _ = connection.ExecuteAsync("""INSERT INTO [AllocatedTickets] (TicketTypeId, TicketId, OrderId, AllocatedAt, Price) VALUES ('Type2', '2', 'ORD2', '2017-01-01', 22)""") |> Async.AwaitTask
        let! _ = connection.ExecuteAsync("""INSERT INTO [AllocatedTickets] (TicketTypeId, TicketId, OrderId, AllocatedAt, Price) VALUES ('Type1', '3', 'ORD3', '2017-01-01', 12)""") |> Async.AwaitTask
        let handler = createHandler ()

        let message = 
            {
                BookTicketsCommand.EventId = "Event1"
                UserId = "User1"
                PaymentReference = "REF001"
                OrderId = "ORD2"
                Tickets = [| {  TicketTypeId = "Type1"; Quantity = 1; PriceEach = 12M }; {  TicketTypeId = "Type2"; Quantity = 1; PriceEach = 22M } |]
            }
        
        let sentAt = System.DateTime(2017, 08, 10, 10, 15, 38)

        // Act
        do! handler.HandleMessage (System.Guid.NewGuid()) sentAt message
        
        // Assert
        Assert.Equal(1, publishState.Events.Count)
        let event = Assert.IsType<TicketsAllocatedEvent>(publishState.Events.[0])
        
        Assert.Equal(message.EventId, event.EventId)
        Assert.Equal(message.OrderId, event.OrderId)
        Assert.Equal(message.PaymentReference, event.PaymentReference)
        Assert.Equal(sentAt, event.RequestedAt)
        Assert.Equal(message.UserId, event.UserId)
        Assert.Equal(2, event.Tickets.Length)

        let tickets = event.Tickets |> Array.map (fun t -> (t.TicketId, t)) |> dict
        
        Assert.True(tickets.ContainsKey("1"))
        let ticket1 = tickets.["1"]
        Assert.Equal(12M, ticket1.Price)
        Assert.Equal(System.DateTime(2017, 01, 01), ticket1.AllocatedAt)
        Assert.Equal("Type1", ticket1.TicketTypeId)
        
        Assert.True(tickets.ContainsKey("2"))
        let ticket2 = tickets.["2"]
        Assert.Equal(22M, ticket2.Price)
        Assert.Equal(System.DateTime(2017, 01, 01), ticket2.AllocatedAt)
        Assert.Equal("Type2", ticket2.TicketTypeId)
        
        let! allocatedTicketCount = connection.QueryFirstAsync<int32>("SELECT COUNT(*) FROM [AllocatedTickets]") |> Async.AwaitTask
        Assert.Equal(3, allocatedTicketCount)
        
        return ()
    }