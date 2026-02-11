import { useState, useEffect, useCallback, useRef } from 'react';
import { createPortal } from 'react-dom';
import { X, FileText, Printer, Download } from 'lucide-react';
import { cn } from '@/lib/utils';
import { LoadingSpinner } from './LoadingSpinner';
import { generatePAPdfBlob } from '@/lib/pdfGenerator';
import type { PARequest } from '@/api/graphqlService';

interface PdfViewerModalProps {
  isOpen: boolean;
  onClose: () => void;
  request: PARequest | null;
}

export function PdfViewerModal({ isOpen, onClose, request }: PdfViewerModalProps) {
  const [pdfUrl, setPdfUrl] = useState<string | null>(null);
  const [isGenerating, setIsGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const printIframeRef = useRef<HTMLIFrameElement | null>(null);
  const printTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const pdfUrlRef = useRef<string | null>(null);

  const reset = useCallback(() => {
    if (pdfUrlRef.current) {
      URL.revokeObjectURL(pdfUrlRef.current);
      pdfUrlRef.current = null;
    }
    setPdfUrl(null);
    setIsGenerating(false);
    setError(null);
  }, []);

  const handleClose = useCallback(() => {
    reset();
    onClose();
  }, [onClose, reset]);

  useEffect(() => {
    if (!isOpen || !request) {
      reset();
      return;
    }
    let cancelled = false;
    setPdfUrl(null);
    setError(null);
    setIsGenerating(true);

    generatePAPdfBlob(request)
      .then((blob) => {
        if (cancelled) return;
        const url = URL.createObjectURL(blob);
        pdfUrlRef.current = url;
        setPdfUrl(url);
        setIsGenerating(false);
      })
      .catch((err) => {
        if (cancelled) return;
        setError(err instanceof Error ? err.message : 'Failed to generate PDF');
        setIsGenerating(false);
      });

    return () => {
      cancelled = true;
    };
  }, [isOpen, request, reset]);

  useEffect(() => {
    return () => {
      if (pdfUrlRef.current) {
        URL.revokeObjectURL(pdfUrlRef.current);
        pdfUrlRef.current = null;
      }
    };
  }, []);

  // Clean up print iframe and timeout on unmount
  useEffect(() => {
    return () => {
      if (printTimeoutRef.current !== null) {
        clearTimeout(printTimeoutRef.current);
        printTimeoutRef.current = null;
      }
      const iframe = printIframeRef.current;
      if (iframe?.parentNode) {
        iframe.remove();
      }
      printIframeRef.current = null;
    };
  }, []);

  if (!isOpen) return null;

  const handlePrint = () => {
    if (!pdfUrl) return;
    if (printIframeRef.current?.parentNode) {
      printIframeRef.current.remove();
      printIframeRef.current = null;
    }
    if (printTimeoutRef.current !== null) {
      clearTimeout(printTimeoutRef.current);
      printTimeoutRef.current = null;
    }
    const iframe = document.createElement('iframe');
    iframe.style.display = 'none';
    iframe.src = pdfUrl;
    printIframeRef.current = iframe;
    document.body.appendChild(iframe);
    iframe.onload = () => {
      try {
        iframe.contentWindow?.print();
      } finally {
        printTimeoutRef.current = setTimeout(() => {
          printTimeoutRef.current = null;
          if (iframe.parentNode) {
            iframe.remove();
          }
          printIframeRef.current = null;
        }, 1000);
      }
    };
  };

  const handleDownload = () => {
    if (!pdfUrl || !request) return;
    const a = document.createElement('a');
    a.href = pdfUrl;
    a.download = `Prior-Auth-${request.id}.pdf`;
    a.click();
  };

  return createPortal(
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/60 backdrop-blur-sm z-[9999]"
        onClick={handleClose}
        aria-hidden="true"
      />

      {/* Modal */}
      <div
        className="fixed inset-0 z-[10000] flex items-center justify-center p-4 pointer-events-none"
        aria-modal="true"
        role="dialog"
        aria-labelledby="pdf-modal-title"
      >
        <div
          className="relative bg-white rounded-2xl shadow-2xl w-full max-w-4xl max-h-[90vh] overflow-hidden border border-gray-200 pointer-events-auto flex flex-col"
          onClick={(e) => e.stopPropagation()}
        >
          {/* Header */}
          <div className="flex items-center justify-between p-5 border-b border-gray-200 shrink-0">
            <h2 id="pdf-modal-title" className="text-lg font-bold text-gray-900">
              Prior Authorization Request
            </h2>
            <button
              onClick={handleClose}
              className="p-2 rounded-lg hover:bg-gray-100 transition-colors click-effect"
              aria-label="Close"
            >
              <X className="w-5 h-5 text-gray-500" />
            </button>
          </div>

          {/* Content */}
          <div className="flex-1 min-h-0 flex flex-col">
            {isGenerating && (
              <div className="flex flex-col items-center justify-center py-16 px-6">
                <div className="relative mb-6">
                  <div className="w-20 h-20 rounded-2xl bg-gradient-to-br from-teal to-teal/80 flex items-center justify-center shadow-lg">
                    <FileText className="w-10 h-10 text-white animate-pulse" />
                  </div>
                  <div className="absolute -bottom-2 -right-2 w-8 h-8 bg-white rounded-full shadow-md flex items-center justify-center">
                    <LoadingSpinner size="sm" />
                  </div>
                </div>
                <h3 className="text-xl font-bold text-gray-900 mb-2">Generating PDF</h3>
                <p className="text-gray-500 text-center max-w-sm">
                  Creating your prior authorization form...
                </p>
              </div>
            )}

            {error && (
              <div className="flex flex-col items-center justify-center py-16 px-6">
                <p className="text-red-600 font-medium">{error}</p>
                <button
                  onClick={handleClose}
                  className="mt-4 px-4 py-2 rounded-lg bg-gray-100 hover:bg-gray-200 transition-colors"
                >
                  Close
                </button>
              </div>
            )}

            {pdfUrl && !isGenerating && !error && (
              <>
                <div className="flex-1 min-h-0 p-4 bg-gray-100">
                  <iframe
                    src={pdfUrl}
                    title="Prior Authorization PDF"
                    className="w-full h-full min-h-[60vh] rounded-lg border border-gray-200 bg-white"
                  />
                </div>
                <div className="flex items-center justify-end gap-3 p-5 border-t border-gray-200 shrink-0">
                  <button
                    onClick={handlePrint}
                    className={cn(
                      'px-4 py-2.5 text-sm font-medium rounded-xl transition-colors flex items-center gap-2',
                      'border border-gray-200 hover:bg-gray-50 text-gray-800'
                    )}
                  >
                    <Printer className="w-4 h-4" />
                    Print
                  </button>
                  <button
                    onClick={handleDownload}
                    className={cn(
                      'px-4 py-2.5 text-sm font-semibold rounded-xl transition-colors flex items-center gap-2',
                      'bg-teal text-white hover:bg-teal/90'
                    )}
                  >
                    <Download className="w-4 h-4" />
                    Download
                  </button>
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </>,
    document.body
  );
}
