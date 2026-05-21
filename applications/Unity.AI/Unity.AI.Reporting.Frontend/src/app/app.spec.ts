import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';
import { ConfigService } from './services/config.service';
import { Turn } from './turn';

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

function makeTurn(sql: string = 'SELECT 1'): Turn {
  return {
    question: 'test',
    embed: {
      card_id: 1, x_field: '', y_field: '', title: 'Test',
      visualization_options: [], SQL: sql,
      tokens: { prompt_tokens: 10, completion_tokens: 5, total_tokens: 15 }
    },
    safeUrl: null,
    iframeLoaded: true,
    sqlPanelOpen: false,
  };
}

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

  describe('fetchSqlExplanation', () => {
    it('should fetch explanation and store it on the turn', async () => {
      const fixture = TestBed.createComponent(App);
      fixture.detectChanges();
      httpTesting.match('/api/chats').forEach(req => req.flush([]));
      const app = fixture.componentInstance as any;
      const turn = makeTurn();

      const promise = app.fetchSqlExplanation(turn);

      const req = httpTesting.expectOne('/api/explain_sql');
      expect(req.request.body).toEqual({ sql: 'SELECT 1' });
      req.flush({ explanation: 'I\'ve selected the number 1.', tokens: { prompt_tokens: 5, completion_tokens: 3, total_tokens: 8 } });

      await promise;
      expect(turn.embed.sql_explanation).toBe('I\'ve selected the number 1.');
      expect(turn.embed.tokens!.total_tokens).toBe(23);
    });

    it('should deduplicate concurrent calls (single request)', async () => {
      const fixture = TestBed.createComponent(App);
      fixture.detectChanges();
      httpTesting.match('/api/chats').forEach(req => req.flush([]));
      const app = fixture.componentInstance as any;
      const turn = makeTurn();

      const promise1 = app.fetchSqlExplanation(turn);
      const promise2 = app.fetchSqlExplanation(turn);

      expect(promise1).toBe(promise2);

      const req = httpTesting.expectOne('/api/explain_sql');
      req.flush({ explanation: 'test', tokens: null });

      await promise1;
      expect(turn.embed.sql_explanation).toBe('test');
    });

    it('should clear cache on failure so retry works', async () => {
      const fixture = TestBed.createComponent(App);
      fixture.detectChanges();
      httpTesting.match('/api/chats').forEach(req => req.flush([]));
      const app = fixture.componentInstance as any;
      const turn = makeTurn();

      const promise1 = app.fetchSqlExplanation(turn);
      httpTesting.expectOne('/api/explain_sql').flush('error', { status: 500, statusText: 'Server Error' });

      await promise1.catch(() => {});

      const promise2 = app.fetchSqlExplanation(turn);
      expect(promise2).not.toBe(promise1);

      httpTesting.expectOne('/api/explain_sql').flush({ explanation: 'recovered', tokens: null });
      await promise2;
      expect(turn.embed.sql_explanation).toBe('recovered');
    });
  });

  describe('toggleSqlPanel', () => {
    it('should open panel and trigger fetch when no explanation exists', async () => {
      const fixture = TestBed.createComponent(App);
      fixture.detectChanges();
      httpTesting.match('/api/chats').forEach(req => req.flush([]));
      const app = fixture.componentInstance as any;
      const turn = makeTurn();

      app.toggleSqlPanel(turn);

      expect(turn.sqlPanelOpen).toBe(true);
      const req = httpTesting.expectOne('/api/explain_sql');
      req.flush({ explanation: 'panel explanation', tokens: null });

      await fixture.whenStable();
      expect(turn.embed.sql_explanation).toBe('panel explanation');
    });

    it('should not fetch when explanation already exists', () => {
      const fixture = TestBed.createComponent(App);
      fixture.detectChanges();
      httpTesting.match('/api/chats').forEach(req => req.flush([]));
      const app = fixture.componentInstance as any;
      const turn = makeTurn();
      turn.embed.sql_explanation = 'already here';

      app.toggleSqlPanel(turn);

      expect(turn.sqlPanelOpen).toBe(true);
      httpTesting.expectNone('/api/explain_sql');
    });

    it('should show fallback and toast on fetch failure', async () => {
      const fixture = TestBed.createComponent(App);
      fixture.detectChanges();
      httpTesting.match('/api/chats').forEach(req => req.flush([]));
      const app = fixture.componentInstance as any;
      const turn = makeTurn();
      const toastSpy = vi.spyOn(app.toastService, 'error');

      app.toggleSqlPanel(turn);
      httpTesting.expectOne('/api/explain_sql').flush('error', { status: 500, statusText: 'Server Error' });

      await new Promise(resolve => setTimeout(resolve));
      expect(turn.embed.sql_explanation).toBe('Unable to generate explanation at this time.');
      expect(turn.embed.sql_explanation_error).toBe(true);
      expect(toastSpy).toHaveBeenCalled();
    });

    it('should retry the fetch when the panel is reopened after a previous failure', async () => {
      const fixture = TestBed.createComponent(App);
      fixture.detectChanges();
      httpTesting.match('/api/chats').forEach(req => req.flush([]));
      const app = fixture.componentInstance as any;
      const turn = makeTurn();

      // First open: fetch fails and leaves the fallback text + error flag.
      app.toggleSqlPanel(turn);
      httpTesting.expectOne('/api/explain_sql').flush('error', { status: 500, statusText: 'Server Error' });
      await new Promise(resolve => setTimeout(resolve));
      expect(turn.embed.sql_explanation_error).toBe(true);

      // Close, then reopen: the fallback text must not block a fresh attempt.
      app.toggleSqlPanel(turn); // close
      app.toggleSqlPanel(turn); // reopen

      httpTesting.expectOne('/api/explain_sql').flush({ explanation: 'recovered on retry', tokens: null });
      await fixture.whenStable();
      expect(turn.embed.sql_explanation).toBe('recovered on retry');
      expect(turn.embed.sql_explanation_error).toBe(false);
    });
  });
});
