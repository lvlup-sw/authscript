"""
AuthScript Intelligence Service

Clinical reasoning engine that extracts evidence from documents
and determines PA form values using LLM-powered analysis.
"""

from contextlib import asynccontextmanager
from typing import AsyncGenerator

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from src.api.analyze import router as analyze_router
from src.config import settings


@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncGenerator[None, None]:
    """Application lifespan manager."""
    # Startup
    print(f"Starting AuthScript Intelligence Service v{settings.version}")
    print(f"Environment: {'development' if settings.debug else 'production'}")
    yield
    # Shutdown
    print("Shutting down Intelligence Service")


app = FastAPI(
    title="AuthScript Intelligence Service",
    description="Clinical reasoning engine for prior authorization",
    version=settings.version,
    lifespan=lifespan,
)

# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.cors_origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Routes
app.include_router(analyze_router, prefix="/analyze", tags=["Analysis"])


@app.get("/health")
async def health_check() -> dict[str, str]:
    """Health check endpoint for Aspire orchestration."""
    return {"status": "healthy", "service": "intelligence"}


@app.get("/")
async def root() -> dict[str, str]:
    """Root endpoint with service information."""
    return {
        "service": "AuthScript Intelligence Service",
        "version": settings.version,
        "docs": "/docs",
    }
