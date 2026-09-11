import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { SHELL_API_BASE } from '../http/api-paths';
import { ShellBootstrap } from '../../shell/shell.model';

@Injectable({ providedIn: 'root' })
export class BootstrapService {
  private readonly http = inject(HttpClient);
  private readonly url = `${SHELL_API_BASE}/bootstrap`;

  getBootstrap(): Observable<ShellBootstrap> {
    return this.http.get<ShellBootstrap>(this.url);
  }
}
