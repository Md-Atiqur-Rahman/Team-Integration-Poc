import { TestBed } from '@angular/core/testing';
import { DemoAuthFlowStore } from './demo-auth-flow.store';

describe('DemoAuthFlowStore', () => {
  let store: DemoAuthFlowStore;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    store = TestBed.inject(DemoAuthFlowStore);
  });

  afterEach(() => vi.unstubAllGlobals());

  it('starts on the login step with no result', () => {
    expect(store.step()).toBe('login');
    expect(store.loginResult()).toBeNull();
  });

  it('onLoginSuccess stores the result and advances to chooseOrg', () => {
    store.onLoginSuccess({
      displayName: 'Himel',
      organizationId: 'IGB2B',
      projectId: 'Ecohub',
      applicationId: 'EcohubApp',
    });

    expect(store.step()).toBe('chooseOrg');
    expect(store.loginResult()?.organizationId).toBe('IGB2B');
  });

  it('selectOrganization advances to chooseProject, and goBack returns to chooseOrg', () => {
    store.selectOrganization();
    expect(store.step()).toBe('chooseProject');

    store.goBack();
    expect(store.step()).toBe('chooseOrg');
  });

  it('selectProject navigates to the dashboard URL with the resolved host context', () => {
    const assign = vi.fn();
    vi.stubGlobal('location', { ...window.location, assign, origin: 'https://localhost:4200', pathname: '/' });

    store.onLoginSuccess({
      displayName: 'Himel',
      organizationId: 'IGB2B',
      projectId: 'Ecohub',
      applicationId: 'EcohubApp',
    });

    store.selectProject();

    expect(assign).toHaveBeenCalledTimes(1);
    const url = assign.mock.calls[0][0] as string;
    expect(url).toContain('organizationId=IGB2B');
    expect(url).toContain('projectId=Ecohub');
    expect(url).toContain('applicationId=EcohubApp');
    expect(url).toContain('displayName=Himel');
  });

  it('selectProject does nothing if there is no login result yet', () => {
    const assign = vi.fn();
    vi.stubGlobal('location', { ...window.location, assign });

    store.selectProject();

    expect(assign).not.toHaveBeenCalled();
  });
});
