import { Component, inject } from '@angular/core';
import { TeamsDashboardStore } from './teams-dashboard.store';

@Component({
  selector: 'app-send-message',
  templateUrl: './send-message.html',
  styleUrl: './send-message.css',
})
export class SendMessage {
  protected readonly store = inject(TeamsDashboardStore);

  protected onContentChange(event: Event): void {
    this.store.messageContent.set((event.target as HTMLTextAreaElement).value);
    this.store.lastSentMessage.set(null);
  }

  protected send(): void {
    void this.store.sendMessage();
  }
}
