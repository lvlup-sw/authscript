/**
 * Extracts initials from a full name string.
 * Returns up to 2 uppercase characters.
 */
export function getInitials(name: string): string {
  return name
    .split(' ')
    .map((part) => part[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
}

/**
 * Returns a Tailwind color class for confidence scores.
 * >= 80: green, >= 60: amber, < 60: red
 */
export function getConfidenceColor(confidence: number): string {
  if (confidence >= 80) return 'text-green-600';
  if (confidence >= 60) return 'text-amber-600';
  return 'text-red-600';
}
