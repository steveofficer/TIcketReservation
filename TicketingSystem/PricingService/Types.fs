namespace PricingService.Types

module Requests =
    type PriceTicketsRequest = {
        UserId : string
        Tickets : TicketQuantity[]
    } and TicketQuantity = {
        TicketTypeId : string
        Quantity : int32
    }

module Responses = 
    type PriceResponse = {
        OrderId : string
        TicketPrices : TicketPrice[]
        TotalPrice : decimal
        AntiForgeryToken : string
    } and TicketPrice = {
        TicketTypeId : string
        Quantity : int32
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
    