module Handlers

open AvailabilityService.Contract.Events
open MongoDB.Driver

type ClientCallback = {
    EventId : string
    EventType : string
    CallbackUrl : string
}

type TicketsCancelledHandler(collection : IMongoCollection<ClientCallback>) =
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsCancelledEvent>()
    override this.HandleMessage (messageId) (sentAt) (message : TicketsCancelledEvent) = async {
        return ()
    }

type TicketsCancellationFailedHandler(collection : IMongoCollection<ClientCallback>) =
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsCancellationFailedEvent>()
    override this.HandleMessage (messageId) (sentAt) (message : TicketsCancellationFailedEvent) = async {
        return ()
    }

type TicketsAllocatedHandler(collection : IMongoCollection<ClientCallback>) =
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsAllocatedEvent>()
    override this.HandleMessage (messageId) (sentAt) (message : TicketsAllocatedEvent) = async {
        return ()    
    }

type TicketsAllocationFailedHandler(collection : IMongoCollection<ClientCallback>) =
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsAllocationFailedEvent>()
    override this.HandleMessage (messageId) (sentAt) (message : TicketsAllocationFailedEvent) = async {
        return ()    
    }