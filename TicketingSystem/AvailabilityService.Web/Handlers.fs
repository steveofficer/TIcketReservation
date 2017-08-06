module AvailabilityService.Handlers

open Suave
open Suave.Successful
open Suave.RequestErrors
open Suave.Operators
open AvailabilityService.Types
open Newtonsoft.Json
open AvailabilityService.Contract.Commands

type ConfirmOrderRequest = {
    UserId : string
    PaymentReference : string
    OrderId : string
    AntiForgeryToken : string
    Tickets : TicketInfo[]
} and TicketInfo = {
    TicketTypeId : string
    Quantity : int32
    PricePer : decimal
}

type AvailabilityResponse = {
    OrderId : string
    TicketAvailability : TicketQuantity[]
} and TicketQuantity = {
    TicketTypeId : string
    Quantity : int32
}

type CancelTicketsRequest = {
    UserId : string
    Tickets : Ticket[]
} and Ticket = {
    TicketTypeId : string
    OrderId : string
    TicketId : string
}

let ``get event ticket availability`` (query : string -> Async<EventTicketInfo[]>) (``event id`` : string) (ctx : HttpContext) = async {
    let at = System.DateTime.UtcNow
    let! event = query ``event id``
    return! [| event :> obj; at :> obj |] |> JsonConvert.SerializeObject |> OK <| ctx
}

let ``confirm order`` verify_signature (send : BookTicketsCommand -> Async<unit>) (``event id`` : string) (ctx : HttpContext) = async {
    let request = 
        ctx.request.rawForm 
        |> System.Text.UTF8Encoding.UTF8.GetString 
        |> (fun s -> JsonConvert.DeserializeObject<ConfirmOrderRequest>(s))
    
    if not(verify_signature request.AntiForgeryToken request.OrderId request.Tickets)
    then 
        return! BAD_REQUEST "The quote is not valid. Antiforgery detection failed" ctx
    else
        let command = { 
            EventId = ``event id``; 
            UserId = request.UserId; 
            OrderId = request.OrderId; 
            PaymentReference = request.PaymentReference; 
            Tickets = request.Tickets |> Array.map(fun t -> { TicketTypeId = t.TicketTypeId; Quantity = t.Quantity; PriceEach = t.PricePer }) 
        }
        do! send command
        return! ACCEPTED request.OrderId ctx
}

let ``cancel tickets`` ``id gen`` (query : string -> string[] -> Async<decimal[]>) (send : CancelTicketsCommand -> Async<unit>) (``event id`` : string) (ctx : HttpContext) = async {
    let request = 
        ctx.request.rawForm 
        |> System.Text.UTF8Encoding.UTF8.GetString 
        |> (fun s -> JsonConvert.DeserializeObject<CancelTicketsRequest>(s))
    
    let cancellationId = ``id gen``()

    return! ACCEPTED "" ctx
}