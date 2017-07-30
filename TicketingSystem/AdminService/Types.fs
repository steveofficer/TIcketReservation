namespace AdminService.Types

type EventSummary = {
    Id : string
    Name : string
    Start : System.DateTime
    End : System.DateTime
}

type EventDetail = {
    Id : string
    Name : string
    Start : System.DateTime
    End : System.DateTime
    Location : string
    Information : string
    Tickets : Ticket[]
} and Ticket = {
    TicketTypeId : string
    mutable Description : string
}

type NewEvent = {
    Name : string
    Start : System.DateTime
    End : System.DateTime
    Location : string
    Information : string
}

type TicketInfo = {
    EventId : string
    Id : string
    Description : string
    Quantity : int32
    Price : decimal
}

type NewTicket = {
    Description : string
    Price : decimal
    Quantity : int32
}

