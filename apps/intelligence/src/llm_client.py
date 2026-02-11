"""LLM client abstraction supporting multiple providers.

Uses strategy pattern for provider selection (OCP compliant).
Providers use singleton pattern with pooled HTTP clients.
"""

import logging
from abc import ABC, abstractmethod
from dataclasses import dataclass

import httpx
from openai import APIError, APITimeoutError, RateLimitError

from src.config import settings

logger = logging.getLogger(__name__)


@dataclass(frozen=True)
class CompletionRequest:
    """Immutable request for LLM completion."""

    system_prompt: str
    user_prompt: str
    temperature: float = 0.0
    max_tokens: int = 500


class LLMProvider(ABC):
    """Base class for LLM providers (Strategy pattern)."""

    @abstractmethod
    async def complete(self, request: CompletionRequest) -> str:
        """Send completion request and return response text."""


class GitHubModelsProvider(LLMProvider):
    """GitHub Models via OpenAI-compatible API."""

    def __init__(self) -> None:
        from openai import AsyncOpenAI

        self._client = AsyncOpenAI(
            api_key=settings.github_token,
            base_url="https://models.inference.ai.azure.com",
            timeout=httpx.Timeout(settings.llm_timeout, connect=5.0),
            max_retries=settings.llm_max_retries,
        )

    async def complete(self, request: CompletionRequest) -> str:
        response = await self._client.chat.completions.create(
            model=settings.github_model,
            messages=[
                {"role": "system", "content": request.system_prompt},
                {"role": "user", "content": request.user_prompt},
            ],
            temperature=request.temperature,
            max_tokens=request.max_tokens,
        )

        return response.choices[0].message.content or ""


class AzureOpenAIProvider(LLMProvider):
    """Azure OpenAI Service."""

    def __init__(self) -> None:
        from openai import AsyncAzureOpenAI

        self._client = AsyncAzureOpenAI(
            api_key=settings.azure_openai_api_key,
            azure_endpoint=settings.azure_openai_endpoint,
            api_version=settings.azure_openai_api_version,
            timeout=httpx.Timeout(settings.llm_timeout, connect=5.0),
            max_retries=settings.llm_max_retries,
        )

    async def complete(self, request: CompletionRequest) -> str:
        response = await self._client.chat.completions.create(
            model=settings.azure_openai_deployment,
            messages=[
                {"role": "system", "content": request.system_prompt},
                {"role": "user", "content": request.user_prompt},
            ],
            temperature=request.temperature,
            max_tokens=request.max_tokens,
        )

        return response.choices[0].message.content or ""


class GeminiProvider(LLMProvider):
    """Google Gemini API."""

    def __init__(self) -> None:
        import google.generativeai as genai

        genai.configure(api_key=settings.google_api_key)  # type: ignore[attr-defined]
        self._model_name = settings.gemini_model

    async def complete(self, request: CompletionRequest) -> str:
        import google.generativeai as genai

        model = genai.GenerativeModel(  # type: ignore[attr-defined]
            model_name=self._model_name,
            system_instruction=request.system_prompt,
            generation_config=genai.GenerationConfig(  # type: ignore[attr-defined]
                temperature=request.temperature,
                max_output_tokens=request.max_tokens,
            ),
        )

        response = await model.generate_content_async(request.user_prompt)
        return response.text or ""


class OpenAIProvider(LLMProvider):
    """OpenAI API (fallback)."""

    def __init__(self) -> None:
        from openai import AsyncOpenAI

        kwargs: dict = {
            "api_key": settings.openai_api_key,
            "timeout": httpx.Timeout(settings.llm_timeout, connect=5.0),
            "max_retries": settings.llm_max_retries,
        }
        if settings.openai_org_id:
            kwargs["organization"] = settings.openai_org_id

        self._client = AsyncOpenAI(**kwargs)

    async def complete(self, request: CompletionRequest) -> str:
        response = await self._client.chat.completions.create(
            model=settings.openai_model,
            messages=[
                {"role": "system", "content": request.system_prompt},
                {"role": "user", "content": request.user_prompt},
            ],
            temperature=request.temperature,
            max_tokens=request.max_tokens,
        )

        return response.choices[0].message.content or ""


# Provider registry (OCP: add new providers without modifying existing code)
_PROVIDERS: dict[str, type[LLMProvider]] = {
    "github": GitHubModelsProvider,
    "azure": AzureOpenAIProvider,
    "gemini": GeminiProvider,
    "openai": OpenAIProvider,
}

# Singleton cached provider
_cached_provider: LLMProvider | None = None


def _get_provider() -> LLMProvider | None:
    """Get the configured LLM provider instance (singleton)."""
    global _cached_provider

    if _cached_provider is not None:
        return _cached_provider

    if not settings.llm_configured:
        return None

    provider_class = _PROVIDERS.get(settings.llm_provider)
    if not provider_class:
        return None

    _cached_provider = provider_class()
    return _cached_provider


async def chat_completion(
    system_prompt: str,
    user_prompt: str,
    temperature: float = 0.0,
    max_tokens: int = 500,
) -> str | None:
    """
    Send a chat completion request to the configured LLM provider.

    Returns None if no provider is configured or on error.
    """
    provider = _get_provider()
    if not provider:
        return None

    try:
        request = CompletionRequest(
            system_prompt=system_prompt,
            user_prompt=user_prompt,
            temperature=temperature,
            max_tokens=max_tokens,
        )
        return await provider.complete(request)
    except APITimeoutError:
        logger.warning("LLM request timed out")
        return None
    except RateLimitError:
        logger.error("LLM rate limit exceeded — propagating to caller")
        raise
    except APIError as e:
        logger.error("LLM API error (status=%s): %s", getattr(e, "status_code", "?"), e)
        return None
    except Exception as e:
        logger.error("Unexpected LLM error: %s", e)
        return None
