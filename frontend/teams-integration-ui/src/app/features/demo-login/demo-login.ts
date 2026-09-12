import { Component, computed, inject, signal } from '@angular/core';
import { parseApiError } from '../../core/api-client/teams-api.client';
import { DemoAuthClient } from '../../core/api-client/demo-auth.client';
import { AutomHeader } from '../../shared/autom-header/autom-header';
import { DemoAuthFlowStore } from './demo-auth-flow.store';

/** Demo-only login screen — not part of the real Autom integration. */
@Component({
  selector: 'app-demo-login',
  imports: [AutomHeader],
  templateUrl: './demo-login.html',
  styleUrl: './demo-login.css',
})
export class DemoLogin {
  private readonly demoAuth = inject(DemoAuthClient);
  private readonly flow = inject(DemoAuthFlowStore);

  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly loginInProgress = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly canSubmit = computed(
    () => this.email().trim().length > 0 && this.password().length > 0 && !this.loginInProgress(),
  );

  protected async login(): Promise<void> {
    if (!this.canSubmit()) {
      return;
    }

    this.loginInProgress.set(true);
    this.errorMessage.set(null);
    try {
      const response = await this.demoAuth.login(this.email().trim(), this.password());
      this.flow.onLoginSuccess(response);
    } catch (error) {
      this.errorMessage.set(parseApiError(error).message);
      this.loginInProgress.set(false);
    }
  }
}
