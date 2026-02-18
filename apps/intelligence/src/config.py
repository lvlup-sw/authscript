"""Application configuration using pydantic-settings."""

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Application settings loaded from environment variables."""

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
    )

    # Application
    version: str = "0.1.0"
    debug: bool = False

    # LLM Provider Selection
    # Supported: "github", "azure", "gemini", "openai"
    llm_provider: str = "github"

    # GitHub Models (OpenAI-compatible)
    # Get token: https://github.com/settings/tokens (no special scopes needed)
    github_token: str = ""
    github_model: str = "gpt-4.1"  # GPT-4o deprecated June 2025

    # Azure OpenAI
    # Get key: Azure Portal > Azure OpenAI > Keys and Endpoint
    azure_openai_api_key: str = ""
    azure_openai_endpoint: str = ""  # e.g., https://your-resource.openai.azure.com
    azure_openai_deployment: str = "gpt-4.1"  # GPT-4o deprecated
    azure_openai_api_version: str = "2024-12-01-preview"

    # Google Gemini
    # Get key: https://aistudio.google.com/apikey
    google_api_key: str = ""
    gemini_model: str = "gemini-2.5-flash"  # 1.5 retired, 2.0 deprecated March 2026

    # OpenAI (fallback/alternative)
    openai_api_key: str = ""
    openai_org_id: str = ""  # Optional: Organization ID for usage tracking
    openai_model: str = "gpt-4.1"

    # LLM Performance
    llm_max_concurrent: int = Field(4, ge=1, description="Max concurrent LLM calls (semaphore limit)")
    llm_timeout: float = Field(30.0, gt=0, description="HTTP timeout for LLM requests (seconds)")
    llm_max_retries: int = Field(2, ge=0, description="Max retries for transient LLM errors")

    # Database
    database_url: str = ""

    # CORS
    cors_origins: list[str] = ["http://localhost:3000", "http://localhost:5000"]

    @property
    def llm_configured(self) -> bool:
        """Check if any LLM provider is configured."""
        if self.llm_provider == "github":
            return bool(self.github_token)
        elif self.llm_provider == "azure":
            return bool(self.azure_openai_api_key and self.azure_openai_endpoint)
        elif self.llm_provider == "gemini":
            return bool(self.google_api_key)
        elif self.llm_provider == "openai":
            return bool(self.openai_api_key)
        return False


settings = Settings()
