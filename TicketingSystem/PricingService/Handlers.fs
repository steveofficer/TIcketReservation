module PricingService.Handlers

open Suave
open Suave.Successful
open Suave.RequestErrors
open Suave.Operators
open Types.Requests
open Types.Responses
open Types.Db
open MongoDB.Bson
open Newtonsoft.Json
open PricingService.Contract.Events

let ``get event ticket prices`` (query : string -> Async<EventPricing option * System.DateTime>) (``event id`` : string) (ctx : HttpContext) = async {
    let! (event, asAt) = query ``event id``
    return!
        match event with
        | Some event -> event |> JsonConvert.SerializeObject |> OK <| ctx
        | None -> NOT_FOUND "Event not found" ctx
}

let ``create quote`` create_signature query (publish : TicketsQuotedEvent -> Async<unit>) (eventId : string) (ctx : HttpContext) = async {
    let request = 
        ctx.request.rawForm 
        |> System.Text.UTF8Encoding.UTF8.GetString 
        |> (fun s -> JsonConvert.DeserializeObject<PriceTicketsRequest>(s))
    
    let computePrices (prices : Map<string,decimal>) = 
        let result = 
            request.Tickets 
            |> Array.map (fun t -> 
                {   
                    TicketTypeId = t.TicketTypeId
                    Quantity = t.Quantity
                    PricePer = prices.[t.TicketTypeId] 
                    TotalPrice = decimal(t.Quantity) * prices.[t.TicketTypeId] 
                }
            )
            |> (fun pricedTickets -> 
                let orderId =  ObjectId.GenerateNewId().ToString()
                { 
                    OrderId = orderId
                    TicketPrices = pricedTickets
                    TotalPrice = pricedTickets |> Array.sumBy (fun t -> t.TotalPrice)
                    AntiForgeryToken = create_signature orderId pricedTickets
                }
            )

        result

    // Get the unique ticket ids that have been requested
    let ticketIds = request.Tickets |> Array.map (fun r -> r.TicketTypeId) |> Set.ofArray
         
    // Get the prices of the tickets, and also remember what time the prices were valid at
    let! (maybePrices : Map<string, decimal> option, asAt) = eventId |> query <| ticketIds
    
    let maybePricedTickets = 
        maybePrices 
        |> Option.bind (fun prices -> if ticketIds |> Set.forall prices.ContainsKey then computePrices prices |> Some else None)

    let asEvent (priced_tickets : PriceResponse) = 
        { 
            EventId = eventId; 
            OrderId = priced_tickets.OrderId; 
            PricesValidAt = asAt; 
            TotalPrice = priced_tickets.TotalPrice; 
            UserId = request.UserId; 
            Tickets = priced_tickets.TicketPrices |> Array.map (fun ticket -> { TicketTypeId = ticket.TicketTypeId; Quantity = ticket.Quantity; PriceEach = ticket.PricePer; TotalPrice = ticket.TotalPrice }) 
        }

    match maybePricedTickets with
    | None -> return! NOT_FOUND "Either the event or the ticket types did not exist" ctx
    | Some priced_tickets ->
        do! publish (priced_tickets |> asEvent)
        return! priced_tickets |> JsonConvert.SerializeObject|> ACCEPTED <| ctx  
} 
