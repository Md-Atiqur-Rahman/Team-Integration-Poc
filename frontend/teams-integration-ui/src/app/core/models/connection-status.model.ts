export type OrganizationConnectionStatus = 'active' | 'needsReconnect' | 'none';

export interface ConnectionStatusResponse {
  isConnected: boolean;
  connectionStatus: OrganizationConnectionStatus;
  connectedAsEmail: string | null;
}
