export type ConnectionStatus = 'active' | 'needsReconnect';

export interface TeamsConfigurationDto {
  organizationId: string;
  projectId: string;
  applicationId: string;
  tenantId: string;
  userObjectId: string;
  teamId: string;
  teamName: string | null;
  channelId: string;
  channelName: string | null;
  connectionStatus: ConnectionStatus;
  connectionFailureCode: string | null;
  connectionFailureDetectedAtUtc: string | null;
  connectionAlertedAtUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface SaveTeamsConfigurationRequest {
  organizationId: string;
  projectId: string;
  applicationId: string;
  teamId: string;
  channelId: string;
}
