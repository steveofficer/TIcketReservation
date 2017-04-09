﻿namespace AvailabilityService.Events

type TicketQuantity = {
    TicketTypeId : string
    Quantity : uint32
}

type TicketPrice = {
    TicketTypeId : string
    TicketId : string
    Price : decimal
}

type TicketsBookedEvent = {
    EventId : string
    OrderId : string
    PaymentReference : string
    BookedAt : System.DateTime
    UserId : string
    Tickets : TicketPrice[]
    TotalPrice : decimal
}

type TicketsCancelledEvent = {
    EventId : string
    OrderId : string
    CancelledAt : System.DateTime
    TicketIds : string[]
    UserId : string
}

type TicketsUnavailableEvent = {
    EventId : string
    OrderId : string
    RequestedAt : System.DateTime
    Tickets : TicketQuantity[]
    UserId : string
}