import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { CommunicationService } from '../../communication/communication.service';
import { IEventDetail } from '../../event-model';

@Component({
  selector: 'event-detail',
  templateUrl: './event-detail.component.html'
})
export class EventDetailComponent implements OnInit {
  constructor(
  	private commService: CommunicationService,
  	private route: ActivatedRoute
  ) {}

  eventId: string;
  activeTab: string;
  event: IEventDetail;

  ngOnInit(): void {
  	this.activeTab = 'event';
  	this.eventId = this.route.snapshot.paramMap.get('id');
  	this.commService.getEventDetails(this.eventId)
  	   .subscribe(e => this.event = e);
  }

  activate(tab: string) {
  	this.activeTab = tab;
  }

  onSubmit() {
    this.commService.updateEvent(this.event).subscribe(_ => console.log("Saved"), err => console.log(err));
  }
}