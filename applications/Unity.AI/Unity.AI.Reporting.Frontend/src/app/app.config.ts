import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { ConfigService } from './services/config.service';
import { routes } from './app.routes';

/**
 * Factory function to initialize config before app starts
 */
export function initializeApp(configService: ConfigService) {
  return (): Promise<void> => {
    return configService.loadConfig();
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeApp,
      deps: [ConfigService],
      multi: true
    }
  ]
};
