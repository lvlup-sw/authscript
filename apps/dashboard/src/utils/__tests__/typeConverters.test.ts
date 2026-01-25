import { describe, it, expect } from 'vitest';
import {
  parseDouble,
  parseInt32,
  parsePercentage,
  parseTimeToSeconds,
  parseTimeToMilliseconds,
  hasValue,
} from '../typeConverters';

describe('parseDouble', () => {
  it('parseDouble_ValidNumber_ReturnsNumber', () => {
    expect(parseDouble('3.14')).toBe(3.14);
  });

  it('parseDouble_ValidNegativeNumber_ReturnsNumber', () => {
    expect(parseDouble('-2.5')).toBe(-2.5);
  });

  it('parseDouble_ValidInteger_ReturnsNumber', () => {
    expect(parseDouble('42')).toBe(42);
  });

  it('parseDouble_Null_ReturnsDefault', () => {
    expect(parseDouble(null)).toBe(0);
    expect(parseDouble(null, 5)).toBe(5);
  });

  it('parseDouble_Undefined_ReturnsDefault', () => {
    expect(parseDouble(undefined)).toBe(0);
    expect(parseDouble(undefined, 10)).toBe(10);
  });

  it('parseDouble_EmptyString_ReturnsDefault', () => {
    expect(parseDouble('')).toBe(0);
    expect(parseDouble('', 7.5)).toBe(7.5);
  });

  it('parseDouble_Unselected_ReturnsDefault', () => {
    expect(parseDouble('unselected')).toBe(0);
    expect(parseDouble('unselected', 3)).toBe(3);
  });

  it('parseDouble_WhitespaceOnly_ReturnsDefault', () => {
    expect(parseDouble('   ')).toBe(0);
  });

  it('parseDouble_InvalidString_ReturnsDefault', () => {
    expect(parseDouble('abc')).toBe(0);
    expect(parseDouble('abc', 99)).toBe(99);
  });

  it('parseDouble_NumberWithWhitespace_ReturnsNumber', () => {
    expect(parseDouble('  3.14  ')).toBe(3.14);
  });
});

describe('parseInt32', () => {
  it('parseInt32_ValidInteger_ReturnsInteger', () => {
    expect(parseInt32('42')).toBe(42);
  });

  it('parseInt32_ValidNegativeInteger_ReturnsInteger', () => {
    expect(parseInt32('-17')).toBe(-17);
  });

  it('parseInt32_FloatValue_ReturnsTruncatedInteger', () => {
    expect(parseInt32('3.9')).toBe(3);
    expect(parseInt32('3.1')).toBe(3);
  });

  it('parseInt32_Null_ReturnsDefault', () => {
    expect(parseInt32(null)).toBe(0);
    expect(parseInt32(null, 5)).toBe(5);
  });

  it('parseInt32_Undefined_ReturnsDefault', () => {
    expect(parseInt32(undefined)).toBe(0);
    expect(parseInt32(undefined, 10)).toBe(10);
  });

  it('parseInt32_EmptyString_ReturnsDefault', () => {
    expect(parseInt32('')).toBe(0);
  });

  it('parseInt32_Unselected_ReturnsDefault', () => {
    expect(parseInt32('unselected')).toBe(0);
    expect(parseInt32('unselected', 8)).toBe(8);
  });

  it('parseInt32_InvalidString_ReturnsDefault', () => {
    expect(parseInt32('abc')).toBe(0);
  });

  it('parseInt32_NumberWithWhitespace_ReturnsInteger', () => {
    expect(parseInt32('  42  ')).toBe(42);
  });
});

