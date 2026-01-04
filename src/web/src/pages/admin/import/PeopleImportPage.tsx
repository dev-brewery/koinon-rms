/**
 * People Import Page
 * Multi-step wizard for importing people data from CSV
 */

import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { useToast } from '@/contexts/ToastContext';
import { CsvUploader } from '@/components/import/CsvUploader';
import { FieldMappingEditor } from '@/components/import/FieldMappingEditor';
import { ValidationPreview } from '@/components/import/ValidationPreview';
import { ImportProgress } from '@/components/import/ImportProgress';
import { PEOPLE_TARGET_FIELDS } from '@/components/import/targetFields';
import { useImport } from '@/hooks/useImport';
import { validateImport, executeImport } from '@/services/api/import';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import type { ValidationError, ImportJobDto } from '@/types/import';

export function PeopleImportPage() {
  const navigate = useNavigate();
  const toast = useToast();
  const {
    currentStep,
    file,
    csvHeaders,
    fieldMappings,
    handleFileSelected,
    handleMappingsChange,
    goToStep,
    reset,
  } = useImport();

  const [validationErrors, setValidationErrors] = useState<ValidationError[]>([]);
  const [importJob, setImportJob] = useState<ImportJobDto | null>(null);

  // Validation mutation
  const validateMutation = useMutation({
    mutationFn: async () => {
      if (!file) throw new Error('No file selected');
      
      const mappingsRecord: Record<string, string> = {};
      fieldMappings.forEach((mapping) => {
        if (mapping.targetField) {
          mappingsRecord[mapping.targetField] = mapping.csvColumn;
        }
      });

      return validateImport(file, 'People', mappingsRecord);
    },
    onSuccess: () => {
      // TODO(#408): Map validation errors from ImportJobDto when backend is ready
      setValidationErrors([]);
      goToStep('validation');
    },
    onError: (error) => {
      toast.error(
        'Validation Failed',
        error instanceof Error ? error.message : 'An unexpected error occurred'
      );
    },
  });

  // Import execution mutation
  const executeMutation = useMutation({
    mutationFn: async () => {
      if (!file) throw new Error('No file selected');
      
      const mappingsRecord: Record<string, string> = {};
      fieldMappings.forEach((mapping) => {
        if (mapping.targetField) {
          mappingsRecord[mapping.targetField] = mapping.csvColumn;
        }
      });

      return executeImport(file, 'People', mappingsRecord);
    },
    onSuccess: (data) => {
      setImportJob(data);
      goToStep('progress');
    },
    onError: (error) => {
      toast.error(
        'Import Failed',
        error instanceof Error ? error.message : 'An unexpected error occurred'
      );
    },
  });

  const handleValidate = () => {
    validateMutation.mutate();
  };

  const handleStartImport = () => {
    executeMutation.mutate();
  };

  const handleCancel = () => {
    if (window.confirm('Are you sure you want to cancel this import?')) {
      reset();
      navigate('/admin/import/history');
    }
  };

  const canProceedToValidation = () => {
    const requiredFields = PEOPLE_TARGET_FIELDS.filter((f) => f.required);
    const mappedFields = new Set(
      fieldMappings.map((m) => m.targetField).filter(Boolean)
    );
    return requiredFields.every((field) => mappedFields.has(field.value));
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <div className="flex items-center gap-2 text-sm text-gray-600 mb-2">
          <a href="/admin" className="hover:text-gray-900">
            Admin
          </a>
          <span>/</span>
          <a href="/admin/import/history" className="hover:text-gray-900">
            Import
          </a>
          <span>/</span>
          <span className="text-gray-900">People Import</span>
        </div>
        <h1 className="text-3xl font-bold text-gray-900">Import People</h1>
        <p className="mt-2 text-gray-600">
          Upload and map CSV data to import people records
        </p>
      </div>

      {/* Progress Steps */}
      <Card className="p-6">
        <div className="flex items-center justify-between">
          {(['upload', 'mapping', 'validation', 'progress'] as const).map(
            (step, index) => {
              const stepLabels = {
                upload: '1. Upload CSV',
                mapping: '2. Map Fields',
                validation: '3. Validate',
                progress: '4. Import',
              };
              const isActive = currentStep === step;
              const isCompleted =
                ['upload', 'mapping', 'validation', 'progress'].indexOf(
                  currentStep
                ) >
                ['upload', 'mapping', 'validation', 'progress'].indexOf(step);

              return (
                <div key={step} className="flex items-center flex-1">
                  <div className="flex items-center">
                    <div
                      className={`
                        w-10 h-10 rounded-full flex items-center justify-center font-semibold
                        ${
                          isActive
                            ? 'bg-blue-600 text-white'
                            : isCompleted
                            ? 'bg-green-600 text-white'
                            : 'bg-gray-200 text-gray-600'
                        }
                      `}
                    >
                      {isCompleted ? '✓' : index + 1}
                    </div>
                    <span
                      className={`ml-3 text-sm font-medium ${
                        isActive ? 'text-blue-600' : 'text-gray-600'
                      }`}
                    >
                      {stepLabels[step]}
                    </span>
                  </div>
                  {index < 3 && (
                    <div
                      className={`flex-1 h-0.5 mx-4 ${
                        isCompleted ? 'bg-green-600' : 'bg-gray-200'
                      }`}
                    />
                  )}
                </div>
              );
            }
          )}
        </div>
      </Card>

      {/* Step Content */}
      {currentStep === 'upload' && (
        <CsvUploader
          onFileSelected={handleFileSelected}
          isLoading={validateMutation.isPending}
        />
      )}

      {currentStep === 'mapping' && (
        <div className="space-y-4">
          <FieldMappingEditor
            csvHeaders={csvHeaders}
            targetFields={PEOPLE_TARGET_FIELDS}
            onMappingsChange={handleMappingsChange}
            importType="people"
          />
          <div className="flex justify-between">
            <Button variant="outline" onClick={() => goToStep('upload')}>
              Back
            </Button>
            <Button
              variant="primary"
              onClick={handleValidate}
              disabled={!canProceedToValidation() || validateMutation.isPending}
            >
              {validateMutation.isPending ? 'Validating...' : 'Validate & Continue'}
            </Button>
          </div>
        </div>
      )}

      {currentStep === 'validation' && (
        <div className="space-y-4">
          <ValidationPreview
            validationErrors={validationErrors}
            onFixErrors={() => goToStep('mapping')}
            onImportAnyway={handleStartImport}
            onCancel={handleCancel}
          />
          <div className="flex justify-between">
            <Button variant="outline" onClick={() => goToStep('mapping')}>
              Back to Mapping
            </Button>
          </div>
        </div>
      )}

      {currentStep === 'progress' && importJob && (
        <div className="space-y-4">
          <ImportProgress
            progress={(importJob.processedRows / importJob.totalRows) * 100}
            processedRows={importJob.processedRows}
            totalRows={importJob.totalRows}
            successCount={importJob.successCount}
            errorCount={importJob.errorCount}
            elapsedSeconds={0}
            status={
              importJob.status === 'Completed'
                ? 'completed'
                : importJob.status === 'Failed'
                ? 'failed'
                : 'importing'
            }
          />
          {importJob.status === 'Completed' && (
            <div className="flex justify-center">
              <Button
                variant="primary"
                onClick={() => navigate('/admin/import/history')}
              >
                View Import History
              </Button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
