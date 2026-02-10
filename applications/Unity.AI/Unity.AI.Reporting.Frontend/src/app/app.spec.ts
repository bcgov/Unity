import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';
import { ConfigService } from './services/config.service';

const mockConfigService = {
  loadConfig: () => Promise.resolve(),
  loadIframeOrigins: () => Promise.resolve(),
  getConfig: () => ({ apiUrl: '/api', environment: 'test', version: '1.0.0' }),
  apiUrl: '/api',
  environment: 'test',
  version: '1.0.0',
  isProduction: false,
  iframeOriginUrls: [],
  iframeOriginsLoaded: true,
};

describe('App', () => {
  let httpTesting: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ConfigService, useValue: mockConfigService },
      ]
    }).compileComponents();

    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render title', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    // Flush sidebar chat list request triggered by SidebarComponent.ngOnInit()
    httpTesting.match('/api/chats').forEach(req => req.flush([]));

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('What would you like to know?');
  });
});
