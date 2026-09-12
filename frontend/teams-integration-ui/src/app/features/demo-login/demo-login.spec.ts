import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../../core/config';
import { DemoAuthFlowStore } from './demo-auth-flow.store';
import { DemoLogin } from './demo-login';

describe('DemoLogin', () => {
  let httpMock: HttpTestingController;
  let flow: DemoAuthFlowStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DemoLogin],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    flow = TestBed.inject(DemoAuthFlowStore);
  });

  afterEach(() => httpMock.verify());

  function loginButton(fixture: { nativeElement: HTMLElement }): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button')!;
  }

  function setInput(fixture: { nativeElement: HTMLElement }, id: string, value: string): void {
    const input = fixture.nativeElement.querySelector<HTMLInputElement>(`#${id}`)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  it('disables Sign in until both email and password are filled', () => {
    const fixture = TestBed.createComponent(DemoLogin);
    fixture.detectChanges();

    expect(loginButton(fixture).disabled).toBe(true);

    setInput(fixture, 'email', 'himel@demo.autom');
    fixture.detectChanges();
    expect(loginButton(fixture).disabled).toBe(true);

    setInput(fixture, 'password', 'Demo@123');
    fixture.detectChanges();
    expect(loginButton(fixture).disabled).toBe(false);
  });

  it('shows an error and stays on the login step when login fails', async () => {
    const fixture = TestBed.createComponent(DemoLogin);
    fixture.detectChanges();
    setInput(fixture, 'email', 'himel@demo.autom');
    setInput(fixture, 'password', 'wrong-password');
    fixture.detectChanges();

    fixture.nativeElement.querySelector('form')!.dispatchEvent(new Event('submit'));

    httpMock
      .expectOne(`${API_BASE_URL}/api/demo/login`)
      .flush({ code: 'invalid_credentials', message: 'Invalid email or password.' }, { status: 401, statusText: 'Unauthorized' });
    await Promise.resolve();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Invalid email or password.');
    expect(flow.step()).toBe('login');
  });

  it('advances the flow to chooseOrg with the login result on success', async () => {
    const fixture = TestBed.createComponent(DemoLogin);
    fixture.detectChanges();
    setInput(fixture, 'email', 'himel@demo.autom');
    setInput(fixture, 'password', 'Demo@123');
    fixture.detectChanges();

    fixture.nativeElement.querySelector('form')!.dispatchEvent(new Event('submit'));

    httpMock.expectOne(`${API_BASE_URL}/api/demo/login`).flush({
      displayName: 'Himel',
      organizationId: 'IGB2B',
      projectId: 'Ecohub',
      applicationId: 'EcohubApp',
    });
    await Promise.resolve();

    expect(flow.step()).toBe('chooseOrg');
    expect(flow.loginResult()?.displayName).toBe('Himel');
  });
});
