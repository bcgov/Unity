"""
API module with Flask routes for the application.
"""
from flask import Flask, request, abort, jsonify
from flask_cors import CORS
import asyncio
from config import config
from database import db_manager, chat_repository, feedback_repository
from metabase import metabase_client
from chat import chat_manager
from sql_generator import sql_generator
from auth import require_auth, get_user_from_token
import re


def _sanitize_field_array(field_array):
    """
    Sanitize field arrays to prevent XSS attacks.
    Only allows alphanumeric characters, underscores, dots, and hyphens.
    """
    if not isinstance(field_array, list):
        return []
    
    sanitized = []
    for field in field_array:
        if isinstance(field, str):
            # Allow only safe characters: letters, numbers, underscore, dot, hyphen
            if re.match(r'^[a-zA-Z0-9_.-]+$', field) and len(field) <= 100:
                sanitized.append(field)
        elif isinstance(field, (int, float)):
            # Allow numeric values
            sanitized.append(field)
    
    return sanitized


def _sanitize_mode(mode):
    """
    Sanitize visualization mode parameter.
    Only allows predefined safe visualization types.
    """
    if not isinstance(mode, str):
        return None
    
    # Whitelist of allowed visualization modes
    allowed_modes = ['table', 'bar', 'line', 'pie', 'map', 'scatter', 'area', 'column']
    
    # Clean the mode string and check against whitelist
    clean_mode = mode.strip().lower()
    if clean_mode in allowed_modes and len(clean_mode) <= 20:
        return clean_mode
    
    return None


def _sanitize_card_id(card_id):
    """
    Sanitize card ID parameter.
    Ensures it's a positive integer within reasonable bounds.
    """
    try:
        # Convert to int if it's a string representation
        if isinstance(card_id, str):
            card_id = int(card_id)
        
        # Validate it's a positive integer within reasonable bounds
        if isinstance(card_id, int) and 1 <= card_id <= 999999999:
            return card_id
            
    except (ValueError, TypeError):
        pass
    
    return None


def create_app():
    """Create and configure Flask application"""
    app = Flask(__name__)
    CORS(app)
    
    # Configure Flask based on environment
    app.config['DEBUG'] = config.app.debug
    app.config['TESTING'] = config.app.testing
    
    return app


app = create_app()


@app.route("/")
def health_check():
    """Health check endpoint"""
    return "Backend is working!"


@app.route("/health")
def health():
    """
    Health endpoint for monitoring - basic liveness check
    Returns 200 if the service is running
    """
    return jsonify({
        "status": "healthy",
        "service": "unity-ai-backend",
        "version": "1.0.0"
    }), 200


@app.route("/ready")
def ready():
    """
    Readiness endpoint for deployment - checks dependencies
    Returns 200 if the service is ready to serve requests
    """
    try:
        # Check database connection
        db_status = "healthy"
        try:
            # Test database connection
            db_manager.get_connection()
            db_status = "healthy"
        except Exception as e:
            db_status = "unhealthy"
            
        # Check JWT secret configuration
        jwt_status = "healthy"
        try:
            from auth import auth_manager
            if not auth_manager.jwt_secret:
                jwt_status = "unhealthy: JWT_SECRET not configured"
        except Exception as e:
            jwt_status = "unhealthy"
            
        # Check environment configuration
        config_status = "healthy"
        try:
            # Test basic config access
            config.app.debug  # This will fail if config is broken
        except Exception as e:
            config_status = "unhealthy"
            
        # Determine overall readiness
        all_healthy = all(
            status == "healthy" 
            for status in [db_status, jwt_status, config_status]
        )
        
        response_data = {
            "status": "ready" if all_healthy else "not ready",
            "service": "unity-ai-backend",
            "version": "1.0.0",
            "checks": {
                "database": db_status,
                "jwt_auth": jwt_status,
                "configuration": config_status
            }
        }
        
        return jsonify(response_data), 200 if all_healthy else 503
        
    except Exception as e:
        return jsonify({
            "status": "not ready",
            "service": "unity-ai-backend",
            "version": "1.0.0"
        }), 503


@app.route("/api/validate-token", methods=["POST"])
@require_auth
def validate_token():
    """
    Validate JWT token and return user information
    """
    user_data = get_user_from_token()
    return jsonify({
        "valid": True,
        "user_id": user_data["user_id"],
        "tenant_id": user_data["tenant"],
        "metabase_url": user_data["mb_url"],
        "expires": user_data["exp"]
    }), 200

@app.route("/api/check-admin", methods=["POST"])
@require_auth
def check_admin():
    """
    Check if the current user has admin privileges
    Returns admin status without exposing JWT contents to frontend
    """
    user_data = get_user_from_token()

    # Extract is_it_admin from JWT payload
    is_admin = user_data.get("is_it_admin", False)

    return jsonify({
        "is_admin": bool(is_admin),
        "user_id": user_data["user_id"]
    }), 200


