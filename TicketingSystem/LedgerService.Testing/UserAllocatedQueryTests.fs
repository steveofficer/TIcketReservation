namespace LedgerService.Testing
open Xunit
open LedgerService.Types

type UserAllocatedQueryTests() = 
    static do 
        MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<Transaction>(fun cm -> 
            cm.AutoMap()
            cm.MapMember(fun c -> c.Details).SetSerializer(LedgerService.Serializer.LedgerTransactionSerializer()) |> ignore
        ) |> ignore
    
    let client = MongoDB.Driver.MongoClient("mongodb://localhost:27017/")
    let database = client.GetDatabase("LedgerServiceTest")
    let collection = database.GetCollection<Transaction>(LedgerService.Queries.``collection name``)
    
    interface System.IDisposable with
        member __.Dispose() = client.DropDatabase("LedgerServiceTest")

    [<Fact>]
    member __.``get user cancellable tickets returns allocated tickets for a user`` ()= async {
        // Arrange
        let! r = 
            collection.BulkWriteAsync(
                [|
                    MongoDB.Driver.InsertOneModel(
                    { 
                        SourceId = System.Guid.NewGuid()
                        EventId = "Event1"
                        OrderId = "Order 1"
                        UserId = "User 1"
                        TransactionDate = System.DateTime.UtcNow
                        Details = { PricesQuotedAt = System.DateTime.UtcNow; Tickets = [| { TicketTypeId = "Type 1"; Quantity = 2; PriceEach = 50M } |]; TotalPrice = 100M } |> Quotation
                    });
                    MongoDB.Driver.InsertOneModel(
                    { 
                        SourceId = System.Guid.NewGuid()
                        EventId = "Event2"
                        OrderId = "Order 2"
                        UserId = "User 1"
                        TransactionDate = System.DateTime.UtcNow
                        Details = { PricesQuotedAt = System.DateTime.UtcNow; Tickets = [| { TicketTypeId = "Type 2"; Quantity = 2; PriceEach = 10.5M } |]; TotalPrice = 21M } |> Quotation
                    })
                    MongoDB.Driver.InsertOneModel(
                    { 
                        SourceId = System.Guid.NewGuid()
                        EventId = "Event2"
                        OrderId = "Order 2"
                        UserId = "User 1"
                        TransactionDate = System.DateTime.UtcNow
                        Details = { AllocationDetails.TotalPrice = 21M; Tickets = [| { TicketTypeId = "Type 2"; TicketId = "Tic1"; Price = 10.5M }; { TicketTypeId = "Type 2"; TicketId = "Tic2"; Price = 10.5M } |] } |> Allocation
                    })
                |]
            ) |> Async.AwaitTask
        
        // Act
        let! (tickets, validAt) = LedgerService.Queries.``get user cancellable tickets`` database "User 1"

        // Assert
        Assert.Equal(2, tickets.Length)
        Assert.Contains({ TicketTypeId = "Type 2"; TicketId = "Tic2"; Price = 10.5M }, tickets)
        Assert.Contains({ TicketTypeId = "Type 2"; TicketId = "Tic1"; Price = 10.5M }, tickets)
    }

    [<Fact>]
    member __.``get user cancellable tickets excludes cancelled tickets for a user`` ()= async {
        // Arrange
        let! r = 
            collection.BulkWriteAsync(
                [|
                    MongoDB.Driver.InsertOneModel(
                    { 
                        SourceId = System.Guid.NewGuid()
                        EventId = "Event1"
                        OrderId = "Order 1"
                        UserId = "User 1"
                        TransactionDate = System.DateTime.UtcNow
                        Details = { PricesQuotedAt = System.DateTime.UtcNow; Tickets = [| { TicketTypeId = "Type 1"; Quantity = 2; PriceEach = 50M } |]; TotalPrice = 100M } |> Quotation
                    });
                    MongoDB.Driver.InsertOneModel(
                    { 
                        SourceId = System.Guid.NewGuid()
                        EventId = "Event2"
                        OrderId = "Order 2"
                        UserId = "User 1"
                        TransactionDate = System.DateTime.UtcNow
                        Details = { PricesQuotedAt = System.DateTime.UtcNow; Tickets = [| { TicketTypeId = "Type 2"; Quantity = 2; PriceEach = 10.5M } |]; TotalPrice = 21M } |> Quotation
                    })
                    MongoDB.Driver.InsertOneModel(
                    { 
                        SourceId = System.Guid.NewGuid()
                        EventId = "Event2"
                        OrderId = "Order 2"
                        UserId = "User 1"
                        TransactionDate = System.DateTime.UtcNow
                        Details = { AllocationDetails.TotalPrice = 21M; Tickets = [| { TicketTypeId = "Type 2"; TicketId = "Tic1"; Price = 10.5M }; { TicketTypeId = "Type 2"; TicketId = "Tic2"; Price = 10.5M } |] } |> Allocation
                    })
                    MongoDB.Driver.InsertOneModel(
                    { 
                        SourceId = System.Guid.NewGuid()
                        EventId = "Event2"
                        OrderId = "Order 2"
                        UserId = "User 1"
                        TransactionDate = System.DateTime.UtcNow
                        Details = { CancellationId = "Cancelled1"; TotalPrice = 10.5M; Tickets = [| { TicketTypeId = "Type 2"; TicketId = "Tic2"; Price = 10.5M } |] } |> Cancellation
                    })
                |]
            ) |> Async.AwaitTask
        
        // Act
        let! (tickets, validAt) = LedgerService.Queries.``get user cancellable tickets`` database "User 1"

        // Assert
        Assert.Equal(1, tickets.Length)
        Assert.Contains({ TicketTypeId = "Type 2"; TicketId = "Tic1"; Price = 10.5M }, tickets)
    }
