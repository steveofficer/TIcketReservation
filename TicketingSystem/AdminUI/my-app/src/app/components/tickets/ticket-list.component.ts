import { Component, OnInit, Input } from '@angular/core';
import { CommunicationService } from '../../communication/communication.service';
import { ITicketDetail, NewTicket } from '../../event-model';

@Component({
  selector: 'ticket-list',
  templateUrl: './ticket-list.component.html'
})
export class TicketListComponent implements OnInit {
  @Input()
  eventId: string;

  @Input()
  tickets: ITicketDetail[];

  newTicket: NewTicket = new NewTicket('', 0, 0);

  constructor(private commService: CommunicationService) {}

  ngOnInit(): void {
  }

  addTicket() {
    this.commService.addTicket(this.eventId, this.newTicket).subscribe(id => console.log(id), err => console.log(err));
  }
}