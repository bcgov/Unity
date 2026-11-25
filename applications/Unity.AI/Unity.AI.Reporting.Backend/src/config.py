"""
Configuration module for the Metabase Reporter application.
Handles environment variables and application settings.
"""
import os
from typing import Dict
from dataclasses import dataclass, field
from dotenv import load_dotenv

load_dotenv()


@dataclass
class DatabaseConfig:
    """Database configuration settings"""
    host: str
    port: str
    name: str
    user: str
    password: str
    
    @property
    def url(self) -> str:
        """Generate database URL for connections with pool settings"""
        # Add connection pool parameters and automatic reconnection
        return (
            f"postgresql+psycopg://{self.user}:{self.password}@{self.host}:{self.port}/{self.name}"
        )


@dataclass
class MetabaseConfig:
    """Metabase API configuration"""
    url: str
    map_region_uuid: str
    api_key: str = ""
    

@dataclass
class AIConfig:
    """AI/LLM configuration settings"""
    # Azure OpenAI settings — only endpoint and key come from env vars
    azure_endpoint: str = ""
    azure_api_key: str = ""
    azure_deployment: str = "gpt-5-mini"
    azure_api_version: str = "2024-10-21"
    azure_embedding_deployment: str = "text-embedding-3-large"
    temperature: float = 0.2
    k_samples: int = 7
    max_self_correction_iterations: int = 3
    retry_k_samples: int = 1

    @property
    def supports_temperature(self) -> bool:
        """Check if the deployed model supports non-default temperature values.
        gpt-5-mini and future gpt-5 variants only accept the default temperature of 1."""
        return "gpt-5" not in self.azure_deployment.lower()


@dataclass
class AppConfig:
    """Main application configuration"""
    flask_env: str
    debug: bool
    testing: bool
    collection_name: str = "embedded_schema"
    semantic_cache_enabled: bool = True
    semantic_cache_threshold: float = 0.95
    fuzzy_match_enabled: bool = True
    # token_sort_ratio measures character overlap, not meaning, so this layer
    # needs a high bar: "north region" vs "south region" scores 93.9. The two
    # things fuzzy uniquely catches — typos (97.7) and word order (100) — clear
    # 96 comfortably, and anything rejected here still falls through to the
    # embedding layer, which is an actual semantic signal.
    fuzzy_match_threshold: float = 96.0
    fuzzy_match_limit: int = 200
    semantic_cache_borderline_low: float = 0.85
    # Top-K is deliberately generous: the discriminator guard filters candidates
    # *after* the LIMIT, and year-variants of a question cluster tightly in
    # embedding space, so a genuine match can sit behind a dozen near-duplicates.
    semantic_cache_top_k: int = 20
    cache_discriminator_guard_enabled: bool = True
    llm_judge_enabled: bool = True
    llm_judge_score_threshold: float = 8.0
    # Retrieval stays wide (semantic_cache_top_k) so the discriminator guard has
    # material to work with, but only the closest few survivors are judged —
    # otherwise one lookup would fan out to 20 concurrent Azure calls.
    llm_judge_max_candidates: int = 3
    preview_row_limit: int = 1000
    data_model_preview_row_limit: int = 1
    cors_allowed_origins: list[str] = field(default_factory=list)


class Config:
    """Central configuration manager"""
    
    def __init__(self):
        self.database = DatabaseConfig(
            # 127.0.0.1, not localhost: on Windows localhost resolves to ::1 first and the
            # Docker-published 5432 hangs on IPv6 before libpq falls back to IPv4.
            host=os.getenv("DB_HOST", "127.0.0.1"),
            port="5432",
            name=os.getenv("DB_NAME", "UnityAI"),
            user=os.getenv("DB_USER", "unity_user"),
            password=os.getenv("DB_PASSWORD", "unity_pass")
        )

        self.metabase = MetabaseConfig(
            url=os.getenv("MB_URL", "").rstrip("/"),
            map_region_uuid=os.getenv("MB_MAP_REGION_UUID", ""),
            api_key=os.getenv("METABASE_API_KEY", "").strip(),
        )

        self.ai = AIConfig(
            azure_endpoint=os.getenv("AZURE_OPENAI_ENDPOINT", ""),
            azure_api_key=os.getenv("AZURE_OPENAI_API_KEY", ""),
            max_self_correction_iterations=int(os.getenv("MAX_SELF_CORRECTION_ITERATIONS", "3")),
            retry_k_samples=int(os.getenv("RETRY_K_SAMPLES", "1")),
        )

        flask_env = os.getenv("FLASK_ENV", "development")
        self.app = AppConfig(
            flask_env=flask_env,
            debug=flask_env != "production",
            testing=False,
            semantic_cache_enabled=os.getenv("SEMANTIC_CACHE_ENABLED", "true").lower() == "true",
            semantic_cache_threshold=float(os.getenv("SEMANTIC_CACHE_THRESHOLD", "0.95")),
            fuzzy_match_enabled=os.getenv("FUZZY_MATCH_ENABLED", "true").lower() == "true",
            fuzzy_match_threshold=float(os.getenv("FUZZY_MATCH_THRESHOLD", "96")),
            fuzzy_match_limit=int(os.getenv("FUZZY_MATCH_LIMIT", "200")),
            semantic_cache_borderline_low=float(os.getenv("SEMANTIC_CACHE_BORDERLINE_LOW", "0.85")),
            semantic_cache_top_k=int(os.getenv("SEMANTIC_CACHE_TOP_K", "20")),
            cache_discriminator_guard_enabled=os.getenv(
                "CACHE_DISCRIMINATOR_GUARD_ENABLED", "true").lower() == "true",
            llm_judge_enabled=os.getenv("LLM_JUDGE_ENABLED", "true").lower() == "true",
            llm_judge_score_threshold=float(os.getenv("LLM_JUDGE_SCORE_THRESHOLD", "8.0")),
            # Clamped: the value is used as a list slice, so 0 would judge
            # nothing (every lookup a guaranteed miss) and a negative value
            # would silently drop the *last* candidate instead. Use
            # LLM_JUDGE_ENABLED to turn the judge off.
            llm_judge_max_candidates=max(1, int(os.getenv("LLM_JUDGE_MAX_CANDIDATES", "3"))),
            preview_row_limit=int(os.getenv("PREVIEW_ROW_LIMIT", "1000")),
            data_model_preview_row_limit=int(os.getenv("DATA_MODEL_PREVIEW_ROW_LIMIT", "1")),
            cors_allowed_origins=[
                o.strip() for o in os.getenv("CORS_ALLOWED_ORIGINS", "").split(",") if o.strip()
            ],
        )
    
    @property
    def metabase_headers(self) -> Dict[str, str]:
        """Metabase API headers. One key per environment, shared by all tenants (AB#33978)."""
        if not self.metabase.api_key:
            raise ValueError(
                "METABASE_API_KEY is not configured. Locally set it in .env; "
                "in OpenShift it comes from the [env]-unity-ai-secrets secret."
            )
        return {"x-api-key": self.metabase.api_key}


# Global config instance
config = Config()