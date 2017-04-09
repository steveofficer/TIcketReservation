namespace AvailabilityService.Types

type TicketQuantity = {
    TicketTypeId : string
    Quantity : uint32
}

module Requests =
    type BookTicketRequest = {
        UserId : string
        PaymentReference : string
        OrderId : string
        Tickets : TicketQuantity[]
    }

module Responses = 
    type AvailabilityResponse = {
        OrderId : string
        TicketAvailability : TicketQuantity[]
        AsAt : System.DateTime
    }

module Db =
    type TicketAvailabilityInfo = {
        TicketTypeId : string
        AvailableQuantity : uint32
        OriginalQuantity : uint32
    }

    type EventAvailability = {
        EventId : string
        Tickets : TicketAvailabilityInfo[]
    }