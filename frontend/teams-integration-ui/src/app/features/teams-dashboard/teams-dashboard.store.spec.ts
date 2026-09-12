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

    httpMock.expectOne(`${API_BASE_URL}/api/teams`).flush({
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

    httpMock.expectOne(`${API_BASE_URL}/api/teams/team-2/channels`).flush({ items: [] });
    await selectPromise;

    expect(store.selectedTeamId()).toBe('team-2');
    expect(store.selectedChannelId()).toBeNull();
  });

  it('save success updates savedConfiguration and clears needsReconnect', async () => {
    store.selectedTeamId.set('t1');
    store.selectedChannelId.set('c1');
    store.needsReconnect.set(true);

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
    expect(store.needsReconnect()).toBe(false);
  });

  it('never writes to localStorage or sessionStorage during a full connected session load', async () => {
    localStorage.clear();
    sessionStorage.clear();

    const initPromise = store.initialize();
    httpMock.expectOne(`${API_BASE_URL}/api/auth/session`).flush({
      isAuthenticated: true,
      isTeamsConnected: true,
      displayName: 'Test User',
    });
    await flushMicrotasks();

    httpMock.expectOne(`${API_BASE_URL}/api/teams`).flush({ items: [] });
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

  it('a 401 reauthentication_required response sets needsReconnect instead of a generic error', async () => {
    const loadPromise = store.loadTeams();

    httpMock
      .expectOne(`${API_BASE_URL}/api/teams`)
      .flush(
        { code: 'reauthentication_required', message: 'Authentication required' },
        { status: 401, statusText: 'Unauthorized' },
      );
    await loadPromise;

    expect(store.needsReconnect()).toBe(true);
    expect(store.errorMessage()).toBeNull();
  });
});
