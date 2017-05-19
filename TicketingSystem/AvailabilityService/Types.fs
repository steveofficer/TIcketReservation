namespace AvailabilityService.Types

type TicketQuantity = {
    TicketTypeId : string
    Quantity : uint32
}

module Requests =
    type ConfirmOrderRequest = {
        UserId : string
        PaymentReference : string
        OrderId : string
    }

module Responses = 
    type AvailabilityResponse = {
        OrderId : string
        TicketAvailability : TicketQuantity[]
    }

module Db =
    type TicketAvailabilityInfo = {
        TicketTypeId : string
        AvailableQuantity : uint32
        OriginalQuantity : uint32
    }

    type EventAvailability = {
        Id : string
        Tickets : TicketAvailabilityInfo[]
    }