"""
Authentication module for JWT token validation and user management.
"""
import os
import jwt
import logging
from datetime import datetime, timedelta, timezone
from functools import wraps
from flask import request, jsonify
from typing import Optional, Dict, Any, Callable

# Configure logging
logger = logging.getLogger(__name__)


class AuthManager:
    """Handles JWT token creation, validation, and user authentication"""
    
    def __init__(self):
        self.jwt_secret = os.getenv('JWT_SECRET')
        if not self.jwt_secret:
            raise ValueError("JWT_SECRET environment variable is required")
        self.jwt_algorithm = 'HS256'
    
    def create_token(self, user_id: str, tenant_id: str, 
                    expires_in_hours: int = 24) -> str:
        """
        Create a JWT token with user information
        
        Args:
            user_id: Unique user identifier
            tenant_id: Tenant/organization identifier
            expires_in_hours: Token expiration time in hours
            
        Returns:
            JWT token string
        """
        payload = {
            'user_id': user_id,
            'tenant': tenant_id,
            'iat': datetime.now(tz=timezone.utc),
            'exp': datetime.now(tz=timezone.utc) + timedelta(hours=expires_in_hours),
            'jti': f"{user_id}_{int(datetime.now(tz=timezone.utc).timestamp())}"  # Unique token ID
        }
        
        return jwt.encode(payload, self.jwt_secret, algorithm=self.jwt_algorithm)
    
    def validate_token(self, token: str) -> Optional[Dict[str, Any]]:
        """
        Validate a JWT token and extract payload

        Args:
            token: JWT token string

        Returns:
            Token payload if valid, None if invalid
        """
        try:
            # Decode without audience/issuer verification since we don't control token creation
            payload = jwt.decode(
                token,
                self.jwt_secret,
                algorithms=[self.jwt_algorithm],
                options={"verify_aud": False, "verify_iss": False}
            )

            # Map Unity JWT claims to AI Reporting format
            # Unity uses 'sub' (subject) instead of 'user_id'
            if 'sub' in payload and 'user_id' not in payload:
                payload['user_id'] = payload['sub']

            # If tenant is missing, use default
            if 'tenant' not in payload:
                payload['tenant'] = 'default'

            return payload
        except jwt.ExpiredSignatureError:
            logger.warning("Token has expired")
            return None
        except jwt.InvalidTokenError as e:
            logger.warning(f"Invalid token: {e}")
            return None
    
    def extract_token_from_request(self) -> Optional[str]:
        """
        Extract JWT token from request headers
        
        Returns:
            Token string if found, None otherwise
        """
        auth_header = request.headers.get('Authorization')
        if not auth_header:
            return None
        
        # Handle "Bearer <token>" format
        if auth_header.startswith('Bearer '):
            return auth_header.split(' ')[1]
        
        return auth_header
    
    def get_current_user(self) -> Optional[Dict[str, Any]]:
        """
        Get current user information from request token
        
        Returns:
            User payload if authenticated, None otherwise
        """
        token = self.extract_token_from_request()
        if not token:
            return None
        
        return self.validate_token(token)


# Global auth manager instance
auth_manager = AuthManager()


def require_auth(f: Callable) -> Callable:
    """
    Decorator to require JWT authentication for routes
    
    Usage:
        @app.route('/protected')
        @require_auth
        def protected_route():
            return jsonify({'message': 'Access granted'})
    """
    @wraps(f)
    def decorated_function(*args, **kwargs):
        try:
            user_data = auth_manager.get_current_user()
            if not user_data:
                return jsonify({
                    'error': 'Authentication required',
                    'message': 'Valid JWT token must be provided in Authorization header'
                }), 401

            # Validate required fields in token
            required_fields = ['user_id', 'tenant']
            missing_fields = [field for field in required_fields if field not in user_data]
            if missing_fields:
                logger.warning(f"Token missing required fields: {missing_fields}")
                return jsonify({
                    'error': 'Invalid token',
                    'message': f'Token missing required fields: {", ".join(missing_fields)}'
                }), 401

            # Make user data available to the route
            request.current_user = user_data
            return f(*args, **kwargs)

        except Exception as e:
            logger.error(f"Authentication error: {e}", exc_info=True)
            return jsonify({
                'error': 'Authentication failed',
                'message': 'An error occurred during authentication'
            }), 401
    
    return decorated_function


def optional_auth(f: Callable) -> Callable:
    """
    Decorator for routes where authentication is optional
    Sets request.current_user to None if no valid token provided
    
    Usage:
        @app.route('/maybe-protected')
        @optional_auth
        def maybe_protected_route():
            if request.current_user:
                return jsonify({'message': f'Hello {request.current_user["user_id"]}'})
            else:
                return jsonify({'message': 'Hello anonymous user'})
    """
    @wraps(f)
    def decorated_function(*args, **kwargs):
        user_data = auth_manager.get_current_user()
        request.current_user = user_data
        return f(*args, **kwargs)
    
    return decorated_function


def get_user_from_token() -> Optional[Dict[str, Any]]:
    """
    Helper function to get current user data from request
    
    Returns:
        User data dict if authenticated, None otherwise
    """
    return getattr(request, 'current_user', None)