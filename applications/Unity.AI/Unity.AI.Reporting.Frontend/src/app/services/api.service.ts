import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { ConfigService } from './config.service';
import { LoggerService } from './logger.service';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private configService: ConfigService,
    private logger: LoggerService
  ) {
    this.logger.info('ApiService initialized');
    this.logConnectionDetails();
  }

  private logConnectionDetails(): void {
    this.logger.info('=== Backend Connection Details ===');
    this.logger.info(`API Base URL: ${this.api_url}`);
    this.logger.info(`Config Service API URL: ${this.configService.apiUrl}`);
    this.logger.info(`Environment: ${this.configService.environment}`);
    this.logger.info('==================================');
  }

  private get api_url(): string {
    return this.configService.apiUrl;
  }

  private getHeaders(): HttpHeaders {
    const headers: any = {
      'Content-Type': 'application/json'
    };

    // Add JWT token to Authorization header if available
    const token = this.authService.getToken();
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    return new HttpHeaders(headers);
  }

  private logRequest(method: string, url: string, body?: any, params?: any): void {
    this.logger.info(`→ ${method} ${url}`);
    if (body && Object.keys(body).length > 0) {
      this.logger.debug('Request body:', body);
    }
    if (params && Object.keys(params).length > 0) {
      this.logger.debug('Request params:', params);
    }
  }

  private logResponse<T>(method: string, url: string): any {
    return tap<T>({
      next: (response) => {
        this.logger.info(`✓ ${method} ${url} - Success`);
        this.logger.debug('Response:', response);
      },
      error: (error) => {
        this.logger.error(`✗ ${method} ${url} - Error`, {
          status: error.status,
          statusText: error.statusText,
          message: error.message,
          url: error.url,
          error: error.error
        });
      }
    });
  }

  // Wrapper methods for common HTTP operations
  post<T>(endpoint: string, body: any = {}): Observable<T> {
    const url = `${this.api_url}${endpoint}`;
    this.logRequest('POST', url, body);
    return this.http.post<T>(url, body, {
      headers: this.getHeaders()
    }).pipe(this.logResponse<T>('POST', url));
  }

  get<T>(endpoint: string, params: any = {}): Observable<T> {
    const url = `${this.api_url}${endpoint}`;
    this.logRequest('GET', url, undefined, params);
    return this.http.get<T>(url, {
      params: params,
      headers: this.getHeaders()
    }).pipe(this.logResponse<T>('GET', url));
  }

  put<T>(endpoint: string, body: any = {}): Observable<T> {
    const url = `${this.api_url}${endpoint}`;
    this.logRequest('PUT', url, body);
    return this.http.put<T>(url, body, {
      headers: this.getHeaders()
    }).pipe(this.logResponse<T>('PUT', url));
  }

  delete<T>(endpoint: string, body: any = {}): Observable<T> {
    const url = `${this.api_url}${endpoint}`;
    this.logRequest('DELETE', url, body);
    return this.http.delete<T>(url, {
      body: body,
      headers: this.getHeaders()
    }).pipe(this.logResponse<T>('DELETE', url));
  }

  // Convenience methods for specific endpoints
  askQuestion<T>(question: string, conversation: any[]): Observable<T> {
    return this.post<T>('/ask', { 
      question, 
      conversation 
    });
  }

  changeDisplay<T>(cardId: number, mode: string, xField: string, yField: string): Observable<T> {
    return this.post<T>('/change_display', {
      card_id: cardId,
      mode,
      x_field: xField,
      y_field: yField
    });
  }

  deleteCard<T>(cardId: number): Observable<T> {
    return this.post<T>('/delete', {
      card_id: cardId
    });
  }

  explainSql<T>(sql: string): Observable<T> {
    return this.post<T>('/explain_sql', {
      sql: sql
    });
  }

  // Chat-related methods
  getChats<T>(): Observable<T> {
    return this.post<T>('/chats', {});
  }

  getChat<T>(chatId: string): Observable<T> {
    return this.post<T>(`/chats/${chatId}`, {});
  }

  saveChat<T>(chatId: string | null, conversation: any[], title: string): Observable<T> {
    return this.post<T>('/chats/save', {
      chat_id: chatId,
      conversation,
      title
    });
  }

  deleteChat<T>(chatId: string): Observable<T> {
    return this.delete<T>(`/chats/${chatId}`, {});
  }

  // Token validation method
  validateToken<T>(): Observable<T> {
    return this.post<T>('/validate-token', {});
  }

  // Admin check method
  checkAdmin<T>(): Observable<T> {
    return this.post<T>('/check-admin', {});
  }

  // Get Metabase URL from backend
  getMetabaseUrl<T>(): Observable<T> {
    return this.get<T>('/metabase-url');
  }

  // Feedback methods
  submitFeedback<T>(chatId: string, feedbackType: string, message: string, context?: {
    currentQuestion?: string;
    currentSql?: string;
    currentSqlExplanation?: string;
    previousQuestion?: string;
    previousSql?: string;
    previousSqlExplanation?: string;
  }): Observable<T> {
    const payload: any = {
      chat_id: chatId,
      feedback_type: feedbackType,
      message: message,
      timestamp: new Date().toISOString(),
      frontend_version: '1.0.0'
    };

    // Add context data if provided
    if (context) {
      if (context.currentQuestion) payload.current_question = context.currentQuestion;
      if (context.currentSql) payload.current_sql = context.currentSql;
      if (context.currentSqlExplanation) payload.current_sql_explanation = context.currentSqlExplanation;
      if (context.previousQuestion) payload.previous_question = context.previousQuestion;
      if (context.previousSql) payload.previous_sql = context.previousSql;
      if (context.previousSqlExplanation) payload.previous_sql_explanation = context.previousSqlExplanation;
    }

    return this.post<T>('/feedback', payload);
  }

  getFeedback<T>(feedbackId: string): Observable<T> {
    return this.get<T>(`/feedback/${feedbackId}`);
  }

  getChatFeedback<T>(chatId: string): Observable<T> {
    return this.get<T>(`/chats/${chatId}/feedback`);
  }

  // Admin methods
  getAllFeedback<T>(limit: number = 100, offset: number = 0): Observable<T> {
    const params = new URLSearchParams();
    params.set('limit', limit.toString());
    params.set('offset', offset.toString());
    return this.get<T>(`/admin/feedback?${params.toString()}`);
  }

  updateFeedbackStatus<T>(feedbackId: string, status: string): Observable<T> {
    return this.put<T>(`/admin/feedback/${feedbackId}/status`, {
      status: status
    });
  }
}