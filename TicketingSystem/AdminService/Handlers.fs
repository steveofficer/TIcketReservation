module Handlers

open Suave
open Suave.Successful
open Suave.RequestErrors
open Suave.Operators
open Newtonsoft.Json
open MongoDB.Bson
open AdminService.Types

let ``get all events`` (query : unit -> Async<EventSummary[]>) (ctx : HttpContext) = async {
    let! events = query()
    return! events |> JsonConvert.SerializeObject |> OK <| ctx
}
    
let ``get event details`` (query : string -> Async<EventDetail option>) (eventId : string) (ctx : HttpContext) = async {
    let! event = query eventId
    match event with
    | Some e -> return! e |> JsonConvert.SerializeObject |> OK <| ctx
    | None -> return! NOT_FOUND "Event not found" ctx
}

let ``get event ticket details`` (query : string -> string -> Async<TicketInfo[]>) (eventId : string) (ticketId : string) (ctx : HttpContext) = async {
    let! ticket = query eventId ticketId
    return! ticket |> JsonConvert.SerializeObject |> OK <| ctx
}

let ``create event`` (command : EventDetail -> Async<unit>) (ctx : HttpContext) = async {
    let event = 
        ctx.request.rawForm 
        |> System.Text.UTF8Encoding.UTF8.GetString 
        |> (fun s -> JsonConvert.DeserializeObject<NewEvent>(s))
    let eventModel = { 
        Id = ObjectId.GenerateNewId().ToString() 
        Name = event.Name 
        Information = event.Information
        Location = event.Location
        Start = event.Start 
        End = event.End 
        Tickets = [||]
    }
    do! command eventModel
    return! ACCEPTED "" ctx
}

let ``create ticket type`` (command : TicketInfo -> Async<unit>) (eventId : string) (ctx : HttpContext) = async {
    let ticket = 
        ctx.request.rawForm 
        |> System.Text.UTF8Encoding.UTF8.GetString 
        |> (fun s -> JsonConvert.DeserializeObject<NewTicket>(s))
    let ticketModel = {
        EventId = eventId
        Id = ObjectId.GenerateNewId().ToString()
        Description = ticket.Description
        Quantity = ticket.Quantity
        Price = ticket.Price    
    }

    do! command ticketModel
    return! ACCEPTED "" ctx
}

let ``update event`` (command : EventDetail -> Async<unit>) (eventId : string) (ctx : HttpContext) = async {
    let event = 
        ctx.request.rawForm 
        |> System.Text.UTF8Encoding.UTF8.GetString 
        |> (fun s -> JsonConvert.DeserializeObject<EventDetail>(s))

    do! command event
    return! ACCEPTED "" ctx
}

let ``update ticket type`` (command : TicketInfo -> Async<unit>) (eventId : string) (ticketId : string) (ctx : HttpContext) = async {
    let ticket = 
        ctx.request.rawForm 
        |> System.Text.UTF8Encoding.UTF8.GetString 
        |> (fun s -> JsonConvert.DeserializeObject<TicketInfo>(s))

    do! command ticket
    return! ACCEPTED "" ctx
}