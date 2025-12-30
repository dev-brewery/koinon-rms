/**
 * TypeScript types for Koinon RMS Label Printing API
 * Provides types for label generation, templates, and batch printing operations
 */

import type { IdKey } from '@/services/api/types';

// ============================================================================
// Label Type Enumeration
// ============================================================================

/**
 * Types of labels that can be printed in the check-in system
 */
export enum LabelType {
  /** Standard child name tag */
  ChildName = 0,
  /** Child security label with pickup code */
  ChildSecurity = 1,
  /** Parent claim ticket for child pickup */
  ParentClaim = 2,
  /** Visitor name tag */
  VisitorName = 3,
  /** Allergy alert label */
  Allergy = 4,
}

// ============================================================================
// Label Data Types
// ============================================================================

/**
 * A single rendered label ready for printing
 */
export interface LabelDto {
  /** Type of label */
  type: LabelType;
  /** Rendered label content (HTML, ZPL, etc.) */
  content: string;
  /** Label format (e.g., 'zpl', 'html', 'pdf') */
  format: string;
  /** Field values used to generate this label */
  fields: Record<string, string>;
}

/**
 * A collection of labels for a single check-in event
 */
export interface LabelSetDto {
  /** Attendance record this label set is for */
  attendanceIdKey: IdKey;
  /** Person being checked in */
  personIdKey: IdKey;
  /** All labels to be printed for this check-in */
  labels: LabelDto[];
}

// ============================================================================
// Label Template Types
// ============================================================================

/**
 * Label template configuration
 */
export interface LabelTemplateDto {
  /** Unique identifier for this template */
  idKey: IdKey;
  /** Display name of the template */
  name: string;
  /** Type of label this template produces */
  type: LabelType;
  /** Output format (e.g., 'zpl', 'html', 'pdf') */
  format: string;
  /** Template markup with field placeholders */
  template: string;
  /** Label width in millimeters */
  widthMm: number;
  /** Label height in millimeters */
  heightMm: number;
}

// ============================================================================
// Label Request Types
// ============================================================================

/**
 * Request to generate labels for a single check-in
 */
export interface LabelRequestDto {
  /** Attendance record to generate labels for */
  attendanceIdKey: IdKey;
  /** Optional: specific label types to generate (default: all applicable) */
  labelTypes?: LabelType[];
  /** Optional: additional custom fields for label templates */
  customFields?: Record<string, string>;
}

/**
 * Request to generate labels for multiple check-ins
 */
export interface BatchLabelRequestDto {
  /** Attendance records to generate labels for */
  attendanceIdKeys: IdKey[];
  /** Optional: specific label types to generate (default: all applicable) */
  labelTypes?: LabelType[];
  /** Optional: additional custom fields for label templates */
  customFields?: Record<string, string>;
}

/**
 * Request to preview a label with custom data
 */
export interface LabelPreviewRequestDto {
  /** Type of label to preview */
  type: LabelType;
  /** Field values to render in preview */
  fields: Record<string, string>;
  /** Optional: specific template to use (default: system default for type) */
  templateIdKey?: IdKey;
}

/**
 * Preview result showing rendered label
 */
export interface LabelPreviewDto {
  /** Type of label previewed */
  type: LabelType;
  /** HTML rendering of the label for browser preview */
  previewHtml: string;
  /** Actual format that would be sent to printer */
  format: string;
}
