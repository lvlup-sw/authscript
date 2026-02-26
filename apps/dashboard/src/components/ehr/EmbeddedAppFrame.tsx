import { useState } from 'react';
import { ShieldCheck } from 'lucide-react';
import { cn } from '@/lib/utils';

interface EmbeddedAppFrameProps {
  src: string;
  visible?: boolean;
  title?: string;
}

export function EmbeddedAppFrame({
  src,
  visible = true,
  title = 'AuthScript Dashboard',
}: EmbeddedAppFrameProps) {
  const [loading, setLoading] = useState(true);

  return (
    <div className="rounded-lg border border-blue-200 bg-white shadow-sm">
      {/* Section Header */}
      <div className="flex items-center gap-2 border-b border-blue-100 bg-blue-50 px-4 py-2.5">
        <ShieldCheck className="h-4 w-4 text-blue-600" aria-hidden="true" />
        <h2 className="text-sm font-semibold text-blue-800">
          AuthScript — Prior Authorization
        </h2>
      </div>

      {/* iframe Wrapper */}
      <div
        data-testid="embedded-frame-wrapper"
        className={cn(
          'relative transition-all duration-300',
          visible ? 'h-[600px]' : 'h-0 overflow-hidden',
        )}
      >
        {loading && (
          <div
            data-testid="embedded-frame-skeleton"
            className="absolute inset-0 flex flex-col gap-4 p-6 bg-white"
          >
            <div className="h-10 w-2/3 rounded bg-gray-200 animate-pulse" />
            <div className="h-6 w-1/2 rounded bg-gray-200 animate-pulse" />
            <div className="flex-1 rounded bg-gray-100 animate-pulse" />
          </div>
        )}
        {/* allow-same-origin + allow-scripts: required because the iframe loads our
            own dashboard (same origin). Safe here since src is always a trusted internal
            URL — never user-controlled. Per athenahealth Embedded Apps spec. */}
        <iframe
          src={src}
          title={title}
          sandbox="allow-forms allow-modals allow-scripts allow-same-origin allow-popups allow-downloads"
          className="h-full w-full border-0"
          onLoad={() => setLoading(false)}
        />
      </div>
    </div>
  );
}
