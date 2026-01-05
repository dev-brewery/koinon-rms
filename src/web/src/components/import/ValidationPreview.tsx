import { useState, useMemo } from 'react';
import { ValidationError, ValidationSeverity } from '@/types/import';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';

export interface ValidationPreviewProps {
  validationErrors: ValidationError[];
  onFixErrors?: () => void;
  onImportAnyway: () => void;
  onCancel: () => void;
  onDownloadReport?: () => void;
}

type SeverityFilter = 'all' | ValidationSeverity;

export function ValidationPreview({
  validationErrors,
  onFixErrors,
  onImportAnyway,
  onCancel,
  onDownloadReport,
}: ValidationPreviewProps) {
  const [severityFilter, setSeverityFilter] = useState<SeverityFilter>('all');
  const [sortBy, setSortBy] = useState<'row' | 'severity'>('row');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc');

  const severityCounts = useMemo(() => {
    return validationErrors.reduce(
      (acc, error) => {
        acc[error.severity]++;
        return acc;
      },
      { error: 0, warning: 0, info: 0 } as Record<ValidationSeverity, number>
    );
  }, [validationErrors]);

  const filteredAndSortedErrors = useMemo(() => {
    const filtered =
      severityFilter === 'all'
        ? validationErrors
        : validationErrors.filter((e) => e.severity === severityFilter);

    const severityOrder: Record<ValidationSeverity, number> = {
      error: 0,
      warning: 1,
      info: 2,
    };

    const sorted = [...filtered].sort((a, b) => {
      let comparison = 0;
      if (sortBy === 'row') {
        comparison = a.rowNumber - b.rowNumber;
      } else {
        comparison = severityOrder[a.severity] - severityOrder[b.severity];
      }
      return sortOrder === 'asc' ? comparison : -comparison;
    });

    return sorted;
  }, [validationErrors, severityFilter, sortBy, sortOrder]);

  const toggleSort = (column: 'row' | 'severity') => {
    if (sortBy === column) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(column);
      setSortOrder('asc');
    }
  };

  const getSeverityIcon = (severity: ValidationSeverity) => {
    switch (severity) {
      case 'error':
        return (
          <span className="text-red-600 font-bold" aria-label="Error">
            ✗
          </span>
        );
      case 'warning':
        return (
          <span className="text-yellow-600 font-bold" aria-label="Warning">
            ⚠
          </span>
        );
      case 'info':
        return (
          <span className="text-blue-600 font-bold" aria-label="Info">
            ℹ
          </span>
        );
    }
  };

  const getSeverityColor = (severity: ValidationSeverity) => {
    switch (severity) {
      case 'error':
        return 'text-red-600';
      case 'warning':
        return 'text-yellow-600';
      case 'info':
        return 'text-blue-600';
    }
  };

  if (validationErrors.length === 0) {
    return (
      <Card className="p-6">
        <div className="text-center text-gray-500">
          <div className="mb-4 text-green-600 text-5xl">✓</div>
          <p className="text-lg font-medium">No validation issues found</p>
          <p className="mt-2 text-sm">All rows are valid and ready to import</p>
          <div className="mt-6 flex justify-center gap-4">
            <Button onClick={onImportAnyway} variant="primary">
              Continue Import
            </Button>
            <Button onClick={onCancel} variant="outline">
              Cancel
            </Button>
          </div>
        </div>
      </Card>
    );
  }

  return (
    <Card className="p-6">
      <div className="space-y-4">
        {/* Header */}
        <div>
          <h2 className="text-xl font-semibold mb-2">Validation Results</h2>
          <div className="flex items-center gap-4 text-sm">
            <span className="text-green-600 font-medium">
              ✓ {validationErrors.length - severityCounts.error - severityCounts.warning} valid
            </span>
            <span className={cn('font-medium', getSeverityColor('warning'))}>
              ⚠ {severityCounts.warning} warnings
            </span>
            <span className={cn('font-medium', getSeverityColor('error'))}>
              ✗ {severityCounts.error} errors
            </span>
          </div>
        </div>

        {/* Filter Controls */}
        <div className="flex items-center gap-4">
          <label htmlFor="severity-filter" className="text-sm font-medium">
            Filter by severity:
          </label>
          <select
            id="severity-filter"
            value={severityFilter}
            onChange={(e) => setSeverityFilter(e.target.value as SeverityFilter)}
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            aria-label="Filter validation results by severity"
          >
            <option value="all">All ({validationErrors.length})</option>
            <option value="error">Errors ({severityCounts.error})</option>
            <option value="warning">Warnings ({severityCounts.warning})</option>
            <option value="info">Info ({severityCounts.info})</option>
          </select>
        </div>

        {/* Table */}
        <div className="overflow-x-auto border border-gray-200 rounded-lg">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th
                  scope="col"
                  className="px-4 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                  onClick={() => toggleSort('row')}
                >
                  <div className="flex items-center gap-1">
                    Row #
                    {sortBy === 'row' && (
                      <span className="text-blue-600">{sortOrder === 'asc' ? '↑' : '↓'}</span>
                    )}
                  </div>
                </th>
                <th
                  scope="col"
                  className="px-4 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                  onClick={() => toggleSort('severity')}
                >
                  <div className="flex items-center gap-1">
                    Severity
                    {sortBy === 'severity' && (
                      <span className="text-blue-600">{sortOrder === 'asc' ? '↑' : '↓'}</span>
                    )}
                  </div>
                </th>
                <th
                  scope="col"
                  className="px-4 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider"
                >
                  Column
                </th>
                <th
                  scope="col"
                  className="px-4 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider"
                >
                  Value
                </th>
                <th
                  scope="col"
                  className="px-4 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider"
                >
                  Issue
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {filteredAndSortedErrors.map((error, index) => (
                <tr key={index} className="hover:bg-gray-50">
                  <td className="px-4 py-3 whitespace-nowrap text-sm font-medium text-gray-900">
                    {error.rowNumber}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm">
                    {getSeverityIcon(error.severity)}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-900">
                    {error.columnName}
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-700 max-w-xs truncate" title={error.value}>
                    {error.value}
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-700">{error.message}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Action Buttons */}
        <div className="flex justify-end gap-4 pt-4 border-t border-gray-200">
          {onFixErrors && severityCounts.error > 0 && (
            <Button onClick={onFixErrors} variant="primary">
              Fix Errors
            </Button>
          )}
          {onDownloadReport && (
            <Button onClick={onDownloadReport} variant="outline">
              Download Report
            </Button>
          )}
          <Button
            onClick={onImportAnyway}
            variant={severityCounts.error > 0 ? 'outline' : 'primary'}
          >
            Import Anyway
          </Button>
          <Button onClick={onCancel} variant="ghost">
            Cancel
          </Button>
        </div>
      </div>
    </Card>
  );
}
