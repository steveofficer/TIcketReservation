import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { CommunicationService } from '../../communication/communication.service';
import { ITicketDetail } from '../../event-model';

@Component({
  selector: 'ticket-detail',
  templateUrl: './ticket-detail.component.html'
})
export class TicketDetailComponent implements OnInit {
  constructor(
  	private commService: CommunicationService,
  	private route: ActivatedRoute
   ) {}

  eventId: string;
  ticketId: string;
  ticket : ITicketDetail;

  ngOnInit(): void {
	this.eventId = this.route.snapshot.paramMap.get('eid');
	this.ticketId = this.route.snapshot.paramMap.get('tid');
	this.commService.getTicketDetails(this.eventId, this.ticketId)
	.subscribe(t =>  this.ticket = t);
  }
}