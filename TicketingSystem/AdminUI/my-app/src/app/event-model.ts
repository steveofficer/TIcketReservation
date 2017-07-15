export class NewEvent {
	constructor(
		Name: string,
	    Start: Date,
	    End: Date,
	    Location: string,
	    Information: string
    ){}
}

export class NewTicket {
	constructor(
		Description: string,
	    Price: Date,
	    Quantity: Date
    ){}
}

export interface IEvent {
	Id: string,
	Name: string,
	Start: Date,
	End: Date
}

export interface ITicketDetail {
	Id: string,
	Description: string,
	Quantity: number,
	Price: number
}

export interface IEventDetail {
	Id: string,
    Name: string,
    Start: Date,
    End: Date,
    Location: string,
    Information: string
}