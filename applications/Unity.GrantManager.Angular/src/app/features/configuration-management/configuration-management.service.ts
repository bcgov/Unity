import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { CONFIGURATION_MANAGEMENT_API_BASE } from '../../core/http/api-paths';
import { ConfigurationManagementBootstrap } from './configuration-management.model';

@Injectable({ providedIn: 'root' })
export class ConfigurationManagementService {
  private readonly http = inject(HttpClient);
  private readonly url = `${CONFIGURATION_MANAGEMENT_API_BASE}/bootstrap`;

  getBootstrap(): Observable<ConfigurationManagementBootstrap> {
    return this.http.get<ConfigurationManagementBootstrap>(this.url);
  }
}
