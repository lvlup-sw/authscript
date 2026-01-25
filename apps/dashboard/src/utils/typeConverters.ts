/**
 * Type converter utilities for parsing string values with robust error handling.
 * Handles edge cases including null, undefined, empty strings, and "unselected" sentinel values.
 */

/**
 * Parse a string value to a double (floating-point number).
 * Handles edge cases: empty strings, null, undefined, "unselected"
 *
 * @param value - String value to parse
 * @param defaultValue - Default value if parsing fails (default: 0)
 * @returns Parsed number or default value
 */
export function parseDouble(
  value: string | undefined | null,
  defaultValue: number = 0
): number {
  // Guard clause: return default for null/undefined
  if (value === null || value === undefined) {
    return defaultValue;
  }

  const trimmed = value.trim();

  // Guard clause: handle empty string and "unselected" sentinel value
  if (trimmed === '' || trimmed === 'unselected') {
    return defaultValue;
  }

  const parsed = parseFloat(trimmed);

  // Return parsed value if valid, otherwise default
  return isNaN(parsed) ? defaultValue : parsed;
}

/**
 * Parse a string value to a 32-bit integer.
 * Truncates decimal values (does not round).
 * Handles edge cases: empty strings, null, undefined, "unselected"
 *
 * @param value - String value to parse
 * @param defaultValue - Default value if parsing fails (default: 0)
 * @returns Parsed integer or default value
 */
export function parseInt32(
  value: string | undefined | null,
  defaultValue: number = 0
): number {
  // Guard clause: return default for null/undefined
  if (value === null || value === undefined) {
    return defaultValue;
  }

  const trimmed = value.trim();

  // Guard clause: handle empty string and "unselected" sentinel value
  if (trimmed === '' || trimmed === 'unselected') {
    return defaultValue;
  }

  const parsed = parseInt(trimmed, 10);

  // Return parsed value if valid, otherwise default
  return isNaN(parsed) ? defaultValue : parsed;
}

/**
 * Parse a string value to a percentage as a number (0-100 scale).
 * Handles multiple formats:
 * - "95%" -> 95
 * - "95" -> 95
 * - "0.95" (values < 1) -> 95 (converted from decimal)
 *
 * @param value - String value to parse
 * @param defaultValue - Default value if parsing fails (default: 0)
 * @returns Percentage as a number (0-100 scale) or default value
 */
export function parsePercentage(
  value: string | undefined | null,
  defaultValue: number = 0
): number {
  // Guard clause: return default for null/undefined
  if (value === null || value === undefined) {
    return defaultValue;
  }

  const trimmed = value.trim();

  // Guard clause: handle empty string and "unselected" sentinel value
  if (trimmed === '' || trimmed === 'unselected') {
    return defaultValue;
  }

  // Handle percentage suffix
  if (trimmed.endsWith('%')) {
    const numPart = trimmed.slice(0, -1);
    const parsed = parseFloat(numPart);
    return isNaN(parsed) ? defaultValue : parsed;
  }

  const parsed = parseFloat(trimmed);

  // Return default for invalid numbers
  if (isNaN(parsed)) {
    return defaultValue;
  }

  // Convert decimal to percentage (0.95 -> 95)
  // Only convert if value is between 0 and 1 (exclusive of 1)
  if (parsed > 0 && parsed < 1) {
    return parsed * 100;
  }

  return parsed;
}

/**
 * Parse a time string value to seconds.
 * Handles multiple formats:
 * - "1:30" (MM:SS) -> 90
 * - "90s" -> 90
 * - "1.5m" -> 90
 * - "1500ms" -> 1.5
 * - "90" (plain number, treated as seconds) -> 90
 *
 * @param value - String value to parse
 * @param defaultValue - Default value if parsing fails (default: 0)
 * @returns Time in seconds or default value
 */
