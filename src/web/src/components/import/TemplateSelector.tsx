/**
 * Template Selector Component
 * Dropdown for selecting saved import templates
 */

import { useState, useEffect, useId } from 'react';
import type { ImportTemplate, ImportType } from '@/types/template';
import { getTemplatesByType, formatRelativeTime } from '@/types/template';

export interface TemplateSelectorProps {
  importType: ImportType;
  onSelect: (template: ImportTemplate | null) => void;
  selectedTemplateId?: string | null;
  disabled?: boolean;
}

export function TemplateSelector({
  importType,
  onSelect,
  selectedTemplateId = null,
  disabled = false,
}: TemplateSelectorProps) {
  const [templates, setTemplates] = useState<ImportTemplate[]>([]);
  const selectId = useId();

  useEffect(() => {
    const loadedTemplates = getTemplatesByType(importType);
    setTemplates(loadedTemplates);
  }, [importType]);

  const handleChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const templateId = event.target.value;
    if (!templateId) {
      onSelect(null);
      return;
    }
    const template = templates.find(t => t.id === templateId);
    onSelect(template || null);
  };

  const selectedTemplate = templates.find(t => t.id === selectedTemplateId);
  const mappedColumnsCount = selectedTemplate
    ? selectedTemplate.mappings.filter(m => m.targetField !== null).length
    : 0;

  if (templates.length === 0) {
    return null;
  }

  return (
    <div className="space-y-2">
      <label
        htmlFor={selectId}
        className="block text-sm font-medium text-gray-700"
      >
        Load from Template
      </label>
      <select
        id={selectId}
        value={selectedTemplateId || ''}
        onChange={handleChange}
        disabled={disabled}
        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
        aria-describedby={selectedTemplate ? `${selectId}-info` : undefined}
      >
        <option value="">-- Select a template --</option>
        {templates.map(template => (
          <option key={template.id} value={template.id}>
            {template.name} ({template.mappings.filter(m => m.targetField).length} columns)
          </option>
        ))}
      </select>

      {selectedTemplate && (
        <p
          id={`${selectId}-info`}
          className="text-sm text-gray-500"
        >
          {mappedColumnsCount} columns mapped · Created {formatRelativeTime(selectedTemplate.createdAt)}
        </p>
      )}
    </div>
  );
}
