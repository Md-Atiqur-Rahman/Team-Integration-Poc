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
  protected readonly displayName = new URLSearchParams(window.location.search).get('displayName');

  ngOnInit(): void {
    void this.store.initialize();
  }

  protected switchUser(): void {
    window.location.assign(`${window.location.origin}${window.location.pathname}`);
  }
}
