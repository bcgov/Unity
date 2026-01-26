# Unity AI Reporting - JWT Authentication Flow

This document explains how JWT authentication works in the Unity AI Reporting platform, specifically the `?token=` URL parameter method for development and testing.

## Overview

The Unity AI Reporting platform uses JWT (JSON Web Token) authentication with a shared secret between frontend and backend. For development/testing, tokens can be embedded in URLs to automatically authenticate users.

## Authentication Components

### 1. JWT Token Structure

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoiZGV2LXVzZXItMTIzIi...
├── Header (Base64URL)
├── Payload (Base64URL) 
└── Signature (HMAC-SHA256)
```

**Header:**
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload:**
```json
{
  "user_id": "dev-user-123",
  "tenant": "default",
  "is_it_admin": false,
  "iat": 1768255639,
  "exp": 1768342039,
  "jti": "dev-user-123_1768255639"
}
```

### 2. Token Generation

**PowerShell Script:** `unity-ai-reporting-verify-devTokenURL.ps1`

**Process:**
1. Reads JWT_SECRET from applications/.env file
2. Creates JWT header (static)
3. Builds payload with user details and timestamps
4. Signs with HMAC-SHA256 using JWT_SECRET from .env
5. Outputs URLs for both local and OpenShift testing

**Usage:**
```powershell
# Run from applications folder
.\documentation\unity-ai-reporting-verify-devTokenURL.ps1 -UserId 'user@gov.bc.ca'

# Run from documentation folder  
.\unity-ai-reporting-verify-devTokenURL.ps1 -UserId 'user@gov.bc.ca'

# Admin user token  
.\unity-ai-reporting-verify-devTokenURL.ps1 -UserId 'admin@gov.bc.ca' -IsAdmin $true

# Custom parameters
.\unity-ai-reporting-verify-devTokenURL.ps1 -UserId "test-user" -Tenant "demo" -ExpiresInMinutes 120

# Quick testing with 1-minute expiration
.\unity-ai-reporting-verify-devTokenURL.ps1 -UserId "test-user" -ExpiresInMinutes 1
```

**Security:** The script automatically reads the JWT_SECRET from your .env file, ensuring consistency with your application configuration without hardcoding secrets.

## Authentication Flow

### 1. URL Token Processing (Frontend)

**File:** `Unity.AI.Reporting.Frontend/src/app/services/auth.service.ts`

When the Angular app loads with `?token=` parameter:

```typescript
private initializeFromUrl(): void {
  const urlParams = new URLSearchParams(window.location.search);
  const token = urlParams.get('token');

  if (token) {
    this.setToken(token);  // Store in localStorage + BehaviorSubject
    
    // Clean URL to remove token from address bar
    const url = new URL(window.location.href);
    url.searchParams.delete('token');
    window.history.replaceState({}, document.title, url.toString());
  }
}
```

**Steps:**
1. Extract token from URL parameter
2. Store in localStorage and reactive state
3. Clean URL for security (remove token from address bar)

### 2. Route Protection (Auth Guard)

**File:** `Unity.AI.Reporting.Frontend/src/app/guards/auth.guard.ts`

All protected routes use the auth guard:

```typescript
export const authGuard: CanActivateFn = async (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const isAuthenticated = await authService.isAuthenticated();

  if (!isAuthenticated) {
    router.navigate(['/access-denied']);
    return false;
  }
  return true;
};
```

**Routes protected:**
- `/` (root)
- `/app` (main application)
- `/admin` (admin panel)

### 3. Token Validation (Frontend)

**File:** `Unity.AI.Reporting.Frontend/src/app/services/auth.service.ts`

Multi-step validation process:

```typescript
async isAuthenticated(): Promise<boolean> {
  const token = this.getToken();
  
  // 1. Check token exists
  if (!token || token.trim() === '') return false;
  
  // 2. Validate JWT format (3 parts)
  const parts = token.split('.');
  if (parts.length !== 3) return false;
  
  // 3. Check expiration
  const payload = this.decodeToken(token);
  if (payload?.exp && Date.now() >= payload.exp * 1000) {
    this.clearToken();
    return false;
  }
  
  // 4. Validate with backend
  return await this.validateTokenWithBackend();
}
```

### 4. Backend Validation

**File:** `Unity.AI.Reporting.Backend/src/api.py`

Backend validation endpoint:

```python
@app.route("/api/validate-token", methods=["POST"])
@require_auth
def validate_token():
    user_data = get_user_from_token()
    return jsonify({
        "valid": True,
        "user_id": user_data["user_id"],
        "tenant_id": user_data["tenant"],
        "expires": user_data["exp"]
    }), 200
```

**File:** `Unity.AI.Reporting.Backend/src/auth.py`

JWT validation logic:

```python
def validate_token(self, token: str) -> Optional[Dict[str, Any]]:
    try:
        payload = jwt.decode(
            token,
            self.jwt_secret,
            algorithms=[self.jwt_algorithm],
            options={"verify_aud": False, "verify_iss": False}
        )
        
        # Handle Unity JWT format compatibility
        if 'sub' in payload and 'user_id' not in payload:
            payload['user_id'] = payload['sub']
        
        if 'tenant' not in payload:
            payload['tenant'] = 'default'
            
        return payload
    except (jwt.ExpiredSignatureError, jwt.InvalidTokenError):
        return None
