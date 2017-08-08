import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule, Routes } from '@angular/router';

import { AppComponent } from './app.component';
import { EventDetailComponent } from './components/events/event-detail.component';
import { EventListComponent } from './components/events/event-list.component';
import { CreateEventComponent } from './components/events/create-event.component';
import { TicketListComponent } from './components/tickets/ticket-list.component'; 
import { TicketDetailComponent } from './components/tickets/ticket-detail.component';
import { CommunicationService } from './communication/communication.service';

const appRoutes: Routes = [
  { path: 'event/:id', component: EventDetailComponent },
  { path: 'events', component: EventListComponent },
  { path: 'create-event', component: CreateEventComponent },
  { path: 'event/:id/tickets', component: TicketListComponent },
  { path: 'event/:eid/tickets/:tid', component: TicketDetailComponent },
  { path: '', redirectTo: '/events', pathMatch: 'full' }
];

@NgModule({
  declarations: [ 
  	AppComponent,
  	EventDetailComponent,
  	EventListComponent,
    CreateEventComponent,
  	TicketListComponent,
  	TicketDetailComponent
  ],
  imports: [ 
  	RouterModule.forRoot(appRoutes),
  	BrowserModule, 
  	HttpClientModule, 
  	RouterModule,
    FormsModule
  ],
  providers: [ CommunicationService ],
  bootstrap: [ AppComponent ]
})
export class AppModule { }