@app.route("/api/admin/feedback", methods=["GET"])
@require_auth
def get_feedback_for_admin():
    """
    Get all feedback entries for admin review
    Requires admin privileges
    """
    user_data = get_user_from_token()

    # Check if user is admin
    is_admin = user_data.get("is_it_admin", False)
    if not is_admin:
        return jsonify({"error": "Admin privileges required"}), 403

    # Get pagination parameters
    try:
        limit = int(request.args.get('limit', 100))
        offset = int(request.args.get('offset', 0))

        # Ensure reasonable limits
        limit = min(limit, 1000)  # Max 1000 entries per request
        offset = max(offset, 0)   # No negative offsets

    except ValueError:
        return jsonify({"error": "Invalid pagination parameters"}), 400

    try:
        feedback_list = feedback_repository.get_all_feedback(limit=limit, offset=offset)
        return jsonify({
            "feedback": feedback_list,
            "limit": limit,
            "offset": offset,
            "count": len(feedback_list)
        }), 200

    except Exception as e:
        print(f"Error retrieving feedback: {e}")
        return jsonify({"error": "Failed to retrieve feedback"}), 500

@app.route("/api/ask", methods=["POST"])
@require_auth
def ask():
    """
    Main endpoint for processing natural language queries.
    Generates SQL and creates Metabase cards.
    """
    data = request.get_json()
    user_data = get_user_from_token()
    
    async def async_ask():
        question = data.get("question")
        conversation = data.get("conversation", [])
        
        # Extract user context from JWT token
        metabase_url = user_data["mb_url"]
        tenant_id = user_data["tenant"]
        
        if not question:
            return abort(400, "question is required")
        
        # Get tenant configuration
        tenant_config = config.get_tenant_config(tenant_id)
        db_id = tenant_config["db_id"]
        collection_id = tenant_config["collection_id"]
        
        print(f"Request - Tenant: {tenant_id}, DB: {db_id}, Collection: {collection_id}")
        
        try:
            # Extract past questions from conversation
            past_questions = chat_manager.extract_past_questions(conversation)
            print(f"Extracted {len(past_questions)} past questions")
            
            # Generate SQL from natural language
            print("Starting SQL generation...")
            try:
                sql, metadata = await sql_generator.generate_sql(
                    question, past_questions, db_id
                )
                print(f"SQL generation completed. SQL exists: {bool(sql)}, Metadata exists: {bool(metadata)}")
            except Exception as sql_error:
                print(f"SQL generation failed with error: {sql_error}")
                print(f"Error type: {type(sql_error)}")
                # Check if it's a rate limit or connection error
                if "429" in str(sql_error) or "rate limit" in str(sql_error).lower():
                    print("Azure OpenAI rate limit exceeded")
                    return jsonify({
                        "error": "Rate limit exceeded", 
                        "message": "Azure OpenAI rate limit exceeded. Please try again in a few moments.",
                        "url": "rate_limit"
                    }), 429
                elif "Connection aborted" in str(sql_error) or "RemoteDisconnected" in str(sql_error):
                    print("Connection error during SQL generation")
                    return jsonify({
                        "error": "Connection error",
                        "message": "Connection error during SQL generation. Please try again.",
                        "url": "connection_error"
                    }), 503
                else:
                    print("Unknown error during SQL generation")
                    return jsonify({
                        "error": "SQL generation failed",
                        "message": "Unable to generate SQL query. Please try again.",
                        "url": "fail"
                    }), 500
            
            if not sql or not metadata:
                print("SQL generation failed - returning fail response")
                return {"url": "fail", "card_id": 0, "x_field": "", "y_field": ""}, 200
            
            print(f"SQL: {sql}")
            print(f"Metadata: {metadata}")
            print("About to create Metabase card...")
            print(f"Metabase client type: {type(metabase_client)}")
            
            # Create Metabase card
            print(f"Creating Metabase card with SQL length: {len(sql)}")
            try:
                print("Calling metabase_client.create_card...")
                card_id = metabase_client.create_card(
                    sql, db_id, collection_id, metadata['title']
                )
                print(f"Card created successfully with ID: {card_id}")
            except Exception as card_error:
                print(f"Error during card creation: {card_error}")
                print(f"Card error type: {type(card_error)}")
                raise card_error
            
            # Generate embed URL
            print("Generating embed URL...")
            try:
                print("Calling metabase_client.generate_embed_url...")
                embed_url = metabase_client.generate_embed_url(card_id)
                print(f"Embed URL generated successfully: {embed_url[:50]}...")
            except Exception as embed_error:
                print(f"Error during embed URL generation: {embed_error}")
                print(f"Embed error type: {type(embed_error)}")
                raise embed_error
            
        except Exception as step_error:
            print(f"Error in specific step: {step_error}")
            raise step_error
        
        return {
            "url": embed_url,
            "card_id": card_id,
            "x_field": metadata.get('x_axis', []),
            "y_field": metadata.get('y_axis', []),
            "title": metadata.get("title", "Untitled"),
            "visualization_options": metadata.get('visualization_options', []),
            "SQL": sql
        }, 200
    
    try:
        return asyncio.run(async_ask())
    except Exception as e:
        print(f"Error in /api/ask: {e}")
        return abort(500, "Internal server error")


