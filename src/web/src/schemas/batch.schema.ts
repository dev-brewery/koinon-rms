/**
 * Batch Form Validation Schema
 * Zod validation for batch create/update forms
 */

import { z } from 'zod';

export const batchFormSchema = z.object({
  name: z.string()
    .min(1, 'Batch name is required')
    .max(100, 'Batch name must be 100 characters or less'),

  batchDate: z.string()
    .min(1, 'Batch date is required')
    .regex(/^\d{4}-\d{2}-\d{2}$/, 'Please enter a valid date (YYYY-MM-DD)')
    .refine(
      (dateStr) => {
        const batchDate = new Date(dateStr);
        const today = new Date();
        today.setHours(0, 0, 0, 0); // Normalize to start of day
        return batchDate <= today;
      },
      { message: 'Batch date cannot be in the future' }
    ),

  controlAmount: z.number()
    .optional()
    .refine(
      (val) => val === undefined || val >= 0,
      { message: 'Control amount must be 0 or greater' }
    ),

  controlItemCount: z.number()
    .optional()
    .refine(
      (val) => val === undefined || val >= 0,
      { message: 'Control item count must be 0 or greater' }
    ),

  campusIdKey: z.string().optional(),

  note: z.string().optional(),
});

export type BatchFormData = z.infer<typeof batchFormSchema>;
