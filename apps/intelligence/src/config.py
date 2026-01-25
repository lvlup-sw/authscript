"""Application configuration using pydantic-settings."""

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

    # OpenAI
    openai_api_key: str = ""
    openai_model: str = "gpt-4o"

    # LlamaParse
    llama_cloud_api_key: str = ""

    # Database
    database_url: str = ""

    # CORS
    cors_origins: list[str] = ["http://localhost:5173", "http://localhost:5000"]


settings = Settings()
