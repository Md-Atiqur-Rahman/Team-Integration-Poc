import { TestBed } from '@angular/core/testing';
import { ChooseOrganization } from './choose-organization';
import { DemoAuthFlowStore } from './demo-auth-flow.store';

describe('ChooseOrganization', () => {
  let flow: DemoAuthFlowStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChooseOrganization],
    }).compileComponents();

    flow = TestBed.inject(DemoAuthFlowStore);
    flow.onLoginSuccess({
      displayName: 'Himel',
      organizationId: 'IGB2B',
      projectId: 'Ecohub',
      applicationId: 'EcohubApp',
    });
  });

  it('renders the organization name and initials from the login result', () => {
    const fixture = TestBed.createComponent(ChooseOrganization);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('IGB2B');
    expect(text).toContain('IG');
  });

  it('advances the flow to chooseProject when the organization row is clicked', () => {
    const fixture = TestBed.createComponent(ChooseOrganization);
    fixture.detectChanges();

    fixture.nativeElement.querySelector('button')!.click();

    expect(flow.step()).toBe('chooseProject');
  });
});
