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

  it('shows Not Connected and a Connect button when the organization has no connection', () => {
    store.orgConnectionStatus.set('none');
    const fixture = TestBed.createComponent(ConnectionCard);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Not Connected');
    expect(fixture.nativeElement.querySelector('button')?.textContent).toContain(
      'Connect Microsoft Teams',
    );
  });

  it('shows the persistent reconnect banner and action when the organization needs reconnecting', () => {
    store.orgConnectionStatus.set('needsReconnect');
    const fixture = TestBed.createComponent(ConnectionCard);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('needs reconnecting');
    expect(fixture.nativeElement.querySelector('button')?.textContent).toContain('Reconnect');
  });

  it('shows an Edit link when connected and viewing Send Message, and it switches to configure', () => {
    store.orgConnectionStatus.set('active');
    store.connectedAsEmail.set('connector@example.com');
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
    store.orgConnectionStatus.set('active');
    store.connectedAsEmail.set('connector@example.com');
    store.viewMode.set('configure');
    const fixture = TestBed.createComponent(ConnectionCard);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).not.toContain('Edit');
  });

  it('shows Connected as the org-shared account, not a personal session', () => {
    store.orgConnectionStatus.set('active');
    store.connectedAsEmail.set('Atiqur.Himel@selisegroup.com');
    const fixture = TestBed.createComponent(ConnectionCard);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Connected');
    expect(text).toContain('Atiqur.Himel@selisegroup.com');
  });

  it('Connect navigates the full browser window rather than making an XHR', () => {
    store.orgConnectionStatus.set('none');
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
