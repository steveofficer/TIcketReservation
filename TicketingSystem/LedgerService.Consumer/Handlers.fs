module Handlers

open AvailabilityService.Contract.Events
open PricingService.Contract.Events
open MongoDB.Driver

type CancellationDetails = {
    EventId : string
    OrderId : string
    UserId : string
    TransactionDate : System.DateTime
    TicketIds : string[]
}

type AllocationDetails = {
    EventId : string
    OrderId : string
    UserId : string
    TransactionDate : System.DateTime
    Tickets : AllocatedTicket[]
} and AllocatedTicket = {
    TicketTypeId : string
    TicketId : string
}

type QuoteDetails = {
    EventId : string
    OrderId : string
    UserId : string
    TransactionDate : System.DateTime
    PricesQuotedAt : System.DateTime
    Tickets : TicketInfo[]
    TotalPrice : decimal
} and TicketInfo = {
    TicketTypeId : string
    Quantity : uint32
    PriceEach : decimal
}

type Transaction =
    | Quotation of QuoteDetails
    | Cancellation of CancellationDetails
    | Allocation of AllocationDetails

type TicketsCancelledHandler(collection : IMongoCollection<Transaction>) =
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsCancelledEvent>()
    override this.Handle(message : TicketsCancelledEvent) = async {
        return ()
    }

type TicketsAllocatedHandler(collection : IMongoCollection<Transaction>) =
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsAllocatedEvent>()
    override this.Handle(message : TicketsAllocatedEvent) = async {
        return ()    
    }

type TicketsQuotedHandler(collection : IMongoCollection<Transaction>) =
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsQuotedEvent>()
    override this.Handle(message : TicketsQuotedEvent) = async {
        return ()    
    }