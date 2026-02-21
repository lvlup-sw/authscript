"""Tests for LLM client singleton pooling, timeout, and retry."""

from unittest.mock import AsyncMock, MagicMock, patch

import pytest


def test_get_provider_returns_singleton():
    """Test that _get_provider returns the same instance on repeated calls."""
    import src.llm_client as llm_mod

    # Reset cached provider
    llm_mod._cached_provider = None

    with patch.object(llm_mod, "settings") as mock_settings:
        mock_settings.llm_configured = True
        mock_settings.llm_provider = "openai"
        mock_settings.openai_api_key = "test-key"
        mock_settings.openai_org_id = ""
        mock_settings.openai_model = "gpt-4.1"
        mock_settings.llm_timeout = 30.0
        mock_settings.llm_max_retries = 2

        provider1 = llm_mod._get_provider()
        provider2 = llm_mod._get_provider()

    assert provider1 is provider2, "Expected same provider instance (singleton)"

    # Cleanup
    llm_mod._cached_provider = None


def test_openai_provider_creates_client_once():
    """Test that OpenAIProvider creates the client in __init__, not per call."""
    from src.llm_client import OpenAIProvider

    with patch("src.llm_client.settings") as mock_settings:
        mock_settings.openai_api_key = "test-key"
        mock_settings.openai_org_id = ""
        mock_settings.openai_model = "gpt-4.1"
        mock_settings.llm_timeout = 30.0
        mock_settings.llm_max_retries = 2

        provider = OpenAIProvider()

    assert hasattr(provider, "_client"), "Provider should store client as _client"


@pytest.mark.asyncio
async def test_chat_completion_uses_provider_singleton():
    """Test that repeated chat_completion calls reuse the same provider."""
    import src.llm_client as llm_mod

    llm_mod._cached_provider = None

    # Mock the provider's complete method
    mock_provider = AsyncMock()
    mock_provider.complete = AsyncMock(return_value="test response")
    llm_mod._cached_provider = mock_provider

    with patch.object(llm_mod, "settings") as mock_settings:
        mock_settings.llm_configured = True

        await llm_mod.chat_completion("system", "user")
        await llm_mod.chat_completion("system", "user")

    # Provider should be reused, not re-created
    assert mock_provider.complete.call_count == 2

    # Cleanup
    llm_mod._cached_provider = None


# --- A4: Structured error handling tests ---


@pytest.mark.asyncio
async def test_chat_completion_returns_none_on_timeout():
    """Test that APITimeoutError is caught and returns None."""
    from openai import APITimeoutError

    import src.llm_client as llm_mod

    mock_provider = AsyncMock()
    mock_provider.complete = AsyncMock(side_effect=APITimeoutError(request=MagicMock()))
    llm_mod._cached_provider = mock_provider

    with patch.object(llm_mod, "settings") as mock_settings:
        mock_settings.llm_configured = True
        result = await llm_mod.chat_completion("system", "user")

    assert result is None

    # Cleanup
    llm_mod._cached_provider = None


@pytest.mark.asyncio
async def test_chat_completion_raises_on_rate_limit():
    """Test that RateLimitError propagates instead of being swallowed."""
    from openai import RateLimitError

    import src.llm_client as llm_mod

    mock_provider = AsyncMock()
    mock_response = MagicMock()
    mock_response.status_code = 429
    mock_response.headers = {}
    mock_provider.complete = AsyncMock(
        side_effect=RateLimitError(
            message="Rate limited",
            response=mock_response,
            body=None,
        )
    )
    llm_mod._cached_provider = mock_provider

    with (
        patch.object(llm_mod, "settings") as mock_settings,
        pytest.raises(RateLimitError),
    ):
        mock_settings.llm_configured = True
        await llm_mod.chat_completion("system", "user")

    # Cleanup
    llm_mod._cached_provider = None


@pytest.mark.asyncio
async def test_chat_completion_returns_none_on_api_error():
    """Test that APIError is caught, logged, and returns None."""
    from openai import APIError

    import src.llm_client as llm_mod

    mock_provider = AsyncMock()
    mock_provider.complete = AsyncMock(
        side_effect=APIError(message="Server error", request=MagicMock(), body=None)
    )
    llm_mod._cached_provider = mock_provider

    with patch.object(llm_mod, "settings") as mock_settings:
        mock_settings.llm_configured = True
        result = await llm_mod.chat_completion("system", "user")

    assert result is None

    # Cleanup
    llm_mod._cached_provider = None


# --- H2: Thread-safe provider singleton ---


def test_get_provider_thread_safe():
    """Concurrent calls to _get_provider return the same instance."""
    import threading

    import src.llm_client as llm_mod

    # Reset cached provider
    llm_mod._cached_provider = None

    results: list[int] = []
    barrier = threading.Barrier(3)

    def get_provider_thread():
        barrier.wait()  # Ensure all threads start simultaneously
        provider = llm_mod._get_provider()
        results.append(id(provider))

    with patch.object(llm_mod, "settings") as mock_settings:
        mock_settings.llm_configured = True
        mock_settings.llm_provider = "openai"
        mock_settings.openai_api_key = "test-key"
        mock_settings.openai_org_id = ""
        mock_settings.openai_model = "gpt-4.1"
        mock_settings.llm_timeout = 30.0
        mock_settings.llm_max_retries = 2

        threads = [threading.Thread(target=get_provider_thread) for _ in range(3)]
        for t in threads:
            t.start()
        for t in threads:
            t.join()

    # All threads should get the same instance
    assert len(set(results)) == 1, f"Expected 1 unique provider, got {len(set(results))}"

    # Cleanup
    llm_mod._cached_provider = None