```

## User Data Storage & Management

### Database Storage & User Ownership

The application extensively stores and reuses the `user_id` throughout the system for complete user data isolation:

**Database Tables with `user_id`:**
```sql
-- Chat conversations owned by users
CREATE TABLE chats (
    chat_id UUID PRIMARY KEY,
    user_id TEXT NOT NULL,        -- User ownership
    tenant_id TEXT NOT NULL,
    title TEXT NOT NULL,
    conversation JSONB NOT NULL,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);

-- User feedback tied to users  
CREATE TABLE feedback (
    feedback_id UUID PRIMARY KEY,
    chat_id UUID REFERENCES chats(chat_id),
    user_id TEXT NOT NULL,        -- User who submitted feedback
    tenant_id TEXT NOT NULL,
    feedback_type TEXT NOT NULL,
    message TEXT,
    created_at TIMESTAMP
);
```

### Data Access Control

**User Isolation:**
```python
# Users can only access their own chats
def get_user_chats(self, user_id: str, tenant_id: str):
    """Get all chats for a specific user"""
    WHERE user_id = %s AND tenant_id = %s

# Users can only modify their own chats
def delete_chat(self, chat_id: str, user_id: str):
    """Delete chat only if owned by user"""
    WHERE chat_id = %s AND user_id = %s
```

### Session Management

**Frontend Storage:**
```typescript
// Store user identity from JWT
getUserId(): string | null {
    const payload = this.decodeToken();
    return payload?.user_id || null;
}

// Used in admin checks
checkAdmin<{ is_admin: boolean; user_id: string }>()
```

### Practical Impact

**With `user_id` = "dev-user-123":**
1. **All chats saved** under this user ID
2. **Chat history persists** across browser sessions  
3. **Data isolation** - can't see other users' chats
4. **Feedback tracking** - tied to specific user
5. **Admin features** - controlled by `is_it_admin` flag

**Multi-User Scenarios:**
```powershell
# Different users get separate data spaces
.\documentation\unity-ai-reporting-verify-devTokenURL.ps1 -UserId "alice@gov.bc.ca"
.\documentation\unity-ai-reporting-verify-devTokenURL.ps1 -UserId "bob@gov.bc.ca"  
.\documentation\unity-ai-reporting-verify-devTokenURL.ps1 -UserId "admin@gov.bc.ca" -IsAdmin $true
```

Each `user_id` creates a completely separate workspace with their own chat history, feedback, and access permissions. The application treats this as a proper multi-tenant system where users are fully isolated from each other's data.

## Environment Configuration

### Local Development
- **URL:** `http://localhost/?token=...`
- **JWT_SECRET:** From `applications/.env`
- **Backend:** Flask development server
- **Database:** Local PostgreSQL container

### OpenShift Development
- **URL:** `https://dev-unity-ai-reporting-d18498-dev.apps.silver.devops.gov.bc.ca/?token=...`
- **JWT_SECRET:** Same as local (environment variable)
- **Backend:** Gunicorn production server
- **Database:** OpenShift PostgreSQL

## Security Considerations

### Token Security
- **Shared Secret:** JWT_SECRET must be identical across environments
- **HTTPS Only:** Production URLs use HTTPS to protect tokens in transit
- **URL Cleanup:** Frontend removes token from URL after processing
- **Expiration:** Tokens have configurable expiration (default 24 hours = 1440 minutes)

### Development vs Production
- **Development:** Token in URL parameter acceptable for testing
- **Production:** Should use secure authentication flow (OAuth, SAML, etc.)
- **Token Scope:** Development tokens include admin flags for testing

## Troubleshooting

### Common Issues

**1. "No token found" error:**
- Check if token exists in localStorage: `localStorage.getItem('jwt_token')`
- Verify URL parameter format: `?token=eyJ...`

**2. "Invalid token format" error:**
- Ensure token has 3 parts separated by dots
- Check for URL encoding issues in long tokens

**3. "Token has expired" error:**
- Generate fresh token with PowerShell script
- Check system clock synchronization

**4. Backend validation fails:**
- Verify JWT_SECRET matches between environments
- Check network connectivity to validation endpoint
- Review backend logs for signature validation errors

### Debug Tools

**Browser Console:**
```javascript
// Check stored token
localStorage.getItem('jwt_token')

// Decode payload (client-side only)
JSON.parse(atob('eyJ1c2VyX2lkIjoi...'.split('.')[1]))
```

**Backend Testing:**
```bash
# Health check
curl https://dev-unity-ai-reporting-d18498-dev.apps.silver.devops.gov.bc.ca/health

# Token validation
curl -H "Authorization: Bearer YOUR_TOKEN" \
     -X POST https://dev-unity-ai-reporting-d18498-dev.apps.silver.devops.gov.bc.ca/api/validate-token
```

## Files Referenced

- `documentation/unity-ai-reporting-verify-QueryTokenURL.ps1` - Token generation script (reads from .env)
- `applications/.env` - Environment configuration with JWT_SECRET (required by PowerShell script)
- `Unity.AI.Reporting.Frontend/src/app/services/auth.service.ts` - Frontend authentication logic
- `Unity.AI.Reporting.Frontend/src/app/guards/auth.guard.ts` - Route protection
- `Unity.AI.Reporting.Backend/src/auth.py` - Backend JWT validation
- `Unity.AI.Reporting.Backend/src/api.py` - Authentication endpoints

**Note:** The PowerShell script requires the `applications/.env` file to be present and properly configured with `JWT_SECRET`. The script will automatically locate this file whether run from the applications or documentation folder.