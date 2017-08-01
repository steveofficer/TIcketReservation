module AdminService.Web.Types

type EventDetail = {
    Id : string
    Name : string
    Start : System.DateTime
    End : System.DateTime
    Location : string
    Information : string
    Tickets : TicketDetail[]
} and TicketDetail = {
    TicketTypeId : string
    Description : string
    Quantity : int32
    Price : decimal
}