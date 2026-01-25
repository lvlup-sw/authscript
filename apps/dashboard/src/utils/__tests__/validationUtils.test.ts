import { describe, it, expect } from 'vitest';
import {
  isValidString,
  isValidNumber,
  hasNonEmptyFields,
  isNonEmptyArray,
  isValidEmail,
  isValidDateString,
  hasRequiredFields,
  allFieldsValid,
} from '../validationUtils';

describe('isValidString', () => {
  it('isValidString_NonEmptyString_ReturnsTrue', () => {
    expect(isValidString('hello')).toBe(true);
  });

  it('isValidString_EmptyString_ReturnsFalse', () => {
    expect(isValidString('')).toBe(false);
  });

  it('isValidString_WhitespaceOnly_ReturnsFalse', () => {
    expect(isValidString('   ')).toBe(false);
  });

  it('isValidString_Null_ReturnsFalse', () => {
    expect(isValidString(null)).toBe(false);
  });

  it('isValidString_Undefined_ReturnsFalse', () => {
    expect(isValidString(undefined)).toBe(false);
  });

  it('isValidString_Number_ReturnsFalse', () => {
    expect(isValidString(123)).toBe(false);
  });

  it('isValidString_Object_ReturnsFalse', () => {
    expect(isValidString({ value: 'test' })).toBe(false);
  });

  it('isValidString_Array_ReturnsFalse', () => {
    expect(isValidString(['test'])).toBe(false);
  });
});

describe('isValidNumber', () => {
  it('isValidNumber_PositiveInteger_ReturnsTrue', () => {
    expect(isValidNumber(42)).toBe(true);
  });

  it('isValidNumber_Zero_ReturnsTrue', () => {
    expect(isValidNumber(0)).toBe(true);
  });

  it('isValidNumber_NegativeNumber_ReturnsTrue', () => {
    expect(isValidNumber(-5)).toBe(true);
  });

  it('isValidNumber_Float_ReturnsTrue', () => {
    expect(isValidNumber(3.14)).toBe(true);
  });

  it('isValidNumber_NaN_ReturnsFalse', () => {
    expect(isValidNumber(NaN)).toBe(false);
  });

  it('isValidNumber_Infinity_ReturnsFalse', () => {
    expect(isValidNumber(Infinity)).toBe(false);
  });

  it('isValidNumber_NegativeInfinity_ReturnsFalse', () => {
    expect(isValidNumber(-Infinity)).toBe(false);
  });

  it('isValidNumber_NumericString_ReturnsFalse', () => {
    expect(isValidNumber('42')).toBe(false);
  });

  it('isValidNumber_Null_ReturnsFalse', () => {
    expect(isValidNumber(null)).toBe(false);
  });

  it('isValidNumber_Undefined_ReturnsFalse', () => {
    expect(isValidNumber(undefined)).toBe(false);
  });
});

describe('hasNonEmptyFields', () => {
  it('hasNonEmptyFields_ObjectWithValues_ReturnsTrue', () => {
    expect(hasNonEmptyFields({ name: 'test', age: 25 })).toBe(true);
  });

  it('hasNonEmptyFields_ObjectWithUnselected_ReturnsFalse', () => {
    expect(hasNonEmptyFields({ status: 'unselected' })).toBe(false);
  });

  it('hasNonEmptyFields_NestedObject_ChecksRecursively', () => {
    expect(hasNonEmptyFields({ nested: { value: 'test' } })).toBe(true);
  });

  it('hasNonEmptyFields_NestedObjectWithUnselected_ReturnsFalse', () => {
    expect(hasNonEmptyFields({ nested: { status: 'unselected' } })).toBe(false);
  });

  it('hasNonEmptyFields_EmptyObject_ReturnsFalse', () => {
    expect(hasNonEmptyFields({})).toBe(false);
  });

  it('hasNonEmptyFields_ObjectWithEmptyString_ReturnsFalse', () => {
    expect(hasNonEmptyFields({ name: '' })).toBe(false);
  });

  it('hasNonEmptyFields_ObjectWithNull_ReturnsFalse', () => {
    expect(hasNonEmptyFields({ value: null })).toBe(false);
  });

  it('hasNonEmptyFields_ObjectWithUndefined_ReturnsFalse', () => {
    expect(hasNonEmptyFields({ value: undefined })).toBe(false);
  });

  it('hasNonEmptyFields_ObjectWithZero_ReturnsTrue', () => {
    expect(hasNonEmptyFields({ count: 0 })).toBe(true);
  });

  it('hasNonEmptyFields_ObjectWithFalse_ReturnsTrue', () => {
    expect(hasNonEmptyFields({ active: false })).toBe(true);
  });

  it('hasNonEmptyFields_MixedValidAndInvalid_ReturnsTrue', () => {
    expect(hasNonEmptyFields({ valid: 'test', invalid: 'unselected' })).toBe(true);
  });

  it('hasNonEmptyFields_ObjectWithEmptyArray_ReturnsFalse', () => {
    expect(hasNonEmptyFields({ items: [] })).toBe(false);
  });

  it('hasNonEmptyFields_ObjectWithNonEmptyArray_ReturnsTrue', () => {
    expect(hasNonEmptyFields({ items: ['a', 'b'] })).toBe(true);
  });
});

