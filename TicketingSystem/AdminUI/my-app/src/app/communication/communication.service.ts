import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { NewEvent, NewTicket, IEvent, ITicketDetail, IEventDetail, IResult } from '../event-model'

import 'rxjs/add/operator/map';
import 'rxjs/add/observable/of';
import 'rxjs/add/observable/empty';

@Injectable()
export class CommunicationService {
	constructor (private http: HttpClient) {}

	getEvents(): Observable<IEvent[]> {
		return this.http.get<IEvent[]>('http://localhost:60000/admin/events');
	}

	createEvent(event: NewEvent): Observable<string> {
		return this.http.put<IResult>('http://localhost:60000/admin/events', event).map(r => r.Id);
	}

	updateEvent(event: IEventDetail): Observable<any> {
		return this.http.post<any>(`http://localhost:60000/admin/events/${event.Id}`, event);
	}

	getEventDetails(eventId: string): Observable<IEventDetail> {
		return this.http.get<IEventDetail>(`http://localhost:60000/admin/events/${eventId}`);	
	}

	getTickets(eventId: string): Observable<ITicketDetail[]> {
		return this.http.get<ITicketDetail[]>(`http://localhost:60000/admin/events/${eventId}/tickets`);
	}

	getTicketDetails(eventId: string, ticketId: string): Observable<ITicketDetail> {
		return this.http.get<ITicketDetail>(`http://localhost:60000/admin/events/${eventId}/tickets/${ticketId}`);
	}
	
	addTicket(eventId: string, ticket: NewTicket): Observable<string> {
		return this.http.put<IResult>(`http://localhost:60000/admin/events/${eventId}/tickets`, ticket).map(r => r.Id);
	}

	updateTicket(eventId: string, ticket: ITicketDetail): Observable<any> {
		return this.http.post<any>(`http://localhost:60000/admin/events/${eventId}/tickets/${ticket.Id}`, ticket);
	}
}
