namespace AvailabilityService.Contract.Commands

type BookTicketsCommand = {
    EventId : string
    UserId : string
    PaymentReference : string
    OrderId : string
    Tickets : TicketInfo[]
} and TicketInfo = {
    TicketTypeId : string
    Quantity : uint32
    PriceEach : decimal
}

type CancelTicketsCommand = {
    EventId : string
    UserId : string
    OrderId : string
    CancellationId : string
    Tickets : CancelledTicket[]
    TotalPrice : decimal
} and CancelledTicket = {
    TicketTypeId : string
    TicketId : string
    Price : decimal
}