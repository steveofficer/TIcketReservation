namespace AvailabilityService.Types

type EventTicketInfo = {
    TicketTypeId : string
    RemainingQuantity : int32
}

type AllocationInfo = {
    TicketTypeId : string
    TicketId : string
    AllocatedAt : System.DateTime
    Price : decimal
}

type CancellationInfo = {
    TicketTypeId : string
    TicketId : string
}