# ===========================================================================
# AuthScript Intelligence Service - Multi-stage Docker Build
# Python 3.11 + FastAPI with uv for fast dependency resolution
# ===========================================================================

# ---------------------------------------------------------------------------
# Stage 1: Build dependencies with uv
# ---------------------------------------------------------------------------
FROM ghcr.io/astral-sh/uv:0.5-python3.11-bookworm-slim AS builder

WORKDIR /app

# Copy dependency files first for layer caching
COPY pyproject.toml ./

# Install dependencies into .venv
RUN --mount=type=cache,target=/root/.cache/uv \
    uv venv .venv && \
    uv pip install --python .venv/bin/python -r pyproject.toml

# Copy application source
COPY src/ ./src/

# ---------------------------------------------------------------------------
# Stage 2: Runtime image
# ---------------------------------------------------------------------------
FROM python:3.11-slim-bookworm AS runtime

# Create non-root user for security
RUN groupadd --gid 1000 appgroup && \
    useradd --uid 1000 --gid appgroup --shell /bin/bash --create-home appuser

WORKDIR /app

# Copy virtual environment from builder
COPY --from=builder /app/.venv ./.venv
COPY --from=builder /app/src ./src

# Set environment variables
ENV PATH="/app/.venv/bin:$PATH"
ENV PYTHONUNBUFFERED=1
ENV PYTHONDONTWRITEBYTECODE=1

# Switch to non-root user
USER appuser

# Expose FastAPI port
EXPOSE 8000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8000/health')" || exit 1

# Run uvicorn
CMD ["uvicorn", "src.main:app", "--host", "0.0.0.0", "--port", "8000"]
