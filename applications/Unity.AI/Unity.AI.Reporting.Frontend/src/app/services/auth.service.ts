import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface JwtPayload {
  user_id?: string;
  tenant?: string;
  jti?: string;
  exp?: number;
  iss?: string;
  aud?: string;
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private tokenSubject = new BehaviorSubject<string | null>(null);
  public token$ = this.tokenSubject.asObservable();

  constructor() {
    this.initializeFromUrl();
  }

  private initializeFromUrl(): void {
    const urlParams = new URLSearchParams(window.location.search);
    const token = urlParams.get('token');
    
    if (token) {
      this.setToken(token);
      // Clean up URL to remove token from address bar
      const url = new URL(window.location.href);
      url.searchParams.delete('token');
      window.history.replaceState({}, document.title, url.toString());
    }
  }

  setToken(token: string): void {
    this.tokenSubject.next(token);
    localStorage.setItem('jwt_token', token);
  }

  getToken(): string | null {
    return this.tokenSubject.value || localStorage.getItem('jwt_token');
  }

  clearToken(): void {
    this.tokenSubject.next(null);
    localStorage.removeItem('jwt_token');
  }

  async isAuthenticated(): Promise<boolean> {
    const token = this.getToken();
    
    // Check for null, undefined, empty string, or whitespace-only strings
    if (!token || token.trim() === '') {
      return false;
    }
    
    // Check if token has the correct JWT format (three parts separated by dots)
    const parts = token.split('.');
    if (parts.length !== 3) {
      return false;
    }
    
    // Check that each part is not empty
    if (parts.some(part => !part || part.trim() === '')) {
      return false;
    }
    
    // Basic token presence and format validation only
    // All cryptographic validation is handled by the backend
    return true;
  }

  isAuthenticatedSync(): boolean {
    const token = this.getToken();
    
    // Check for null, undefined, empty string, or whitespace-only strings
    if (!token || token.trim() === '') {
      return false;
    }
    
    // Check if token has the correct JWT format (three parts separated by dots)
    const parts = token.split('.');
    if (parts.length !== 3) {
      return false;
    }
    
    // Check that each part is not empty
    if (parts.some(part => !part || part.trim() === '')) {
      return false;
    }
    
    // Basic token presence and format validation only
    return true;
  }

  async validateTokenWithBackend(): Promise<boolean> {
    const token = this.getToken();
    if (!token) {
      return false;
    }

    try {
      // Make request to backend validation endpoint
      const response = await fetch('/api/validate-token', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        }
      });

      if (response.ok) {
        const data = await response.json();
        return data.valid === true;
      }
      
      return false;
    } catch (error) {
      console.error('Token validation error:', error);
      return false;
    }
  }

  decodeToken(token?: string): JwtPayload | null {
    const jwtToken = token || this.getToken();
    
    // Strict validation
    if (!jwtToken || jwtToken.trim() === '') {
      return null;
    }
    
    try {
      // Split JWT into parts
      const parts = jwtToken.split('.');
      if (parts.length !== 3) {
        return null;
      }
      
      // Validate each part exists and is not empty
      const [header, payload, signature] = parts;
      if (!header || !payload || !signature) {
        return null;
      }
      
      // Decode base64url payload (second part)
      const base64 = this.base64urlDecode(payload);
      const decodedPayload = JSON.parse(base64);
      
      // Validate payload is an object
      if (typeof decodedPayload !== 'object' || decodedPayload === null) {
        return null;
      }
      
      return decodedPayload as JwtPayload;
    } catch (error) {
      console.error('Token decode error:', error);
      return null;
    }
  }

  private base64urlDecode(base64url: string): string {
    // Convert base64url to base64
    let base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
    
    // Add padding if needed
    while (base64.length % 4) {
      base64 += '=';
    }
    
    return atob(base64);
  }


  getTenantId(): string | null {
    const payload = this.decodeToken();
    return payload?.tenant || null;
  }

  getUserId(): string | null {
    const payload = this.decodeToken();
    return payload?.user_id || null;
  }
}