"""LLM client abstraction supporting multiple providers.

Uses strategy pattern for provider selection (OCP compliant).
"""

from abc import ABC, abstractmethod
from dataclasses import dataclass

from src.config import settings


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

    async def complete(self, request: CompletionRequest) -> str:
        from openai import AsyncOpenAI

        client = AsyncOpenAI(
            api_key=settings.github_token,
            base_url="https://models.inference.ai.azure.com",
        )

        response = await client.chat.completions.create(
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

    async def complete(self, request: CompletionRequest) -> str:
        from openai import AsyncAzureOpenAI

        client = AsyncAzureOpenAI(
            api_key=settings.azure_openai_api_key,
            azure_endpoint=settings.azure_openai_endpoint,
            api_version=settings.azure_openai_api_version,
        )

        response = await client.chat.completions.create(
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

    async def complete(self, request: CompletionRequest) -> str:
        import google.generativeai as genai

        genai.configure(api_key=settings.google_api_key)  # type: ignore[attr-defined]

        model = genai.GenerativeModel(  # type: ignore[attr-defined]
            model_name=settings.gemini_model,
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

    async def complete(self, request: CompletionRequest) -> str:
        from openai import AsyncOpenAI

        if settings.openai_org_id:
            client = AsyncOpenAI(
                api_key=settings.openai_api_key,
                organization=settings.openai_org_id,
            )
        else:
            client = AsyncOpenAI(api_key=settings.openai_api_key)

        response = await client.chat.completions.create(
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


def _get_provider() -> LLMProvider | None:
    """Get the configured LLM provider instance."""
    if not settings.llm_configured:
        return None

    provider_class = _PROVIDERS.get(settings.llm_provider)
    if not provider_class:
        return None

    return provider_class()


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
    except Exception:
        # Log error in production; return None to trigger fallback behavior
        return None
