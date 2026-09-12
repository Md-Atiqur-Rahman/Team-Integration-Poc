import { Component, inject } from '@angular/core';
import { hasHostContextInUrl } from './core/config';
import { ChooseOrganization } from './features/demo-login/choose-organization';
import { ChooseProject } from './features/demo-login/choose-project';
import { DemoAuthFlowStore } from './features/demo-login/demo-auth-flow.store';
import { DemoLogin } from './features/demo-login/demo-login';
import { TeamsDashboard } from './features/teams-dashboard/teams-dashboard';

@Component({
  selector: 'app-root',
  imports: [TeamsDashboard, DemoLogin, ChooseOrganization, ChooseProject],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly showDashboard = hasHostContextInUrl();
  protected readonly flow = inject(DemoAuthFlowStore);
}
