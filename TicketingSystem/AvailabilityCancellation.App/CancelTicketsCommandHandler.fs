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
    cancelTickets : IDbConnection -> TicketsCancelledEvent -> Async<unit>) =
    inherit RabbitMQ.Subscriber.PublishingMessageHandler<CancelTicketsCommand>(publish)
    
    override this.HandleMessage (messageId) (sentAt) (message : CancelTicketsCommand) = async {
        // Create the cancellation event that we are going to publish
        let cancellation = {
            EventId = message.EventId
            OrderId = message.OrderId
            CancellationId = message.CancellationId
            RequestedAt = sentAt
            Tickets = message.Tickets |> Array.map (fun t -> { TicketTypeId = t.TicketTypeId; TicketId = t.TicketId })
            TotalPrice = message.TotalPrice
            UserId = message.UserId
        }

        use! conn = factory()
        
        // Check to see if the cancellation has already been handled
        let! alreadyHandled = cancellationExists conn message.CancellationId
        
        if not alreadyHandled 
        then
            // It hasn't been handled yet, so record the cancellation
            do! cancelTickets conn cancellation
            
            
        // Publish the event
        do! this.Publish cancellation
        return () 
    }