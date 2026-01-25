/**
 * Common validation schemas
 */

import { z } from 'zod';

/**
 * ISO 8601 datetime string validation
 */
export const IsoDateTimeSchema = z
  .string()
  .datetime({ message: 'Must be valid ISO 8601 datetime' });

/**
 * Date-only string validation (YYYY-MM-DD format)
 */
export const IsoDateSchema = z
  .string()
  .regex(/^\d{4}-\d{2}-\d{2}$/, 'Must be YYYY-MM-DD format');

/**
 * Sort direction
 */
export const SortDirectionSchema = z.enum(['asc', 'desc']);
export type SortDirection = z.infer<typeof SortDirectionSchema>;

/**
 * Standard pagination query parameters
 * Accepts string values from query params and transforms to numbers
 */
export const PaginationQuerySchema = z.object({
  pageNumber: z
    .string()
    .optional()
    .transform((val) => (val ? parseInt(val, 10) : 1))
    .pipe(z.number().int().min(1).default(1)),
  pageSize: z
    .string()
    .optional()
    .transform((val) => (val ? parseInt(val, 10) : 20))
    .pipe(z.number().int().min(1).max(100).default(20)),
  sortDirection: SortDirectionSchema.default('desc'),
});
export type PaginationQuery = z.infer<typeof PaginationQuerySchema>;

/**
 * Non-empty string with trimming
 */
export const NonEmptyStringSchema = z
  .string()
  .min(1, 'This field is required')
  .transform((val) => val.trim())
  .refine((val) => val.length > 0, 'This field is required');

/**
 * UUID validation
 */
export const UuidSchema = z.string().uuid('Must be a valid UUID');