describe('parsePercentage', () => {
  it('parsePercentage_PercentSuffix_ReturnsDecimal', () => {
    expect(parsePercentage('95%')).toBe(95);
  });

  it('parsePercentage_WholeNumber_ReturnsAsIs', () => {
    expect(parsePercentage('95')).toBe(95);
  });

  it('parsePercentage_DecimalLessThanOne_ReturnsAsPercent', () => {
    expect(parsePercentage('0.95')).toBe(95);
  });

  it('parsePercentage_DecimalGreaterThanOne_ReturnsAsIs', () => {
    expect(parsePercentage('1.5')).toBe(1.5);
  });

  it('parsePercentage_Zero_ReturnsZero', () => {
    expect(parsePercentage('0')).toBe(0);
    expect(parsePercentage('0%')).toBe(0);
  });

  it('parsePercentage_Null_ReturnsDefault', () => {
    expect(parsePercentage(null)).toBe(0);
    expect(parsePercentage(null, 50)).toBe(50);
  });

  it('parsePercentage_Undefined_ReturnsDefault', () => {
    expect(parsePercentage(undefined)).toBe(0);
  });

  it('parsePercentage_EmptyString_ReturnsDefault', () => {
    expect(parsePercentage('')).toBe(0);
  });

  it('parsePercentage_Unselected_ReturnsDefault', () => {
    expect(parsePercentage('unselected')).toBe(0);
  });

  it('parsePercentage_InvalidString_ReturnsDefault', () => {
    expect(parsePercentage('abc')).toBe(0);
  });

  it('parsePercentage_PercentWithWhitespace_ReturnsDecimal', () => {
    expect(parsePercentage('  95%  ')).toBe(95);
  });
});

describe('parseTimeToSeconds', () => {
  it('parseTimeToSeconds_MMSSFormat_ReturnsSeconds', () => {
    expect(parseTimeToSeconds('1:30')).toBe(90);
  });

  it('parseTimeToSeconds_MMSSFormatZeroSeconds_ReturnsSeconds', () => {
    expect(parseTimeToSeconds('2:00')).toBe(120);
  });

  it('parseTimeToSeconds_MMSSFormatLeadingZero_ReturnsSeconds', () => {
    expect(parseTimeToSeconds('0:45')).toBe(45);
  });

  it('parseTimeToSeconds_SecondsSuffix_ReturnsSeconds', () => {
    expect(parseTimeToSeconds('90s')).toBe(90);
  });

  it('parseTimeToSeconds_SecondsSuffixUppercase_ReturnsSeconds', () => {
    expect(parseTimeToSeconds('90S')).toBe(90);
  });

  it('parseTimeToSeconds_MinutesSuffix_ReturnsSeconds', () => {
    expect(parseTimeToSeconds('1.5m')).toBe(90);
  });

  it('parseTimeToSeconds_MinutesSuffixUppercase_ReturnsSeconds', () => {
    expect(parseTimeToSeconds('2M')).toBe(120);
  });

  it('parseTimeToSeconds_NoSuffix_ReturnsSeconds', () => {
    expect(parseTimeToSeconds('90')).toBe(90);
  });

  it('parseTimeToSeconds_MillisecondsSuffix_ReturnsSeconds', () => {
    expect(parseTimeToSeconds('1500ms')).toBe(1.5);
  });

  it('parseTimeToSeconds_Null_ReturnsDefault', () => {
    expect(parseTimeToSeconds(null)).toBe(0);
    expect(parseTimeToSeconds(null, 60)).toBe(60);
  });

  it('parseTimeToSeconds_Undefined_ReturnsDefault', () => {
    expect(parseTimeToSeconds(undefined)).toBe(0);
  });

  it('parseTimeToSeconds_EmptyString_ReturnsDefault', () => {
    expect(parseTimeToSeconds('')).toBe(0);
  });

  it('parseTimeToSeconds_Unselected_ReturnsDefault', () => {
    expect(parseTimeToSeconds('unselected')).toBe(0);
  });

  it('parseTimeToSeconds_InvalidString_ReturnsDefault', () => {
    expect(parseTimeToSeconds('abc')).toBe(0);
  });

  it('parseTimeToSeconds_WithWhitespace_ReturnsSeconds', () => {
    expect(parseTimeToSeconds('  1:30  ')).toBe(90);
  });
});

