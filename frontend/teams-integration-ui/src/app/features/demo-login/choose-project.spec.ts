import { TestBed } from '@angular/core/testing';
import { ChooseProject } from './choose-project';
import { DemoAuthFlowStore } from './demo-auth-flow.store';

describe('ChooseProject', () => {
  let flow: DemoAuthFlowStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChooseProject],
    }).compileComponents();

    flow = TestBed.inject(DemoAuthFlowStore);
    flow.onLoginSuccess({
      displayName: 'Himel',
      organizationId: 'IGB2B',
      projectId: 'Ecohub',
      applicationId: 'EcohubApp',
    });
  });

  afterEach(() => vi.unstubAllGlobals());

  it('renders the project from the login result', () => {
    const fixture = TestBed.createComponent(ChooseProject);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Ecohub');
  });

  it('hides the project row when the search query does not match', () => {
    const fixture = TestBed.createComponent(ChooseProject);
    fixture.detectChanges();

    const search = fixture.nativeElement.querySelector('input')!;
    search.value = 'nomatch';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.project-row')).toBeNull();
  });

  it('triggers the final navigation when the project row is clicked', () => {
    const assign = vi.fn();
    vi.stubGlobal('location', { ...window.location, assign, origin: 'https://localhost:4200', pathname: '/' });

    const fixture = TestBed.createComponent(ChooseProject);
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.project-row')!.click();

    expect(assign).toHaveBeenCalledTimes(1);
    expect(assign.mock.calls[0][0]).toContain('projectId=Ecohub');
  });
});
