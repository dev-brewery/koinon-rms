/**
 * Communication Template Form Page
 * Create or edit a communication template
 */

import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { z } from 'zod';
import {
  useCommunicationTemplate,
  useCreateCommunicationTemplate,
  useUpdateCommunicationTemplate,
} from '@/hooks/useCommunicationTemplates';
import type {
  CreateCommunicationTemplateRequest,
  UpdateCommunicationTemplateRequest,
} from '@/services/api/communicationTemplates';
import { useToast } from '@/contexts/ToastContext';

// ============================================================================
// Validation Schema
// ============================================================================

const templateFormSchema = z.object({
  name: z.string().min(1, 'Name is required').max(200, 'Name must be 200 characters or less'),
  communicationType: z.enum(['Email', 'Sms']),
  subject: z.string().max(500, 'Subject must be 500 characters or less').optional(),
  body: z.string().min(1, 'Body is required'),
  description: z.string().max(500, 'Description must be 500 characters or less').optional(),
  isActive: z.boolean(),
});

type TemplateFormData = z.infer<typeof templateFormSchema>;

// ============================================================================
// Component
// ============================================================================

export function TemplateFormPage() {
  const { idKey } = useParams<{ idKey: string }>();
  const navigate = useNavigate();
  const { success, error: showError } = useToast();
  const isEditMode = !!idKey;

  const { data: template, isLoading } = useCommunicationTemplate(idKey);
  const createMutation = useCreateCommunicationTemplate();
  const updateMutation = useUpdateCommunicationTemplate();

  // Form state
  const [name, setName] = useState('');
  const [communicationType, setCommunicationType] = useState<'Email' | 'Sms'>('Email');
  const [subject, setSubject] = useState('');
  const [body, setBody] = useState('');
  const [description, setDescription] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  // Load existing template data in edit mode
  useEffect(() => {
    if (template && idKey) {
      setName(template.name);
      setCommunicationType(template.communicationType as 'Email' | 'Sms');
      setSubject(template.subject || '');
      setBody(template.body);
      setDescription(template.description || '');
      setIsActive(template.isActive);
    }
  }, [template, idKey]);

  const validateField = (fieldName: string, value: unknown) => {
    const formData = {
      name,
      communicationType,
      subject,
      body,
      description,
      isActive,
      [fieldName]: value,
    };

    const result = templateFormSchema.safeParse(formData);
    if (!result.success) {
      const error = result.error.issues.find(issue => issue.path[0] === fieldName);
      if (error) {
        setValidationErrors(prev => ({ ...prev, [fieldName]: error.message }));
      } else {
        setValidationErrors(prev => {
          // eslint-disable-next-line @typescript-eslint/no-unused-vars
          const { [fieldName]: _removed, ...rest } = prev;
          return rest;
        });
      }
    } else {
      setValidationErrors(prev => {
        // eslint-disable-next-line @typescript-eslint/no-unused-vars
        const { [fieldName]: _removed, ...rest } = prev;
        return rest;
      });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Validate all fields before submit
    const formData: TemplateFormData = {
      name,
      communicationType,
      subject,
      body,
      description,
      isActive,
    };

    const result = templateFormSchema.safeParse(formData);
    if (!result.success) {
      const errors: Record<string, string> = {};
      result.error.issues.forEach(issue => {
        const fieldName = issue.path[0] as string;
        errors[fieldName] = issue.message;
      });
      setValidationErrors(errors);
      return;
    }

    setValidationErrors({});

    try {
      if (isEditMode && idKey) {
        // Update existing template
        const request: UpdateCommunicationTemplateRequest = {
          name,
          subject: communicationType === 'Email' ? subject || undefined : undefined,
          body,
          description: description || undefined,
          isActive,
        };

        await updateMutation.mutateAsync({ idKey, request });
        success('Success', 'Template updated successfully');
        navigate('/admin/communications/templates');
      } else {
        // Create new template
        const request: CreateCommunicationTemplateRequest = {
          name,
          communicationType,
          subject: communicationType === 'Email' ? subject || undefined : undefined,
          body,
          description: description || undefined,
          isActive,
        };

        await createMutation.mutateAsync(request);
        success('Success', 'Template created successfully');
        navigate('/admin/communications/templates');
      }
    } catch (err) {
      showError(
        'Error',
        err instanceof Error ? err.message : 'Failed to save template'
      );
    }
  };

  if (isEditMode && isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
        <p className="ml-4 text-gray-500">Loading template...</p>
      </div>
    );
  }

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link
          to="/admin/communications/templates"
          className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100 transition-colors"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
        </Link>
        <div>
          <h1 className="text-3xl font-bold text-gray-900">
            {isEditMode ? 'Edit Template' : 'Create Template'}
          </h1>
          <p className="mt-1 text-gray-600">
            {isEditMode
              ? 'Update template details and content'
              : 'Create a reusable template for communications'}
          </p>
        </div>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit} className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="space-y-6">
          {/* Name */}
          <div>
            <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
              Name <span className="text-red-500">*</span>
            </label>
            <input
              id="name"
              type="text"
              required
              maxLength={200}
              value={name}
              onChange={(e) => setName(e.target.value)}
              onBlur={() => validateField('name', name)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="e.g., Welcome Email"
            />
            {validationErrors.name && (
              <p className="text-sm text-red-600 mt-1">{validationErrors.name}</p>
            )}
          </div>

          {/* Communication Type (only for create) */}
          {!isEditMode && (
            <div>
              <label htmlFor="communicationType" className="block text-sm font-medium text-gray-700 mb-1">
                Communication Type <span className="text-red-500">*</span>
              </label>
              <select
                id="communicationType"
                required
                value={communicationType}
                onChange={(e) => setCommunicationType(e.target.value as 'Email' | 'Sms')}
                onBlur={() => validateField('communicationType', communicationType)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              >
                <option value="Email">Email</option>
                <option value="Sms">SMS</option>
              </select>
              {validationErrors.communicationType && (
                <p className="text-sm text-red-600 mt-1">{validationErrors.communicationType}</p>
              )}
            </div>
          )}

          {/* Subject (only for Email) */}
          {communicationType === 'Email' && (
            <div>
              <label htmlFor="subject" className="block text-sm font-medium text-gray-700 mb-1">
                Subject
              </label>
              <input
                id="subject"
                type="text"
                maxLength={500}
                value={subject}
                onChange={(e) => setSubject(e.target.value)}
                onBlur={() => validateField('subject', subject)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                placeholder="e.g., Welcome to our community!"
              />
              {validationErrors.subject && (
                <p className="text-sm text-red-600 mt-1">{validationErrors.subject}</p>
              )}
            </div>
          )}

          {/* Body */}
          <div>
            <label htmlFor="body" className="block text-sm font-medium text-gray-700 mb-1">
              Message Body <span className="text-red-500">*</span>
            </label>
            <textarea
              id="body"
              required
              rows={12}
              value={body}
              onChange={(e) => setBody(e.target.value)}
              onBlur={() => validateField('body', body)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 font-mono text-sm"
              placeholder={communicationType === 'Email' 
                ? 'Enter your email message here...'
                : 'Enter your SMS message here...'}
            />
            {validationErrors.body && (
              <p className="text-sm text-red-600 mt-1">{validationErrors.body}</p>
            )}
            <p className="mt-2 text-xs text-gray-500">
              Available merge fields: <strong>{'{{'}FirstName{'}}'}</strong>,
              {' '}<strong>{'{{'}LastName{'}}'}</strong>,
              {' '}<strong>{'{{'}Email{'}}'}</strong>,
              {' '}<strong>{'{{'}Phone{'}}'}</strong>
            </p>
          </div>

          {/* Description */}
          <div>
            <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
              Description
            </label>
            <textarea
              id="description"
              rows={3}
              maxLength={500}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              onBlur={() => validateField('description', description)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="Internal notes about this template (optional)"
            />
            {validationErrors.description && (
              <p className="text-sm text-red-600 mt-1">{validationErrors.description}</p>
            )}
          </div>

          {/* Is Active */}
          <div className="flex items-center gap-2">
            <input
              id="isActive"
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
            />
            <label htmlFor="isActive" className="text-sm font-medium text-gray-700">
              Active
            </label>
            <span className="text-xs text-gray-500">
              (Inactive templates are hidden from selection)
            </span>
          </div>
        </div>

        {/* Actions */}
        <div className="mt-6 flex items-center justify-end gap-3 pt-6 border-t border-gray-200">
          <Link
            to="/admin/communications/templates"
            className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
          >
            Cancel
          </Link>
          <button
            type="submit"
            disabled={createMutation.isPending || updateMutation.isPending}
            className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50"
          >
            {createMutation.isPending || updateMutation.isPending
              ? 'Saving...'
              : isEditMode
              ? 'Update Template'
              : 'Create Template'}
          </button>
        </div>
      </form>
    </div>
  );
}