export function parseTimeToSeconds(
  value: string | undefined | null,
  defaultValue: number = 0
): number {
  // Guard clause: return default for null/undefined
  if (value === null || value === undefined) {
    return defaultValue;
  }

  const trimmed = value.trim();

  // Guard clause: handle empty string and "unselected" sentinel value
  if (trimmed === '' || trimmed === 'unselected') {
    return defaultValue;
  }

  // Handle MM:SS format
  if (trimmed.includes(':')) {
    const parts = trimmed.split(':');
    if (parts.length === 2) {
      const minutes = parseInt(parts[0], 10);
      const seconds = parseInt(parts[1], 10);
      if (!isNaN(minutes) && !isNaN(seconds)) {
        return minutes * 60 + seconds;
      }
    }
    return defaultValue;
  }

  const lowerTrimmed = trimmed.toLowerCase();

  // Handle milliseconds suffix
  if (lowerTrimmed.endsWith('ms')) {
    const numPart = lowerTrimmed.slice(0, -2);
    const parsed = parseFloat(numPart);
    return isNaN(parsed) ? defaultValue : parsed / 1000;
  }

  // Handle seconds suffix
  if (lowerTrimmed.endsWith('s')) {
    const numPart = lowerTrimmed.slice(0, -1);
    const parsed = parseFloat(numPart);
    return isNaN(parsed) ? defaultValue : parsed;
  }

  // Handle minutes suffix
  if (lowerTrimmed.endsWith('m')) {
    const numPart = lowerTrimmed.slice(0, -1);
    const parsed = parseFloat(numPart);
    return isNaN(parsed) ? defaultValue : parsed * 60;
  }

  // Plain number treated as seconds
  const parsed = parseFloat(trimmed);
  return isNaN(parsed) ? defaultValue : parsed;
}

/**
 * Parse a time string value to milliseconds.
 * Handles multiple formats:
 * - "1:30" (MM:SS) -> 90000
 * - "90s" -> 90000
 * - "1.5m" -> 90000
 * - "1500ms" -> 1500
 * - "90" (plain number, treated as seconds) -> 90000
 *
 * @param value - String value to parse
 * @param defaultValue - Default value if parsing fails (default: 0)
 * @returns Time in milliseconds or default value
 */
export function parseTimeToMilliseconds(
  value: string | undefined | null,
  defaultValue: number = 0
): number {
  // Guard clause: return default for null/undefined
  if (value === null || value === undefined) {
    return defaultValue;
  }

  const trimmed = value.trim();

  // Guard clause: handle empty string and "unselected" sentinel value
  if (trimmed === '' || trimmed === 'unselected') {
    return defaultValue;
  }

  const lowerTrimmed = trimmed.toLowerCase();

  // Handle milliseconds suffix directly
  if (lowerTrimmed.endsWith('ms')) {
    const numPart = lowerTrimmed.slice(0, -2);
    const parsed = parseFloat(numPart);
    return isNaN(parsed) ? defaultValue : parsed;
  }

  // For all other formats, convert to seconds first then to milliseconds
  const seconds = parseTimeToSeconds(value, defaultValue / 1000);
  return seconds * 1000;
}

/**
 * Check if a value is meaningful (not null, undefined, empty, or "unselected").
 * Useful for form validation and conditional rendering.
 *
 * @param value - Value to check
 * @returns True if the value is meaningful, false otherwise
 */
export function hasValue(value: unknown): boolean {
  // Handle null and undefined
  if (value === null || value === undefined) {
    return false;
  }

  // Handle NaN
  if (typeof value === 'number' && isNaN(value)) {
    return false;
  }

  // Handle strings - empty and "unselected" are falsy
  if (typeof value === 'string') {
    const trimmed = value.trim();
    return trimmed !== '' && trimmed !== 'unselected';
  }

  // Handle arrays - empty arrays are falsy
  if (Array.isArray(value)) {
    return value.length > 0;
  }

  // All other values (numbers including 0, booleans, objects) are truthy
  return true;
}
