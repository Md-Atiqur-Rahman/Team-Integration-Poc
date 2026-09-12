export const API_BASE_URL = 'https://localhost:7059';

export interface HostContext {
  organizationId: string;
  projectId: string;
  applicationId: string;
}

export const DEFAULT_HOST_CONTEXT: HostContext = {
  organizationId: 'demo-org',
  projectId: 'demo-project',
  applicationId: 'demo-application',
};
