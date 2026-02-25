import { useState } from 'react';
import { Pen, Check } from 'lucide-react';
import { cn } from '@/lib/utils';

interface SignEncounterButtonProps {
  onSign: () => void;
  signed?: boolean;
}

export function SignEncounterButton({
  onSign,
  signed: signedProp,
}: SignEncounterButtonProps) {
  const [signedInternal, setSignedInternal] = useState(false);
  const isSigned = signedProp ?? signedInternal;

  function handleClick() {
    if (isSigned) return;
    setSignedInternal(true);
    onSign();
  }

  return (
    <button
      type="button"
      disabled={isSigned}
      onClick={handleClick}
      className={cn(
        'inline-flex items-center gap-2 rounded-lg px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all duration-300',
        isSigned
          ? 'cursor-not-allowed bg-gray-400'
          : 'bg-green-600 hover:bg-green-700 active:bg-green-800',
      )}
    >
      {isSigned ? (
        <Check className="h-4 w-4" aria-hidden="true" />
      ) : (
        <Pen className="h-4 w-4" aria-hidden="true" />
      )}
      {isSigned ? 'Encounter Signed' : 'Sign Encounter'}
    </button>
  );
}
