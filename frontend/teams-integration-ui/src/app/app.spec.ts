import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  afterEach(() => vi.unstubAllGlobals());

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('shows the demo login screen when no host context is in the URL', () => {
    vi.stubGlobal('location', { ...window.location, search: '' });

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('app-demo-login')).not.toBeNull();
    expect(compiled.querySelector('app-teams-dashboard')).toBeNull();
  });

  it('shows the dashboard when host context is present in the URL', () => {
    vi.stubGlobal('location', {
      ...window.location,
      search: '?organizationId=demo-org&projectId=demo-project&applicationId=demo-application',
    });

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('app-teams-dashboard')).not.toBeNull();
    expect(compiled.querySelector('app-demo-login')).toBeNull();
  });
});
