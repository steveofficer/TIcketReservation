import { Injectable } from '@angular/core';
import { Http, Response } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import { NewEvent, NewTicket, IEvent, ITicketDetail, IEventDetail } from '../event-model'

import 'rxjs/add/operator/map';
import 'rxjs/add/observable/of';
import 'rxjs/add/observable/empty';

@Injectable()
export class CommunicationService {
	constructor (private http: Http) {}

	getEvents(): Observable<IEvent[]> {
		return this.http.get('/api/events').map(r => <IEvent[]>r.json());
	}

	createEvent(event: NewEvent): Observable<string> {
		return this.http.put('/api/events', event).map(r => r.json());
	}

	updateEvent(event: IEventDetail): Observable<any> {
		return this.http.post(`/api/events/${event.Id}`, event).map(r => {});
	}

	getEventDetails(eventId: string): Observable<IEventDetail> {
		return this.http.get(`/api/events/${eventId}`).map(r => <IEvent>r.json())	
	}

	getTickets(eventId: string): Observable<ITicketDetail[]> {
		return this.http.get(`/api/events/${eventId}}/tickets`).map(r => <ITicketDetail[]>r.json())
	}

	getTicketDetails(eventId: string, ticketId: string): Observable<ITicketDetail> {
		return this.http.get(`/api/events/${eventId}/tickets/${ticketId}`).map(r => <ITicketDetail>r.json());
	}
	
	addTicket(eventId: string, ticket: NewTicket): Observable<string> {
		return this.http.put(`/api/events/${eventId}/tickets`, ticket).map(r => r.json());
	}

	updateTicket(eventId: string, ticket: ITicketDetail): Observable<any> {
		return this.http.post(`/api/events/${eventId}/tickets/${ticket.Id}`, ticket).map(r => r.json());
	}
}
