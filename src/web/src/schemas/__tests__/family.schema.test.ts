/**
 * Family Schema Tests
 * Unit tests for family form validation
 */

import { describe, it, expect } from 'vitest';
import { familyFormSchema, familyAddressSchema } from '../family.schema';

describe('familyAddressSchema', () => {
  const validAddress = {
    street1: '123 Main St',
    city: 'Springfield',
    state: 'CA',
    postalCode: '12345',
  };

  it('should validate a complete address', () => {
    const result = familyAddressSchema.safeParse(validAddress);
    expect(result.success).toBe(true);
  });

  it('should accept optional street2', () => {
    const result = familyAddressSchema.safeParse({
      ...validAddress,
      street2: 'Apt 4B',
    });
    expect(result.success).toBe(true);
  });

  it('should require street1', () => {
    const result = familyAddressSchema.safeParse({
      ...validAddress,
      street1: '',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Street address is required');
    }
  });

  it('should require city', () => {
    const result = familyAddressSchema.safeParse({
      ...validAddress,
      city: '',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('City is required');
    }
  });

  it('should validate state format', () => {
    const result = familyAddressSchema.safeParse({
      ...validAddress,
      state: 'California', // Should be 2 letters
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('2 characters');
    }
  });

  it('should require uppercase state code', () => {
    const result = familyAddressSchema.safeParse({
      ...validAddress,
      state: 'ca', // Should be uppercase
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('uppercase');
    }
  });

  it('should validate postal code format', () => {
    const validCodes = ['12345', '12345-6789'];
    validCodes.forEach((postalCode) => {
      const result = familyAddressSchema.safeParse({
        ...validAddress,
        postalCode,
      });
      expect(result.success).toBe(true);
    });
  });

  it('should reject invalid postal codes', () => {
    const invalidCodes = ['1234', 'abcde', '12345-678', '123456'];
    invalidCodes.forEach((postalCode) => {
      const result = familyAddressSchema.safeParse({
        ...validAddress,
        postalCode,
      });
      expect(result.success).toBe(false);
    });
  });
});

describe('familyFormSchema', () => {
  const validFamily = {
    name: 'Smith Family',
  };

  it('should validate a minimal valid family', () => {
    const result = familyFormSchema.safeParse(validFamily);
    expect(result.success).toBe(true);
  });

  it('should require family name', () => {
    const result = familyFormSchema.safeParse({
      name: '',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Family name is required');
    }
  });

  it('should enforce maximum length on family name', () => {
    const result = familyFormSchema.safeParse({
      name: 'a'.repeat(101),
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('100 characters or less');
    }
  });

  it('should accept optional campusId', () => {
    const result = familyFormSchema.safeParse({
      ...validFamily,
      campusId: 'abc123',
    });
    expect(result.success).toBe(true);
  });

  it('should accept empty campusId', () => {
    const result = familyFormSchema.safeParse({
      ...validFamily,
      campusId: '',
    });
    expect(result.success).toBe(true);
  });

  it('should accept family with complete address', () => {
    const result = familyFormSchema.safeParse({
      ...validFamily,
      street1: '123 Main St',
      street2: 'Apt 4B',
      city: 'Springfield',
      state: 'CA',
      postalCode: '12345',
    });
    expect(result.success).toBe(true);
  });

  it('should validate partial address - require all if any field is set', () => {
    const result = familyFormSchema.safeParse({
      ...validFamily,
      street1: '123 Main St',
      // Missing city, state, postalCode
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.length).toBeGreaterThan(0);
      const errorPaths = result.error.issues.map((e) => e.path[0]);
      expect(errorPaths).toContain('city');
      expect(errorPaths).toContain('state');
      expect(errorPaths).toContain('postalCode');
    }
  });

  it('should validate state format when partial address provided', () => {
    const result = familyFormSchema.safeParse({
      ...validFamily,
      street1: '123 Main St',
      city: 'Springfield',
      state: 'California', // Invalid - should be 2 letters
      postalCode: '12345',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      const stateError = result.error.issues.find((e) => e.path[0] === 'state');
      expect(stateError?.message).toContain('2 uppercase letters');
    }
  });

  it('should validate postal code format when partial address provided', () => {
    const result = familyFormSchema.safeParse({
      ...validFamily,
      street1: '123 Main St',
      city: 'Springfield',
      state: 'CA',
      postalCode: '1234', // Invalid - too short
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      const postalError = result.error.issues.find((e) => e.path[0] === 'postalCode');
      expect(postalError?.message).toContain('Invalid postal code format');
    }
  });

  it('should allow empty address fields when no address is provided', () => {
    const result = familyFormSchema.safeParse({
      ...validFamily,
      street1: '',
      street2: '',
      city: '',
      state: '',
      postalCode: '',
    });
    expect(result.success).toBe(true);
  });

  it('should accept extended postal code format', () => {
    const result = familyFormSchema.safeParse({
      ...validFamily,
      street1: '123 Main St',
      city: 'Springfield',
      state: 'CA',
      postalCode: '12345-6789',
    });
    expect(result.success).toBe(true);
  });
});
