"""
Configuration module for the Metabase Reporter application.
Handles environment variables and application settings.
"""
import os
import json
from pathlib import Path
from typing import Dict, Any
from dataclasses import dataclass
from dotenv import load_dotenv

load_dotenv()

DEFAULT_TENANT = "Default Grants Program"


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
    default_db_id: int
    map_region_uuid: str
    

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
    fuzzy_match_threshold: float = 92.0
    fuzzy_match_limit: int = 200
    semantic_cache_borderline_low: float = 0.85
    semantic_cache_top_k: int = 5
    llm_judge_enabled: bool = False
    llm_judge_score_threshold: float = 8.0
    preview_row_limit: int = 1000


class Config:
    """Central configuration manager"""
    
    def __init__(self):
        # Load tenant mappings first so db_id can be derived from them
        self.tenant_mappings = self._load_tenant_mappings()
        default_db_id = self.tenant_mappings.get(DEFAULT_TENANT, {}).get("db_id", 5)

        self.database = DatabaseConfig(
            host=os.getenv("DB_HOST", "localhost"),
            port="5432",
            name=os.getenv("DB_NAME", "unity_ai"),
            user=os.getenv("DB_USER", "unity_user"),
            password=os.getenv("DB_PASSWORD", "unity_pass")
        )

        self.metabase = MetabaseConfig(
            url=os.getenv("MB_URL", ""),
            default_db_id=default_db_id,
            map_region_uuid=os.getenv("MB_MAP_REGION_UUID", ""),
        )

        self.ai = AIConfig(
            azure_endpoint=os.getenv("AZURE_OPENAI_ENDPOINT", ""),
            azure_api_key=os.getenv("AZURE_OPENAI_API_KEY", ""),
        )

        flask_env = os.getenv("FLASK_ENV", "development")
        self.app = AppConfig(
            flask_env=flask_env,
            debug=flask_env != "production",
            testing=False,
            semantic_cache_enabled=os.getenv("SEMANTIC_CACHE_ENABLED", "true").lower() == "true",
            semantic_cache_threshold=float(os.getenv("SEMANTIC_CACHE_THRESHOLD", "0.95")),
            fuzzy_match_enabled=os.getenv("FUZZY_MATCH_ENABLED", "true").lower() == "true",
            fuzzy_match_threshold=float(os.getenv("FUZZY_MATCH_THRESHOLD", "92")),
            fuzzy_match_limit=int(os.getenv("FUZZY_MATCH_LIMIT", "200")),
            semantic_cache_borderline_low=float(os.getenv("SEMANTIC_CACHE_BORDERLINE_LOW", "0.85")),
            semantic_cache_top_k=int(os.getenv("SEMANTIC_CACHE_TOP_K", "5")),
            llm_judge_enabled=os.getenv("LLM_JUDGE_ENABLED", "false").lower() == "true",
            llm_judge_score_threshold=float(os.getenv("LLM_JUDGE_SCORE_THRESHOLD", "8.0")),
            preview_row_limit=int(os.getenv("PREVIEW_ROW_LIMIT", "1000")),
        )
    
    def _load_tenant_mappings(self) -> Dict[str, Dict[str, Any]]:
        """
        Load tenant to database/collection mappings from JSON file.
        Falls back to default configuration if file is not found.
        A tenant_config.local.json alongside the base file is merged on top
        (per-tenant key override) and is never committed — use it for local
        secrets like api_key.
        """

        # Try Docker path first, then local path (same directory as this file)
        config_path_pairs = [
            (
                "/app/backend/src/tenant_config.json",
                "/app/backend/src/tenant_config.local.json",
            ),
            (
                Path(__file__).parent / "tenant_config.json",
                Path(__file__).parent / "tenant_config.local.json",
            ),
        ]

        for config_file, local_override_file in config_path_pairs:
            try:
                with open(config_file, 'r') as f:
                    mappings = json.load(f)
                    if DEFAULT_TENANT not in mappings.keys():
                        mappings = {DEFAULT_TENANT: mappings}

                if Path(local_override_file).is_file():
                    with open(local_override_file, 'r') as f:
                        overrides = json.load(f)
                        for tenant, values in overrides.items():
                            if tenant in mappings:
                                mappings[tenant].update(values)
                            else:
                                mappings[tenant] = values
                    print(f"Applied local tenant overrides from {local_override_file}")

                print(f"Loaded tenant mappings from {config_file}")
                return mappings
            except FileNotFoundError:
                continue

        # Fallback to hardcoded defaults if no config file found
        print("No tenant_config.json found, using hardcoded defaults")
        return {
            DEFAULT_TENANT: {
                "db_id": 5,
                "collection_id": 16,
                "schema_types": ["public"]
            }
        }
    
    def get_tenant_config(self, tenant_id: str) -> Dict[str, Any]:
        """Get configuration for a specific tenant"""
        return self.tenant_mappings.get(tenant_id, self.tenant_mappings[DEFAULT_TENANT])

    def get_tenant_metabase_headers(self, tenant_id: str) -> Dict[str, str]:
        """Get Metabase API headers for a specific tenant using its api_key from tenant config."""
        tenant_config = self.get_tenant_config(tenant_id)
        api_key = tenant_config.get("api_key", "")
        if not api_key:
            raise ValueError(
                f"Metabase api_key is not configured for tenant '{tenant_id}'. "
                "For local development add it to tenant_config.local.json; "
                "in OpenShift check the [env]-unity-ai-tenant-config secret."
            )
        return {"x-api-key": api_key}

    @property
    def metabase_headers(self) -> Dict[str, str]:
        """Get headers for Metabase API requests (uses default tenant)"""
        return self.get_tenant_metabase_headers(DEFAULT_TENANT)


# Global config instance
config = Config()