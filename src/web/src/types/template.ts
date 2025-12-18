/**
 * Template Types
 * Type definitions for import field mapping templates
 */

import type { FieldMapping } from '@/components/import/FieldMappingEditor';

export type ImportType = 'people' | 'attendance';

export interface ImportTemplate {
  id: string;
  name: string;
  importType: ImportType;
  mappings: FieldMapping[];
  createdAt: string;
  updatedAt: string;
}

export interface TemplateStorage {
  templates: ImportTemplate[];
  version: number;
}

export const TEMPLATE_STORAGE_KEY = 'koinon_import_templates';
export const TEMPLATE_STORAGE_VERSION = 1;

/**
 * Gets the display label for an import type
 */
export function getImportTypeLabel(importType: ImportType): string {
  switch (importType) {
    case 'people':
      return 'People';
    case 'attendance':
      return 'Attendance';
    default:
      return importType;
  }
}

/**
 * Formats a date string as relative time (e.g., "2 days ago")
 */
export function formatRelativeTime(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffSeconds = Math.floor(diffMs / 1000);
  const diffMinutes = Math.floor(diffSeconds / 60);
  const diffHours = Math.floor(diffMinutes / 60);
  const diffDays = Math.floor(diffHours / 24);
  const diffWeeks = Math.floor(diffDays / 7);
  const diffMonths = Math.floor(diffDays / 30);

  if (diffSeconds < 60) {
    return 'just now';
  } else if (diffMinutes < 60) {
    return diffMinutes === 1 ? '1 minute ago' : `${diffMinutes} minutes ago`;
  } else if (diffHours < 24) {
    return diffHours === 1 ? '1 hour ago' : `${diffHours} hours ago`;
  } else if (diffDays < 7) {
    return diffDays === 1 ? '1 day ago' : `${diffDays} days ago`;
  } else if (diffWeeks < 4) {
    return diffWeeks === 1 ? '1 week ago' : `${diffWeeks} weeks ago`;
  } else {
    return diffMonths === 1 ? '1 month ago' : `${diffMonths} months ago`;
  }
}

/**
 * Generates a unique ID for a template
 */
export function generateTemplateId(): string {
  return `tmpl_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;
}

/**
 * Loads templates from localStorage
 */
export function loadTemplates(): ImportTemplate[] {
  try {
    const stored = localStorage.getItem(TEMPLATE_STORAGE_KEY);
    if (!stored) {
      return [];
    }
    const data: TemplateStorage = JSON.parse(stored);
    return data.templates || [];
  } catch {
    return [];
  }
}

/**
 * Saves templates to localStorage
 */
export function saveTemplates(templates: ImportTemplate[]): void {
  const storage: TemplateStorage = {
    templates,
    version: TEMPLATE_STORAGE_VERSION,
  };
  localStorage.setItem(TEMPLATE_STORAGE_KEY, JSON.stringify(storage));
}

/**
 * Adds a new template
 */
export function addTemplate(template: Omit<ImportTemplate, 'id' | 'createdAt' | 'updatedAt'>): ImportTemplate {
  const templates = loadTemplates();
  const now = new Date().toISOString();
  const newTemplate: ImportTemplate = {
    ...template,
    id: generateTemplateId(),
    createdAt: now,
    updatedAt: now,
  };
  templates.push(newTemplate);
  saveTemplates(templates);
  return newTemplate;
}

/**
 * Deletes a template by ID
 */
export function deleteTemplate(templateId: string): boolean {
  const templates = loadTemplates();
  const index = templates.findIndex(t => t.id === templateId);
  if (index === -1) {
    return false;
  }
  templates.splice(index, 1);
  saveTemplates(templates);
  return true;
}

/**
 * Gets templates filtered by import type
 */
export function getTemplatesByType(importType: ImportType): ImportTemplate[] {
  return loadTemplates().filter(t => t.importType === importType);
}