describe('isNonEmptyArray', () => {
  it('isNonEmptyArray_ArrayWithElements_ReturnsTrue', () => {
    expect(isNonEmptyArray([1, 2, 3])).toBe(true);
  });

  it('isNonEmptyArray_SingleElement_ReturnsTrue', () => {
    expect(isNonEmptyArray(['single'])).toBe(true);
  });

  it('isNonEmptyArray_EmptyArray_ReturnsFalse', () => {
    expect(isNonEmptyArray([])).toBe(false);
  });

  it('isNonEmptyArray_Null_ReturnsFalse', () => {
    expect(isNonEmptyArray(null)).toBe(false);
  });

  it('isNonEmptyArray_Undefined_ReturnsFalse', () => {
    expect(isNonEmptyArray(undefined)).toBe(false);
  });

  it('isNonEmptyArray_String_ReturnsFalse', () => {
    expect(isNonEmptyArray('not an array')).toBe(false);
  });

  it('isNonEmptyArray_Object_ReturnsFalse', () => {
    expect(isNonEmptyArray({ length: 1 })).toBe(false);
  });
});

describe('isValidEmail', () => {
  it('isValidEmail_StandardEmail_ReturnsTrue', () => {
    expect(isValidEmail('user@example.com')).toBe(true);
  });

  it('isValidEmail_EmailWithSubdomain_ReturnsTrue', () => {
    expect(isValidEmail('user@mail.example.com')).toBe(true);
  });

  it('isValidEmail_EmailWithPlus_ReturnsTrue', () => {
    expect(isValidEmail('user+tag@example.com')).toBe(true);
  });

  it('isValidEmail_EmailWithDots_ReturnsTrue', () => {
    expect(isValidEmail('first.last@example.com')).toBe(true);
  });

  it('isValidEmail_MissingAtSign_ReturnsFalse', () => {
    expect(isValidEmail('userexample.com')).toBe(false);
  });

  it('isValidEmail_MissingDomain_ReturnsFalse', () => {
    expect(isValidEmail('user@')).toBe(false);
  });

  it('isValidEmail_MissingLocalPart_ReturnsFalse', () => {
    expect(isValidEmail('@example.com')).toBe(false);
  });

  it('isValidEmail_EmptyString_ReturnsFalse', () => {
    expect(isValidEmail('')).toBe(false);
  });

  it('isValidEmail_MultipleAtSigns_ReturnsFalse', () => {
    expect(isValidEmail('user@@example.com')).toBe(false);
  });

  it('isValidEmail_MissingTLD_ReturnsFalse', () => {
    expect(isValidEmail('user@example')).toBe(false);
  });
});

describe('isValidDateString', () => {
  it('isValidDateString_ISODate_ReturnsTrue', () => {
    expect(isValidDateString('2024-01-15')).toBe(true);
  });

  it('isValidDateString_ISODateTime_ReturnsTrue', () => {
    expect(isValidDateString('2024-01-15T10:30:00')).toBe(true);
  });

  it('isValidDateString_ISODateTimeWithZ_ReturnsTrue', () => {
    expect(isValidDateString('2024-01-15T10:30:00Z')).toBe(true);
  });

  it('isValidDateString_USFormat_ReturnsTrue', () => {
    expect(isValidDateString('01/15/2024')).toBe(true);
  });

  it('isValidDateString_InvalidFormat_ReturnsFalse', () => {
    expect(isValidDateString('not-a-date')).toBe(false);
  });

  it('isValidDateString_EmptyString_ReturnsFalse', () => {
    expect(isValidDateString('')).toBe(false);
  });

  it('isValidDateString_InvalidMonth_ReturnsFalse', () => {
    expect(isValidDateString('2024-13-01')).toBe(false);
  });

  it('isValidDateString_InvalidDay_ReturnsFalse', () => {
    expect(isValidDateString('2024-01-32')).toBe(false);
  });
});

