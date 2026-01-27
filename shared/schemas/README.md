# Shared Schemas

This directory contains extracted OpenAPI specifications from producer services.

## Contract Ownership Model

| Contract | Producer | Consumer | File |
|----------|----------|----------|------|
| ClinicalBundle | Gateway (.NET) | Intelligence (Python) | `gateway.openapi.json` |
| PAFormResponse | Intelligence (Python) | Gateway (.NET) | `intelligence.openapi.json` |

## How It Works

1. **Extraction**: `npm run sync:schemas` extracts OpenAPI specs from each producer
2. **Generation**: Consumer types are auto-generated from producer specs
3. **Validation**: CI fails if generated code drifts from committed code

## Files

- `gateway.openapi.json` - Extracted from Gateway API (auto-generated)
- `intelligence.openapi.json` - Extracted from Intelligence API (auto-generated)

## Do Not Edit

These files are auto-generated. To change the schema:
1. Modify the source models in the producer service
2. Run `npm run sync:schemas` to regenerate
