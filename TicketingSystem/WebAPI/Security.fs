module Security
open System.Security.Cryptography

let writeTicket (stream : System.IO.StreamWriter) (ticketTypeId : string) (quantity : int32) (price : decimal) =
    stream.Write(ticketTypeId)
    stream.Write(quantity)
    stream.Write(price)

let createHash (key : string) (orderId : string) (data : (string*int32*decimal)[]) =
    use buffer = new System.IO.MemoryStream()
    use writer = new System.IO.StreamWriter(buffer)

    writer.Write(key)
    writer.Write(orderId)
    data |> Array.iter (fun (id, qty, price) -> writeTicket writer id qty price)

    writer.Dispose()

    use hasher = SHA256.Create()
    hasher.ComputeHash(buffer.GetBuffer()) 
    |> System.Convert.ToBase64String

let ``create signature`` (key : string) (orderId : string) (data : PricingService.Types.Responses.TicketPrice[]) =
    let ticketData = data |> Array.map (fun t -> (t.TicketTypeId, t.Quantity, t.PricePer))
    createHash key orderId ticketData

let ``validate signature`` (key : string) (signature : string) (orderId : string) (data : AvailabilityService.Handlers.TicketInfo[]) =
    let ticketData = data |> Array.map (fun t -> (t.TicketTypeId, t.Quantity, t.PricePer))
    let computedHash = createHash key orderId ticketData
    computedHash = signature
