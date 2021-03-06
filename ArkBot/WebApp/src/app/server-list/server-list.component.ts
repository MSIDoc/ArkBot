import { Component, OnInit, OnDestroy } from '@angular/core';

import { DataService } from '../data.service';
import { NotificationsService } from 'angular2-notifications';
import { MessageService } from '../message.service';

@Component({
  selector: 'app-server-list',
  templateUrl: './server-list.component.html',
  styleUrls: ['./server-list.component.css']
})
export class ServerListComponent implements OnInit, OnDestroy {
  serverUpdatedSubscription: any;
  serverUpdateInterval: any;

  constructor(
    public dataService: DataService,
    private messageService: MessageService,
    private notificationsService: NotificationsService) {
    }

  ngOnInit() {
    this.serverUpdatedSubscription = this.messageService.serverUpdated$.subscribe(serverKey => this.showServerUpdateNotification(serverKey));

    this.serverUpdateInterval = window.setInterval(() => {
        this.dataService.updateServer(null);
      }, 60000);
  }

  ngOnDestroy() {
    this.serverUpdatedSubscription.unsubscribe();
    window.clearInterval(this.serverUpdateInterval);
  }

  showServerUpdateNotification(serverKey: string): void {
    this.notificationsService.success(
      'Server Update',
      `${serverKey} was updated; Reloading data...`,
      {
          showProgressBar: true,
          pauseOnHover: true,
          clickToClose: true
      }
    );
  }
}
