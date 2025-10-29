import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private api_url = environment.apiUrl;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

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

  // Wrapper methods for common HTTP operations
  post<T>(endpoint: string, body: any = {}): Observable<T> {
    return this.http.post<T>(`${this.api_url}${endpoint}`, body, {
      headers: this.getHeaders()
    });
  }

  get<T>(endpoint: string, params: any = {}): Observable<T> {
    return this.http.get<T>(`${this.api_url}${endpoint}`, { 
      params: params,
      headers: this.getHeaders()
    });
  }

  put<T>(endpoint: string, body: any = {}): Observable<T> {
    return this.http.put<T>(`${this.api_url}${endpoint}`, body, {
      headers: this.getHeaders()
    });
  }

  delete<T>(endpoint: string, body: any = {}): Observable<T> {
    return this.http.delete<T>(`${this.api_url}${endpoint}`, { 
      body: body,
      headers: this.getHeaders()
    });
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