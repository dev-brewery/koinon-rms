/**
 * Schedule Schema Tests
 * Unit tests for schedule form validation
 */

import { describe, it, expect } from 'vitest';
import { scheduleFormSchema } from '../schedule.schema';

describe('scheduleFormSchema', () => {
  const validSchedule = {
    name: 'Sunday Morning Service',
    dayOfWeek: 0,
    timeOfDay: '09:00',
    checkInStartOffset: 60,
    checkInEndOffset: 60,
    isActive: true,
    isPublic: true,
    order: 0,
    autoInactivate: false,
  };

  it('should validate a complete valid schedule', () => {
    const result = scheduleFormSchema.safeParse(validSchedule);
    expect(result.success).toBe(true);
  });

  it('should require schedule name', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      name: '',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Schedule name is required');
    }
  });

  it('should enforce maximum length on schedule name', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      name: 'a'.repeat(101),
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('100 characters or less');
    }
  });

  it('should accept optional description', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      description: 'Main worship service',
    });
    expect(result.success).toBe(true);
  });

  it('should enforce maximum length on description', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      description: 'a'.repeat(501),
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('500 characters or less');
    }
  });

  it('should validate dayOfWeek range (0-6)', () => {
    const validDays = [0, 1, 2, 3, 4, 5, 6];
    validDays.forEach((dayOfWeek) => {
      const result = scheduleFormSchema.safeParse({
        ...validSchedule,
        dayOfWeek,
      });
      expect(result.success).toBe(true);
    });
  });

  it('should reject dayOfWeek outside range', () => {
    const invalidDays = [-1, 7, 10];
    invalidDays.forEach((dayOfWeek) => {
      const result = scheduleFormSchema.safeParse({
        ...validSchedule,
        dayOfWeek,
      });
      expect(result.success).toBe(false);
    });
  });

  it('should validate time format (HH:MM)', () => {
    const validTimes = ['00:00', '09:00', '12:30', '23:59'];
    validTimes.forEach((timeOfDay) => {
      const result = scheduleFormSchema.safeParse({
        ...validSchedule,
        timeOfDay,
      });
      expect(result.success).toBe(true);
    });
  });

  it('should reject invalid time formats', () => {
    const invalidTimes = ['9:00', '25:00', '12:60', '12:00 AM', 'invalid'];
    invalidTimes.forEach((timeOfDay) => {
      const result = scheduleFormSchema.safeParse({
        ...validSchedule,
        timeOfDay,
      });
      expect(result.success).toBe(false);
    });
  });

  it('should validate checkInStartOffset range', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      checkInStartOffset: 30,
    });
    expect(result.success).toBe(true);
  });

  it('should reject negative checkInStartOffset', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      checkInStartOffset: -5,
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('0 or greater');
    }
  });

  it('should reject checkInStartOffset over 24 hours', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      checkInStartOffset: 1441,
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('1440 minutes');
    }
  });

  it('should validate checkInEndOffset range', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      checkInEndOffset: 90,
    });
    expect(result.success).toBe(true);
  });

  it('should reject negative checkInEndOffset', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      checkInEndOffset: -10,
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('0 or greater');
    }
  });

  it('should default checkInStartOffset to 60', () => {
    const { ...scheduleWithoutOffset } = validSchedule;
    const result = scheduleFormSchema.safeParse(scheduleWithoutOffset);
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.checkInStartOffset).toBe(60);
    }
  });

  it('should default checkInEndOffset to 60', () => {
    const { ...scheduleWithoutOffset } = validSchedule;
    const result = scheduleFormSchema.safeParse(scheduleWithoutOffset);
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.checkInEndOffset).toBe(60);
    }
  });

  it('should validate effectiveStartDate format', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      effectiveStartDate: '2024-01-01',
    });
    expect(result.success).toBe(true);
  });

  it('should reject invalid effectiveStartDate format', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      effectiveStartDate: '01/01/2024',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('YYYY-MM-DD');
    }
  });

  it('should validate effectiveEndDate format', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      effectiveEndDate: '2024-12-31',
    });
    expect(result.success).toBe(true);
  });

  it('should reject end date before start date', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      effectiveStartDate: '2024-12-31',
      effectiveEndDate: '2024-01-01',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      const endDateError = result.error.issues.find((e) => e.path[0] === 'effectiveEndDate');
      expect(endDateError?.message).toContain('after start date');
    }
  });

  it('should accept same start and end dates', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      effectiveStartDate: '2024-06-15',
      effectiveEndDate: '2024-06-15',
    });
    expect(result.success).toBe(true);
  });

  it('should accept empty effectiveStartDate', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      effectiveStartDate: '',
    });
    expect(result.success).toBe(true);
  });

  it('should accept empty effectiveEndDate', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      effectiveEndDate: '',
    });
    expect(result.success).toBe(true);
  });

  it('should default isActive to true', () => {
    const { ...scheduleWithoutActive } = validSchedule;
    const result = scheduleFormSchema.safeParse(scheduleWithoutActive);
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.isActive).toBe(true);
    }
  });

  it('should default isPublic to true', () => {
    const { ...scheduleWithoutPublic } = validSchedule;
    const result = scheduleFormSchema.safeParse(scheduleWithoutPublic);
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.isPublic).toBe(true);
    }
  });

  it('should default order to 0', () => {
    const { ...scheduleWithoutOrder } = validSchedule;
    const result = scheduleFormSchema.safeParse(scheduleWithoutOrder);
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.order).toBe(0);
    }
  });

  it('should validate order as non-negative', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      order: 5,
    });
    expect(result.success).toBe(true);
  });

  it('should reject negative order', () => {
    const result = scheduleFormSchema.safeParse({
      ...validSchedule,
      order: -1,
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('0 or greater');
    }
  });

  it('should default autoInactivate to false', () => {
    const { ...scheduleWithoutAuto } = validSchedule;
    const result = scheduleFormSchema.safeParse(scheduleWithoutAuto);
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.autoInactivate).toBe(false);
    }
  });

  it('should accept minimal schedule with just name', () => {
    const result = scheduleFormSchema.safeParse({
      name: 'Test Schedule',
    });
    expect(result.success).toBe(true);
  });
});
