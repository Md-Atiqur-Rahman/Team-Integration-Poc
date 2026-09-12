import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ChannelConfiguration } from './channel-configuration';
import { TeamsDashboardStore } from './teams-dashboard.store';

describe('ChannelConfiguration', () => {
  let store: TeamsDashboardStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChannelConfiguration],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    store = TestBed.inject(TeamsDashboardStore);
  });

  function saveButton(fixture: { nativeElement: HTMLElement }): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button')!;
  }

  it('disables Save Configuration until both a team and a channel are selected', () => {
    const fixture = TestBed.createComponent(ChannelConfiguration);
    fixture.detectChanges();

    expect(saveButton(fixture).disabled).toBe(true);

    store.selectedTeamId.set('t1');
    fixture.detectChanges();
    expect(saveButton(fixture).disabled).toBe(true);

    store.selectedChannelId.set('c1');
    fixture.detectChanges();
    expect(saveButton(fixture).disabled).toBe(false);
  });

  it('disables Save Configuration while a save is in progress', () => {
    const fixture = TestBed.createComponent(ChannelConfiguration);
    store.selectedTeamId.set('t1');
    store.selectedChannelId.set('c1');
    store.saveInProgress.set(true);
    fixture.detectChanges();

    expect(saveButton(fixture).disabled).toBe(true);
  });
});
