import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../../core/config';
import { SendMessage } from './send-message';
import { TeamsDashboardStore } from './teams-dashboard.store';

describe('SendMessage', () => {
  let store: TeamsDashboardStore;
  let httpMock: HttpTestingController;

  const activeConfiguration = {
    organizationId: 'demo-org',
    projectId: 'demo-project',
    applicationId: 'demo-application',
    tenantId: 'tenant-1',
    userObjectId: 'user-1',
    teamId: 't1',
    teamName: 'Team One',
    channelId: 'c1',
    channelName: 'General',
    connectionStatus: 'active' as const,
    connectionFailureCode: null,
    connectionFailureDetectedAtUtc: null,
    connectionAlertedAtUtc: null,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SendMessage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    store = TestBed.inject(TeamsDashboardStore);
    httpMock = TestBed.inject(HttpTestingController);
    store.orgConnectionStatus.set('active');
    store.connectedAsEmail.set('connector@example.com');
  });

  function saveButton(fixture: { nativeElement: HTMLElement }): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button')!;
  }

  function textarea(fixture: { nativeElement: HTMLElement }): HTMLTextAreaElement {
    return fixture.nativeElement.querySelector('textarea')!;
  }

  it('disables Send Message when there is no active configuration', () => {
    store.messageContent.set('Hello');
    const fixture = TestBed.createComponent(SendMessage);
    fixture.detectChanges();

    expect(saveButton(fixture).disabled).toBe(true);
  });

  it('disables Send Message when the message is blank', () => {
    store.savedConfiguration.set(activeConfiguration);
    const fixture = TestBed.createComponent(SendMessage);
    fixture.detectChanges();

    expect(saveButton(fixture).disabled).toBe(true);

    textarea(fixture).value = '   ';
    textarea(fixture).dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(saveButton(fixture).disabled).toBe(true);
  });

  it('enables Send Message once a configuration is active and the message is non-blank', () => {
    store.savedConfiguration.set(activeConfiguration);
    const fixture = TestBed.createComponent(SendMessage);
    fixture.detectChanges();

    textarea(fixture).value = 'Hello team';
    textarea(fixture).dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(saveButton(fixture).disabled).toBe(false);
  });

  it('clears the textarea and shows a confirmation link after a successful send', async () => {
    store.savedConfiguration.set(activeConfiguration);
    const fixture = TestBed.createComponent(SendMessage);
    fixture.detectChanges();

    textarea(fixture).value = 'Hello team';
    textarea(fixture).dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const sendPromise = store.sendMessage();
    httpMock
      .expectOne((req) => req.url.startsWith(`${API_BASE_URL}/api/teams/messages`))
      .flush({ id: 'msg-1', createdDateTime: '2026-01-01T00:00:00Z', webUrl: 'https://teams.example/msg-1' });
    await sendPromise;
    fixture.detectChanges();

    expect(store.messageContent()).toBe('');
    const link = fixture.nativeElement.querySelector('a');
    expect(link?.getAttribute('href')).toBe('https://teams.example/msg-1');
  });
});
