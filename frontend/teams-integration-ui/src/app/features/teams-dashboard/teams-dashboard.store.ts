import { Injectable, computed, inject, signal } from '@angular/core';
import { TeamsApiClient, parseApiError } from '../../core/api-client/teams-api.client';
import { DEFAULT_HOST_CONTEXT, HostContext } from '../../core/config';
import { OrganizationConnectionStatus } from '../../core/models/connection-status.model';
import { ChannelItem, TeamItem } from '../../core/models/team.model';
import { TeamsConfigurationDto } from '../../core/models/teams-configuration.model';
import { SendMessageResponse } from '../../core/models/send-message.model';

type ConfigurationLoadState = 'loading' | 'loaded' | 'none' | 'error';
type ViewMode = 'loading' | 'configure' | 'send';

@Injectable({ providedIn: 'root' })
export class TeamsDashboardStore {
  private readonly api = inject(TeamsApiClient);

  readonly hostContext = signal<HostContext>(readHostContextFromUrl());

  // Reflects the ORGANIZATION's stored Teams connection (shared across every user in that
  // organization), not this browser's own Entra session — any user in the org sees 'active' the
  // moment anyone in the org has connected, with no OAuth of their own.
  readonly orgConnectionStatus = signal<'loading' | OrganizationConnectionStatus>('loading');
  readonly connectedAsEmail = signal<string | null>(null);

  readonly teams = signal<TeamItem[]>([]);
  readonly teamsLoading = signal(false);

  readonly channels = signal<ChannelItem[]>([]);
  readonly channelsLoading = signal(false);

  readonly selectedTeamId = signal<string | null>(null);
  readonly selectedChannelId = signal<string | null>(null);

  readonly configurationLoadState = signal<ConfigurationLoadState>('loading');
  readonly savedConfiguration = signal<TeamsConfigurationDto | null>(null);
  readonly viewMode = signal<ViewMode>('loading');

  readonly saveInProgress = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly messageContent = signal('');
  readonly sendInProgress = signal(false);
  readonly lastSentMessage = signal<SendMessageResponse | null>(null);

  readonly isConnected = computed(() => {
    const status = this.orgConnectionStatus();
    return status === 'active' || status === 'needsReconnect';
  });

  readonly canSave = computed(
    () => !!this.selectedTeamId() && !!this.selectedChannelId() && !this.saveInProgress(),
  );

  readonly canSendMessage = computed(
    () =>
      this.isConnected() &&
      this.savedConfiguration() !== null &&
      this.messageContent().trim().length > 0 &&
      !this.sendInProgress(),
  );

  readonly connectUrl = computed(() => this.api.connectUrl(window.location.href));

  async initialize(): Promise<void> {
    await this.loadConnectionStatus();
  }

  async loadConnectionStatus(): Promise<void> {
    this.orgConnectionStatus.set('loading');
    try {
      const status = await this.api.getConnectionStatus(this.hostContext().organizationId);
      this.orgConnectionStatus.set(status.connectionStatus);
      this.connectedAsEmail.set(status.connectedAsEmail);
      if (status.connectionStatus === 'active') {
        await Promise.all([this.loadTeams(), this.loadConfiguration()]);
      }
    } catch (error) {
      this.errorMessage.set(parseApiError(error).message);
    }
  }

  async loadTeams(): Promise<void> {
    this.teamsLoading.set(true);
    try {
      this.teams.set(await this.api.getTeams(this.hostContext().organizationId));
    } catch (error) {
      this.handleApiError(error);
    } finally {
      this.teamsLoading.set(false);
    }
  }

  async selectTeam(teamId: string): Promise<void> {
    this.selectedTeamId.set(teamId);
    this.selectedChannelId.set(null);
    this.channels.set([]);
    await this.loadChannels(teamId);
  }

  async loadChannels(teamId: string): Promise<void> {
    this.channelsLoading.set(true);
    try {
      this.channels.set(await this.api.getChannels(teamId, this.hostContext().organizationId));
    } catch (error) {
      this.handleApiError(error);
    } finally {
      this.channelsLoading.set(false);
    }
  }

  selectChannel(channelId: string): void {
    this.selectedChannelId.set(channelId);
  }

  async loadConfiguration(): Promise<void> {
    this.configurationLoadState.set('loading');
    try {
      const configuration = await this.api.getConfiguration(this.hostContext());
      this.savedConfiguration.set(configuration);
      this.configurationLoadState.set(configuration ? 'loaded' : 'none');
      this.viewMode.set(configuration ? 'send' : 'configure');
    } catch (error) {
      this.configurationLoadState.set('error');
      this.handleApiError(error);
    }
  }

  async saveConfiguration(): Promise<void> {
    const teamId = this.selectedTeamId();
    const channelId = this.selectedChannelId();
    if (!teamId || !channelId) {
      return;
    }

    this.saveInProgress.set(true);
    this.errorMessage.set(null);
    try {
      const configuration = await this.api.saveConfiguration({
        ...this.hostContext(),
        teamId,
        channelId,
      });
      this.savedConfiguration.set(configuration);
      this.configurationLoadState.set('loaded');
      this.orgConnectionStatus.set('active');
      this.viewMode.set('send');
    } catch (error) {
      this.handleApiError(error);
    } finally {
      this.saveInProgress.set(false);
    }
  }

  editConfiguration(): void {
    this.viewMode.set('configure');
    void this.prefillFromSavedConfiguration();
  }

  private async prefillFromSavedConfiguration(): Promise<void> {
    const configuration = this.savedConfiguration();
    if (!configuration) {
      return;
    }

    this.selectedTeamId.set(configuration.teamId);
    await this.loadChannels(configuration.teamId);
    // Let the newly loaded <option> elements actually render before selecting one — setting
    // selectedChannelId in the same tick loadChannels() resolves can run before the channel
    // <select>'s options exist yet, so the native element falls back to its first option.
    await new Promise((resolve) => setTimeout(resolve, 0));
    this.selectedChannelId.set(configuration.channelId);
  }

  async sendMessage(): Promise<void> {
    if (!this.canSendMessage()) {
      return;
    }

    this.sendInProgress.set(true);
    this.errorMessage.set(null);
    try {
      const response = await this.api.sendMessage(this.hostContext(), this.messageContent().trim());
      this.lastSentMessage.set(response);
      this.messageContent.set('');
    } catch (error) {
      this.handleApiError(error);
    } finally {
      this.sendInProgress.set(false);
    }
  }

  markNeedsReconnect(): void {
    this.orgConnectionStatus.set('needsReconnect');
  }

  private handleApiError(error: unknown): void {
    const apiError = parseApiError(error);
    if (apiError.code !== 'reauthentication_required') {
      this.errorMessage.set(apiError.message);
    }
  }
}

function readHostContextFromUrl(): HostContext {
  const params = new URLSearchParams(window.location.search);
  return {
    organizationId: params.get('organizationId') ?? DEFAULT_HOST_CONTEXT.organizationId,
    projectId: params.get('projectId') ?? DEFAULT_HOST_CONTEXT.projectId,
    applicationId: params.get('applicationId') ?? DEFAULT_HOST_CONTEXT.applicationId,
  };
}
