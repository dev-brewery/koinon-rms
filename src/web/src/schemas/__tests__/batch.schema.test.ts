/**
 * batchFormSchema tests
 */

import { describe, it, expect } from 'vitest';
import { batchFormSchema } from '../batch.schema';

describe('batchFormSchema', () => {
  const validBase = {
    name: 'Sunday 9am',
    batchDate: '2024-01-15',
    controlAmount: 100,
    controlItemCount: 5,
    campusIdKey: 'c-123',
    note: 'Regular batch',
  };

  it('accepts a fully populated valid batch', () => {
    const result = batchFormSchema.safeParse(validBase);
    expect(result.success).toBe(true);
  });

  it('requires a non-empty name', () => {
    const result = batchFormSchema.safeParse({ ...validBase, name: '' });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(
        result.error.issues.some((i) => i.message === 'Batch name is required')
      ).toBe(true);
    }
  });

  it('rejects names over 100 characters', () => {
    const result = batchFormSchema.safeParse({
      ...validBase,
      name: 'x'.repeat(101),
    });
    expect(result.success).toBe(false);
  });

  it('rejects badly-formatted batch dates', () => {
    const bad = batchFormSchema.safeParse({
      ...validBase,
      batchDate: '2024/01/15',
    });
    expect(bad.success).toBe(false);
    const empty = batchFormSchema.safeParse({ ...validBase, batchDate: '' });
    expect(empty.success).toBe(false);
  });

  it('rejects future batch dates', () => {
    const future = new Date();
    future.setFullYear(future.getFullYear() + 1);
    const iso = future.toISOString().slice(0, 10);
    const result = batchFormSchema.safeParse({ ...validBase, batchDate: iso });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(
        result.error.issues.some((i) =>
          i.message.includes('Batch date cannot be in the future')
        )
      ).toBe(true);
    }
  });

  it('accepts today as a valid batch date', () => {
    const today = new Date().toISOString().slice(0, 10);
    const result = batchFormSchema.safeParse({ ...validBase, batchDate: today });
    expect(result.success).toBe(true);
  });

  it('rejects negative control amounts', () => {
    const result = batchFormSchema.safeParse({
      ...validBase,
      controlAmount: -1,
    });
    expect(result.success).toBe(false);
  });

  it('rejects negative control item counts', () => {
    const result = batchFormSchema.safeParse({
      ...validBase,
      controlItemCount: -5,
    });
    expect(result.success).toBe(false);
  });

  it('allows optional fields to be omitted', () => {
    const result = batchFormSchema.safeParse({
      name: 'Sunday',
      batchDate: '2024-01-15',
    });
    expect(result.success).toBe(true);
  });
});
