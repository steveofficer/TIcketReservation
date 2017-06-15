namespace AvailabilityBooking
open AvailabilityService.Contract.Commands
open AvailabilityService.Contract.Events
open MongoDB.Driver

type EventAvailability = {
    Id : string
    Tickets : TicketAvailability[]
    HandledOrders : System.Collections.Generic.List<AllocatedOrder>
    mutable Version : uint32
} and TicketAvailability = {
    TicketTypeId : string
    mutable AvailableQuantity : uint32
} and AllocatedOrder = {
    OrderId : string
    Tickets : AllocatedTicket []
} and AllocatedTicket = {
    TicketTypeId : string
    TicketId : string
    Price : decimal
}

type BookTicketsCommandHandler(publish, collection : IMongoCollection<EventAvailability>) =
    inherit RabbitMQ.Subscriber.PublishingMessageHandler<BookTicketsCommand>(publish)
    override this.Handle(message : BookTicketsCommand) = async {
        let (|EventNotFound|EventFound|) (event : EventAvailability) = 
            if System.String.IsNullOrEmpty(event.Id) then EventNotFound
            else EventFound event

        let outbound = 
            match collection.FindSync(fun e -> e.Id = message.EventId).SingleOrDefault() with
            | EventNotFound -> 
                {
                    EventId = message.EventId
                    OrderId = message.OrderId
                    RequestedAt = System.DateTime.UtcNow
                    Tickets = message.Tickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; Quantity = t.Quantity })
                    UserId = message.UserId
                    Reason = "The event could not be found"
                } :> obj
            | EventFound e -> this.HandleAllocateOrder message e
        
        do! publish outbound
    }

    member private this.HandleAllocateOrder(message : BookTicketsCommand) (e) =
        let availability = e.Tickets |> Array.map (fun t -> (t.TicketTypeId, t.AvailableQuantity)) |> Map.ofArray

        let (|SufficientQuantity|InsufficientQuantity|) (e : EventAvailability) = 
            let requestedTickets = message.Tickets
            if requestedTickets |> Array.forall (fun t -> availability.[t.TicketTypeId] >= t.Quantity)
            then InsufficientQuantity
            else SufficientQuantity
        
        match e with
        | InsufficientQuantity ->
            {
                EventId = message.EventId
                OrderId = message.OrderId
                RequestedAt = System.DateTime.UtcNow
                Tickets = message.Tickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; Quantity = t.Quantity })
                UserId = message.UserId
                Reason = "Insufficient Availability"
            } :> obj
        
        | SufficientQuantity ->
            message.Tickets 
            |> Array.iter (fun t -> e.Tickets |> Array.find (fun i -> i.TicketTypeId = t.TicketTypeId) |> (fun x -> x.AvailableQuantity <- x.AvailableQuantity - t.Quantity))

            let allocation = {
                OrderId = message.OrderId
                Tickets = message.Tickets |> Array.collect (fun t -> [| for x in 0u .. t.Quantity do yield { TicketTypeId = t.TicketTypeId; TicketId = System.Guid.NewGuid().ToString(); Price = t.PriceEach } |])
            }
            e.HandledOrders.Add  |> ignore
            
            // Update with an optimistic concurrency check.
            let version = e.Version
            e.Version <- version + 1u

            let result = collection.ReplaceOne((fun x -> x.Id = message.EventId && x.Version = version), e)
            if result.IsAcknowledged && result.ModifiedCount > 0L
            then failwith "Optimistic Concurrency Failure"
            
            {
                EventId = message.EventId
                OrderId = message.OrderId
                PaymentReference = message.PaymentReference
                RequestedAt = System.DateTime.UtcNow
                UserId = message.UserId
                TotalPrice = allocation.Tickets |> Array.map (fun e -> e.Price) |> Array.sum
                Tickets = allocation.Tickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; TicketId = System.Guid.NewGuid().ToString(); Price = t.Price })
            } :> obj