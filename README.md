# TicketReservation
Code for TM470 final project. A microservice based ticket reservation system

It consists of the following microservices:
 - Availability.Booking : This service is concerned with subscribing to Booking commands and allocating tickets
 - Availability.Cancellation : This service is concerned with subscribing to Cancallation commands and cancelling tickets
 - Ledger : This service records a history of transactions such as Quotes, Allocations and Cancellations
 - Gateway : This service handles Allocation events, AllocationFailed events, CancellationSuccess events and CancellationFailed events and forwarding them on to external systems