@app.route("/api/change_display", methods=["POST"])
@require_auth
def change_display():
    """Update visualization type for a Metabase card"""
    data = request.get_json()
    
    try:
        mode = data.get("mode")
        card_id = data.get("card_id")
        x_field = data.get("x_field", [])
        y_field = data.get("y_field", [])
        visualization_options = data.get("visualization_options", [])
        
        if not all([mode, card_id]):
            return abort(400, "mode and card_id are required")
        
        # Validate and sanitize input
        safe_mode = _sanitize_mode(mode)
        if not safe_mode:
            return abort(400, "Invalid mode parameter")
        
        safe_card_id = _sanitize_card_id(card_id)
        if not safe_card_id:
            return abort(400, "Invalid card_id parameter")
        
        # Ensure field arrays contain only safe values
        safe_x_field = _sanitize_field_array(x_field) if isinstance(x_field, list) else []
        safe_y_field = _sanitize_field_array(y_field) if isinstance(y_field, list) else []
        safe_visualization_options = _sanitize_field_array(visualization_options) if isinstance(visualization_options, list) else []
        
        # Update card visualization
        metabase_client.update_card_visualization(safe_card_id, safe_mode, safe_x_field, safe_y_field)
        
        # Generate new embed URL
        embed_url = metabase_client.generate_embed_url(safe_card_id)
        
        return jsonify({
            "url": embed_url,
            "card_id": safe_card_id,
            "x_field": safe_x_field,
            "y_field": safe_y_field,
            "visualization_options": safe_visualization_options
        }), 200
        
    except Exception as e:
        print(f"Error in /api/change_display: {e}")
        return abort(500, "Internal server error")


@app.route("/api/delete", methods=["POST"])
@require_auth
def delete_question():
    """Delete a Metabase card"""
    data = request.get_json()
    
    try:
        card_id = data.get("card_id")
        
        if not card_id:
            return abort(400, "card_id is required")
        
        success = metabase_client.delete_card(card_id)
        return {"success": success}
        
    except Exception as e:
        print(f"Error in /api/delete: {e}")
        return {"success": False}


@app.route("/api/explain_sql", methods=["POST"])
@require_auth
def explain_sql():
    """Generate a user-friendly explanation for SQL query"""
    data = request.get_json()
    
    async def async_explain_sql():
        sql = data.get("sql")
        
        if not sql:
            return abort(400, "sql is required")
        
        # Generate explanation using the sql_generator
        explanation = await sql_generator.explain_sql(sql)
        
        return {
            "explanation": explanation
        }, 200
    
    try:
        return asyncio.run(async_explain_sql())
    except Exception as e:
        print(f"Error in /api/explain_sql: {e}")
        return {
            "explanation": "This query retrieves and analyzes your data."
        }, 200


# Chat management endpoints

@app.route("/api/chats", methods=["POST"])
@require_auth
def get_chats():
    """Get all chats for a user"""
    data = request.get_json()
    user_data = get_user_from_token()
    
    try:
        # Extract user context from JWT token
        user_id = user_data["user_id"]
        tenant_id = user_data["tenant"]
        
        chats = chat_manager.get_user_chats(user_id, tenant_id)
        return jsonify(chats), 200
        
    except Exception as e:
        print(f"Error getting chats: {e}")
        return abort(500, "Internal server error")


@app.route("/api/chats/<chat_id>", methods=["POST"])
@require_auth
def get_chat(chat_id):
    """Get a specific chat and validate/recreate cards"""
    user_data = get_user_from_token()
    
    try:
        # Extract user ID from JWT token
        user_id = user_data["user_id"]
        
        chat_data = chat_manager.get_chat_with_card_validation(chat_id, user_id)
        
        if not chat_data:
            return abort(404, "Chat not found")
        
        return jsonify(chat_data), 200
        
    except Exception as e:
        print(f"Error getting chat: {e}")
        return abort(500, "Internal server error")


