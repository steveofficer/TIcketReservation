import { Component, OnInit, Input } from '@angular/core';
import { CommunicationService } from '../../communication/communication.service';
import { ITicket } from '../../event-model';

@Component({
  selector: 'ticket-list',
  templateUrl: './ticket-list.component.html'
})
export class TicketListComponent implements OnInit {
  @Input()
  eventId: string;

  constructor(private commService: CommunicationService) {}

  ticketsLoaded = false;
  tickets : ITicket[] = [];

  ngOnInit(): void {
	console.log(this.eventId);
	this.commService.getTickets(this.eventId)
	.subscribe(t => {
		this.tickets = t;
		this.ticketsLoaded = true;
	})
  }
}