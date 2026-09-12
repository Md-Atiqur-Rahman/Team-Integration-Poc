import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API_BASE_URL } from '../config';
import { DemoLoginResponse } from '../models/demo-auth.model';

/** Demo-only login client — not part of the real Autom integration. */
@Injectable({ providedIn: 'root' })
export class DemoAuthClient {
  private readonly http = inject(HttpClient);

  login(email: string, password: string): Promise<DemoLoginResponse> {
    return firstValueFrom(
      this.http.post<DemoLoginResponse>(`${API_BASE_URL}/api/demo/login`, { email, password }),
    );
  }
}
