namespace AvailabilityService.Contract.Events

type TicketsAllocatedEvent = {
    EventId : string
    OrderId : string
    PaymentReference : string
    RequestedAt : System.DateTime
    UserId : string
    Tickets : AllocatedTicket[]
    TotalPrice : decimal
} and AllocatedTicket = {
    TicketTypeId : string
    TicketId : string
    AllocatedAt : System.DateTime
    Price : decimal
}

type TicketsCancelledEvent = {
    EventId : string
    OrderId : string
    CancellationId : string
    RequestedAt : System.DateTime
    Tickets : CancelledTicket[]
    TotalPrice : decimal
    UserId : string
} and CancelledTicket = {
    TicketTypeId : string
    TicketId : string
}

type TicketsAllocationFailedEvent = {
    EventId : string
    OrderId : string
    RequestedAt : System.DateTime
    Tickets : TicketQuantity[]
    UserId : string
    Reason : string
} and TicketQuantity = {
    TicketTypeId : string
    Quantity : int32
}