@app.route("/api/chats/save", methods=["POST"])
@require_auth
def save_chat():
    """Save or update a chat"""
    data = request.get_json()
    user_data = get_user_from_token()
    
    try:
        # Extract user context from JWT token
        user_id = user_data["user_id"]
        tenant_id = user_data["tenant"]
        metabase_url = user_data["mb_url"]
        
        chat_id = data.get("chat_id")
        title = data.get("title")
        conversation = data.get("conversation")
        
        if not all([title, conversation]):
            return abort(400, "title and conversation are required")
        
        result_chat_id = chat_manager.save_chat(
            user_id, tenant_id, metabase_url, title, conversation, chat_id
        )
        
        return {"chat_id": result_chat_id}, 200
        
    except Exception as e:
        print(f"Error saving chat: {e}")
        return abort(500, "Internal server error")


@app.route("/api/chats/<chat_id>", methods=["DELETE"])
@require_auth
def delete_chat(chat_id):
    """Delete a chat"""
    data = request.get_json()
    user_data = get_user_from_token()
    
    try:
        # Extract user ID from JWT token
        user_id = user_data["user_id"]
        
        success = chat_manager.delete_chat(chat_id, user_id)
        
        if not success:
            return abort(404, "Chat not found")
        
        return {"success": True}, 200
        
    except Exception as e:
        print(f"Error deleting chat: {e}")
        return abort(500, "Internal server error")


# Feedback endpoints

@app.route("/api/feedback", methods=["POST"])
@require_auth
def submit_feedback():
    """Submit feedback/bug report for a chat"""
    data = request.get_json()
    user_data = get_user_from_token()
    
    try:
        # Extract user context from JWT token
        user_id = user_data["user_id"]
        tenant_id = user_data["tenant"]
        
        # Get request data
        chat_id = data.get("chat_id")
        feedback_type = data.get("feedback_type", "bug_report")
        message = data.get("message", "").strip()
        user_agent = request.headers.get("User-Agent")

        current_question = data.get("current_question")
        current_sql = data.get("current_sql")
        current_sql_explanation = data.get("current_sql_explanation")
        previous_question = data.get("previous_question")
        previous_sql = data.get("previous_sql")
        previous_sql_explanation = data.get("previous_sql_explanation")
        
        # Get metadata from request
        metadata = {
            "timestamp": data.get("timestamp"),
            "frontend_version": data.get("frontend_version"),
            "browser_info": {
                "user_agent": user_agent,
                "ip_address": request.remote_addr
            }
        }
        
        if not chat_id:
            return abort(400, "chat_id is required")
        
        # Validate that the chat exists and belongs to the user
        chat_data = chat_repository.get_chat(chat_id, user_id)
        if not chat_data:
            return abort(404, "Chat not found")
        
        # Submit feedback
        feedback_id = feedback_repository.submit_feedback(
            chat_id=chat_id,
            user_id=user_id,
            tenant_id=tenant_id,
            feedback_type=feedback_type,
            message=message,
            user_agent=user_agent,
            metadata=metadata,
            current_question = data.get("current_question")
            current_sql = data.get("current_sql")
            current_sql_explanation = data.get("current_sql_explanation")
            previous_question = data.get("previous_question")
            previous_sql = data.get("previous_sql")
            previous_sql_explanation = data.get("previous_sql_explanation")
        )
        
        print(f"Feedback submitted: {feedback_id} for chat {chat_id} by user {user_id}")
        
        return {
            "success": True,
            "feedback_id": feedback_id,
            "message": "Feedback submitted successfully"
        }, 200
        
    except Exception as e:
        print(f"Error submitting feedback: {e}")
        return abort(500, "Internal server error")


@app.route("/api/feedback/<feedback_id>", methods=["GET"])
@require_auth
def get_feedback(feedback_id):
    """Get a specific feedback entry (for admin use)"""
    user_data = get_user_from_token()
    
    try:
        feedback_data = feedback_repository.get_feedback(feedback_id)
        
        if not feedback_data:
            return abort(404, "Feedback not found")
        
        # Only allow users to see their own feedback
        if feedback_data["user_id"] != user_data["user_id"]:
            return abort(403, "Access denied")
        
        return jsonify(feedback_data), 200
        
    except Exception as e:
        print(f"Error getting feedback: {e}")
        return abort(500, "Internal server error")


@app.route("/api/chats/<chat_id>/feedback", methods=["GET"])
@require_auth
def get_chat_feedback(chat_id):
    """Get all feedback for a specific chat"""
    user_data = get_user_from_token()
    
    try:
        # Validate that the chat exists and belongs to the user
        user_id = user_data["user_id"]
        chat_data = chat_repository.get_chat(chat_id, user_id)
        if not chat_data:
            return abort(404, "Chat not found")
        
        feedback_list = feedback_repository.get_feedback_by_chat(chat_id)
        return jsonify(feedback_list), 200
        
    except Exception as e:
        print(f"Error getting chat feedback: {e}")
        return abort(500, "Internal server error")

