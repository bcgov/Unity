import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { LoggerService } from './logger.service';
import { ConfigService } from './config.service';

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

  constructor(
    private logger: LoggerService,
    private configService: ConfigService
  ) {
    this.initializeFromUrl();
    this.initializePostMessageListener();
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

  private initializePostMessageListener(): void {
    // Listen for token from parent window (when embedded in iframe)
    window.addEventListener('message', (event: MessageEvent) => {
      // Check if this is an AUTH_TOKEN message
      if (event.data && event.data.type === 'AUTH_TOKEN' && event.data.token) {
        // Store the token
        this.setToken(event.data.token);

        // Send acknowledgment back to parent
        if (event.source && typeof (event.source as Window).postMessage === 'function') {
          (event.source as Window).postMessage(
            { type: 'AUTH_TOKEN_RECEIVED', success: true },
            event.origin
          );
        }
      }
    });
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
    console.log('--- AuthService.isAuthenticated() called ---');
    const token = this.getToken();

    // Check for null, undefined, empty string, or whitespace-only strings
    if (!token || token.trim() === '') {
      console.log('❌ No token found or empty token');
      return false;
    }

    console.log('Token found, validating format...');

    // Check if token has the correct JWT format (three parts separated by dots)
    const parts = token.split('.');
    if (parts.length !== 3) {
      console.log('❌ Invalid token format - does not have 3 parts');
      return false;
    }

    // Check that each part is not empty
    if (parts.some(part => !part || part.trim() === '')) {
      console.log('❌ Invalid token format - has empty parts');
      return false;
    }

    console.log('✓ Token format is valid');

    // Decode and check expiration
    const payload = this.decodeToken(token);
    if (!payload) {
      console.log('❌ Failed to decode token payload');
      return false;
    }

    // Check if token has expired
    if (payload.exp) {
      const currentTime = Math.floor(Date.now() / 1000); // Current time in seconds
      const expirationTime = payload.exp;

      console.log('Token expiration check:');
      console.log('  Current time:', currentTime, '(' + new Date(currentTime * 1000).toISOString() + ')');
      console.log('  Expiration time:', expirationTime, '(' + new Date(expirationTime * 1000).toISOString() + ')');

      if (currentTime >= expirationTime) {
        console.log('❌ Token has expired');
        // Clear expired token
        this.clearToken();
        return false;
      }

      const timeRemaining = expirationTime - currentTime;
      console.log('✓ Token is valid for', timeRemaining, 'more seconds');
    } else {
      console.log('⚠ Token has no expiration field (exp)');
    }

    // Validate token signature and authenticity with backend
    // To remove backend validation and only validate structure and expiry in frontend return true here
    console.log('Validating token with backend...');
    const isValidOnBackend = await this.validateTokenWithBackend();

    if (!isValidOnBackend) {
      console.log('❌ Backend rejected token (invalid signature or unauthorized)');
      this.clearToken();
      return false;
    }

    console.log('✓ Backend confirmed token is valid');
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
      // Make request to backend validation endpoint using configured API URL
      const apiUrl = this.configService.apiUrl;
      const response = await fetch(`${apiUrl}/validate-token`, {
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
      this.logger.error('Token validation error:', error);
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
      this.logger.error('Token decode error:', error);
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