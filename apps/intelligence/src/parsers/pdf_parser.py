"""PDF parsing utilities using LlamaParse."""

import os
from typing import Optional

from src.config import settings


async def parse_pdf(pdf_bytes: bytes) -> str:
    """
    Parse PDF document and extract text using LlamaParse.

    Falls back to basic extraction if LlamaParse is unavailable.
    """
    if settings.llama_cloud_api_key:
        return await _parse_with_llamaparse(pdf_bytes)
    else:
        return _fallback_parse(pdf_bytes)


async def _parse_with_llamaparse(pdf_bytes: bytes) -> str:
    """Parse PDF using LlamaParse API."""
    try:
        from llama_parse import LlamaParse

        parser = LlamaParse(
            api_key=settings.llama_cloud_api_key,
            result_type="markdown",
            parsing_instruction="""
            This is a medical clinical document. Extract:
            - Patient demographics (name, DOB, MRN)
            - Diagnoses and ICD-10 codes
            - Dates of service
            - Clinical findings and assessments
            - Treatment history (medications, physical therapy, imaging)
            - Preserve table structures for lab results
            """,
            language="en",
        )

        # LlamaParse expects a file path, so we need to write temp file
        import tempfile

        with tempfile.NamedTemporaryFile(suffix=".pdf", delete=False) as f:
            f.write(pdf_bytes)
            temp_path = f.name

        try:
            documents = await parser.aload_data(temp_path)
            return "\n\n".join(doc.text for doc in documents)
        finally:
            os.unlink(temp_path)

    except Exception as e:
        print(f"LlamaParse error: {e}, falling back to basic extraction")
        return _fallback_parse(pdf_bytes)


def _fallback_parse(pdf_bytes: bytes) -> str:
    """Basic PDF text extraction fallback."""
    try:
        import fitz  # PyMuPDF

        doc = fitz.open(stream=pdf_bytes, filetype="pdf")
        text_parts = []
        for page in doc:
            text_parts.append(page.get_text())
        return "\n\n".join(text_parts)
    except ImportError:
        return "[PDF parsing unavailable - install pymupdf for fallback support]"
    except Exception as e:
        return f"[PDF parsing error: {e}]"
