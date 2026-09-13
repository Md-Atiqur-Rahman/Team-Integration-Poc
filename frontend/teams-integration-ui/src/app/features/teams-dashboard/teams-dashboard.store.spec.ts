import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../../core/config';
import { reauthenticationInterceptor } from '../../core/interceptors/reauthentication.interceptor';
import { TeamsDashboardStore } from './teams-dashboard.store';

describe('TeamsDashboardStore', () => {
  let store: TeamsDashboardStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([reauthenticationInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    store = TestBed.inject(TeamsDashboardStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  /** Lets pending microtasks (chained promise continuations) settle before the next httpMock expectation. */
  async function flushMicrotasks(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 0));
  }

  it('loading Teams populates the store', async () => {
    const loadPromise = store.loadTeams();

    httpMock
      .expectOne((req) => req.url.startsWith(`${API_BASE_URL}/api/teams?`))
      .flush({
        items: [{ id: 't1', displayName: 'Team One', description: null, isArchived: false }],
      });
    await loadPromise;

    expect(store.teams()).toEqual([
      { id: 't1', displayName: 'Team One', description: null, isArchived: false },
    ]);
    expect(store.teamsLoading()).toBe(false);
  });

  it('selecting a team clears the previously selected channel', async () => {
    store.selectedChannelId.set('old-channel');

    const selectPromise = store.selectTeam('team-2');

    httpMock
      .expectOne((req) => req.url.startsWith(`${API_BASE_URL}/api/teams/team-2/channels?`))
      .flush({ items: [] });
    await selectPromise;

    expect(store.selectedTeamId()).toBe('team-2');
    expect(store.selectedChannelId()).toBeNull();
  });

  it('save success updates savedConfiguration and clears the reconnect state', async () => {
    store.selectedTeamId.set('t1');
    store.selectedChannelId.set('c1');
    store.orgConnectionStatus.set('needsReconnect');

    const savePromise = store.saveConfiguration();

    const request = httpMock.expectOne(`${API_BASE_URL}/api/teams/configuration`);
    expect(request.request.method).toBe('PUT');
    request.flush({
      organizationId: 'demo-org',
      projectId: 'demo-project',
      applicationId: 'demo-application',
      tenantId: 'tenant-1',
      userObjectId: 'user-1',
      teamId: 't1',
      teamName: 'Team One',
      channelId: 'c1',
      channelName: 'General',
      connectionStatus: 'active',
      connectionFailureCode: null,
      connectionFailureDetectedAtUtc: null,
      connectionAlertedAtUtc: null,
      createdAtUtc: '2026-01-01T00:00:00Z',
      updatedAtUtc: '2026-01-01T00:00:00Z',
    });
    await savePromise;

    expect(store.configurationLoadState()).toBe('loaded');
    expect(store.savedConfiguration()?.teamName).toBe('Team One');
    expect(store.orgConnectionStatus()).toBe('active');
    expect(store.viewMode()).toBe('send');
  });

  it('loadConfiguration sets viewMode to configure when none is saved, send when one exists', async () => {
    const noneLoadPromise = store.loadConfiguration();
    httpMock
      .expectOne((req) => req.url.startsWith(`${API_BASE_URL}/api/teams/configuration`))
      .flush(
        { code: 'configuration_not_found', message: 'No configuration.' },
        { status: 404, statusText: 'Not Found' },
      );
    await noneLoadPromise;
    expect(store.viewMode()).toBe('configure');

    const loadedPromise = store.loadConfiguration();
    httpMock.expectOne((req) => req.url.startsWith(`${API_BASE_URL}/api/teams/configuration`)).flush({
      organizationId: 'demo-org',
      projectId: 'demo-project',
      applicationId: 'demo-application',
      tenantId: 'tenant-1',
      userObjectId: 'user-1',
      teamId: 't1',
      teamName: 'Team One',
      channelId: 'c1',
      channelName: 'General',
      connectionStatus: 'active',
      connectionFailureCode: null,
      connectionFailureDetectedAtUtc: null,
      connectionAlertedAtUtc: null,
      createdAtUtc: '2026-01-01T00:00:00Z',
      updatedAtUtc: '2026-01-01T00:00:00Z',
    });
    await loadedPromise;
    expect(store.viewMode()).toBe('send');
  });

  it('editConfiguration switches to configure and pre-fills the saved team/channel', async () => {
    store.savedConfiguration.set({
      organizationId: 'demo-org',
      projectId: 'demo-project',
      applicationId: 'demo-application',
      tenantId: 'tenant-1',
      userObjectId: 'user-1',
      teamId: 't1',
      teamName: 'Team One',
      channelId: 'c1',
      channelName: 'General',
      connectionStatus: 'active',
      connectionFailureCode: null,
      connectionFailureDetectedAtUtc: null,
      connectionAlertedAtUtc: null,
      createdAtUtc: '2026-01-01T00:00:00Z',
      updatedAtUtc: '2026-01-01T00:00:00Z',
    });

    store.editConfiguration();
    expect(store.viewMode()).toBe('configure');
    expect(store.selectedTeamId()).toBe('t1');

    httpMock
      .expectOne((req) => req.url.startsWith(`${API_BASE_URL}/api/teams/t1/channels?`))
      .flush({
        items: [{ id: 'c1', displayName: 'General', description: null, membershipType: null, isArchived: false }],
      });
    // Two ticks: one for loadChannels()'s own resolution, one for the store's deliberate
    // extra setTimeout(0) that lets the <option> elements render before selecting one.
    await flushMicrotasks();
    await flushMicrotasks();

    expect(store.selectedChannelId()).toBe('c1');
  });

  it('never writes to localStorage or sessionStorage during a full connected session load', async () => {
    localStorage.clear();
    sessionStorage.clear();

    const initPromise = store.initialize();
    httpMock
      .expectOne((req) => req.url.startsWith(`${API_BASE_URL}/api/auth/connection-status`))
      .flush({
        isConnected: true,
        connectionStatus: 'active',
        connectedAsEmail: 'connector@example.com',
      });
    await flushMicrotasks();

    httpMock.expectOne((req) => req.url.startsWith(`${API_BASE_URL}/api/teams?`)).flush({ items: [] });
    httpMock
      .expectOne((req) => req.url.startsWith(`${API_BASE_URL}/api/teams/configuration`))
      .flush(
        { code: 'configuration_not_found', message: 'No configuration.' },
        { status: 404, statusText: 'Not Found' },
      );
    await initPromise;

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });

  it('a connected organization loads teams and configuration without any OAuth of its own', async () => {
    const initPromise = store.initialize();
    httpMock
      .expectOne((req) => req.url.startsWith(`${API_BASE_URL}/api/auth/connection-status`))
      .flush({
        isConnected: true,
        connectionStatus: 'active',
        connectedAsEmail: 'Atiqur.Himel@selisegroup.com',
      });
    await flushMicrotasks();

    httpMock.expectOne((req) => req.url.startsWith(`${API_BASE_URL}/api/teams?`)).flush({ items: [] });
    httpMock
      .expectOne((req) => req.url.startsWith(`${API_BASE_URL}/api/teams/configuration`))
      .flush(
        { code: 'configuration_not_found', message: 'No configuration.' },
        { status: 404, statusText: 'Not Found' },
      );
    await initPromise;

    expect(store.orgConnectionStatus()).toBe('active');
    expect(store.connectedAsEmail()).toBe('Atiqur.Himel@selisegroup.com');
    expect(store.isConnected()).toBe(true);
  });

  it('an organization with no connection does not attempt to load teams or configuration', async () => {
    const initPromise = store.initialize();
    httpMock
      .expectOne((req) => req.url.startsWith(`${API_BASE_URL}/api/auth/connection-status`))
      .flush({ isConnected: false, connectionStatus: 'none', connectedAsEmail: null });
    await initPromise;

    expect(store.orgConnectionStatus()).toBe('none');
    expect(store.isConnected()).toBe(false);
  });

  it('a 401 reauthentication_required response sets the org connection to needsReconnect instead of a generic error', async () => {
    const loadPromise = store.loadTeams();

    httpMock
      .expectOne((req) => req.url.startsWith(`${API_BASE_URL}/api/teams?`))
      .flush(
        { code: 'reauthentication_required', message: 'Authentication required' },
        { status: 401, statusText: 'Unauthorized' },
      );
    await loadPromise;

    expect(store.orgConnectionStatus()).toBe('needsReconnect');
    expect(store.errorMessage()).toBeNull();
  });
});
