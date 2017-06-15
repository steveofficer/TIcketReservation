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

let ``get event ticket availability`` (query : string -> Async<EventAvailability option * System.DateTime>) (``event id`` : string) (ctx : HttpContext) = async {
    let! (event, at) = query ``event id``
    return!
        match event with
        | Some event -> event |> JsonConvert.SerializeObject |> OK <| ctx
        | None -> "Not found" |> NOT_FOUND <| ctx
}

let ``confirm order`` (send : BookTicketsCommand -> Async<unit>) (``event id`` : string) (ctx : HttpContext) = async {
    let request = 
        ctx.request.rawForm 
        |> System.Text.UTF8Encoding.UTF8.GetString 
        |> (fun s -> JsonConvert.DeserializeObject<ConfirmOrderRequest>(s))
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