/**
 * Family Form Validation Schema
 * Zod validation for family create/update forms
 */

import { z } from 'zod';

export const familyAddressSchema = z.object({
  street1: z.string().min(1, 'Street address is required'),
  street2: z.string().optional().or(z.literal('')),
  city: z.string().min(1, 'City is required'),
  state: z.string()
    .length(2, 'State must be 2 characters (e.g., CA)')
    .regex(/^[A-Z]{2}$/, 'State must be 2 uppercase letters'),
  postalCode: z.string()
    .min(5, 'Postal code must be at least 5 digits')
    .regex(/^\d{5}(-\d{4})?$/, 'Invalid postal code format (e.g., 12345 or 12345-6789)'),
});

export const familyFormSchema = z.object({
  name: z.string()
    .min(1, 'Family name is required')
    .max(100, 'Family name must be 100 characters or less'),

  campusId: z.string()
    .optional()
    .default(''),

  // Address fields (only for create, optional)
  street1: z.string()
    .optional()
    .default(''),
  street2: z.string()
    .optional()
    .default(''),
  city: z.string()
    .optional()
    .default(''),
  state: z.string()
    .optional()
    .default(''),
  postalCode: z.string()
    .optional()
    .default(''),
}).superRefine((data, ctx) => {
  // If any address field is filled, validate the complete address
  const hasAnyAddressField = data.street1 || data.city || data.state || data.postalCode;

  if (hasAnyAddressField) {
    if (!data.street1) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'Street address is required when adding an address',
        path: ['street1'],
      });
    }
    if (!data.city) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'City is required when adding an address',
        path: ['city'],
      });
    }
    if (!data.state) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'State is required when adding an address',
        path: ['state'],
      });
    } else if (data.state.length !== 2 || !/^[A-Z]{2}$/.test(data.state)) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'State must be 2 uppercase letters (e.g., CA)',
        path: ['state'],
      });
    }
    if (!data.postalCode) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'Postal code is required when adding an address',
        path: ['postalCode'],
      });
    } else if (!/^\d{5}(-\d{4})?$/.test(data.postalCode)) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'Invalid postal code format (e.g., 12345 or 12345-6789)',
        path: ['postalCode'],
      });
    }
  }
});

export type FamilyFormData = z.infer<typeof familyFormSchema>;
export type FamilyAddressFormData = z.infer<typeof familyAddressSchema>;
