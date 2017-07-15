import { Component, OnInit } from '@angular/core';
import { CommunicationService } from '../../communication/communication.service';
import { IEvent } from '../../event-model';

@Component({
  selector: 'event-list',
  templateUrl: './event-list.component.html'
})
export class EventListComponent implements OnInit {
  constructor(private commService: CommunicationService) {}

  eventsLoaded = false;
  events : IEvent[] = [];
  error: string = null;

  ngOnInit(): void {
	  this.reload();
  }

  reload() {
    this.commService.getEvents()
    .subscribe(
      e => {
        this.events = e;
        this.eventsLoaded = true;
      },
      err => this.error = `Error while loading events: ${err.statusText}`
    );
  }
}