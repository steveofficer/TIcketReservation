namespace PricingService.Events

type QuotedTicket = {
    TicketTypeId : string
    Quantity : uint32
    PriceEach : decimal
    TotalPrice : decimal
}

type TicketsQuotedEvent = {
    EventId : string
    OrderId : string
    PricesValidAt : System.DateTime
    Tickets : QuotedTicket[]
    TotalPrice : decimal
    UserId : string
}