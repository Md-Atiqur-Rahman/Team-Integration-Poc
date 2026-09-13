import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API_BASE_URL, HostContext } from '../config';
import { ApiError } from '../models/api-error.model';
import { ConnectionStatusResponse } from '../models/connection-status.model';
import { SendMessageResponse } from '../models/send-message.model';
import { ChannelItem, TeamItem } from '../models/team.model';
import { SaveTeamsConfigurationRequest, TeamsConfigurationDto } from '../models/teams-configuration.model';

interface ItemsResponse<T> {
  items: T[];
}

@Injectable({ providedIn: 'root' })
export class TeamsApiClient {
  private readonly http = inject(HttpClient);

  connectUrl(returnUrl: string): string {
    return `${API_BASE_URL}/api/auth/connect?returnUrl=${encodeURIComponent(returnUrl)}`;
  }

  getConnectionStatus(organizationId: string): Promise<ConnectionStatusResponse> {
    return firstValueFrom(
      this.http.get<ConnectionStatusResponse>(
        `${API_BASE_URL}/api/auth/connection-status?organizationId=${encodeURIComponent(organizationId)}`,
      ),
    );
  }

  async getTeams(organizationId: string): Promise<TeamItem[]> {
    const response = await firstValueFrom(
      this.http.get<ItemsResponse<TeamItem>>(
        `${API_BASE_URL}/api/teams?organizationId=${encodeURIComponent(organizationId)}`,
      ),
    );
    return response.items;
  }

  async getChannels(teamId: string, organizationId: string): Promise<ChannelItem[]> {
    const response = await firstValueFrom(
      this.http.get<ItemsResponse<ChannelItem>>(
        `${API_BASE_URL}/api/teams/${encodeURIComponent(teamId)}/channels?organizationId=${encodeURIComponent(organizationId)}`,
      ),
    );
    return response.items;
  }

  async getConfiguration(hostContext: HostContext): Promise<TeamsConfigurationDto | null> {
    const params = new URLSearchParams(Object.entries(hostContext));
    try {
      return await firstValueFrom(
        this.http.get<TeamsConfigurationDto>(
          `${API_BASE_URL}/api/teams/configuration?${params.toString()}`,
        ),
      );
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        return null;
      }
      throw error;
    }
  }

  saveConfiguration(request: SaveTeamsConfigurationRequest): Promise<TeamsConfigurationDto> {
    return firstValueFrom(
      this.http.put<TeamsConfigurationDto>(`${API_BASE_URL}/api/teams/configuration`, request),
    );
  }

  sendMessage(hostContext: HostContext, content: string): Promise<SendMessageResponse> {
    const params = new URLSearchParams(Object.entries(hostContext));
    return firstValueFrom(
      this.http.post<SendMessageResponse>(
        `${API_BASE_URL}/api/teams/messages?${params.toString()}`,
        { content },
      ),
    );
  }
}

export function parseApiError(error: unknown): ApiError {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as { code?: string; message?: string; detail?: string; title?: string } | null;
    return {
      code: body?.code ?? null,
      message: body?.message ?? body?.detail ?? body?.title ?? 'The request could not be completed.',
    };
  }

  return { code: null, message: 'Unable to reach the service. Check your connection and retry.' };
}
