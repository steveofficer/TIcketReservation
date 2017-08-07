namespace AvailabilityCancellation
open AvailabilityService.Contract.Commands
open AvailabilityService.Contract.Events
open AvailabilityService.Types
open MongoDB.Driver
open System.Data

/// When a cancel tickets command is received we need to replenish the available ticket pool with
/// the tickets that have been cancelled.
type CancelTicketsCommandHandler
    (
    publish, 
    factory : unit -> Async<IDbConnection>,
    cancellationExists : IDbConnection -> string -> Async<bool>,
    ticketsCanBeCancelled : IDbConnection -> string[] -> Async<bool>,
    cancelTickets : IDbConnection -> TicketsCancelledEvent -> Async<unit>) =
    inherit RabbitMQ.Subscriber.PublishingMessageHandler<CancelTicketsCommand>(publish)
    
    override this.HandleMessage (messageId) (sentAt) (message : CancelTicketsCommand) = async {
        

        use! conn = factory()
        
        // Check to see if the cancellation has already been handled
        let! alreadyHandled = cancellationExists conn message.CancellationId
        
        if not alreadyHandled 
        then
            // Check that the tickets actually exist and that they can be cancelled
            let! canBeCancelled = ticketsCanBeCancelled conn (message.Tickets |> Array.map (fun t -> t.TicketId)) 
            if canBeCancelled
            then 
                // Create the cancellation event that we are going to publish
                let cancellation = {
                    EventId = message.EventId
                    OrderId = message.OrderId
                    CancellationId = message.CancellationId
                    RequestedAt = sentAt
                    Tickets = message.Tickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; TicketId = t.TicketId; Price = t.Price })
                    TotalPrice = message.TotalPrice
                    UserId = message.UserId
                }
                // It hasn't been handled yet, so record the cancellation
                do! cancelTickets conn cancellation
                do! this.Publish cancellation
            else 
                let failure = {
                    TicketsCancellationFailedEvent.EventId = message.EventId
                    OrderId = message.OrderId
                    CancellationId = message.CancellationId
                    RequestedAt = sentAt
                    TotalPrice = message.TotalPrice
                    UserId = message.UserId
                    Reason = "Cancellation failed"
                }
                // The tickets can't be cancelled so fail the request
                do! this.Publish failure
        
        return ()
    }