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

/** True once the demo login has placed host context in the URL (or it was linked/bookmarked directly). */
export function hasHostContextInUrl(): boolean {
  return new URLSearchParams(window.location.search).has('organizationId');
}
