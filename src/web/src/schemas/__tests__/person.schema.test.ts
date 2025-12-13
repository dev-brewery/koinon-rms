/**
 * Person Schema Tests
 * Unit tests for person form validation
 */

import { describe, it, expect } from 'vitest';
import { personFormSchema, phoneNumberSchema } from '../person.schema';

describe('phoneNumberSchema', () => {
  it('should validate a basic phone number', () => {
    const result = phoneNumberSchema.safeParse({
      number: '555-1234',
      isMessagingEnabled: true,
    });
    expect(result.success).toBe(true);
  });

  it('should accept various phone number formats', () => {
    const formats = [
      '(555) 123-4567',
      '555-123-4567',
      '555 123 4567',
      '5551234567',
      '+1 555 123 4567',
    ];

    formats.forEach((number) => {
      const result = phoneNumberSchema.safeParse({ number, isMessagingEnabled: false });
      expect(result.success).toBe(true);
    });
  });

  it('should reject invalid phone number formats', () => {
    const result = phoneNumberSchema.safeParse({
      number: 'invalid-phone',
      isMessagingEnabled: true,
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('Invalid phone number format');
    }
  });

  it('should require phone number', () => {
    const result = phoneNumberSchema.safeParse({
      number: '',
      isMessagingEnabled: true,
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Phone number is required');
    }
  });

  it('should default isMessagingEnabled to true', () => {
    const result = phoneNumberSchema.safeParse({
      number: '555-1234',
    });
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.isMessagingEnabled).toBe(true);
    }
  });
});

describe('personFormSchema', () => {
  const validPerson = {
    firstName: 'John',
    lastName: 'Doe',
    gender: 'Male' as const,
  };

  it('should validate a minimal valid person', () => {
    const result = personFormSchema.safeParse(validPerson);
    expect(result.success).toBe(true);
  });

  it('should require first name', () => {
    const result = personFormSchema.safeParse({
      ...validPerson,
      firstName: '',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].path).toEqual(['firstName']);
      expect(result.error.issues[0].message).toBe('First name is required');
    }
  });

  it('should require last name', () => {
    const result = personFormSchema.safeParse({
      ...validPerson,
      lastName: '',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].path).toEqual(['lastName']);
      expect(result.error.issues[0].message).toBe('Last name is required');
    }
  });

  it('should enforce maximum length on first name', () => {
    const result = personFormSchema.safeParse({
      ...validPerson,
      firstName: 'a'.repeat(51),
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('50 characters or less');
    }
  });

  it('should enforce maximum length on last name', () => {
    const result = personFormSchema.safeParse({
      ...validPerson,
      lastName: 'a'.repeat(51),
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('50 characters or less');
    }
  });

  it('should accept optional fields', () => {
    const result = personFormSchema.safeParse({
      ...validPerson,
      nickName: 'Johnny',
      middleName: 'Michael',
      email: 'john.doe@example.com',
      birthDate: '1990-01-15',
      campusId: 'abc123',
    });
    expect(result.success).toBe(true);
  });

  it('should accept empty strings for optional fields', () => {
    const result = personFormSchema.safeParse({
      ...validPerson,
      nickName: '',
      middleName: '',
      email: '',
      birthDate: '',
      campusId: '',
    });
    expect(result.success).toBe(true);
  });

  it('should validate email format', () => {
    const result = personFormSchema.safeParse({
      ...validPerson,
      email: 'invalid-email',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('valid email address');
    }
  });

  it('should accept valid email addresses', () => {
    const emails = [
      'test@example.com',
      'user.name@example.co.uk',
      'test+tag@example.com',
    ];

    emails.forEach((email) => {
      const result = personFormSchema.safeParse({
        ...validPerson,
        email,
      });
      expect(result.success).toBe(true);
    });
  });

  it('should validate gender enum', () => {
    const validGenders = ['Unknown', 'Male', 'Female'] as const;

    validGenders.forEach((gender) => {
      const result = personFormSchema.safeParse({
        ...validPerson,
        gender,
      });
      expect(result.success).toBe(true);
    });
  });

  it('should default gender to Unknown', () => {
    const result = personFormSchema.safeParse({
      firstName: 'John',
      lastName: 'Doe',
    });
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.gender).toBe('Unknown');
    }
  });

  it('should validate date format for birthDate', () => {
    const result = personFormSchema.safeParse({
      ...validPerson,
      birthDate: '01/15/1990', // Invalid format
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('YYYY-MM-DD');
    }
  });

  it('should reject future birth dates', () => {
    const futureDate = new Date();
    futureDate.setFullYear(futureDate.getFullYear() + 1);
    const result = personFormSchema.safeParse({
      ...validPerson,
      birthDate: futureDate.toISOString().split('T')[0],
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('cannot be in the future');
    }
  });

  it('should accept valid birth dates', () => {
    const result = personFormSchema.safeParse({
      ...validPerson,
      birthDate: '1990-01-15',
    });
    expect(result.success).toBe(true);
  });

  it('should validate phone numbers array', () => {
    const result = personFormSchema.safeParse({
      ...validPerson,
      phoneNumbers: [
        { number: '555-1234', isMessagingEnabled: true },
        { number: '555-5678', isMessagingEnabled: false },
      ],
    });
    expect(result.success).toBe(true);
  });

  it('should reject invalid phone numbers in array', () => {
    const result = personFormSchema.safeParse({
      ...validPerson,
      phoneNumbers: [
        { number: 'invalid', isMessagingEnabled: true },
      ],
    });
    expect(result.success).toBe(false);
  });

  it('should default phoneNumbers to empty array', () => {
    const result = personFormSchema.safeParse(validPerson);
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.phoneNumbers).toEqual([]);
    }
  });
});
