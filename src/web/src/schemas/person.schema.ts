/**
 * Person Form Validation Schema
 * Zod validation for person create/update forms
 */

import { z } from 'zod';

export const phoneNumberSchema = z.object({
  number: z.string().min(1, 'Phone number is required').regex(/^[\d\s\-()+]+$/, 'Invalid phone number format'),
  phoneTypeValueId: z.string().optional(),
  isMessagingEnabled: z.boolean().default(true),
});

export const personFormSchema = z.object({
  firstName: z.string()
    .min(1, 'First name is required')
    .max(50, 'First name must be 50 characters or less'),

  lastName: z.string()
    .min(1, 'Last name is required')
    .max(50, 'Last name must be 50 characters or less'),

  nickName: z.string()
    .max(50, 'Nick name must be 50 characters or less')
    .optional()
    .default(''),

  middleName: z.string()
    .max(50, 'Middle name must be 50 characters or less')
    .optional()
    .default(''),

  email: z.union([
    z.string().email('Please enter a valid email address'),
    z.literal(''),
  ]).optional()
    .default(''),

  gender: z.enum(['Unknown', 'Male', 'Female'])
    .optional()
    .default('Unknown'),

  birthDate: z.union([
    z.string()
      .regex(/^\d{4}-\d{2}-\d{2}$/, 'Please enter a valid date (YYYY-MM-DD)')
      .refine(
        (val) => {
          const date = new Date(val);
          return date <= new Date();
        },
        'Birth date cannot be in the future'
      ),
    z.literal(''),
  ]).optional()
    .default(''),

  campusId: z.string()
    .optional()
    .default(''),

  phoneNumbers: z.array(phoneNumberSchema)
    .optional()
    .default([]),
});

export type PersonFormData = z.infer<typeof personFormSchema>;
export type PhoneNumberFormData = z.infer<typeof phoneNumberSchema>;
