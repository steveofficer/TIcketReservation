import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommunicationService } from '../../communication/communication.service';
import { NewEvent } from '../../event-model';

@Component({
  selector: 'create-event',
  templateUrl: './create-event.component.html'
})
export class CreateEventComponent {
  constructor(
  	private commService: CommunicationService,
  	private router: Router
  ) {}

  event: NewEvent = new NewEvent("", new Date(), new Date(), "", "");
  error: string;

  onSubmit() {
  	this.commService.createEvent(this.event).subscribe(
  		id => this.router.navigate(['/event', id]),
  		err => this.error = `Error while saving event: ${err.statusText}`
  	);
  }
}