export interface DemoLoginRequest {
  email: string;
  password: string;
}

export interface DemoLoginResponse {
  displayName: string;
  organizationId: string;
  projectId: string;
  applicationId: string;
}
