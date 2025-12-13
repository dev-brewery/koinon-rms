/**
 * Group Schema Tests
 * Unit tests for group form validation
 */

import { describe, it, expect } from 'vitest';
import { groupFormSchema, groupFormSchemaForEdit } from '../group.schema';

describe('groupFormSchema', () => {
  const validGroup = {
    name: 'Elementary Check-in',
    groupTypeId: 'checkin-area',
    isActive: true,
  };

  it('should validate a minimal valid group', () => {
    const result = groupFormSchema.safeParse(validGroup);
    expect(result.success).toBe(true);
  });

  it('should require group name', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      name: '',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Group name is required');
    }
  });

  it('should enforce maximum length on group name', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      name: 'a'.repeat(101),
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('100 characters or less');
    }
  });

  it('should require group type', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      groupTypeId: '',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Group type is required');
    }
  });

  it('should accept optional description', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      description: 'A group for elementary children',
    });
    expect(result.success).toBe(true);
  });

  it('should enforce maximum length on description', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      description: 'a'.repeat(501),
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('500 characters or less');
    }
  });

  it('should accept empty description', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      description: '',
    });
    expect(result.success).toBe(true);
  });

  it('should accept optional parentGroupId', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      parentGroupId: 'parent123',
    });
    expect(result.success).toBe(true);
  });

  it('should accept empty parentGroupId', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      parentGroupId: '',
    });
    expect(result.success).toBe(true);
  });

  it('should accept optional campusId', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      campusId: 'campus123',
    });
    expect(result.success).toBe(true);
  });

  it('should validate capacity as positive number', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      capacity: '25',
    });
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.capacity).toBe(25);
    }
  });

  it('should transform capacity string to number', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      capacity: '100',
    });
    expect(result.success).toBe(true);
    if (result.success) {
      expect(typeof result.data.capacity).toBe('number');
      expect(result.data.capacity).toBe(100);
    }
  });

  it('should reject negative capacity', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      capacity: '-5',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('greater than 0');
    }
  });

  it('should reject zero capacity', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      capacity: '0',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('greater than 0');
    }
  });

  it('should reject non-numeric capacity', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      capacity: 'abc',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('expected number');
    }
  });

  it('should accept empty capacity', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      capacity: '',
    });
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.capacity).toBeUndefined();
    }
  });

  it('should default isActive to true', () => {
    const result = groupFormSchema.safeParse({
      name: 'Test Group',
      groupTypeId: 'type123',
    });
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.isActive).toBe(true);
    }
  });

  it('should accept isActive as false', () => {
    const result = groupFormSchema.safeParse({
      ...validGroup,
      isActive: false,
    });
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.isActive).toBe(false);
    }
  });
});

describe('groupFormSchemaForEdit', () => {
  const validEditGroup = {
    name: 'Updated Group Name',
    isActive: true,
  };

  it('should validate group update without groupTypeId', () => {
    const result = groupFormSchemaForEdit.safeParse(validEditGroup);
    expect(result.success).toBe(true);
  });

  it('should not require groupTypeId for edit', () => {
    const result = groupFormSchemaForEdit.safeParse({
      name: 'Test Group',
    });
    expect(result.success).toBe(true);
  });

  it('should enforce same validation rules as create schema', () => {
    const result = groupFormSchemaForEdit.safeParse({
      name: '',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Group name is required');
    }
  });
});
