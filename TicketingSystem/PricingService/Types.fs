namespace PricingService.Types

module Requests =
    type TicketQuantity = {
        TicketTypeId : string
        Quantity : uint32
    }

    type PriceTicketsRequest = {
        UserId : string
        Tickets : TicketQuantity[]
    }

module Responses = 
    type TicketPrice = {
        TicketTypeId : string
        Quantity : uint32
        PricePer : decimal
        TotalPrice : decimal
    }
    
    type PriceResponse = {
        OrderId : string
        TicketPrices : TicketPrice[]
        TotalPrice : decimal
    }

module Db =
    type TicketPriceInfo = {
        TicketTypeId : string
        Price : decimal
    }

    type EventPricing = {
        Id : string
        Tickets : TicketPriceInfo[]
    }