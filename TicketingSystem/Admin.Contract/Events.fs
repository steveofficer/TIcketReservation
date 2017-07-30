namespace Admin.Contract

type EventTicketTypeCreatedEvent = {
    EventId : string
    TicketTypeId : string
    Description : string
    Quantity : int32
    Price : decimal
}

type EventTicketTypePriceChangedEvent = {
    EventId : string
    TicketTypeId : string
    Price : decimal
    EffectiveFrom : System.DateTime
}

type EventTicketTypeQuantityChangedEvent = {
    EventId : string
    TicketTypeId : string
    Quantity : decimal
}
