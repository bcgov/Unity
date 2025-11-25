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
    console.log('=== Loading Application Configuration ===');
    console.log('Current hostname:', window.location.hostname);
    console.log('Current origin:', window.location.origin);

    try {
      this.config = await firstValueFrom(
        this.http.get<AppConfig>('/config.json')
      );
      console.log('✓ Configuration loaded successfully from /config.json');
      console.log('  API URL:', this.config.apiUrl);
      console.log('  Environment:', this.config.environment);
      console.log('  Version:', this.config.version);
      console.log('=========================================');
    } catch (error) {
      console.error('✗ Failed to load configuration from /config.json:', error);
      // Fallback to default config
      this.config = {
        apiUrl: '/api',
        environment: 'production',
        version: 'unknown'
      };
      console.warn('Using fallback configuration:');
      console.warn('  API URL:', this.config.apiUrl);
      console.warn('  Environment:', this.config.environment);
      console.warn('  Version:', this.config.version);
      console.log('=========================================');
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
