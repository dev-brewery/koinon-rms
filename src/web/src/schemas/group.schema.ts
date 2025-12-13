/**
 * Group Form Validation Schema
 * Zod validation for group create/update forms
 */

import { z } from 'zod';

export const groupFormSchema = z.object({
  name: z.string()
    .min(1, 'Group name is required')
    .max(100, 'Group name must be 100 characters or less'),

  description: z.string()
    .max(500, 'Description must be 500 characters or less')
    .optional()
    .default(''),

  groupTypeId: z.string()
    .min(1, 'Group type is required'),

  parentGroupId: z.string()
    .optional()
    .default(''),

  campusId: z.string()
    .optional()
    .default(''),

  capacity: z.preprocess(
    (val) => {
      if (val === '' || val === null || val === undefined) return undefined;
      if (typeof val === 'string') {
        const parsed = parseInt(val, 10);
        return isNaN(parsed) ? val : parsed;
      }
      return val;
    },
    z.number().positive('Capacity must be greater than 0').optional()
  ),

  isActive: z.boolean()
    .optional()
    .default(true),
});

export const groupFormSchemaForEdit = groupFormSchema.omit({ groupTypeId: true });

export type GroupFormData = z.infer<typeof groupFormSchema>;
export type GroupFormDataForEdit = z.infer<typeof groupFormSchemaForEdit>;
