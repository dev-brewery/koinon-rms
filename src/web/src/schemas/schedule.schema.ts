/**
 * Schedule Form Validation Schema
 * Zod validation for schedule create/update forms
 */

import { z } from 'zod';

export const scheduleFormSchema = z.object({
  name: z.string()
    .min(1, 'Schedule name is required')
    .max(100, 'Schedule name must be 100 characters or less'),

  description: z.string()
    .max(500, 'Description must be 500 characters or less')
    .default(''),

  dayOfWeek: z.number()
    .min(0, 'Day of week must be between 0 (Sunday) and 6 (Saturday)')
    .max(6, 'Day of week must be between 0 (Sunday) and 6 (Saturday)')
    .optional(),

  timeOfDay: z.union([
    z.string().regex(
      /^([01]\d|2[0-3]):([0-5]\d)(:([0-5]\d))?$/,
      'Time must be in HH:MM or HH:MM:SS format (e.g., 09:00 or 09:00:00)',
    ),
    z.literal(''),
  ]).optional(),

  checkInStartOffset: z.number()
    .min(0, 'Check-in start offset must be 0 or greater')
    .max(1440, 'Check-in start offset cannot exceed 24 hours (1440 minutes)')
    .default(60),

  checkInEndOffset: z.number()
    .min(0, 'Check-in end offset must be 0 or greater')
    .max(1440, 'Check-in end offset cannot exceed 24 hours (1440 minutes)')
    .default(60),

  isActive: z.boolean().default(true),

  isPublic: z.boolean().default(true),

  order: z.number()
    .min(0, 'Order must be 0 or greater')
    .default(0),

  effectiveStartDate: z.union([
    z.string().regex(/^\d{4}-\d{2}-\d{2}$/, 'Please enter a valid date (YYYY-MM-DD)'),
    z.literal(''),
  ]).default(''),

  effectiveEndDate: z.union([
    z.string().regex(/^\d{4}-\d{2}-\d{2}$/, 'Please enter a valid date (YYYY-MM-DD)'),
    z.literal(''),
  ]).default(''),

  autoInactivate: z.boolean().default(false),
}).superRefine((data, ctx) => {
  // Validate that end date is after start date if both are provided
  if (data.effectiveStartDate && data.effectiveEndDate) {
    const startDate = new Date(data.effectiveStartDate);
    const endDate = new Date(data.effectiveEndDate);

    if (endDate < startDate) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'End date must be after start date',
        path: ['effectiveEndDate'],
      });
    }
  }

  // Weekly schedules require BOTH day-of-week and time-of-day to be paired.
  // Valid: both set OR both unset. Invalid: exactly one set (XOR).
  const hasDay = data.dayOfWeek !== undefined;
  const hasTime = !!data.timeOfDay;
  if (hasDay !== hasTime) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Weekly schedules require both a day of week and a time of day.',
      path: hasTime ? ['dayOfWeek'] : ['timeOfDay'],
    });
  }
});

export type ScheduleFormData = z.infer<typeof scheduleFormSchema>;
