import { Component } from '@angular/core';
import { TeamsDashboard } from './features/teams-dashboard/teams-dashboard';

@Component({
  selector: 'app-root',
  imports: [TeamsDashboard],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {}
