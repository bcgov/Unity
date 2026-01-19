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

  private _initializationPromise: Promise<void>;

  constructor(
    private logger: LoggerService,
    private configService: ConfigService
  ) {
    this.initializeFromUrl();
    // Start initialization but don't block constructor
    this._initializationPromise = this.initializeAsync();
  }

  private async initializeAsync(): Promise<void> {
    try {
      console.log('🔧 AUTH SERVICE: Loading iframe origins...');
      await this.configService.loadIframeOrigins();
      console.log('🔧 AUTH SERVICE: Iframe origins loaded, registering postMessage listener');
      this.initializePostMessageListener();
      this.sendReadyMessageToParent();
      console.log('🔧 AUTH SERVICE: Initialization complete');
    } catch (error) {
      console.error('❌ AUTH SERVICE: Initialization failed:', error);
      // Fail secure - register listener anyway but with no origins
      this.initializePostMessageListener();
      this.sendReadyMessageToParent();
    }
  }

  /**
   * Ensure initialization is complete before proceeding
   */
  private async ensureInitialized(): Promise<void> {
    if (this._initializationPromise) {
      await this._initializationPromise;
    }
  }

  private initializeFromUrl(): void {
    const urlParams = new URLSearchParams(window.location.search);
    const token = urlParams.get('token');

    if (token) {
      // For local development, mark URL tokens as authorized
      const isLocalDev = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';
      this.setToken(token, isLocalDev); // Mark as authorized only for localhost
      console.log('🔧 AUTH SERVICE: URL token loaded, authorized:', isLocalDev);
      
      // Clean up URL to remove token from address bar
      const url = new URL(window.location.href);
      url.searchParams.delete('token');
      window.history.replaceState({}, document.title, url.toString());
    } else {
      // No URL token - check if we have a stored token
      const storedToken = this.getToken();
      if (storedToken && !this.isInIframe()) {
        console.log('🔍 AUTH SERVICE: Direct access detected with stored token - checking if still valid');
        
        // If accessing directly (not in iframe) and we have a token, 
        // this might be after a Unity Grant Manager logout
        // Clear the token unless explicitly authorized
        const isAuthorized = localStorage.getItem('ai_reporting_authorized') === 'true';
        if (!isAuthorized) {
          console.log('❌ AUTH SERVICE: Direct access with unauthorized token - clearing');
          this.clearToken();
        }
      }
    }
  }
  
  private isInIframe(): boolean {
    try {
      return window.self !== window.top;
    } catch (e) {
      return true; // If we can't check, assume we're in an iframe for security
    }
  }

  private initializePostMessageListener(): void {
    console.log('AUTH SERVICE: Registering postMessage listener');
    // Listen for token from parent window (when embedded in iframe)
    
    window.addEventListener('message', (event: MessageEvent) => {
      console.log('AI Reporting: Received postMessage from origin:', event.origin);
      
      // Validate origin against configured iframe origins
      const allowedOrigins = this.configService.iframeOriginUrls;
      const originsLoaded = this.configService.iframeOriginsLoaded;
      const currentOrigin = window.location.origin;
      
      console.log('🔍 AUTH SERVICE ORIGIN VALIDATION:');
      console.log('  Event origin:', event.origin);
      console.log('  Current origin:', currentOrigin);
      console.log('  Allowed origins:', allowedOrigins);
      console.log('  Allowed origins length:', allowedOrigins.length);
      console.log('  Origins loaded:', originsLoaded);
      console.log('  Origin included in allowed list:', allowedOrigins.includes(event.origin));
      
      // Reject self-origin postMessages (prevents security bypass)
      if (event.origin === currentOrigin) {
        console.log('❌ AI Reporting AUTH SERVICE: Rejected self-origin postMessage (security bypass attempt)');
        return;
      }
      
      // If origins are not loaded yet, reject for security (fail-secure)
      if (!originsLoaded) {
        console.log('❌ AI Reporting AUTH SERVICE: Origins not loaded yet - rejecting for security');
        return;
      }
      
      // If origins are loaded and configured, validate
      if (allowedOrigins.length > 0 && !allowedOrigins.includes(event.origin)) {
        console.log('❌ AI Reporting AUTH SERVICE: Rejected token from unconfigured origin:', event.origin);
        console.log('❌ AI Reporting AUTH SERVICE: Allowed origins:', allowedOrigins);
        return;
      } else {
        console.log('✅ AI Reporting AUTH SERVICE: Origin validation passed');
      }
      
      // Check if this is an AUTH_TOKEN message
      if (event.data && event.data.type === 'AUTH_TOKEN' && event.data.token) {
        console.log('AI Reporting: Received JWT token via postMessage');
        
        this.setToken(event.data.token, true); // Mark as authorized from postMessage

        // Send acknowledgment back to parent
        if (event.source && typeof (event.source as Window).postMessage === 'function') {
          (event.source as Window).postMessage(
            { type: 'AUTH_TOKEN_RECEIVED', success: true },
            event.origin
          );
        }
        
        console.log('AI Reporting: Token stored and acknowledgment sent');
        
        // Navigate to main app route to trigger auth guard re-evaluation
        if (window.location.pathname === '/' || window.location.pathname.includes('access-denied')) {
          console.log('AI Reporting: Navigating to /app after token receipt');
          // Force a re-evaluation by the auth guard without triggering logout detection
          setTimeout(() => {
            // Use Angular router if available, otherwise use location API carefully
            try {
              // Try to get Angular router from the global window object
              const windowAny = window as any;
              if (windowAny.ng && windowAny.ng.getComponent) {
                // Use Angular navigation if possible
                console.log('AI Reporting: Using Angular navigation to /app');
                window.location.hash = '#/app';
              } else {
                // Fallback: use pushState to avoid page reload
                console.log('AI Reporting: Using pushState navigation to /app');
                window.history.pushState(null, '', '/app');
                // Manually trigger a route check
                window.dispatchEvent(new PopStateEvent('popstate', { state: null }));
              }
            } catch (error) {
              console.error('AI Reporting: Navigation failed, forcing reload:', error);
              // Last resort: reload the page (but token should persist)
              window.location.href = '/app';
            }
          }, 200);
        }
      }
      
      // Check if this is a LOGOUT message from parent
      if (event.data && event.data.type === 'LOGOUT') {
        console.log('🚪 AI Reporting: Received LOGOUT signal from Unity Grant Manager');
        this.clearToken();
        console.log('🚪 AI Reporting: Token cleared due to LOGOUT signal');
        
        // Redirect to access denied or home page
        setTimeout(() => {
          window.location.href = '/access-denied';
        }, 100);
      }
      
      // Check if this is a parent logout detection
      if (event.data && event.data.type === 'PARENT_LOGOUT') {
        console.log('🚪 AI Reporting: Received PARENT_LOGOUT signal');
        this.clearToken();
        window.location.href = '/access-denied';
      }
    });
  }

  private sendReadyMessageToParent(): void {
    // Only send READY message if we're in an iframe
    if (window.self !== window.top && window.parent) {
      try {
        // Send READY message to parent to request authentication token
        window.parent.postMessage({ type: 'READY' }, '*');
        this.logger.info('Sent READY message to parent window');
        
        // Set up parent window logout detection
        this.setupParentLogoutDetection();
      } catch (error) {
        this.logger.error('Failed to send READY message to parent:', error);
      }
    }
  }

  private setupParentLogoutDetection(): void {
    console.log('🔧 AUTH SERVICE: Setting up parent window logout detection');
    
    // Method 1: Listen for beforeunload on parent (if accessible)
    try {
      if (window.parent && window.parent !== window) {
        // Simplified parent check - only clear if we're sure parent closed
        let parentCheckCount = 0;
        const checkParentStatus = () => {
          try {
            // Try to access parent - if this fails consistently, parent might be gone
            if (window.parent && window.parent.location) {
              parentCheckCount = 0; // Reset counter if parent is accessible
            }
          } catch (e) {
            parentCheckCount++;
            // Only clear token after multiple failed attempts (parent likely closed)
            if (parentCheckCount >= 3) {
              console.log('🔍 AUTH SERVICE: Parent window consistently inaccessible - clearing token');
              this.clearToken();
              return;
            }
          }
        };
        
        // Check parent status less frequently and with retry logic
        setInterval(checkParentStatus, 10000);
      }
    } catch (error) {
      console.log('⚠️ AUTH SERVICE: Cannot access parent window for logout detection');
    }
    
    // Method 2: Removed blur detection as it's too sensitive
    // window.addEventListener('blur', () => { ... });
    
    // Method 3: Removed visibility change detection as it triggers on normal tab switches
    // document.addEventListener('visibilitychange', () => { ... });
    
    // Method 4: Only clear token on actual window close, not navigation
    window.addEventListener('beforeunload', (event) => {
      // Only clear if this is a real page close/refresh, not iframe navigation
      if (this.isInIframe() && !document.referrer.includes(window.location.hostname)) {
        console.log('🔍 AUTH SERVICE: Window closing (not navigation) - clearing token');
        this.clearToken();
      }
    });
    
    // Method 5: Don't use pagehide as it's too aggressive for SPAs
    // Commented out to prevent false positives during navigation
    // window.addEventListener('pagehide', () => {
    //   console.log('🔍 AUTH SERVICE: Page hide event - clearing token');
    //   this.clearToken();
    // });
    
    console.log('✅ AUTH SERVICE: Parent logout detection setup complete');
  }

  setToken(token: string, fromPostMessage: boolean = false): void {
    console.log('💾 AUTH SERVICE: Setting token', {
      tokenLength: token.length,
      fromPostMessage,
      timestamp: new Date().toISOString()
    });
    
    this.tokenSubject.next(token);
    localStorage.setItem('ai_reporting', token);
    
    // Track when token was set for session management
    localStorage.setItem('ai_reporting_timestamp', Date.now().toString());
    
    // Track if token came from authorized postMessage
    if (fromPostMessage) {
      localStorage.setItem('ai_reporting_authorized', 'true');
      console.log('✅ Token marked as authorized (from postMessage)');
    } else {
      console.log('⚠️ Token set but not authorized (awaiting postMessage validation)');
    }
  }

  getToken(): string | null {
    return this.tokenSubject.value || localStorage.getItem('ai_reporting');
  }

  clearToken(): void {
    console.log('❌ AUTH SERVICE: Clearing token', {
      hadToken: !!this.getToken(),
      wasAuthorized: localStorage.getItem('ai_reporting_authorized') === 'true',
      timestamp: new Date().toISOString()
    });
    
    this.tokenSubject.next(null);
    localStorage.removeItem('ai_reporting');
    localStorage.removeItem('ai_reporting_authorized');
    localStorage.removeItem('ai_reporting_timestamp');
  }

  async isAuthenticated(): Promise<boolean> {
    console.log('🔍 isAuthenticated() called at:', new Date().toISOString());
    
    // Wait for initialization to complete
    await this.ensureInitialized();
    console.log('🔍 Initialization complete, proceeding with auth check');
    
    const token = this.getToken();
    console.log('🔍 Token retrieved:', {
      hasToken: !!token,
      tokenLength: token?.length || 0,
      tokenPreview: token?.substring(0, 20) + '...' || 'none'
    });

    // Check for null, undefined, empty string, or whitespace-only strings
    if (!token || token.trim() === '') {
      console.log('❌ No token found or empty token');
      return false;
    }

    // Check token age for session management
    const tokenTimestamp = localStorage.getItem('ai_reporting_timestamp');
    console.log('🔍 Token timestamp check:', {
      timestamp: tokenTimestamp,
      timestampDate: tokenTimestamp ? new Date(parseInt(tokenTimestamp)).toISOString() : null
    });
    
    if (tokenTimestamp) {
      const tokenAge = Date.now() - parseInt(tokenTimestamp);
      const maxAge = 8 * 60 * 60 * 1000; // 8 hours in milliseconds
      
      console.log('🔍 Token age check:', {
        tokenAge: tokenAge,
        tokenAgeMinutes: Math.floor(tokenAge / (60 * 1000)),
        maxAgeMinutes: Math.floor(maxAge / (60 * 1000)),
        isExpired: tokenAge > maxAge
      });
      
      if (tokenAge > maxAge) {
        console.log('❌ Token expired due to age (8+ hours old) - clearing');
        this.clearToken();
        return false;
      }
    }
    
    // Check if token was authorized via ORIGIN_URL validation
    const isAuthorized = localStorage.getItem('ai_reporting_authorized') === 'true';
    console.log('🔍 Authorization check:', {
      isAuthorized: isAuthorized,
      authValue: localStorage.getItem('ai_reporting_authorized'),
      authTimestamp: localStorage.getItem('ai_reporting_timestamp')
    });
    
    if (!isAuthorized) {
      console.log('❌ Token not authorized via ORIGIN_URL validation');
      console.log('🕰️ AUTH DEBUG: Token present but not authorized - allowing postMessage system time to authorize');
      // Don't clear token immediately - might be a race condition
      // Let the postMessage system have a chance to authorize it
      return false;
    }
    console.log('✅ Token authorization check passed');

    console.log('🔍 Token found, validating format...');

    // Check if token has the correct JWT format (three parts separated by dots)
    const parts = token.split('.');
    console.log('🔍 Token parts check:', {
      partsCount: parts.length,
      expectedParts: 3,
      isValidFormat: parts.length === 3
    });
    
    if (parts.length !== 3) {
      console.log('❌ Invalid token format - does not have 3 parts');
      return false;
    }

    // Check that each part is not empty
    const hasEmptyParts = parts.some(part => !part || part.trim() === '');
    console.log('🔍 Token parts content check:', {
      hasEmptyParts: hasEmptyParts,
      partLengths: parts.map(p => p.length)
    });
    
    if (hasEmptyParts) {
      console.log('❌ Invalid token format - has empty parts');
      return false;
    }

    console.log('✓ Token format is valid');

    // Decode and check expiration
    const payload = this.decodeToken(token);
    console.log('🔍 Token decode result:', {
      payloadDecoded: !!payload,
      payloadPreview: payload ? {
        userId: payload.user_id,
        tenant: payload.tenant,
        exp: payload.exp,
        isAdmin: payload['is_it_admin']
      } : null
    });
    
    if (!payload) {
      console.log('❌ Failed to decode token payload');
      return false;
    }

    // Check if token has expired
    if (payload.exp) {
      const currentTime = Math.floor(Date.now() / 1000); // Current time in seconds
      const expirationTime = payload.exp;

      console.log('🔍 Token expiration check:');
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
    console.log('🔍 Validating token with backend...');
    const isValidOnBackend = await this.validateTokenWithBackend();
    console.log('🔍 Backend validation result:', isValidOnBackend);

    if (!isValidOnBackend) {
      console.log('❌ Backend rejected token (invalid signature or unauthorized)');
      this.clearToken();
      return false;
    }

    console.log('✓ Backend confirmed token is valid');
    console.log('🎉 isAuthenticated() returning TRUE at:', new Date().toISOString());
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
    console.log('🔍 validateTokenWithBackend() starting:', {
      hasToken: !!token,
      tokenLength: token?.length || 0
    });
    
    if (!token) {
      console.log('❌ validateTokenWithBackend: No token available');
      return false;
    }

    try {
      // Make request to backend validation endpoint using configured API URL
      const apiUrl = this.configService.apiUrl;
      console.log('🔍 Backend validation request:', {
        apiUrl: apiUrl,
        endpoint: `${apiUrl}/validate-token`,
        hasAuthHeader: true
      });
      
      const response = await fetch(`${apiUrl}/validate-token`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        }
      });

      console.log('🔍 Backend validation response:', {
        status: response.status,
        statusText: response.statusText,
        ok: response.ok,
        headers: Object.fromEntries(response.headers.entries())
      });

      if (response.ok) {
        const data = await response.json();
        console.log('🔍 Backend validation response data:', data);
        const isValid = data.valid === true;
        console.log('🔍 Backend validation result:', isValid);
        return isValid;
      } else {
        console.log('❌ Backend validation failed with status:', response.status);
        try {
          const errorData = await response.text();
          console.log('❌ Backend validation error response:', errorData);
        } catch (e) {
          console.log('❌ Could not read error response body');
        }
      }

      return false;
    } catch (error) {
      console.error('❌ Token validation network error:', error);
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

  /**
   * Get current authentication state for debugging
   */
  getAuthDebugInfo(): any {
    const token = this.getToken();
    const payload = token ? this.decodeToken(token) : null;
    
    return {
      hasToken: !!token,
      tokenLength: token?.length || 0,
      isAuthorized: localStorage.getItem('ai_reporting_authorized') === 'true',
      originsLoaded: this.configService.iframeOriginsLoaded,
      allowedOrigins: this.configService.iframeOriginUrls,
      currentOrigin: window.location.origin,
      isLocalDev: window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1',
      tokenPayload: payload ? {
        userId: payload.user_id,
        tenant: payload.tenant,
        isAdmin: payload['is_it_admin'],
        exp: payload.exp,
        expiresAt: payload.exp ? new Date(payload.exp * 1000).toISOString() : null
      } : null,
      timestamp: new Date().toISOString()
    };
  }
}