describe('hasRequiredFields', () => {
  it('hasRequiredFields_AllFieldsPresent_ReturnsTrue', () => {
    const obj = { name: 'John', age: 30, email: 'john@example.com' };
    expect(hasRequiredFields(obj, ['name', 'age'])).toBe(true);
  });

  it('hasRequiredFields_AllRequiredFieldsPresent_ReturnsTrue', () => {
    const obj = { name: 'John', age: 30 };
    expect(hasRequiredFields(obj, ['name', 'age'])).toBe(true);
  });

  it('hasRequiredFields_MissingField_ReturnsFalse', () => {
    const obj: { name: string; age?: number } = { name: 'John' };
    expect(hasRequiredFields(obj, ['name', 'age'])).toBe(false);
  });

  it('hasRequiredFields_FieldIsNull_ReturnsFalse', () => {
    const obj = { name: 'John', age: null };
    expect(hasRequiredFields(obj, ['name', 'age'])).toBe(false);
  });

  it('hasRequiredFields_FieldIsUndefined_ReturnsFalse', () => {
    const obj = { name: 'John', age: undefined };
    expect(hasRequiredFields(obj, ['name', 'age'])).toBe(false);
  });

  it('hasRequiredFields_EmptyRequiredList_ReturnsTrue', () => {
    const obj = { name: 'John' };
    expect(hasRequiredFields(obj, [])).toBe(true);
  });

  it('hasRequiredFields_FieldIsZero_ReturnsTrue', () => {
    const obj = { name: 'John', count: 0 };
    expect(hasRequiredFields(obj, ['name', 'count'])).toBe(true);
  });

  it('hasRequiredFields_FieldIsFalse_ReturnsTrue', () => {
    const obj = { name: 'John', active: false };
    expect(hasRequiredFields(obj, ['name', 'active'])).toBe(true);
  });

  it('hasRequiredFields_FieldIsEmptyString_ReturnsTrue', () => {
    const obj = { name: '', age: 30 };
    expect(hasRequiredFields(obj, ['name', 'age'])).toBe(true);
  });
});

describe('allFieldsValid', () => {
  it('allFieldsValid_AllFieldsPassValidator_ReturnsTrue', () => {
    const obj = { a: 'hello', b: 'world' };
    expect(allFieldsValid(obj, isValidString)).toBe(true);
  });

  it('allFieldsValid_SomeFieldsFailValidator_ReturnsFalse', () => {
    const obj = { a: 'hello', b: '' };
    expect(allFieldsValid(obj, isValidString)).toBe(false);
  });

  it('allFieldsValid_EmptyObject_ReturnsTrue', () => {
    const obj = {};
    expect(allFieldsValid(obj, isValidString)).toBe(true);
  });

  it('allFieldsValid_AllNumbersValid_ReturnsTrue', () => {
    const obj = { x: 1, y: 2, z: 3 };
    expect(allFieldsValid(obj, isValidNumber)).toBe(true);
  });

  it('allFieldsValid_ContainsNaN_ReturnsFalse', () => {
    const obj = { x: 1, y: NaN };
    expect(allFieldsValid(obj, isValidNumber)).toBe(false);
  });

  it('allFieldsValid_CustomValidator_ReturnsTrue', () => {
    const obj = { a: 10, b: 20 };
    const isPositive = (value: unknown): boolean =>
      typeof value === 'number' && value > 0;
    expect(allFieldsValid(obj, isPositive)).toBe(true);
  });

  it('allFieldsValid_CustomValidator_ReturnsFalse', () => {
    const obj = { a: 10, b: -5 };
    const isPositive = (value: unknown): boolean =>
      typeof value === 'number' && value > 0;
    expect(allFieldsValid(obj, isPositive)).toBe(false);
  });
});
