module Handlers

open AvailabilityService.Contract.Events
open PricingService.Contract.Events
open LedgerService.Types
open MongoDB.Driver

type TicketsCancelledHandler(handled : System.Guid -> Async<bool>) =
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsCancelledEvent>()
    
    override this.HandleMessage (messageId) (sentAt) (message : TicketsCancelledEvent) = async {
        // If a record already exists for this message then we have already handled it and we can ignore it.
        let! r = handled messageId
        return
            match r with
            | false ->
                // Transform the event to the relevant transaction type and store it in the database
                {
                    SourceId = messageId
                    EventId = message.EventId
                    OrderId = message.OrderId
                    UserId = message.UserId
                    TransactionDate = message.RequestedAt
                    Details = { TicketIds = message.TicketIds} |> Cancellation 
                } 
                |> ``record cancellation``
            | true -> ()
    }

type TicketsAllocatedHandler(handled : System.Guid -> Async<bool>) =
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsAllocatedEvent>()
    override this.HandleMessage (messageId) (sentAt) (message : TicketsAllocatedEvent) = async {
        // If a record already exists for this message then we have already handled it and we can ignore it.
        let! count = 
        return 
            match count with
            | 0L ->
                // Transform the event to the relevant transaction type and store it in the database
                {
                    SourceId = messageId
                    EventId = message.EventId
                    OrderId = message.OrderId
                    UserId = message.UserId
                    TransactionDate = message.RequestedAt
                    Details = { AllocationDetails.Tickets = message.Tickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; TicketId = t.TicketId }) } |> Allocation
                } 
                |> collection.InsertOne
            | _ -> ()
    }

type TicketsQuotedHandler(handled : System.Guid -> Async<bool>) =
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsQuotedEvent>()
    override this.HandleMessage (messageId) (sentAt) (message : TicketsQuotedEvent) = async {
        // If a record already exists for this message then we have already handled it and we can ignore it.
        let! count = collection.CountAsync(fun t -> t.SourceId = messageId) |> Async.AwaitTask
        return
            match count with
            | 0L ->
                // Transform the event to the relevant transaction type and store it in the database
                {
                    SourceId = messageId
                    EventId = message.EventId
                    OrderId = message.OrderId
                    UserId = message.UserId
                    TransactionDate = sentAt
                    Details = 
                        { 
                            PricesQuotedAt = message.PricesValidAt
                            TotalPrice = message.TotalPrice 
                            Tickets = message.Tickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; Quantity = t.Quantity; PriceEach = t.PriceEach }) 
                        } |> Quotation
                } 
                |> collection.InsertOne
            | _ -> ()
    }