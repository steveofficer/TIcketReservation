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
    type EventTicketInfo = {
        TicketTypeId : string
        RemainingQuantity : int32
    }

    type AllocationInfo = {
        TicketTypeId : string
        TicketId : string
        Price : decimal
    }