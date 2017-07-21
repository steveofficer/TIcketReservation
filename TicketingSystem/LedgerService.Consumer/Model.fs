module Model

type CancellationDetails = {
    TicketIds : string[]
}

type AllocationDetails = {
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
    Quantity : uint32
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