describe('parseTimeToMilliseconds', () => {
  it('parseTimeToMilliseconds_MMSSFormat_ReturnsMilliseconds', () => {
    expect(parseTimeToMilliseconds('1:30')).toBe(90000);
  });

  it('parseTimeToMilliseconds_SecondsSuffix_ReturnsMilliseconds', () => {
    expect(parseTimeToMilliseconds('90s')).toBe(90000);
  });

  it('parseTimeToMilliseconds_MinutesSuffix_ReturnsMilliseconds', () => {
    expect(parseTimeToMilliseconds('1.5m')).toBe(90000);
  });

  it('parseTimeToMilliseconds_MillisecondsSuffix_ReturnsMilliseconds', () => {
    expect(parseTimeToMilliseconds('1500ms')).toBe(1500);
  });

  it('parseTimeToMilliseconds_NoSuffix_ReturnsMilliseconds', () => {
    expect(parseTimeToMilliseconds('90')).toBe(90000);
  });

  it('parseTimeToMilliseconds_Null_ReturnsDefault', () => {
    expect(parseTimeToMilliseconds(null)).toBe(0);
    expect(parseTimeToMilliseconds(null, 1000)).toBe(1000);
  });

  it('parseTimeToMilliseconds_Undefined_ReturnsDefault', () => {
    expect(parseTimeToMilliseconds(undefined)).toBe(0);
  });

  it('parseTimeToMilliseconds_EmptyString_ReturnsDefault', () => {
    expect(parseTimeToMilliseconds('')).toBe(0);
  });

  it('parseTimeToMilliseconds_Unselected_ReturnsDefault', () => {
    expect(parseTimeToMilliseconds('unselected')).toBe(0);
  });

  it('parseTimeToMilliseconds_InvalidString_ReturnsDefault', () => {
    expect(parseTimeToMilliseconds('abc')).toBe(0);
  });
});

describe('hasValue', () => {
  it('hasValue_NonEmptyString_ReturnsTrue', () => {
    expect(hasValue('test')).toBe(true);
  });

  it('hasValue_StringWithSpaces_ReturnsTrue', () => {
    expect(hasValue('hello world')).toBe(true);
  });

  it('hasValue_Null_ReturnsFalse', () => {
    expect(hasValue(null)).toBe(false);
  });

  it('hasValue_Undefined_ReturnsFalse', () => {
    expect(hasValue(undefined)).toBe(false);
  });

  it('hasValue_EmptyString_ReturnsFalse', () => {
    expect(hasValue('')).toBe(false);
  });

  it('hasValue_Unselected_ReturnsFalse', () => {
    expect(hasValue('unselected')).toBe(false);
  });

  it('hasValue_EmptyArray_ReturnsFalse', () => {
    expect(hasValue([])).toBe(false);
  });

  it('hasValue_NonEmptyArray_ReturnsTrue', () => {
    expect(hasValue([1, 2, 3])).toBe(true);
    expect(hasValue(['a'])).toBe(true);
  });

  it('hasValue_Zero_ReturnsTrue', () => {
    expect(hasValue(0)).toBe(true);
  });

  it('hasValue_False_ReturnsTrue', () => {
    expect(hasValue(false)).toBe(true);
  });

  it('hasValue_True_ReturnsTrue', () => {
    expect(hasValue(true)).toBe(true);
  });

  it('hasValue_Object_ReturnsTrue', () => {
    expect(hasValue({ key: 'value' })).toBe(true);
  });

  it('hasValue_EmptyObject_ReturnsTrue', () => {
    // Empty objects still have a value, unlike empty arrays
    expect(hasValue({})).toBe(true);
  });

  it('hasValue_WhitespaceOnlyString_ReturnsFalse', () => {
    expect(hasValue('   ')).toBe(false);
  });

  it('hasValue_NaN_ReturnsFalse', () => {
    expect(hasValue(NaN)).toBe(false);
  });
});
