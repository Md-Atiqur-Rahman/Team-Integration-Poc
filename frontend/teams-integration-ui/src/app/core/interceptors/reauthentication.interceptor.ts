import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { TeamsDashboardStore } from '../../features/teams-dashboard/teams-dashboard.store';

export const reauthenticationInterceptor: HttpInterceptorFn = (req, next) => {
  const store = inject(TeamsDashboardStore);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        (error.error as { code?: string } | null)?.code === 'reauthentication_required'
      ) {
        store.markNeedsReconnect();
      }

      return throwError(() => error);
    }),
  );
};
