"""
API module with Flask routes for the application.
"""
from flask import Flask, request, abort, jsonify
from flask_cors import CORS
import asyncio
import aiohttp
import logging
import datetime
from config import config
from database import db_manager, chat_repository, feedback_repository, cache_repository
from metabase import metabase_client
from chat import chat_manager
from sql_generator import sql_generator
from model_generator import data_model_generator
from auth import require_auth, get_user_from_token, require_data_model_permission
from embeddings import embedding_manager
from static_routes import add_static_routes
import cache_reranker
import os
import re

# Define constants
ADMIN_PRIVILEGES_REQUIRED = "Admin privileges required"
INTERNAL_SERVER_ERROR = "Internal server error"
CHAT_NOT_FOUND = "Chat not found"

# Configure logging
logger = logging.getLogger(__name__)
logging.basicConfig(
    level=logging.INFO if config.app.flask_env == "production" else logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)

# Suppress verbose urllib3 connection logs
logging.getLogger('urllib3.connectionpool').setLevel(logging.WARNING)


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
        pass  # Invalid card_id — fall through to return None

    return None


def _error_response(error_type, message, status, detail=None):
    """Build a consistent error JSON response.

    Schema:
        error_type  – machine-readable key (rate_limit | connection_error | ai_failure | server_error)
        message     – user-facing text
        detail      – optional developer-facing detail (not shown to users)
    """
    body = {"error_type": error_type, "message": message}
    if detail:
        body["detail"] = detail
    return jsonify(body), status


def _classify_sql_generation_error(error):
    """Classify an SQL generation error and return an appropriate (response, status) tuple."""
    error_str = str(error)
    error_lower = error_str.lower()
    logger.error(f"SQL generation failed with error: {error}", exc_info=True)
    logger.debug(f"Error type: {type(error)}")

    # Check if it's a rate limit
    if "429" in error_str or "rate limit" in error_lower:
        logger.warning("Azure OpenAI rate limit exceeded")
        return _error_response(
            "rate_limit",
            "Azure OpenAI rate limit exceeded. Please try again in a few moments.",
            429,
            detail=error_str
        )

    # Check if it's a connection / network / config error
    connection_keywords = [
        "connection aborted", "remotedisconnected", "connection error",
        "connectionerror", "invalidurl", "invalid url", "name or service not known",
        "nodename nor servname", "getaddrinfo failed", "timeout",
        "cannot connect", "connection refused", "unreachable"
    ]
    if any(kw in error_lower for kw in connection_keywords):
        logger.error("Connection error during SQL generation")
        return _error_response(
            "connection_error",
            "Connection error during SQL generation. Please try again.",
            503,
            detail=error_str
        )

    # Check if it's an authentication/authorization error (e.g. missing/invalid Azure OpenAI API key)
    if "401" in error_str or "unauthorized" in error_lower or "403" in error_str or "forbidden" in error_lower:
        logger.error("Azure OpenAI authentication/authorization error - check API key configuration")
        return _error_response(
            "server_error",
            "Service configuration error. Please contact support.",
            503,
            detail=error_str
        )

    logger.error("Unknown error during SQL generation")
    return _error_response(
        "server_error",
        "Something went wrong during SQL generation. Please try again.",
        500,
        detail=error_str
    )


def create_app():
    """Create and configure Flask application"""
    app = Flask(__name__)
    CORS(app)
    
    # Configure Flask based on environment
    app.config['DEBUG'] = config.app.debug
    app.config['TESTING'] = config.app.testing
    
    return app


app = create_app()

# Add static file serving for Angular frontend
add_static_routes(app)


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
        try:
            # Test database connection
            db_manager.get_connection()
            db_status = "healthy"
        except Exception:
            db_status = "unhealthy"
            
        # Check JWT secret configuration
        jwt_status = "healthy"
        try:
            from auth import auth_manager
            if not auth_manager.jwt_secret:
                jwt_status = "unhealthy: JWT_SECRET not configured"
        except Exception:
            jwt_status = "unhealthy"
            
        # Check environment configuration
        config_status = "healthy"
        try:
            # Test basic config access
            config.app.debug  # This will fail if config is broken
        except Exception:
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
        
    except Exception:
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


@app.route("/api/iframe-origins", methods=["GET"])
def get_iframe_origins():
    """
    Get allowed iframe origins from environment variable
    This endpoint does not require authentication as it's used for security validation
    """
    import os
    
    # Get ORIGIN_URL from environment variable
    origin_url = os.environ.get("ORIGIN_URL", "")
    
    if not origin_url:
        return jsonify({"iframe_origins": []}), 200
    
    # Parse comma-separated origins
    origins = [origin.strip() for origin in origin_url.split(",") if origin.strip()]
    
    return jsonify({"iframe_origins": origins}), 200


