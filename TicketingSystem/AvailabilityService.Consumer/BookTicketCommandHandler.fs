namespace AvailabilityBooking
open AvailabilityService.Contract.Commands
open AvailabilityService.Contract.Events
open MongoDB.Bson
open AvailabilityService.Types.Db
open System.Data.SqlClient
open System.Collections.Generic

type BookTicketsCommandHandler
    (
    publish, 
    factory : unit -> Async<SqlConnection>,
    findExistingAllocation : SqlConnection -> string -> Async<AllocationInfo[]>, 
    reserveTickets : SqlConnection -> IDictionary<string, uint32> -> Async<SqlTransaction option>, 
    recordAllocation : SqlTransaction -> TicketsAllocatedEvent -> Async<unit>) =
    inherit RabbitMQ.Subscriber.PublishingMessageHandler<BookTicketsCommand>(publish)
    override this.Handle(message : BookTicketsCommand) = async {
        use! conn = factory()

        // First check to see if the tickets have already been allocated. If so we just need to republish the result
        let! existingAllocation = findExistingAllocation conn message.OrderId
        
        match existingAllocation with
        | [||] -> 
            // There are no existing allocations, so we need to try to find tickets
            // Pre generate the tickets so we don't hold a lock for too long, only lock when we need to.
            let allocatedTickets = 
                message.Tickets 
                |> Array.collect (fun t -> [| for _ in 0u .. t.Quantity do yield { AllocatedTicket.TicketTypeId = t.TicketTypeId; TicketId = ObjectId.GenerateNewId().ToString(); Price = t.PriceEach } |])
            
            let allocatedEvent = 
                {
                    EventId = message.EventId
                    OrderId = message.OrderId
                    PaymentReference = message.PaymentReference
                    RequestedAt = System.DateTime.UtcNow
                    UserId = message.UserId
                    TotalPrice = allocatedTickets |> Array.map (fun t -> t.Price) |> Array.sum
                    Tickets = allocatedTickets
                }

            // Now query and lock the records that we are interested in
            let! result = message.Tickets |> Array.map (fun t -> (t.TicketTypeId, t.Quantity)) |> dict |> reserveTickets conn
            match result with
            | None -> 
                // Either none of the tickets are available, or some of them aren't available. Either way, we can't fulfill the order as requested.
                let failedMessage = 
                    {
                        EventId = message.EventId
                        OrderId = message.OrderId
                        RequestedAt = System.DateTime.UtcNow
                        Tickets = message.Tickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; Quantity = t.Quantity })
                        UserId = message.UserId
                        Reason = "The tickets were not available"
                    } :> obj
                do! publish failedMessage
            
            | Some transaction ->
                // We have available tickets. Create the tickets and release them.
                do! recordAllocation transaction allocatedEvent
                do! publish allocatedEvent
        
        | allocatedTickets ->
            // We have previously allocated these tickets. Republish the event as there might have been a previous failure that prevented this from happening.
            let allocatedEvent = 
                {
                    EventId = message.EventId
                    OrderId = message.OrderId
                    PaymentReference = message.PaymentReference
                    RequestedAt = System.DateTime.UtcNow
                    UserId = message.UserId
                    TotalPrice = allocatedTickets |> Array.map (fun e -> e.Price) |> Array.sum
                    Tickets = allocatedTickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; TicketId = t.TicketId; Price = t.Price})
                } :> obj
            do! publish allocatedEvent
    }