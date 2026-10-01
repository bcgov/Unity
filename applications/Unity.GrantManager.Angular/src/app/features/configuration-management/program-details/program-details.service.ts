import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { CONFIGURATION_MANAGEMENT_API_BASE } from '../../../core/http/api-paths';
import { ProgramDetails } from './program-details.model';

@Injectable({ providedIn: 'root' })
export class ProgramDetailsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${CONFIGURATION_MANAGEMENT_API_BASE}/program-details`;

  get(): Observable<ProgramDetails> {
    return this.http.get<ProgramDetails>(this.url);
  }

  update(details: ProgramDetails): Observable<void> {
    return this.http.put<void>(this.url, details);
  }
}
