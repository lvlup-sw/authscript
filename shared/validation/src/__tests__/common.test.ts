import { describe, it, expect } from 'vitest';
import {
  IsoDateTimeSchema,
  IsoDateSchema,
  SortDirectionSchema,
  PaginationQuerySchema,
  NonEmptyStringSchema,
  UuidSchema,
} from '../common';

describe('IsoDateTimeSchema', () => {
  it('IsoDateTimeSchema_ValidDateTime_Passes', () => {
    const result = IsoDateTimeSchema.safeParse('2026-01-24T10:30:00Z');
    expect(result.success).toBe(true);
  });

  it('IsoDateTimeSchema_InvalidFormat_Fails', () => {
    const result = IsoDateTimeSchema.safeParse('2026-01-24');
    expect(result.success).toBe(false);
  });

  it('IsoDateTimeSchema_InvalidString_Fails', () => {
    const result = IsoDateTimeSchema.safeParse('not-a-date');
    expect(result.success).toBe(false);
  });
});

describe('IsoDateSchema', () => {
  it('IsoDateSchema_ValidDate_Passes', () => {
    const result = IsoDateSchema.safeParse('2026-01-24');
    expect(result.success).toBe(true);
  });

  it('IsoDateSchema_InvalidFormat_Fails', () => {
    const result = IsoDateSchema.safeParse('01-24-2026');
    expect(result.success).toBe(false);
  });

  it('IsoDateSchema_DateTime_Fails', () => {
    const result = IsoDateSchema.safeParse('2026-01-24T10:30:00Z');
    expect(result.success).toBe(false);
  });
});

describe('SortDirectionSchema', () => {
  it('SortDirectionSchema_Asc_Passes', () => {
    const result = SortDirectionSchema.safeParse('asc');
    expect(result.success).toBe(true);
    expect(result.data).toBe('asc');
  });

  it('SortDirectionSchema_Desc_Passes', () => {
    const result = SortDirectionSchema.safeParse('desc');
    expect(result.success).toBe(true);
    expect(result.data).toBe('desc');
  });

  it('SortDirectionSchema_InvalidValue_Fails', () => {
    const result = SortDirectionSchema.safeParse('up');
    expect(result.success).toBe(false);
  });
});

describe('PaginationQuerySchema', () => {
  it('PaginationQuerySchema_ValidParams_ParsesCorrectly', () => {
    const result = PaginationQuerySchema.safeParse({
      pageNumber: '2',
      pageSize: '50',
      sortDirection: 'asc',
    });
    expect(result.success).toBe(true);
    expect(result.data).toEqual({
      pageNumber: 2,
      pageSize: 50,
      sortDirection: 'asc',
    });
  });

  it('PaginationQuerySchema_MissingParams_UsesDefaults', () => {
    const result = PaginationQuerySchema.safeParse({});
    expect(result.success).toBe(true);
    expect(result.data).toEqual({
      pageNumber: 1,
      pageSize: 20,
      sortDirection: 'desc',
    });
  });

  it('PaginationQuerySchema_InvalidPageSize_Clamps', () => {
    const result = PaginationQuerySchema.safeParse({
      pageSize: '200', // Over max of 100
    });
    expect(result.success).toBe(false);
  });

  it('PaginationQuerySchema_NegativePageNumber_Fails', () => {
    const result = PaginationQuerySchema.safeParse({
      pageNumber: '-1',
    });
    expect(result.success).toBe(false);
  });
});

describe('NonEmptyStringSchema', () => {
  it('NonEmptyStringSchema_ValidString_TrimsAndPasses', () => {
    const result = NonEmptyStringSchema.safeParse('  hello  ');
    expect(result.success).toBe(true);
    expect(result.data).toBe('hello');
  });

  it('NonEmptyStringSchema_EmptyString_Fails', () => {
    const result = NonEmptyStringSchema.safeParse('');
    expect(result.success).toBe(false);
  });

  it('NonEmptyStringSchema_WhitespaceOnly_Fails', () => {
    const result = NonEmptyStringSchema.safeParse('   ');
    expect(result.success).toBe(false);
  });
});

describe('UuidSchema', () => {
  it('UuidSchema_ValidUuid_Passes', () => {
    const result = UuidSchema.safeParse('550e8400-e29b-41d4-a716-446655440000');
    expect(result.success).toBe(true);
  });

  it('UuidSchema_InvalidUuid_Fails', () => {
    const result = UuidSchema.safeParse('not-a-uuid');
    expect(result.success).toBe(false);
  });

  it('UuidSchema_TooShort_Fails', () => {
    const result = UuidSchema.safeParse('550e8400-e29b-41d4');
    expect(result.success).toBe(false);
  });
});
