"""
Authentication module for JWT token validation and user management.
"""
import os
import jwt
import logging
import warnings

# Suppress PyJWT's InsecureKeyLengthWarning - key length is enforced at deployment (64+ chars required)
warnings.filterwarnings("ignore", message=".*key.*length.*", category=UserWarning)
from datetime import datetime, timedelta, timezone
from functools import wraps
from flask import abort, g, request, jsonify
from typing import Optional, Dict, Any, Callable
import ai_models
from tenant_registry import TenantEntry, parse_schema_types, tenant_registry

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

            # No fallback tenant: a token without 'tenant' is rejected by
            # require_auth (AB#33978).
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

            # Validate required fields in token. mb_db_id / mb_collection_id come
            # from UGM (the tenant's Metabase database and collection, AB#33978); the
            # ai_* claims are AI Setup's AI Reporting models and API version (AB#35030).
            required_fields = ['user_id', 'tenant', 'mb_db_id', 'mb_collection_id']
            missing_fields = [field for field in required_fields if not user_data.get(field)]
            if missing_fields:
                logger.warning(f"Token missing required fields: {missing_fields}")
                return jsonify({
                    'error': 'Invalid token',
                    'message': f'Token missing required fields: {", ".join(missing_fields)}'
                }), 401

            missing_models = [field for field in AI_MODEL_CLAIMS if not user_data.get(field)]
            if missing_models:
                logger.warning(f"Token missing AI Setup model claims: {missing_models}")
                return jsonify({
                    'error': 'AI Reporting not set up',
                    'message': "AI Reporting's models are not set in AI Setup. Ask IT Operations to set them."
                }), 401

            entry = tenant_entry_from_token(user_data)
            if entry is None or entry.models is None:
                logger.warning("Token has invalid Metabase ids or AI Setup model claims")
                return jsonify({
                    'error': 'Invalid token',
                    'message': 'Token has invalid Metabase ids or AI Reporting models'
                }), 401

            # Record the tenant (and its models) so token-less code (embed-all, /ready) knows it.
            tenant_registry.register(entry)

            # Make user data and AI Setup's models available to the route
            g.current_user = user_data
            g.tenant_entry = entry
            ai_models.set_current(entry.models)
            return f(*args, **kwargs)

        except Exception as e:
            logger.exception(f"Authentication error: {e}")
            return jsonify({
                'error': 'Authentication failed',
                'message': 'An error occurred during authentication'
            }), 401
    
    return decorated_function


def optional_auth(f: Callable) -> Callable:
    """
    Decorator for routes where authentication is optional
    Sets g.current_user to None if no valid token provided
    
    Usage:
        @app.route('/maybe-protected')
        @optional_auth
        def maybe_protected_route():
            if g.current_user:
                return jsonify({'message': f'Hello {g.current_user["user_id"]}'})
            else:
                return jsonify({'message': 'Hello anonymous user'})
    """
    @wraps(f)
    def decorated_function(*args, **kwargs):
        user_data = auth_manager.get_current_user()
        g.current_user = user_data
        return f(*args, **kwargs)
    
    return decorated_function


def tenant_entry_from_token(user_data: Dict[str, Any]) -> Optional[TenantEntry]:
    """Build the tenant's registry entry from the token claims, or None if the ids aren't integers."""
    try:
        db_id = int(str(user_data['mb_db_id']).strip())
        collection_id = int(str(user_data['mb_collection_id']).strip())
    except (KeyError, TypeError, ValueError):
        return None
    if db_id <= 0 or collection_id <= 0:
        return None
    return TenantEntry(
        tenant_name=str(user_data['tenant']),
        db_id=db_id,
        collection_id=collection_id,
        schema_types=parse_schema_types(user_data.get('schema_types')),
        chat_deployment=_claim_or_none(user_data, 'ai_chat_deployment'),
        chat_api_version=_claim_or_none(user_data, 'ai_chat_api_version'),
        embedding_deployment=_claim_or_none(user_data, 'ai_embedding_deployment'),
        embedding_api_version=_claim_or_none(user_data, 'ai_embedding_api_version'),
    )


# AI Setup's AI Reporting models, each with its API version, sent by UGM (AB#35030).
AI_MODEL_CLAIMS = ('ai_chat_deployment', 'ai_chat_api_version',
                   'ai_embedding_deployment', 'ai_embedding_api_version')


def _claim_or_none(user_data: Dict[str, Any], name: str) -> Optional[str]:
    value = str(user_data.get(name) or '').strip()
    return value or None


def get_user_from_token() -> Dict[str, Any]:
    """
    Helper function to get current user data from request.
    Must be called from endpoints decorated with @require_auth.

    Returns:
        User data dict from the authenticated token
    """
    user_data = getattr(g, 'current_user', None)
    if not user_data:
        abort(401, description="User data not found in token")
    return user_data


def _claim_is_true(value: Any) -> bool:
    """
    Permission claims minted by GrantManager arrive as the strings 'true'/'false'
    (C# bool.ToString().ToLower()). A naive bool() is wrong here since bool("false")
    is True in Python, so compare against the literal string.
    """
    return str(value).strip().lower() == "true"


def require_data_model_permission(f: Callable) -> Callable:
    """
    Decorator to require the can_edit_data_model permission claim.

    Must be layered AFTER @require_auth so that g.current_user is already populated;
    this reads the already-decoded payload and does not re-decode or re-validate the
    token.

    Usage:
        @app.route('/api/data-models/create', methods=['POST'])
        @require_auth
        @require_data_model_permission
        def create_data_models():
            ...
    """
    @wraps(f)
    def decorated_function(*args, **kwargs):
        user_data = get_user_from_token()
        if not _claim_is_true(user_data.get("can_edit_data_model")):
            return jsonify({
                "error": "permission_denied",
                "message": "You do not have permission to use the Data Model feature."
            }), 403
        return f(*args, **kwargs)

    return decorated_function