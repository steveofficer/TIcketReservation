namespace AvailabilityService.Types

module Requests =
    type ConfirmOrderRequest = {
        UserId : string
        PaymentReference : string
        OrderId : string
        Tickets : TicketInfo[]
    } and TicketInfo = {
        TicketTypeId : string
        Quantity : uint32
        PricePer : decimal
    }

module Responses = 
    type AvailabilityResponse = {
        OrderId : string
        TicketAvailability : TicketQuantity[]
    } and TicketQuantity = {
        TicketTypeId : string
        Quantity : uint32
    }

module Db =
    type EventAvailability = {
        Id : string
        Tickets : TicketAvailabilityInfo[]
    } and TicketAvailabilityInfo = {
        TicketTypeId : string
        AvailableQuantity : uint32
        OriginalQuantity : uint32
    }