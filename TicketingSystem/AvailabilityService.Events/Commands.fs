namespace AvailabilityService.Contract.Commands

type BookTicketsCommand = {
    UserId : string
    PaymentReference : string
    OrderId : string
}

