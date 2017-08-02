module LedgerService.Types

type CancellationDetails = {
    CancellationId : string
    TotalPrice : decimal
    Tickets : CancelledTicket[]
} and CancelledTicket = {
    TicketTypeId : string
    TicketId : string
}

type AllocationDetails = {
    TotalPrice : decimal
    Tickets : AllocatedTicket[]
} and AllocatedTicket = {
    TicketTypeId : string
    TicketId : string
}

type QuoteDetails = {
    PricesQuotedAt : System.DateTime
    Tickets : TicketInfo[]
    TotalPrice : decimal
} and TicketInfo = {
    TicketTypeId : string
    Quantity : int32
    PriceEach : decimal
}

type TransactionDetails =
    | Quotation of QuoteDetails
    | Cancellation of CancellationDetails
    | Allocation of AllocationDetails
        
type Transaction = {
    SourceId : System.Guid
    EventId : string
    OrderId : string
    UserId : string
    TransactionDate : System.DateTime    
    Details : TransactionDetails
}