import { Injectable, signal } from '@angular/core';
import { DemoLoginResponse } from '../../core/models/demo-auth.model';

export type DemoAuthStep = 'login' | 'chooseOrg' | 'chooseProject';

/** Drives the demo-only Login -> Choose Organization -> Choose Project flow. */
@Injectable({ providedIn: 'root' })
export class DemoAuthFlowStore {
  readonly step = signal<DemoAuthStep>('login');
  readonly loginResult = signal<DemoLoginResponse | null>(null);

  onLoginSuccess(result: DemoLoginResponse): void {
    this.loginResult.set(result);
    this.step.set('chooseOrg');
  }

  selectOrganization(): void {
    this.step.set('chooseProject');
  }

  goBack(): void {
    this.step.set('chooseOrg');
  }

  selectProject(): void {
    const result = this.loginResult();
    if (!result) {
      return;
    }

    const params = new URLSearchParams({
      organizationId: result.organizationId,
      projectId: result.projectId,
      applicationId: result.applicationId,
      displayName: result.displayName,
    });
    window.location.assign(`${window.location.origin}${window.location.pathname}?${params.toString()}`);
  }
}
