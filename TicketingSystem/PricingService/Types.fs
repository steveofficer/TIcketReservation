namespace PricingService.Types

module Requests =
    type PriceTicketsRequest = {
        UserId : string
        Tickets : TicketQuantity[]
    } and TicketQuantity = {
        TicketTypeId : string
        Quantity : uint32
    }

module Responses = 
    type PriceResponse = {
        OrderId : string
        TicketPrices : TicketPrice[]
        TotalPrice : decimal
    } and TicketPrice = {
        TicketTypeId : string
        Quantity : uint32
        PricePer : decimal
        TotalPrice : decimal
    }

module Db =
    type EventPricing = {
        Id : string
        Tickets : TicketPriceInfo[]
    } and TicketPriceInfo = {
        TicketTypeId : string
        Price : decimal
    }