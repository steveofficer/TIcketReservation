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
        use! conn = factory()
        
        // Check to see if the cancellation has already been handled
        let! alreadyHandeled = cancellationExists conn message.CancellationId
        
        if alreadyHandeled 
        then
            // Just re-publish the event
            do! this.Publish 12
        else
            // Record the cancellation
            cancelTickets
            // Publish the event
            do! this.Publish conn
        return () 
    }