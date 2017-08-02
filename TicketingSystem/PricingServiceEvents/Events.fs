namespace PricingService.Contract.Events

type TicketsQuotedEvent = {
    EventId : string
    OrderId : string
    PricesValidAt : System.DateTime
    Tickets : QuotedTicket[]
    TotalPrice : decimal
    UserId : string
} and QuotedTicket = {
    TicketTypeId : string
    Quantity : int32
    PriceEach : decimal
    TotalPrice : decimal
}