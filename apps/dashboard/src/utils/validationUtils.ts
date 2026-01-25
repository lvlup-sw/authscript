/**
 * Validation utility functions for type checking and data validation.
 * Ported from ares-elite-platform patterns.
 */

/**
 * Validates that a value is a non-empty string.
 * @param value - Value to check
 * @returns true if value is a non-empty string (after trimming whitespace)
 */
export function isValidString(value: unknown): value is string {
  if (typeof value !== 'string') return false;
  return value.trim().length > 0;
}

/**
 * Validates that a value is a valid number (not NaN or Infinity).
 * @param value - Value to check
 * @returns true if value is a finite number
 */
export function isValidNumber(value: unknown): value is number {
  if (typeof value !== 'number') return false;
  return Number.isFinite(value);
}

/**
 * Recursively checks if an object has at least one non-empty field.
 * Treats 'unselected' string values as empty.
 * @param obj - Object to check
 * @returns true if object has at least one non-empty value
 */
export function hasNonEmptyFields(obj: Record<string, unknown>): boolean {
  const keys = Object.keys(obj);
  if (keys.length === 0) return false;

  for (const key of keys) {
    const value = obj[key];

    // Null or undefined are empty
    if (value === null || value === undefined) continue;

    // 'unselected' string is treated as empty
    if (value === 'unselected') continue;

    // Empty string is empty
    if (typeof value === 'string' && value.trim() === '') continue;

    // Empty array is empty
    if (Array.isArray(value)) {
      if (value.length === 0) continue;
      return true;
    }

    // Recursively check nested objects
    if (typeof value === 'object' && value !== null) {
      if (hasNonEmptyFields(value as Record<string, unknown>)) {
        return true;
      }
      continue;
    }

    // Numbers (including 0), booleans (including false), and non-empty strings are valid
    return true;
  }

  return false;
}

/**
 * Validates that a value is a non-empty array.
 * @param value - Value to check
 * @returns true if value is an array with at least one element
 */
export function isNonEmptyArray<T>(value: unknown): value is T[] {
  if (!Array.isArray(value)) return false;
  return value.length > 0;
}

/**
 * Validates an email address using basic format validation.
 * @param email - Email string to validate
 * @returns true if email has valid format
 */
export function isValidEmail(email: string): boolean {
  if (!email || email.trim() === '') return false;

  // Basic email regex: local@domain.tld
  // Requires: local part, single @, domain, dot, TLD
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  // Check for multiple @ signs
  const atCount = (email.match(/@/g) || []).length;
  if (atCount !== 1) return false;

  return emailRegex.test(email);
}

/**
 * Validates that a date string is parseable.
 * @param dateString - Date string to validate
 * @returns true if date string can be parsed to a valid date
 */
export function isValidDateString(dateString: string): boolean {
  if (!dateString || dateString.trim() === '') return false;

  const date = new Date(dateString);

  // Check if date is valid (not Invalid Date)
  if (isNaN(date.getTime())) return false;

  // Additional validation for ISO date format to catch invalid months/days
  // that JavaScript Date might "correct" (e.g., month 13 becomes month 1 of next year)
  if (dateString.match(/^\d{4}-\d{2}-\d{2}/)) {
    const parts = dateString.split('T')[0].split('-');
    const year = parseInt(parts[0], 10);
    const month = parseInt(parts[1], 10);
    const day = parseInt(parts[2], 10);

    // Check month is valid (1-12)
    if (month < 1 || month > 12) return false;

    // Check day is valid for the month
    const daysInMonth = new Date(year, month, 0).getDate();
    if (day < 1 || day > daysInMonth) return false;
  }

  return true;
}

/**
 * Checks that all required fields exist and are non-null/undefined.
 * Note: Empty strings, 0, and false are considered valid (present).
 * @param obj - Object to check
 * @param requiredFields - Array of field names that must be present
 * @returns true if all required fields exist and are not null/undefined
 */
export function hasRequiredFields<T extends Record<string, unknown>>(
  obj: T,
  requiredFields: (keyof T)[]
): boolean {
  if (requiredFields.length === 0) return true;

  for (const field of requiredFields) {
    if (!(field in obj)) return false;
    if (obj[field] === null || obj[field] === undefined) return false;
  }

  return true;
}

/**
 * Checks that all fields in an object pass the provided validator function.
 * @param obj - Object whose fields to validate
 * @param validator - Function to validate each field value
 * @returns true if all fields pass validation (or object is empty)
 */
export function allFieldsValid<T extends Record<string, unknown>>(
  obj: T,
  validator: (value: unknown) => boolean
): boolean {
  const values = Object.values(obj);
  if (values.length === 0) return true;

  return values.every(validator);
}
