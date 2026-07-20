import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { loadThemeScripts, loadThemeStylesheets } from './app/core/theme-assets';

loadThemeStylesheets();
loadThemeScripts();

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
