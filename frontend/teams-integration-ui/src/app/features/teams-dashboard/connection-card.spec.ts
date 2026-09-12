import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ConnectionCard } from './connection-card';
import { TeamsDashboardStore } from './teams-dashboard.store';

describe('ConnectionCard', () => {
  let store: TeamsDashboardStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConnectionCard],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    store = TestBed.inject(TeamsDashboardStore);
  });

  it('shows Not Connected and a Connect button when signed out', () => {
    store.session.set({ isAuthenticated: false, isTeamsConnected: false, displayName: null });
    const fixture = TestBed.createComponent(ConnectionCard);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Not Connected');
    expect(fixture.nativeElement.querySelector('button')?.textContent).toContain(
      'Connect Microsoft Teams',
    );
  });

  it('shows the persistent reconnect banner and action when needsReconnect is set', () => {
    store.session.set({ isAuthenticated: true, isTeamsConnected: true, displayName: 'Test User' });
    store.needsReconnect.set(true);
    const fixture = TestBed.createComponent(ConnectionCard);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('needs reconnecting');
    expect(fixture.nativeElement.querySelector('button')?.textContent).toContain('Reconnect');
  });

  it('shows an Edit link when connected and viewing Send Message, and it switches to configure', () => {
    store.session.set({ isAuthenticated: true, isTeamsConnected: true, displayName: 'Test User' });
    store.viewMode.set('send');
    const fixture = TestBed.createComponent(ConnectionCard);
    fixture.detectChanges();

    const editButton = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (button) => (button as HTMLButtonElement).textContent?.trim() === 'Edit',
    ) as HTMLButtonElement | undefined;
    expect(editButton).toBeTruthy();

    editButton!.click();
    expect(store.viewMode()).toBe('configure');
  });

  it('does not show an Edit link while the configuration form is already showing', () => {
    store.session.set({ isAuthenticated: true, isTeamsConnected: true, displayName: 'Test User' });
    store.viewMode.set('configure');
    const fixture = TestBed.createComponent(ConnectionCard);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).not.toContain('Edit');
  });

  it('Connect navigates the full browser window rather than making an XHR', () => {
    store.session.set({ isAuthenticated: false, isTeamsConnected: false, displayName: null });
    const fixture = TestBed.createComponent(ConnectionCard);
    fixture.detectChanges();

    const assign = vi.fn();
    vi.stubGlobal('location', { ...window.location, assign });

    fixture.nativeElement.querySelector('button')!.click();

    expect(assign).toHaveBeenCalledTimes(1);
    expect(assign.mock.calls[0][0]).toContain('/api/auth/connect');

    vi.unstubAllGlobals();
  });
});
