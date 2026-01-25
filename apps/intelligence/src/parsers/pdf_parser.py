"""PDF parsing utilities using PyMuPDF4LLM.

Extracts text from clinical documents in markdown format optimized for LLM processing.
"""

import os
import tempfile


async def parse_pdf(pdf_bytes: bytes) -> str:
    """
    Parse PDF document and extract text as markdown.

    Uses PyMuPDF4LLM for LLM-optimized extraction with table preservation.
    """
    # PyMuPDF4LLM requires a file path
    with tempfile.NamedTemporaryFile(suffix=".pdf", delete=False) as f:
        f.write(pdf_bytes)
        temp_path = f.name

    try:
        return _extract_markdown(temp_path)
    finally:
        os.unlink(temp_path)


def _extract_markdown(pdf_path: str) -> str:
    """Extract markdown from PDF using PyMuPDF4LLM."""
    try:
        import pymupdf4llm

        # Extract as markdown with table detection
        md_text = pymupdf4llm.to_markdown(
            pdf_path,
            page_chunks=False,  # Return single string, not list
            write_images=False,  # Skip image extraction
        )

        return str(md_text)

    except Exception as e:
        # Fallback to basic PyMuPDF extraction
        return _fallback_extract(pdf_path, error=str(e))


def _fallback_extract(pdf_path: str, error: str = "") -> str:
    """Basic text extraction fallback using PyMuPDF."""
    try:
        import pymupdf

        doc = pymupdf.open(pdf_path)
        text_parts = []
        for page in doc:
            text_parts.append(page.get_text())
        doc.close()

        if error:
            return f"[Fallback extraction due to: {error}]\n\n" + "\n\n".join(text_parts)
        return "\n\n".join(text_parts)

    except Exception as e:
        return f"[PDF parsing error: {e}]"
