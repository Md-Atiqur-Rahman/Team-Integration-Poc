import { Component, inject } from '@angular/core';
import { TeamsDashboardStore } from './teams-dashboard.store';

@Component({
  selector: 'app-connection-card',
  templateUrl: './connection-card.html',
  styleUrl: './connection-card.css',
})
export class ConnectionCard {
  protected readonly store = inject(TeamsDashboardStore);

  protected connect(): void {
    window.location.assign(this.store.connectUrl());
  }

  protected edit(): void {
    this.store.editConfiguration();
  }
}
