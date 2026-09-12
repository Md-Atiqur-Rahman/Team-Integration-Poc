import { Component, computed, inject } from '@angular/core';
import { AutomHeader } from '../../shared/autom-header/autom-header';
import { DemoAuthFlowStore } from './demo-auth-flow.store';

@Component({
  selector: 'app-choose-organization',
  imports: [AutomHeader],
  templateUrl: './choose-organization.html',
  styleUrl: './choose-organization.css',
})
export class ChooseOrganization {
  protected readonly flow = inject(DemoAuthFlowStore);

  protected readonly organizationId = computed(() => this.flow.loginResult()?.organizationId ?? '');

  protected readonly initials = computed(() => this.organizationId().slice(0, 2).toUpperCase());

  protected select(): void {
    this.flow.selectOrganization();
  }
}
