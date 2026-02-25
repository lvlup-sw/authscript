import { useState } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { EhrHeader, EncounterNote, SignEncounterButton, EmbeddedAppFrame } from '@/components/ehr';
import { DEMO_EHR_PATIENT, DEMO_ENCOUNTER } from '@/lib/demoData';

export function EhrDemoPage() {
  const [signed, setSigned] = useState(false);

  const handleSign = () => {
    setSigned(true);
  };

  return (
    <div className="fixed inset-0 z-[100] min-h-screen bg-gray-100 overflow-auto">
      <EhrHeader patient={DEMO_EHR_PATIENT} />
      <div className="max-w-5xl mx-auto p-6 space-y-6">
        <EncounterNote encounter={DEMO_ENCOUNTER} />
        <div className="flex justify-end">
          <SignEncounterButton onSign={handleSign} signed={signed} />
        </div>
        {signed && (
          <div
            className="animate-fade-in"
            style={{ animation: 'fadeSlideIn 0.5s ease-out both' }}
          >
            <EmbeddedAppFrame
              src="/?quickDemo=true"
              visible={signed}
              title="AuthScript Dashboard"
            />
          </div>
        )}
      </div>

      {/* Inline keyframes for the slide-in animation */}
      <style>{`
        @keyframes fadeSlideIn {
          from {
            opacity: 0;
            transform: translateY(24px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }
      `}</style>
    </div>
  );
}

export const Route = createFileRoute('/ehr-demo')({
  component: EhrDemoPage,
});
