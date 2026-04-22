/**
 * QR Code validation tests
 */

import { describe, it, expect } from 'vitest';
import {
  validateQrCode,
  isValidIdKey,
  createFamilyQrCode,
} from '../qrValidation';

describe('validateQrCode', () => {
  it('rejects empty strings', () => {
    expect(validateQrCode('')).toEqual({
      valid: false,
      error: 'QR code is empty',
    });
  });

  it('rejects non-string input (casts to avoid TS error)', () => {
    // @ts-expect-error testing runtime guard
    expect(validateQrCode(null).valid).toBe(false);
    // @ts-expect-error testing runtime guard
    expect(validateQrCode(undefined).valid).toBe(false);
    // @ts-expect-error testing runtime guard
    expect(validateQrCode(123).valid).toBe(false);
  });

  it('rejects QR codes with wrong prefix', () => {
    expect(validateQrCode('https://example.com/foo').valid).toBe(false);
    expect(validateQrCode('family/abc').error).toBe('Invalid QR code format');
    expect(validateQrCode('koinon://person/abc').error).toBe(
      'Invalid QR code format'
    );
  });

  it('rejects QR codes missing id key after prefix', () => {
    expect(validateQrCode('koinon://family/')).toEqual({
      valid: false,
      error: 'QR code missing family ID',
    });
  });

  it('rejects QR codes with invalid IdKey characters', () => {
    expect(validateQrCode('koinon://family/abc def')).toEqual({
      valid: false,
      error: 'Invalid family ID format',
    });
    expect(validateQrCode('koinon://family/abc<script>')).toEqual({
      valid: false,
      error: 'Invalid family ID format',
    });
    expect(validateQrCode('koinon://family/abc.xyz')).toEqual({
      valid: false,
      error: 'Invalid family ID format',
    });
  });

  it('accepts valid alphanumeric IdKey', () => {
    expect(validateQrCode('koinon://family/abcDEF123')).toEqual({
      valid: true,
      idKey: 'abcDEF123',
    });
  });

  it('accepts IdKey with dashes and underscores', () => {
    const result = validateQrCode('koinon://family/abc_def-123');
    expect(result.valid).toBe(true);
    expect(result.idKey).toBe('abc_def-123');
  });
});

describe('isValidIdKey', () => {
  it('accepts alphanumeric ids', () => {
    expect(isValidIdKey('abc123')).toBe(true);
    expect(isValidIdKey('ABC')).toBe(true);
    expect(isValidIdKey('a_b-c')).toBe(true);
  });

  it('rejects empty / falsy', () => {
    expect(isValidIdKey('')).toBe(false);
    // @ts-expect-error testing runtime guard
    expect(isValidIdKey(null)).toBe(false);
    // @ts-expect-error testing runtime guard
    expect(isValidIdKey(undefined)).toBe(false);
  });

  it('rejects ids with disallowed characters', () => {
    expect(isValidIdKey('abc def')).toBe(false);
    expect(isValidIdKey('abc/def')).toBe(false);
    expect(isValidIdKey('abc<script>')).toBe(false);
    expect(isValidIdKey('abc.def')).toBe(false);
  });
});

describe('createFamilyQrCode', () => {
  it('creates a koinon family url from a valid IdKey', () => {
    expect(createFamilyQrCode('abc123')).toBe('koinon://family/abc123');
  });

  it('throws on invalid IdKey (prevents bad QR generation)', () => {
    expect(() => createFamilyQrCode('')).toThrow('Invalid IdKey format');
    expect(() => createFamilyQrCode('abc def')).toThrow(
      'Invalid IdKey format'
    );
    expect(() => createFamilyQrCode('abc<script>')).toThrow(
      'Invalid IdKey format'
    );
  });
});
