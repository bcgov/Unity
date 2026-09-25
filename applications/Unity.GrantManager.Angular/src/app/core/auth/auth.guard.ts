import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { catchError, map, of, tap } from 'rxjs';

import { AUTH_CONFIG } from './auth.config';
import { BootstrapService } from './bootstrap.service';
import { ShellService } from '../../shell/shell.service';

// Server-side [Authorize] enforcement still happens on the bootstrap/API endpoints
// themselves - this guard only decides what the SPA shows before those calls resolve,
// and where to send the browser on a 401 (see plan section 3: auth continuity).
// Also populates ShellService so ShellComponent renders with data already in hand,
// instead of fetching the same bootstrap payload a second time.
export const authGuard: CanActivateFn = () => {
  const bootstrapService = inject(BootstrapService);
  const shellService = inject(ShellService);

  return bootstrapService.getBootstrap().pipe(
    tap((bootstrap) => shellService.setBootstrap(bootstrap)),
    map(() => true),
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 || error.status === 403) {
        const returnUrl = window.location.pathname + window.location.search;
        const loginUrl = `${AUTH_CONFIG.loginPath}?${AUTH_CONFIG.returnUrlParam}=${encodeURIComponent(returnUrl)}`;
        window.location.href = loginUrl;
      }
      return of(false);
    })
  );
};
