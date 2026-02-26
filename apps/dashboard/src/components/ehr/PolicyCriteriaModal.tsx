import { createPortal } from 'react-dom';
import { X, ClipboardList, Info } from 'lucide-react';
import { LCD_L34220_POLICY } from '@/lib/demoData';

interface PolicyCriteriaModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function PolicyCriteriaModal({ isOpen, onClose }: PolicyCriteriaModalProps) {
  if (!isOpen) return null;

  const policy = LCD_L34220_POLICY;

  return createPortal(
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/60 backdrop-blur-sm z-[9999]"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Modal */}
      <div
        className="fixed inset-0 z-[10000] flex items-center justify-center p-4 pointer-events-none"
        aria-modal="true"
        role="dialog"
        aria-labelledby="policy-criteria-title"
      >
        <div
          className="relative bg-white rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-hidden border border-gray-200 pointer-events-auto flex flex-col"
          onClick={(e) => e.stopPropagation()}
        >
          {/* Header */}
          <div className="flex items-center justify-between p-5 border-b border-gray-200 shrink-0">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-blue-50 flex items-center justify-center">
                <ClipboardList className="w-5 h-5 text-blue-600" />
              </div>
              <div>
                <h2 id="policy-criteria-title" className="text-lg font-bold text-gray-900">
                  Policy Criteria
                </h2>
                <p className="text-sm text-gray-500">
                  {policy.policyId} — {policy.procedureName}
                </p>
              </div>
            </div>
            <button
              onClick={onClose}
              className="p-2 rounded-lg hover:bg-gray-100 transition-colors"
              aria-label="Close"
            >
              <X className="w-5 h-5 text-gray-500" />
            </button>
          </div>

          {/* Content */}
          <div className="flex-1 overflow-y-auto p-5 space-y-4">
            <div className="flex items-start gap-2 rounded-lg bg-blue-50 p-3 text-sm text-blue-800">
              <Info className="w-4 h-4 mt-0.5 shrink-0" />
              <p>
                The following criteria must be documented for <strong>{policy.payer}</strong> to
                approve <strong>CPT {policy.procedureCode}</strong>. AuthScript will automatically
                verify each criterion against the encounter documentation.
              </p>
            </div>

            <ol className="space-y-3">
              {policy.criteria.map((criterion, index) => (
                <li
                  key={criterion.label}
                  className="rounded-lg border border-gray-200 p-4"
                >
                  <div className="flex items-start gap-3">
                    <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-gray-100 text-xs font-bold text-gray-600">
                      {index + 1}
                    </span>
                    <div>
                      <h3 className="font-semibold text-gray-900">{criterion.label}</h3>
                      <p className="mt-1 text-sm text-gray-600">{criterion.requirement}</p>
                    </div>
                  </div>
                </li>
              ))}
            </ol>
          </div>

          {/* Footer */}
          <div className="flex items-center justify-end p-5 border-t border-gray-200 shrink-0">
            <button
              onClick={onClose}
              className="px-4 py-2.5 text-sm font-medium rounded-xl border border-gray-200 hover:bg-gray-50 transition-colors text-gray-800"
            >
              Close
            </button>
          </div>
        </div>
      </div>
    </>,
    document.body,
  );
}
