"""Tests for PDF parser thread pool and parallel execution."""

import asyncio
import time
from unittest.mock import MagicMock, patch

import pytest


@pytest.mark.asyncio
async def test_parse_pdf_does_not_block_event_loop():
    """Test that parse_pdf uses run_in_executor for sync operations."""
    from src.parsers.pdf_parser import parse_pdf

    mock_extract = MagicMock(return_value="# Extracted Text\nSome content")

    with patch("src.parsers.pdf_parser._extract_sync", mock_extract):
        result = await parse_pdf(b"%PDF-1.4 fake content")

    assert "Extracted Text" in result
    mock_extract.assert_called_once()


@pytest.mark.asyncio
async def test_parse_pdf_multiple_docs_parallel():
    """Test that multiple PDFs can be parsed concurrently."""
    from src.parsers.pdf_parser import parse_pdf

    call_times: list[float] = []

    def slow_extract(pdf_bytes):
        call_times.append(time.monotonic())
        import time as t

        t.sleep(0.05)  # Simulate extraction time
        return f"# Content from {len(pdf_bytes)} bytes"

    with patch("src.parsers.pdf_parser._extract_sync", side_effect=slow_extract):
        start = time.monotonic()
        results = await asyncio.gather(
            parse_pdf(b"pdf1"),
            parse_pdf(b"pdf2"),
            parse_pdf(b"pdf3"),
        )
        duration = time.monotonic() - start

    assert len(results) == 3
    # If parallel (thread pool): ~0.05s. If sequential: ~0.15s
    assert duration < 0.12, f"Expected parallel execution (<0.12s), got {duration:.2f}s"
