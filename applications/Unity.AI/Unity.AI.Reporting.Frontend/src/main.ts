import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { RootComponent } from './app/root/root.component';
import { provideHttpClient } from '@angular/common/http';

console.log('AI Reporting: Starting application - origin validation will handle security');

bootstrapApplication(RootComponent, {
  ...appConfig,
  providers: [
    ...(appConfig.providers || []),
    provideHttpClient()
  ]
})
  .catch((err) => {
    // Handle bootstrap error silently
  });
