DROP TABLE EventTickets;
DROP TABLE AllocatedOrders;
DROP TABLE AllocatedTickets;

CREATE TABLE EventTickets(
	EventId VARCHAR(50) NOT NULL,
	TicketTypeId VARCHAR(50) NOT NULL,
	OriginalQuantity INT NOT NULL,
	RemainingQuantity INT NOT NULL,
	CONSTRAINT PK_EventTicketID PRIMARY KEY (TicketTypeId)
);

CREATE TABLE AllocatedTickets(
	TicketTypeId VARCHAR(50) NOT NULL,
	TicketId VARCHAR(50) NOT NULL,
	OrderId INT NOT NULL,
	Price MONEY NOT NULL,
	CONSTRAINT PK_AllocatedTicketID PRIMARY KEY (TicketId),
	CONSTRAINT FK_TicketTypes FOREIGN KEY (TicketTypeId) REFERENCES EventTickets(TicketTypeId)
);