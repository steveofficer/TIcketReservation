namespace AvailabilityCancellation
open AvailabilityService.Contract.Commands
open AvailabilityService.Contract.Events
open AvailabilityService.Types.Db
open MongoDB.Driver
open System.Data

/// When a cancel tickets command is received we need to replenish the available ticket pool with
/// the tickets that have been cancelled.
type CancelTicketsCommandHandler(
    publish, 
    factory : unit -> Async<IDbConnection>,
    findExistingCancellation : IDbConnection -> string[] -> Async<CancellationInfo[]>,
    cancelTickets : IDbConnection -> TicketsCancelledEvent -> Async<unit>) =
    inherit RabbitMQ.Subscriber.PublishingMessageHandler<CancelTicketsCommand>(publish)
    
    override this.HandleMessage (messageId) (sentAt) (message : CancelTicketsCommand) = async {
        use! conn = factory()
        
        // Check to see if the cancellation has already been handled
        let! cancelledTickets = hasHandeledCancellation conn messageId
        
        match cancelledTickets with
        | [||] ->
            // Cancel all of the tickets
            {}
        | cancelled ->
            let toCancel = message.TicketIds |> Array.except cancelled

        // If it has then republish the event
        // If it hasn't then cancel the tickets
        // And publish the event
        return () 
    }