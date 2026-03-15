import { ShieldAlert } from 'lucide-react';
import { motion, AnimatePresence } from 'motion/react';

interface AuthDetectionBannerProps {
  visible: boolean;
  payer: string;
  policyId: string;
  cptCode: string;
}

export function AuthDetectionBanner({
  visible,
  payer,
  policyId,
  cptCode,
}: AuthDetectionBannerProps) {
  return (
    <AnimatePresence>
      {visible && (
        <motion.div
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -20 }}
          transition={{ duration: 0.3 }}
          className="overflow-hidden rounded-lg border border-amber-200 bg-amber-50 shadow-sm"
        >
          <div className="px-4 py-1">
            <span className="text-[10px] font-semibold uppercase tracking-widest text-teal-600">
              Authorization Determination Engine
            </span>
          </div>
          <div className="flex items-center gap-3 border-t border-amber-100 px-4 py-2.5">
            <ShieldAlert className="h-5 w-5 shrink-0 text-amber-600" />
            <span className="text-sm font-medium text-amber-900">
              PA Required — {payer} {policyId} applies to CPT {cptCode}
            </span>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
