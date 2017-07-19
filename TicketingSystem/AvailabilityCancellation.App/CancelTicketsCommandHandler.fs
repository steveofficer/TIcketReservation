namespace AvailabilityCancellation
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

type CancelTicketsCommandHandler(publish, collection : IMongoCollection<EventAvailability>) =
    inherit RabbitMQ.Subscriber.PublishingMessageHandler<CancelTicketsCommand>(publish)
    override this.HandleMessage (messageId) (sentAt) (message : CancelTicketsCommand) = async {
        return ()    
    }