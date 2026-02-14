"""
Static file serving for Angular frontend.
This module adds routes to serve the Angular static files from Flask.
"""
from flask import send_from_directory, send_file
import os
import logging

logger = logging.getLogger(__name__)

# Path to frontend static files (Angular 17+ uses browser subdirectory)
FRONTEND_DIR = '/app/frontend'
# Check if browser subdirectory exists (Angular 17+)
if os.path.exists(os.path.join(FRONTEND_DIR, 'browser')):
    FRONTEND_DIR = os.path.join(FRONTEND_DIR, 'browser')


def _is_path_traversal(path):
    """Security: Check if a path attempts directory traversal."""
    return '..' in path or path.startswith('/')


def _try_serve_file(path):
    """Try to serve a specific static file, returning a response or None."""
    try:
        # Verify the resolved path is within FRONTEND_DIR
        # send_from_directory has built-in path traversal protection
        requested_path = os.path.join(FRONTEND_DIR, path)
        real_path = os.path.realpath(requested_path)
        real_frontend = os.path.realpath(FRONTEND_DIR)

        if real_path.startswith(real_frontend) and os.path.isfile(real_path):
            return send_from_directory(FRONTEND_DIR, path)
    except (OSError, ValueError) as e:
        logger.warning(f"Error serving file {path}: {e}")
        # Fall through to serve index.html
    return None


def _serve_index():
    """Serve index.html for Angular client-side routing."""
    index_path = os.path.join(FRONTEND_DIR, 'index.html')
    if os.path.exists(index_path):
        return send_file(index_path)
    return f"Error: index.html not found at {index_path}", 500


def add_static_routes(app):
    """Add routes to serve static frontend files"""

    @app.route('/', defaults={'path': ''})
    @app.route('/<path:path>')
    def serve_frontend(path):
        """
        Serve Angular frontend files.
        Falls back to index.html for client-side routing.
        """
        # Don't serve /api routes - those are handled by API endpoints
        if path.startswith('api/'):
            return None  # Let Flask handle 404

        # Security: Validate path to prevent directory traversal
        if path and _is_path_traversal(path):
            logger.warning(f"Rejected potentially malicious path: {path}")
            return "Invalid path", 400

        # Try to serve the requested file
        if path:
            response = _try_serve_file(path)
            if response:
                return response

        # For Angular routes, always serve index.html
        return _serve_index()

    logger.info(f"Static file routes configured. Serving from: {FRONTEND_DIR}")
