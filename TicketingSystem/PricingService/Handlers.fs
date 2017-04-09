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

let ``get event ticket prices`` (query : string -> Async<EventPricing option * System.DateTime>) (``event id`` : string) (ctx : HttpContext) = async {
    let! (event, asAt) = query ``event id``
    return!
        match event with
        | Some event -> event |> JsonConvert.SerializeObject |> OK <| ctx
        | None -> NOT_FOUND "Event not found" ctx
}

let ``get ticket prices`` query (publish : PriceResponse -> Async<unit>) (eventId : string) (ctx : HttpContext) = async {
    let request = 
        ctx.request.rawForm 
        |> System.Text.UTF8Encoding.UTF8.GetString 
        |> (fun s -> JsonConvert.DeserializeObject<PriceTicketsRequest>(s))
    let computePrices (prices : Map<string,decimal>) (pricesValidAt : System.DateTime) = 
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
                { 
                    OrderId = ObjectId.GenerateNewId().ToString()
                    TicketPrices = pricedTickets
                    TotalPrice = pricedTickets |> Array.sumBy (fun t -> t.TotalPrice)
                    AsAt = pricesValidAt
                }
            )

        result

    // Get the unique ticket ids that have been requested
    let ticketIds = request.Tickets |> Array.map (fun r -> r.TicketTypeId) |> Set.ofArray
         
    // Get the prices of the tickets, and also remember what time the prices were valid at
    let! (maybePrices : Map<string, decimal> option, asAt) = eventId |> query <| ticketIds
    match maybePrices with
    | None -> return! NOT_FOUND "Non-existing event was requested" ctx
    | Some prices ->
        let maybePricedTickets = if ticketIds |> Set.forall prices.ContainsKey then computePrices prices asAt |> Some else None
        match maybePricedTickets with
        | None -> return! NOT_FOUND "Non-existing Ticket Id(s) were requested" ctx
        | Some pricedTickets -> 
            do! publish pricedTickets
            return! pricedTickets |> JsonConvert.SerializeObject |> OK <| ctx  
} 
