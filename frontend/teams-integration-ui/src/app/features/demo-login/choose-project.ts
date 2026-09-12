import { Component, computed, inject, signal } from '@angular/core';
import { AutomHeader } from '../../shared/autom-header/autom-header';
import { DemoAuthFlowStore } from './demo-auth-flow.store';

@Component({
  selector: 'app-choose-project',
  imports: [AutomHeader],
  templateUrl: './choose-project.html',
  styleUrl: './choose-project.css',
})
export class ChooseProject {
  protected readonly flow = inject(DemoAuthFlowStore);

  protected readonly query = signal('');

  protected readonly projectId = computed(() => this.flow.loginResult()?.projectId ?? '');

  protected readonly isVisible = computed(() =>
    this.projectId().toLowerCase().includes(this.query().trim().toLowerCase()),
  );

  protected onQueryChange(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
  }

  protected select(): void {
    this.flow.selectProject();
  }

  protected back(): void {
    this.flow.goBack();
  }
}
