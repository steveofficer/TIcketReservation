module AvailabilityService.Handlers

open Suave
open Suave.Successful
open Suave.RequestErrors
open Suave.Operators
open Types.Requests
open Types.Responses
open Types.Db
open MongoDB.Bson
open Newtonsoft.Json
open AvailabilityService.Contract.Commands

let ``get event ticket availability`` (query : string -> Async<EventTicketInfo[]>) (``event id`` : string) (ctx : HttpContext) = async {
    let at = System.DateTime.UtcNow
    let! event = query ``event id``
    return! event |> JsonConvert.SerializeObject |> OK <| ctx
}

let ``confirm order`` verify_signature (send : BookTicketsCommand -> Async<unit>) (``event id`` : string) (ctx : HttpContext) = async {
    let request = 
        ctx.request.rawForm 
        |> System.Text.UTF8Encoding.UTF8.GetString 
        |> (fun s -> JsonConvert.DeserializeObject<ConfirmOrderRequest>(s))
    
    if not(verify_signature request.OrderId request.Tickets)
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