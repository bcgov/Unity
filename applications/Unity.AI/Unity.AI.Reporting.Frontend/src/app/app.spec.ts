import { APP_INITIALIZER, ApplicationInitStatus  } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { App } from './app';
import { initializeApp } from './app.config';
import { ConfigService } from './services/config.service';

describe('App', () => {
  const token = '/?token=REPLACE_WITH_JWT_TOKEN';
  let originalUrl: string;

  beforeAll(() => {
    // Save the real URL so we can restore it later (useful when running multiple specs)
    originalUrl = window.location.pathname + window.location.search + window.location.hash;
    // Set the test URL BEFORE Angular APP_INITIALIZER runs
    history.pushState({}, '', token);
  });

  afterAll(() => {
    // Restore to avoid cross-test pollution
    history.pushState({}, '', originalUrl || '/');
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideHttpClient(),
        ConfigService,
        {
          provide: APP_INITIALIZER,
          useFactory: initializeApp,
          deps: [ConfigService],
          multi: true
        }
      ]
    }).compileComponents();
  });

  it('should create the app', async () => {
    // Wait for all APP_INITIALIZERs to finish
    await TestBed.inject(ApplicationInitStatus).donePromise;

    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render title', async () => {
    await TestBed.inject(ApplicationInitStatus).donePromise;

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('What would you like to know?');
  });
});
