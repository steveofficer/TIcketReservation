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
    override this.Handle(message : TicketsCancelledEvent) = async {
        return ()
    }

type TicketsAllocatedHandler(collection : IMongoCollection<ClientCallback>) =
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsAllocatedEvent>()
    override this.Handle(message : TicketsAllocatedEvent) = async {
        return ()    
    }

type TicketsAllocationFailedHandler(collection : IMongoCollection<ClientCallback>) =
    inherit RabbitMQ.Subscriber.MessageHandler<TicketsAllocationFailedEvent>()
    override this.Handle(message : TicketsAllocationFailedEvent) = async {
        return ()    
    }