/**
 * TemplateSelector Component
 * Dropdown to select and apply communication templates
 */

import { useState, useEffect, useRef } from 'react';
import {
  useCommunicationTemplates,
  useCommunicationTemplate,
} from '@/hooks/useCommunicationTemplates';

interface TemplateSelectorProps {
  communicationType: 'Email' | 'Sms';
  onSelect: (template: { subject?: string; body: string }) => void;
  disabled?: boolean;
}

export function TemplateSelector({
  communicationType,
  onSelect,
  disabled = false,
}: TemplateSelectorProps) {
  const [selectedIdKey, setSelectedIdKey] = useState<string>('');
  const currentRequestRef = useRef<string>('');

  const { data, isLoading } = useCommunicationTemplates({
    type: communicationType,
    isActive: true,
  });

  // Fetch full template details when one is selected
  const { data: fullTemplate, isLoading: isLoadingTemplate } = useCommunicationTemplate(
    selectedIdKey || undefined
  );

  const templates = data?.data || [];

  const handleSelectChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const templateIdKey = e.target.value;

    if (!templateIdKey) {
      // "No template" selected
      currentRequestRef.current = '';
      onSelect({ subject: '', body: '' });
      return;
    }

    // Track current request to ignore stale responses
    currentRequestRef.current = templateIdKey;
    setSelectedIdKey(templateIdKey);
  };

  // Apply template when full details are loaded
  useEffect(() => {
    // Ignore stale responses - only process if this is the current request
    if (fullTemplate && selectedIdKey && currentRequestRef.current === selectedIdKey) {
      onSelect({
        subject: fullTemplate.subject || '',
        body: fullTemplate.body,
      });
      // Reset selected to prevent re-triggering
      setSelectedIdKey('');
      currentRequestRef.current = '';
    }
  }, [fullTemplate, selectedIdKey, onSelect]);

  return (
    <div>
      <label htmlFor="template-selector" className="block text-sm font-medium text-gray-700 mb-1">
        Use Template (optional)
      </label>
      <select
        id="template-selector"
        onChange={handleSelectChange}
        disabled={disabled || isLoading || isLoadingTemplate}
        value=""
        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
      >
        <option value="">Select a template...</option>
        {templates.map((template) => (
          <option key={template.idKey} value={template.idKey}>
            {template.name}
          </option>
        ))}
      </select>
      {templates.length === 0 && !isLoading && (
        <p className="mt-1 text-xs text-gray-500">
          No active {communicationType.toLowerCase()} templates available
        </p>
      )}
      {isLoadingTemplate && (
        <p className="mt-1 text-xs text-gray-500">Loading template...</p>
      )}
    </div>
  );
}
