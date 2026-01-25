# AuthScript Intelligence Service

Clinical reasoning engine for prior authorization.

## Features

- Evidence Extraction: Analyzes clinical data against policy criteria
- Form Generation: Populates PA form fields with extracted evidence
- Multi-Provider LLM: Supports GitHub, Azure, Gemini, OpenAI

## Development

```bash
cd apps/intelligence
uv sync
uv run uvicorn src.main:app --reload
```

## API Documentation

Visit `/docs` for interactive Swagger UI.
