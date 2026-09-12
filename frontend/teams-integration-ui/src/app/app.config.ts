import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { reauthenticationInterceptor } from './core/interceptors/reauthentication.interceptor';
import { withCredentialsInterceptor } from './core/interceptors/with-credentials.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([withCredentialsInterceptor, reauthenticationInterceptor])),
  ],
};