@app.route("/api/auth-debug", methods=["GET"])
def auth_debug():
    """
    Debug endpoint for authentication troubleshooting
    Returns environment and configuration info (no auth required)
    """
    import os
    
    debug_info = {
        "origin_url_env": os.environ.get("ORIGIN_URL", "NOT_SET"),
        "hostname": os.environ.get("HOSTNAME", "unknown"),
        "environment": os.environ.get("FLASK_ENV", "unknown"),
        "timestamp": datetime.datetime.now().isoformat()
    }
    
    return jsonify(debug_info), 200


@app.route("/api/metabase-url", methods=["GET"])
@require_auth
def get_metabase_url():
    """
    Get the Metabase URL from server configuration
    Returns the configured Metabase embed URL for the user's tenant
    """

    metabase_url = config.metabase.url

    if not metabase_url:
        return jsonify({"error": "Metabase URL not configured"}), 404

    return jsonify({
        "metabase_url": metabase_url
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
        return jsonify({"error": ADMIN_PRIVILEGES_REQUIRED}), 403

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
        logger.error(f"Error retrieving feedback: {e}", exc_info=True)
        return jsonify({"error": "Failed to retrieve feedback"}), 500

def _build_viz_settings(visualization_options: list) -> dict:
    """Return Metabase visualization settings dict for the given options list."""
    if "map" in visualization_options:
        return {"map.region": config.metabase.map_region_uuid}
    return {}


def _shape_card_data(card_data, limit=None):
    """Turn Metabase's `{cols, rows}` payload into the frontend preview shape.

    Truncates rows to `limit` (defaults to `config.app.preview_row_limit` when
    None — the data-model preview passes its own smaller limit). Returns None when
    the payload is missing or malformed so the frontend can fall back to button-only.
    """
    if not isinstance(card_data, dict):
        return None
    cols = card_data.get("cols")
    rows = card_data.get("rows")
    if not isinstance(cols, list) or not isinstance(rows, list):
        return None

    limit = config.app.preview_row_limit if limit is None else limit
    columns = [
        (c.get("display_name") or c.get("name") or "") if isinstance(c, dict) else str(c)
        for c in cols
    ]
    total_rows = len(rows)
    return {
        "columns": columns,
        "rows": rows[:limit],
        "total_rows": total_rows,
        "truncated": total_rows > limit,
    }


def _attach_preview_to_proposal(proposal, db_id, tenant_id):
    """Enrich a model proposal with real columns + a sample row from Metabase.

    Reads the authoritative columns and a one-row sample (the same source the
    frontend table renders), replacing the generator's regex-inferred `columns`.
    This is what fixes added columns being missing and bogus `...` headers in the
    review preview.

    The generator already executed the validated SQL once (bounded) and stashes the
    raw `{cols, rows}` payload in the private `_preview_raw` key — reuse it instead of
    running the query a second time. Falls back to executing once if that key is
    absent (e.g. a path that didn't run the SQL). On invalid SQL or any failure, the
    generator's existing `columns` are kept and `preview_data` is None.
    """
    raw = proposal.pop("_preview_raw", None)
    proposal["preview_data"] = None

    if raw is None:
        # No pre-fetched rows — only execute if the SQL validated.
        if not proposal.get("valid") or not proposal.get("sql"):
            return proposal
        try:
            raw = metabase_client.execute_sql(
                proposal["sql"], db_id, tenant_id,
                max_rows=config.app.data_model_preview_row_limit,
            )
        except Exception as e:
            logger.warning("Could not attach preview data to proposal: %s", e)
            return proposal

    shaped = _shape_card_data(raw, limit=config.app.data_model_preview_row_limit)
    if shaped is not None:
        proposal["columns"] = shaped["columns"]
        proposal["preview_data"] = shaped
    return proposal


def _fuzzy_cache_lookup(tenant_id, db_id, schema_types, collection_name, normalized_query):
    """Layer 1.5: rapidfuzz match against recent normalized queries."""
    recent = cache_repository.get_recent_normalized_queries(
        tenant_id, db_id, schema_types, collection_name, config.app.fuzzy_match_limit
    )
    fuzzy_match = cache_reranker.fuzzy_matcher.find_best(
        normalized_query, recent, config.app.fuzzy_match_threshold
    )
    if not fuzzy_match:
        return None
    cache_hit = cache_repository.find_exact(
        tenant_id, db_id, schema_types, collection_name, fuzzy_match["normalized_query"]
    )
    if cache_hit:
        cache_hit["hit_type_override"] = "fuzzy_hit"
        logger.info(
            f"[cache:fuzzy_hit] tenant={tenant_id} db={db_id} score={fuzzy_match['score']:.1f}"
        )
    return cache_hit


async def _llm_judge_lookup(tenant_id, db_id, normalized_query, borderline):
    """Run LLM judge over borderline candidates in parallel; return the best hit or None."""
    borderline_sims = ', '.join(f"{c['similarity']:.4f}" for c in borderline)
    logger.info(
        f"[cache:borderline] tenant={tenant_id} db={db_id} "
        f"count={len(borderline)} similarities=[{borderline_sims}]"
    )
    async with aiohttp.ClientSession() as judge_session:
        results = await asyncio.gather(*[
            cache_reranker.llm_judge.score_candidate(
                normalized_query, candidate["query_text"], judge_session, config.ai
            )
            for candidate in borderline
        ])

    best_candidate, best_score = None, -1  # sentinel; valid scores are 0..10
    for candidate, (score, judge_tokens) in zip(borderline, results):
        logger.info(
            f"[cache:llm_judge] tenant={tenant_id} db={db_id} "
            f"similarity={candidate['similarity']:.4f} score={score} tokens={judge_tokens}"
        )
        is_better_score = score > best_score
        is_tiebreak = (score == best_score and best_candidate is not None
                       and candidate["similarity"] > best_candidate["similarity"])
        if is_better_score or is_tiebreak:
            best_score, best_candidate = score, candidate

    if best_candidate is not None and best_score >= config.app.llm_judge_score_threshold:
        best_candidate["hit_type_override"] = "llm_judge_hit"
        logger.info(
            f"[cache:llm_judge_selected] tenant={tenant_id} db={db_id} "
            f"score={best_score} similarity={best_candidate['similarity']:.4f} "
            f"threshold={config.app.llm_judge_score_threshold}"
        )
        return best_candidate

    logger.info(
        f"[cache:llm_judge_miss] tenant={tenant_id} db={db_id} "
        f"best_score={best_score} threshold={config.app.llm_judge_score_threshold}"
    )
    return None


async def _embedding_cache_lookup(tenant_id, db_id, schema_types, collection_name, normalized_query):
    """Layer 2: dense embedding top-K search with optional LLM judge for borderline zone.

    Returns (cache_hit_or_None, query_embedding).
    """
    loop = asyncio.get_event_loop()
    query_embedding = await loop.run_in_executor(
        None, embedding_manager.embed_query, normalized_query
    )
    candidates = cache_repository.find_similar_topk(
        tenant_id, db_id, schema_types, collection_name,
        query_embedding,
        threshold=config.app.semantic_cache_borderline_low,
        k=config.app.semantic_cache_top_k,
    )
    if candidates:
        candidate_sims = ', '.join(f"{c['similarity']:.4f}" for c in candidates)
        logger.info(
            f"[cache:candidates] tenant={tenant_id} db={db_id} "
            f"count={len(candidates)} similarities=[{candidate_sims}]"
        )

    if candidates and candidates[0]["similarity"] >= config.app.semantic_cache_threshold:
        return candidates[0], query_embedding

    if candidates and config.app.llm_judge_enabled:
        borderline = [c for c in candidates if c["similarity"] < config.app.semantic_cache_threshold]
        if borderline:
            cache_hit = await _llm_judge_lookup(tenant_id, db_id, normalized_query, borderline)
            return cache_hit, query_embedding

    return None, query_embedding


async def _semantic_cache_lookup(tenant_id, db_id, schema_types, collection_name, normalized_query):
    """Orchestrate all three cache layers; return (cache_hit_or_None, query_embedding_or_None)."""
    cache_hit = cache_repository.find_exact(
        tenant_id, db_id, schema_types, collection_name, normalized_query
    )
    if cache_hit:
        return cache_hit, None

    if config.app.fuzzy_match_enabled:
        cache_hit = _fuzzy_cache_lookup(tenant_id, db_id, schema_types, collection_name, normalized_query)
        if cache_hit:
            return cache_hit, None

    return await _embedding_cache_lookup(tenant_id, db_id, schema_types, collection_name, normalized_query)


async def _serve_cache_hit(cache_hit, db_id, collection_id, tenant_id):
    """Validate cached SQL and build the cache-hit response. Returns None if SQL is no longer valid."""
    cached = cache_hit["response_payload"]
    try:
        loop = asyncio.get_event_loop()
        is_valid, _ = await loop.run_in_executor(
            None, lambda: metabase_client.validate_sql(cached["sql"], db_id, tenant_id)
        )
    except Exception:
        is_valid = False

    if not is_valid:
        logger.info(
            f"[cache:rejected] tenant={tenant_id} db={db_id} "
            f"similarity={cache_hit['similarity']:.4f} reason=sql_validation_failed"
        )
        return None

    hit_type = cache_hit.get("hit_type_override") or (
        "exact_hit" if cache_hit["similarity"] >= 1.0 else "semantic_hit"
    )
    tokens_saved = cached.get("tokens", {}).get("total_tokens", 0)
    card_id, card_data = metabase_client.create_card(
        cached["sql"], db_id, collection_id, cached["title"],
        tenant_id=tenant_id,
        visualization_settings=_build_viz_settings(cached.get("visualization_options", [])),
    )
    cache_repository.touch(cache_hit["cache_id"])

    if hit_type == "semantic_hit":
        logger.info(
            f"[cache:semantic_hit] tenant={tenant_id} db={db_id} "
            f"similarity={cache_hit['similarity']:.4f} "
            f"threshold={config.app.semantic_cache_threshold} tokens_saved={tokens_saved}"
        )
    else:
        logger.info(
            f"[cache:{hit_type}] tenant={tenant_id} db={db_id} "
            f"similarity={cache_hit['similarity']:.4f} tokens_saved={tokens_saved}"
        )

    return jsonify({
        "card_id": card_id,
        "x_field": cached.get("x_field", []),
        "y_field": cached.get("y_field", []),
        "title": cached["title"],
        "visualization_options": cached.get("visualization_options", []),
        "SQL": cached["sql"],
        "tokens": {"prompt_tokens": 0, "completion_tokens": 0, "total_tokens": 0},
        "card_data": _shape_card_data(card_data),
        "from_cache": True,
        "cache_similarity": round(cache_hit["similarity"], 4),
        "cache_hit_type": hit_type,
        "cache_original_query": cache_hit.get("query_text", "") if hit_type == "llm_judge_hit" else None,
    }), 200


async def _store_query_cache(tenant_id, db_id, schema_types, collection_name,
                             question, normalized_query, query_embedding, sql, metadata, sql_tokens):
    """Persist a successful SQL generation result to the semantic cache (non-fatal)."""
    try:
        if query_embedding is None:
            loop = asyncio.get_event_loop()
            query_embedding = await loop.run_in_executor(
                None, embedding_manager.embed_query, normalized_query
            )
        cache_repository.save(
            tenant_id, db_id, schema_types, collection_name,
            question, normalized_query, query_embedding,
            {
                "sql": sql,
                "title": metadata.get("title", "Untitled"),
                "x_field": metadata.get("x_axis", []),
                "y_field": metadata.get("y_axis", []),
                "visualization_options": metadata.get("visualization_options", []),
                "tokens": sql_tokens,
            },
        )
        cache_repository.ensure_hnsw_index()
        logger.info(f"Cache stored: tenant={tenant_id} tokens={sql_tokens.get('total_tokens', 0)}")
    except Exception as cache_err:
        logger.warning(f"Cache store failed (non-fatal): {cache_err}")


async def _try_serve_from_cache(tenant_id, db_id, schema_types, collection_name,
                                collection_id, normalized_query):
    """Run all cache layers; return (response_or_None, query_embedding_or_None)."""
    cache_hit, query_embedding = await _semantic_cache_lookup(
        tenant_id, db_id, schema_types, collection_name, normalized_query
    )
    if not cache_hit:
        return None, query_embedding
    return await _serve_cache_hit(cache_hit, db_id, collection_id, tenant_id), query_embedding


async def _async_ask(data, user_data):
    """Core logic for /api/ask, extracted to module level to avoid nesting penalties."""
    question = data.get("question")
    conversation = data.get("conversation", [])
    is_retry = bool(data.get("is_retry", False))
    retry_error_type = data.get("retry_error_type") or None
    retry_error_detail = data.get("retry_error_detail") or None
    tenant_id = user_data["tenant"]

    if not question:
        return abort(400, "question is required")

    tenant_config = config.get_tenant_config(tenant_id)
    db_id = tenant_config["db_id"]
    collection_id = tenant_config["collection_id"]
    schema_types = tenant_config.get("schema_types", ["public"])
    collection_name = config.app.collection_name

    logger.info(f"Request - Tenant: {tenant_id}, DB: {db_id}, Collection: {collection_id}")

    past_questions = chat_manager.extract_past_questions(conversation)
    logger.debug(f"Extracted {len(past_questions)} past questions")

    normalized_query = cache_reranker.normalize_query(question)
    query_embedding = None

    # ── Semantic cache lookup ────────────────────────────────────────────────
    if config.app.semantic_cache_enabled and not is_retry:
        response, query_embedding = await _try_serve_from_cache(
            tenant_id, db_id, schema_types, collection_name, collection_id, normalized_query
        )
        if response is not None:
            return response
        logger.info(
            f"[cache:miss] tenant={tenant_id} db={db_id} query=\"{normalized_query[:80]}\""
        )
    # ── End cache lookup ─────────────────────────────────────────────────────

    logger.info("Starting SQL generation...")
    try:
        sql, metadata, sql_tokens, error_detail = await sql_generator.generate_sql(
            question, past_questions, db_id, tenant_id=tenant_id,
            is_retry=is_retry, retry_error_type=retry_error_type,
            retry_error_detail=retry_error_detail
        )
    except Exception as e:
        return _classify_sql_generation_error(e)

    logger.info(f"SQL generation completed. SQL exists: {bool(sql)}, Metadata exists: {bool(metadata)}")
    logger.debug(f"SQL generation tokens: {sql_tokens}")

    if not sql or not metadata:
        logger.warning("SQL generation failed - returning fail response")
        return _error_response("ai_failure", "I couldn't generate a report from that question.", 422)

    logger.debug(f"SQL: {sql}")
    logger.debug(f"Metadata: {metadata}")
    logger.info(f"Creating Metabase card with SQL length: {len(sql)}")

    card_id, card_data = metabase_client.create_card(
        sql, db_id, collection_id, metadata['title'],
        tenant_id=tenant_id,
        visualization_settings=_build_viz_settings(metadata.get("visualization_options", [])),
    )
    logger.info(f"Card created successfully with ID: {card_id}")

    # ── Store result in semantic cache ───────────────────────────────────────
    if config.app.semantic_cache_enabled and not error_detail:
        await _store_query_cache(
            tenant_id, db_id, schema_types, collection_name,
            question, normalized_query, query_embedding, sql, metadata, sql_tokens
        )
    # ── End cache store ──────────────────────────────────────────────────────

    return {
        "card_id": card_id,
        "x_field": metadata.get('x_axis', []),
        "y_field": metadata.get('y_axis', []),
        "title": metadata.get("title", "Untitled"),
        "visualization_options": metadata.get('visualization_options', []),
        "SQL": sql,
        "tokens": sql_tokens,
        "card_data": _shape_card_data(card_data),
    }, 200


@app.route("/api/ask", methods=["POST"])
@require_auth
def ask():
    """
    Main endpoint for processing natural language queries.
    Generates SQL and creates Metabase cards.
    """
    data = request.get_json()
    user_data = get_user_from_token()
    try:
        return asyncio.run(_async_ask(data, user_data))
    except Exception as e:
        logger.error(f"Error in /api/ask: {e}", exc_info=True)
        return _error_response(
            "server_error",
            "Something went wrong on our end. Please try again.",
            500,
            detail=str(e)
        )


@app.route("/api/data-models/views", methods=["POST"])
@require_auth
@require_data_model_permission
def get_data_model_views():
    """Discover available worksheet/scoresheet views for the tenant."""
    user_data = get_user_from_token()
    tenant_id = user_data["tenant"]

    tenant_config = config.get_tenant_config(tenant_id)
    db_id = tenant_config["db_id"]

    logger.info(f"Data model views request - Tenant: {tenant_id}, DB: {db_id}")

    try:
        views = data_model_generator.discover_views(db_id, tenant_id)
        return jsonify({"views": views}), 200
    except Exception as e:
        logger.error(f"Error in /api/data-models/views: {e}", exc_info=True)
        return _error_response(
            "server_error",
            "Failed to discover available views. Please try again.",
            500,
            detail=str(e)
        )


@app.route("/api/data-models/core-fields", methods=["POST"])
@require_auth
@require_data_model_permission
def get_data_model_core_fields():
    """Return the curated list of public.Applications columns users can opt-in to."""
    return jsonify({"core_fields": data_model_generator.get_core_fields()}), 200


@app.route("/api/data-models/preview", methods=["POST"])
@require_auth
@require_data_model_permission
def preview_data_models():
    """Generate an AI model proposal for a selected view."""
    user_data = get_user_from_token()
    tenant_id = user_data["tenant"]

    tenant_config = config.get_tenant_config(tenant_id)
    db_id = tenant_config["db_id"]

    body = request.get_json(silent=True) or {}
    view_name = body.get("view_name", "").strip()
    view_names = body.get("view_names", [])
    core_fields_raw = body.get("core_fields")
    selected_versions_raw = body.get("selected_versions")

    # Accept either view_names (multi) or view_name (single, backward-compat)
    if view_names and isinstance(view_names, list):
        view_names = [v.strip() for v in view_names if isinstance(v, str) and v.strip()]
    elif view_name:
        view_names = [view_name]
    else:
        return jsonify({"error": "view_name or view_names is required"}), 400

    if not view_names:
        return jsonify({"error": "At least one view name is required"}), 400

    # core_fields is optional; None falls back to defaults inside the generator
    core_fields = None
    if isinstance(core_fields_raw, list):
        core_fields = [c.strip() for c in core_fields_raw if isinstance(c, str) and c.strip()]

    # selected_versions is optional; None means use all versions
    selected_versions = None
    if isinstance(selected_versions_raw, list):
        selected_versions = [v.strip() for v in selected_versions_raw if isinstance(v, str) and v.strip()]

    logger.info(
        f"Data model preview request - Tenant: {tenant_id}, Views: {view_names}, "
        f"CoreFields: {core_fields}"
    )

    try:
        if len(view_names) == 1:
            proposal = asyncio.run(
                data_model_generator.preview_model(
                    view_names[0], db_id, tenant_id, core_fields, selected_versions
                )
            )
        else:
            proposal = asyncio.run(
                data_model_generator.preview_combined_model(
                    view_names, db_id, tenant_id, core_fields
                )
            )
        _attach_preview_to_proposal(proposal, db_id, tenant_id)
        return jsonify({"proposal": proposal}), 200
    except Exception as e:
        logger.error(f"Error in /api/data-models/preview: {e}", exc_info=True)
        return _error_response(
            "server_error",
            "Failed to generate data model proposal. Please try again.",
            500,
            detail=str(e)
        )


@app.route("/api/data-models/create", methods=["POST"])
@require_auth
@require_data_model_permission
def create_data_models():
    """Step 2: Create user-approved data models in Metabase."""
    user_data = get_user_from_token()
    tenant_id = user_data["tenant"]

    tenant_config = config.get_tenant_config(tenant_id)
    db_id = tenant_config["db_id"]
    collection_id = tenant_config["collection_id"]

    body = request.get_json(silent=True) or {}
    definitions = body.get("models", [])

    if not isinstance(definitions, list) or not definitions:
        return jsonify({"error": "models array is required"}), 400

    # Validate each entry has required string fields
    for defn in definitions:
        if not isinstance(defn, dict):
            return jsonify({"error": "Each model must be an object"}), 400
        if not all(isinstance(defn.get(k), str) for k in ("name", "description", "sql")):
            return jsonify({"error": "Each model requires name, description, sql"}), 400

    logger.info(
        f"Data model create request - Tenant: {tenant_id}, count: {len(definitions)}"
    )

    try:
        result = data_model_generator.create_models(
            definitions, db_id, collection_id, tenant_id
        )
        metabase_base = config.metabase.url
        return jsonify({
            "models": [
                {**m, "metabase_url": f"{metabase_base}/model/{m['card_id']}"}
                for m in result["created"]
            ],
            "errors": result["errors"],
        }), 200
    except Exception as e:
        logger.error(f"Error in /api/data-models/create: {e}", exc_info=True)
        return _error_response(
            "server_error",
            "Failed to create data models. Please try again.",
            500,
        )


@app.route("/api/data-models/list", methods=["POST"])
@require_auth
@require_data_model_permission
def list_data_models():
    """List existing model cards in the tenant's collection."""
    user_data = get_user_from_token()
    tenant_id = user_data["tenant"]

    tenant_config = config.get_tenant_config(tenant_id)
    collection_id = tenant_config["collection_id"]

    try:
        models = data_model_generator.discover_existing_models(collection_id, tenant_id)
        return jsonify({"models": models}), 200
    except Exception as e:
        logger.error(f"Error in /api/data-models/list: {e}", exc_info=True)
        return _error_response(
            "server_error", "Failed to list existing models.", 500, detail=str(e)
        )


@app.route("/api/data-models/detail", methods=["POST"])
@require_auth
@require_data_model_permission
def data_model_detail():
    """Fetch full detail (SQL + columns) for one model card."""
    user_data = get_user_from_token()
    tenant_id = user_data["tenant"]

    body = request.get_json(silent=True) or {}
    card_id = body.get("card_id")
    if card_id is not None:
        try:
            card_id = int(card_id)
        except (ValueError, TypeError):
            return jsonify({"error": "card_id must be a valid integer"}), 400
    else:
        return jsonify({"error": "card_id is required"}), 400

    try:
        card = metabase_client.get_card(card_id, tenant_id)
        tenant_config = config.get_tenant_config(tenant_id)
        db_id = tenant_config["db_id"]
        sql, columns = metabase_client.card_sql_and_columns(card, db_id, tenant_id)
        return jsonify({
            "card_id": card_id,
            "name": card.get("name", ""),
            "description": card.get("description") or "",
            "sql": sql,
            "columns": columns,
        }), 200
    except Exception as e:
        logger.error(f"Error in /api/data-models/detail: {e}", exc_info=True)
        return _error_response(
            "server_error", "Failed to fetch model detail.", 500, detail=str(e)
        )


@app.route("/api/data-models/preview-data", methods=["POST"])
@require_auth
@require_data_model_permission
def data_model_preview_data():
    """Execute a saved model's SQL via Metabase and return preview rows."""
    user_data = get_user_from_token()
    tenant_id = user_data["tenant"]

    body = request.get_json(silent=True) or {}
    card_id = body.get("card_id")
    if card_id is not None:
        try:
            card_id = int(card_id)
        except (ValueError, TypeError):
            return jsonify({"error": "card_id must be a valid integer"}), 400
    else:
        return jsonify({"error": "card_id is required"}), 400

    try:
        card = metabase_client.get_card(card_id, tenant_id)
        tenant_config = config.get_tenant_config(tenant_id)
        db_id = tenant_config["db_id"]
        sql, _ = metabase_client.card_sql_and_columns(card, db_id, tenant_id)
        raw = metabase_client.execute_sql(
            sql, db_id, tenant_id, max_rows=config.app.data_model_preview_row_limit
        )
        shaped = _shape_card_data(raw, limit=config.app.data_model_preview_row_limit)
        if shaped is None:
            return _error_response("server_error", "Could not read query results.", 500)
        return jsonify(shaped), 200
    except Exception as e:
        logger.error(f"Error in /api/data-models/preview-data: {e}", exc_info=True)
        return _error_response(
            "server_error", "Failed to preview model data.", 500, detail=str(e)
        )


@app.route("/api/data-models/modify-preview", methods=["POST"])
@require_auth
@require_data_model_permission
def modify_data_model_preview():
    """Generate a modified-variant preview from an existing model."""
    user_data = get_user_from_token()
    tenant_id = user_data["tenant"]

    tenant_config = config.get_tenant_config(tenant_id)
    db_id = tenant_config["db_id"]
    collection_id = tenant_config["collection_id"]

    body = request.get_json(silent=True) or {}
    logger.debug(
        "modify-preview request body: card_id=%s (type=%s), prompt_len=%d, view_names=%s, core_fields=%s",
        body.get("card_id"), type(body.get("card_id")).__name__,
        len(body.get("prompt", "")), body.get("view_names", []), body.get("core_fields", []),
    )
    card_id = body.get("card_id")
    prompt = body.get("prompt", "").strip()
    view_names = body.get("view_names", [])
    core_fields_raw = body.get("core_fields")

    # Accept card_id as int or numeric string
    if card_id is not None:
        try:
            card_id = int(card_id)
        except (ValueError, TypeError):
            return jsonify({"error": "card_id must be a valid integer"}), 400
    else:
        return jsonify({"error": "card_id is required"}), 400

    # Distinguish "key not sent" (None) from "key sent as list" (intent, even if empty).
    # An empty list is a valid intent signal — the user toggled the picker.
    core_fields_provided = isinstance(core_fields_raw, list)
    core_fields = None
    if core_fields_provided:
        core_fields = [c.strip() for c in core_fields_raw if isinstance(c, str) and c.strip()]

    if not prompt and not view_names and not core_fields_provided:
        return jsonify({
            "error": "At least one of prompt, view_names, or core_fields is required"
        }), 400

    try:
        result = asyncio.run(
            data_model_generator.preview_model_modification(
                card_id, prompt, view_names, db_id, collection_id, tenant_id,
                core_fields=core_fields,
            )
        )
        _attach_preview_to_proposal(result, db_id, tenant_id)
        return jsonify({"proposal": result}), 200
    except ValueError:
        # Detail is logged server-side; the client gets a generic message so
        # internal exception text isn't exposed externally.
        logger.warning(
            "Validation error in /api/data-models/modify-preview", exc_info=True
        )
        return _error_response("bad_request", "Invalid request parameters.", 400)
    except Exception as e:
        logger.error(f"Error in /api/data-models/modify-preview: {e}", exc_info=True)
        return _error_response(
            "server_error", "Failed to generate modified model.", 500
        )


@app.route("/api/change_display", methods=["POST"])
@require_auth
def change_display():
    """Update visualization type for a Metabase card"""
    data = request.get_json()
    user_data = get_user_from_token()

    try:
        tenant_id = user_data["tenant"]
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
        metabase_client.update_card_visualization(safe_card_id, safe_mode, safe_x_field, safe_y_field,
                                                    tenant_id=tenant_id)
        
        return jsonify({
            "card_id": safe_card_id,
            "x_field": safe_x_field,
            "y_field": safe_y_field,
            "visualization_options": safe_visualization_options
        }), 200

    except Exception as e:
        logger.error(f"Error in /api/change_display: {e}", exc_info=True)
        return abort(500, INTERNAL_SERVER_ERROR)


@app.route("/api/delete", methods=["POST"])
@require_auth
def delete_question():
    """Delete a Metabase card"""
    data = request.get_json()
    user_data = get_user_from_token()

    try:
        tenant_id = user_data["tenant"]
        card_id = data.get("card_id")

        if not card_id:
            return abort(400, "card_id is required")

        # Validate and sanitize card_id
        safe_card_id = _sanitize_card_id(card_id)
        if not safe_card_id:
            return abort(400, "Invalid card_id parameter")

        success = metabase_client.delete_card(safe_card_id, tenant_id=tenant_id)
        return {"success": success}

    except Exception as e:
        logger.error(f"Error in /api/delete: {e}", exc_info=True)
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
        explanation, explanation_tokens = await sql_generator.explain_sql(sql)

        return {
            "explanation": explanation,
            "tokens": explanation_tokens  # Include token usage from explanation
        }, 200
    
    try:
        return asyncio.run(async_explain_sql())
    except Exception as e:
        logger.error(f"Error in /api/explain_sql: {e}", exc_info=True)
        return {
            "explanation": "This query retrieves and analyzes your data."
        }, 200


# Chat management endpoints

@app.route("/api/chats", methods=["POST"])
@require_auth
def get_chats():
    """Get all chats for a user"""
    user_data = get_user_from_token()
    
    try:
        # Extract user context from JWT token
        user_id = user_data["user_id"]
        tenant_id = user_data["tenant"]
        
        chats = chat_manager.get_user_chats(user_id, tenant_id)
        return jsonify(chats), 200

    except Exception as e:
        logger.error(f"Error getting chats: {e}", exc_info=True)
        return abort(500, INTERNAL_SERVER_ERROR)


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
            return abort(404, CHAT_NOT_FOUND)

        return jsonify(chat_data), 200

    except Exception as e:
        logger.error(f"Error getting chat: {e}", exc_info=True)
        return abort(500, INTERNAL_SERVER_ERROR)


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
        
        chat_id = data.get("chat_id")
        title = data.get("title")
        conversation = data.get("conversation")
        
        if not all([title, conversation]):
            return abort(400, "title and conversation are required")
        
        result_chat_id = chat_manager.save_chat(
            user_id, tenant_id, config.metabase.url, title, conversation, chat_id
        )

        return {"chat_id": result_chat_id}, 200

    except Exception as e:
        logger.error(f"Error saving chat: {e}", exc_info=True)
        return abort(500, INTERNAL_SERVER_ERROR)


@app.route("/api/chats/<chat_id>", methods=["DELETE"])
@require_auth
def delete_chat(chat_id):
    """Delete a chat"""
    user_data = get_user_from_token()
    
    try:
        # Extract user ID from JWT token
        user_id = user_data["user_id"]
        
        success = chat_manager.delete_chat(chat_id, user_id)

        if not success:
            return abort(404, CHAT_NOT_FOUND)

        return {"success": True}, 200

    except Exception as e:
        logger.error(f"Error deleting chat: {e}", exc_info=True)
        return abort(500, INTERNAL_SERVER_ERROR)


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
        user_agent = request.headers.get("User-Agent", "")

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
            return abort(404, CHAT_NOT_FOUND)
        
        # Submit feedback
        feedback_id = feedback_repository.submit_feedback(
            chat_id=chat_id,
            user_id=user_id,
            tenant_id=tenant_id,
            feedback_type=feedback_type,
            message=message,
            user_agent=user_agent,
            metadata=metadata,
            current_question=current_question,
            current_sql=current_sql,
            current_sql_explanation=current_sql_explanation,
            previous_question=previous_question,
            previous_sql=previous_sql,
            previous_sql_explanation=previous_sql_explanation
        )

        logger.info(f"Feedback submitted: {feedback_id} for chat {chat_id} by user {user_id}")

        return {
            "success": True,
            "feedback_id": feedback_id,
            "message": "Feedback submitted successfully"
        }, 200

    except Exception as e:
        logger.error(f"Error submitting feedback: {e}", exc_info=True)
        return abort(500, INTERNAL_SERVER_ERROR)


@app.route("/api/feedback/<feedback_id>", methods=["GET"])
@require_auth
def get_feedback(feedback_id):
    """
    Get a specific feedback entry (admin only)
    Requires admin privileges
    """
    user_data = get_user_from_token()

    # Check if user is admin
    is_admin = user_data.get("is_it_admin", False)
    if not is_admin:
        return jsonify({"error": ADMIN_PRIVILEGES_REQUIRED}), 403

    try:
        feedback_data = feedback_repository.get_feedback(feedback_id)

        if not feedback_data:
            return abort(404, "Feedback not found")

        return jsonify(feedback_data), 200

    except Exception as e:
        logger.error(f"Error getting feedback: {e}", exc_info=True)
        return abort(500, INTERNAL_SERVER_ERROR)


@app.route("/api/admin/feedback/<feedback_id>/status", methods=["PUT"])
@require_auth
def update_feedback_status(feedback_id):
    """
    Update feedback status for admin users
    Requires admin privileges
    """
    user_data = get_user_from_token()

    # Check if user is admin
    is_admin = user_data.get("is_it_admin", False)
    if not is_admin:
        return jsonify({"error": ADMIN_PRIVILEGES_REQUIRED}), 403

    data = request.get_json()
    new_status = data.get("status")

    if not new_status:
        return jsonify({"error": "status is required"}), 400

    # Validate status value
    valid_statuses = ["open", "in_progress", "resolved", "closed"]
    if new_status not in valid_statuses:
        return jsonify({"error": f"Invalid status. Must be one of: {', '.join(valid_statuses)}"}), 400

    try:
        success = feedback_repository.update_feedback_status(feedback_id, new_status)

        if not success:
            return jsonify({"error": "Feedback not found"}), 404

        return jsonify({
            "success": True,
            "feedback_id": feedback_id,
            "status": new_status,
            "message": "Feedback status updated successfully"
        }), 200

    except Exception as e:
        logger.error(f"Error updating feedback status: {e}", exc_info=True)
        return jsonify({"error": "Failed to update feedback status"}), 500

