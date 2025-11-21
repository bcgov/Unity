import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface AppConfig {
  apiUrl: string;
  environment: string;
  version: string;
}

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  private config: AppConfig | null = null;

  constructor(private http: HttpClient) {}

  /**
   * Load configuration from config.json
   * This should be called before the app initializes
   */
  async loadConfig(): Promise<void> {
    try {
      this.config = await firstValueFrom(
        this.http.get<AppConfig>('/config.json')
      );
      console.log('Configuration loaded:', this.config);
    } catch (error) {
      console.error('Failed to load configuration:', error);
      // Fallback to default config
      this.config = {
        apiUrl: '/api',
        environment: 'production',
        version: 'unknown'
      };
    }
  }

  /**
   * Get the full configuration object
   */
  getConfig(): AppConfig {
    if (!this.config) {
      throw new Error('Configuration not loaded. Call loadConfig() first.');
    }
    return this.config;
  }

  /**
   * Get the API URL
   */
  get apiUrl(): string {
    return this.getConfig().apiUrl;
  }

  /**
   * Get the environment
   */
  get environment(): string {
    return this.getConfig().environment;
  }

  /**
   * Get the version
   */
  get version(): string {
    return this.getConfig().version;
  }

  /**
   * Check if running in production
   */
  get isProduction(): boolean {
    return this.getConfig().environment === 'production';
  }
}
