module Handlers

open AvailabilityService.Contract.Events
open PricingService.Contract.Events
open LedgerService.Types
open MongoDB.Driver

type TicketsCancelledHandler
    (
    handled : System.Guid -> Async<bool>,
    ``record cancellation`` : Transaction -> Async<unit>) =
    
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsCancelledEvent>()
    
    override this.HandleMessage (messageId) (sentAt) (message : TicketsCancelledEvent) = async {
        // If a record already exists for this message then we have already handled it and we can ignore it.
        let! exists = handled messageId
        if not exists
        then
            // Transform the event to the relevant transaction type and store it in the database
            let details = { 
                CancellationId = message.CancellationId
                TotalPrice = message.TotalPrice 
                Tickets = message.Tickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; TicketId = t.TicketId; Price = t.Price })
            }
            return! 
                {
                    SourceId = messageId
                    EventId = message.EventId
                    OrderId = message.OrderId
                    UserId = message.UserId
                    TransactionDate = message.RequestedAt
                    Details = Cancellation details 
                } 
                |> ``record cancellation``
        else return ()
    }

type TicketsAllocatedHandler
    (
    handled : System.Guid -> Async<bool>,
    ``record allocation`` : Transaction -> Async<unit>) =
    
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsAllocatedEvent>()
    
    override this.HandleMessage (messageId) (sentAt) (message : TicketsAllocatedEvent) = async {
        // If a record already exists for this message then we have already handled it and we can ignore it.
        let! exists = handled messageId
        if not exists
        then
            // Transform the event to the relevant transaction type and store it in the database
            let details = { 
                AllocationDetails.TotalPrice = message.TotalPrice
                Tickets = message.Tickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; TicketId = t.TicketId; Price = t.Price }) 
            }
            return! 
                {
                    SourceId = messageId
                    EventId = message.EventId
                    OrderId = message.OrderId
                    UserId = message.UserId
                    TransactionDate = message.RequestedAt
                    Details = Allocation details 
                } 
                |> ``record allocation``
        else return ()
    }

type TicketsQuotedHandler
    (
    handled : System.Guid -> Async<bool>,
    ``record quotation`` : Transaction -> Async<unit>) =
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsQuotedEvent>()
    override this.HandleMessage (messageId) (sentAt) (message : TicketsQuotedEvent) = async {
        // If a record already exists for this message then we have already handled it and we can ignore it.
        let! exists = handled messageId
        if not exists
        then
            // Transform the event to the relevant transaction type and store it in the database
            let details = { 
                PricesQuotedAt = message.PricesValidAt
                TotalPrice = message.TotalPrice 
                Tickets = message.Tickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; Quantity = t.Quantity; PriceEach = t.PriceEach }) 
            }
            return! 
                {
                    SourceId = messageId
                    EventId = message.EventId
                    OrderId = message.OrderId
                    UserId = message.UserId
                    TransactionDate = sentAt
                    Details = Quotation details 
                } 
                |> ``record quotation``
        else return ()
    }