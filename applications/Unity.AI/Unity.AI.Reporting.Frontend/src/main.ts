import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { RootComponent } from './app/root/root.component';

console.log('AI Reporting: Starting application - origin validation will handle security');

bootstrapApplication(RootComponent, appConfig)
  .catch((err) => {
    // Handle bootstrap error silently
  });
