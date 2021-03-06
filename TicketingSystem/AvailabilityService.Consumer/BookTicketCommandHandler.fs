﻿namespace AvailabilityBooking
open AvailabilityService.Contract.Commands
open AvailabilityService.Contract.Events
open AvailabilityService.Types
open System.Data
open System.Collections.Generic

type BookTicketsCommandHandler
    (
    publish, 
    ``id gen`` : unit -> string,
    factory : unit -> Async<IDbConnection>,
    findExistingAllocation : IDbConnection -> string -> Async<AllocationInfo[]>, 
    reserveTickets : IDbConnection -> IDictionary<string, int32> -> Async<IDbTransaction option>, 
    recordAllocation : IDbTransaction -> TicketsAllocatedEvent -> Async<unit>) =
    inherit RabbitMQ.Subscriber.PublishingMessageHandler<BookTicketsCommand>(publish)
    
    member private this.``handle new allocation`` (requestedAt) (message : BookTicketsCommand) (conn) = async {
        // Pre generate the tickets so we don't hold a lock for too long, only lock when we need to.
        let allocatedTickets = 
            message.Tickets 
            |> Array.collect (fun t -> [| for _ in 1 .. t.Quantity do yield { AllocatedTicket.TicketTypeId = t.TicketTypeId; AllocatedAt = System.DateTime.UtcNow; TicketId = ``id gen``(); Price = t.PriceEach } |])
            
        let allocatedEvent = {
            EventId = message.EventId
            OrderId = message.OrderId
            PaymentReference = message.PaymentReference
            RequestedAt = requestedAt
            UserId = message.UserId
            TotalPrice = allocatedTickets |> Array.map (fun t -> t.Price) |> Array.sum
            Tickets = allocatedTickets
        }

        // Now query and lock the records that we are interested in
        let! reservation_result = 
            message.Tickets 
            |> Array.map (fun t -> (t.TicketTypeId, t.Quantity)) 
            |> dict 
            |> reserveTickets conn
        match reservation_result with
        | None -> 
            // Either none of the tickets are available, or some of them aren't available. Either way, we can't fulfill the order as requested.
            let failedMessage = 
                {
                    EventId = message.EventId
                    OrderId = message.OrderId
                    PaymentReference = message.PaymentReference
                    RequestedAt = requestedAt
                    FailedAt = System.DateTime.UtcNow
                    Tickets = message.Tickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; Quantity = t.Quantity })
                    UserId = message.UserId
                    Reason = "The tickets were not available"
                }
            do! this.Publish failedMessage
            
        | Some transaction ->
            // We have available tickets.
            // Record the allocation
            do! recordAllocation transaction allocatedEvent
            
            // Commit the transaction now.
            transaction.Commit()

            // If this fails then the message will be retried. Because the allocation has already committed the publish will be retried.
            do! this.Publish allocatedEvent
            
        return ()
    }

    override this.HandleMessage(messageId) (sentAt) (message : BookTicketsCommand) = async {
        // Open a connection to the database
        use! conn = factory()

        // First check to see if the tickets have already been allocated. If so we just need to re-publish the result, it might be a retry of a failed attempt
        let! existingAllocation = findExistingAllocation conn message.OrderId
        
        match existingAllocation with
        | [||] -> do! this.``handle new allocation`` sentAt message conn
        | allocatedTickets ->
            // We have previously allocated these tickets. Re-publish the event as there might have been a previous failure that prevented this from happening.
            let allocatedEvent = {
                EventId = message.EventId
                OrderId = message.OrderId
                PaymentReference = message.PaymentReference
                RequestedAt = sentAt
                UserId = message.UserId
                TotalPrice = allocatedTickets |> Array.map (fun e -> e.Price) |> Array.sum
                Tickets = allocatedTickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; AllocatedAt = t.AllocatedAt; TicketId = t.TicketId; Price = t.Price})
            }
            do! this.Publish allocatedEvent
    }

    