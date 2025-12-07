/**
 * QR Code validation utilities for koinon://family/{idKey} format
 * Prevents XSS and ensures IdKey format compliance
 */

const QR_PREFIX = 'koinon://family/';
const IDKEY_PATTERN = /^[A-Za-z0-9_-]+$/;

export interface QrValidationResult {
  valid: boolean;
  idKey?: string;
  error?: string;
}

/**
 * Validates a QR code string and extracts the IdKey if valid
 * @param text - The raw QR code text to validate
 * @returns Validation result with IdKey or error message
 */
export function validateQrCode(text: string): QrValidationResult {
  // Check if text is empty or null
  if (!text || typeof text !== 'string') {
    return {
      valid: false,
      error: 'QR code is empty',
    };
  }

  // Check for correct prefix
  if (!text.startsWith(QR_PREFIX)) {
    return {
      valid: false,
      error: 'Invalid QR code format',
    };
  }

  // Extract IdKey
  const idKey = text.substring(QR_PREFIX.length);

  // Validate IdKey exists
  if (!idKey || idKey.length === 0) {
    return {
      valid: false,
      error: 'QR code missing family ID',
    };
  }

  // Validate IdKey format (alphanumeric, underscore, hyphen only)
  if (!isValidIdKey(idKey)) {
    return {
      valid: false,
      error: 'Invalid family ID format',
    };
  }

  return {
    valid: true,
    idKey,
  };
}

/**
 * Validates IdKey format (alphanumeric, underscore, hyphen only)
 * Prevents XSS and injection attacks
 * @param idKey - The IdKey to validate
 * @returns True if IdKey format is valid
 */
export function isValidIdKey(idKey: string): boolean {
  if (!idKey || typeof idKey !== 'string') {
    return false;
  }

  return IDKEY_PATTERN.test(idKey);
}

/**
 * Creates a QR code value for a family IdKey
 * @param familyIdKey - The family's IdKey
 * @returns The formatted QR code string
 */
export function createFamilyQrCode(familyIdKey: string): string {
  if (!isValidIdKey(familyIdKey)) {
    throw new Error('Invalid IdKey format');
  }

  return `${QR_PREFIX}${familyIdKey}`;
}
