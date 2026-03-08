interface EvidenceTagProps {
  source: string;
}

export function EvidenceTag({ source }: EvidenceTagProps) {
  return (
    <span className="inline-flex items-center rounded px-1.5 py-0.5 bg-slate-100 font-mono text-[10px] uppercase tracking-widest text-slate-500">
      <span className="sr-only">Sourced from: </span>
      {source}
    </span>
  );
}
