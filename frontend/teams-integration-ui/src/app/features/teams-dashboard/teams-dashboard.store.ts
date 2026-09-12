import { Injectable, computed, inject, signal } from '@angular/core';
import { TeamsApiClient, parseApiError } from '../../core/api-client/teams-api.client';
import { DEFAULT_HOST_CONTEXT, HostContext } from '../../core/config';
import { SessionStatus } from '../../core/models/session-status.model';
import { ChannelItem, TeamItem } from '../../core/models/team.model';
import { TeamsConfigurationDto } from '../../core/models/teams-configuration.model';
import { SendMessageResponse } from '../../core/models/send-message.model';

type ConfigurationLoadState = 'loading' | 'loaded' | 'none' | 'error';

@Injectable({ providedIn: 'root' })
export class TeamsDashboardStore {
  private readonly api = inject(TeamsApiClient);

  readonly hostContext = signal<HostContext>(readHostContextFromUrl());
  readonly session = signal<SessionStatus | 'loading'>('loading');

  readonly teams = signal<TeamItem[]>([]);
  readonly teamsLoading = signal(false);

  readonly channels = signal<ChannelItem[]>([]);
  readonly channelsLoading = signal(false);

  readonly selectedTeamId = signal<string | null>(null);
  readonly selectedChannelId = signal<string | null>(null);

  readonly configurationLoadState = signal<ConfigurationLoadState>('loading');
  readonly savedConfiguration = signal<TeamsConfigurationDto | null>(null);

  readonly saveInProgress = signal(false);
  readonly needsReconnect = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly messageContent = signal('');
  readonly sendInProgress = signal(false);
  readonly lastSentMessage = signal<SendMessageResponse | null>(null);

  readonly isConnected = computed(() => {
    const session = this.session();
    return session !== 'loading' && session.isTeamsConnected;
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
    await this.loadSession();
  }

  async loadSession(): Promise<void> {
    this.session.set('loading');
    try {
      const session = await this.api.getSession();
      this.session.set(session);
      if (session.isTeamsConnected) {
        await Promise.all([this.loadTeams(), this.loadConfiguration()]);
      }
    } catch (error) {
      this.errorMessage.set(parseApiError(error).message);
    }
  }

  async loadTeams(): Promise<void> {
    this.teamsLoading.set(true);
    try {
      this.teams.set(await this.api.getTeams());
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
      this.channels.set(await this.api.getChannels(teamId));
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
      if (configuration?.connectionStatus === 'needsReconnect') {
        this.needsReconnect.set(true);
      }
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
      this.needsReconnect.set(false);
    } catch (error) {
      this.handleApiError(error);
    } finally {
      this.saveInProgress.set(false);
    }
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
    this.needsReconnect.set(true);
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
