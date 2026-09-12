import { Component, inject } from '@angular/core';
import { TeamsDashboardStore } from './teams-dashboard.store';

@Component({
  selector: 'app-channel-configuration',
  templateUrl: './channel-configuration.html',
  styleUrl: './channel-configuration.css',
})
export class ChannelConfiguration {
  protected readonly store = inject(TeamsDashboardStore);

  protected onTeamChange(event: Event): void {
    const teamId = (event.target as HTMLSelectElement).value;
    if (teamId) {
      void this.store.selectTeam(teamId);
    }
  }

  protected onChannelChange(event: Event): void {
    const channelId = (event.target as HTMLSelectElement).value;
    if (channelId) {
      this.store.selectChannel(channelId);
    }
  }

  protected save(): void {
    void this.store.saveConfiguration();
  }
}
