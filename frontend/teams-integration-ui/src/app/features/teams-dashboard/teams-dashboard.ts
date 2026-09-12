import { Component, OnInit, inject } from '@angular/core';
import { ChannelConfiguration } from './channel-configuration';
import { ConnectionCard } from './connection-card';
import { SendMessage } from './send-message';
import { TeamsDashboardStore } from './teams-dashboard.store';

@Component({
  selector: 'app-teams-dashboard',
  imports: [ConnectionCard, ChannelConfiguration, SendMessage],
  templateUrl: './teams-dashboard.html',
  styleUrl: './teams-dashboard.css',
})
export class TeamsDashboard implements OnInit {
  protected readonly store = inject(TeamsDashboardStore);

  ngOnInit(): void {
    void this.store.initialize();
  }
}
