import { useState, useEffect, useMemo } from 'react';
import { TargetField } from './targetFields';

export interface FieldMapping {
  csvColumn: string;
  targetField: string | null;
}

export interface FieldMappingEditorProps {
  csvHeaders: string[];
  targetFields: TargetField[];
  initialMappings?: FieldMapping[];
  onMappingsChange: (mappings: FieldMapping[]) => void;
  importType: 'people' | 'attendance';
}

function getSuggestedMapping(csvHeader: string, targetFields: TargetField[]): string | null {
  const normalized = csvHeader.toLowerCase().replace(/[_\s-]/g, '');
  
  const exactMatch = targetFields.find(f => 
    f.value.toLowerCase() === normalized || 
    f.label.toLowerCase().replace(/[_\s-]/g, '') === normalized
  );
  if (exactMatch) return exactMatch.value;
  
  const partialMatch = targetFields.find(f => 
    normalized.includes(f.value.toLowerCase()) ||
    f.value.toLowerCase().includes(normalized)
  );
  if (partialMatch) return partialMatch.value;
  
  return null;
}

export function FieldMappingEditor({
  csvHeaders,
  targetFields,
  initialMappings = [],
  onMappingsChange,
  importType: _importType
}: FieldMappingEditorProps) {
  const [mappings, setMappings] = useState<FieldMapping[]>([]);

  useEffect(() => {
    if (initialMappings.length > 0) {
      setMappings(initialMappings);
    } else {
      const suggested = csvHeaders.map(header => ({
        csvColumn: header,
        targetField: getSuggestedMapping(header, targetFields)
      }));
      setMappings(suggested);
      onMappingsChange(suggested);
    }
  }, [csvHeaders, targetFields, initialMappings, onMappingsChange]); // SYNC OK

  const handleMappingChange = (csvColumn: string, targetField: string | null) => {
    const newMappings = mappings.map(m =>
      m.csvColumn === csvColumn ? { ...m, targetField } : m
    );
    setMappings(newMappings);
    onMappingsChange(newMappings);
  };

  const groupedFields = useMemo(() => {
    const groups: Record<string, TargetField[]> = {};
    targetFields.forEach(field => {
      if (!groups[field.group]) {
        groups[field.group] = [];
      }
      groups[field.group].push(field);
    });
    return groups;
  }, [targetFields]);

  const duplicates = useMemo(() => {
    const fieldCounts: Record<string, number> = {};
    mappings.forEach(m => {
      if (m.targetField) {
        fieldCounts[m.targetField] = (fieldCounts[m.targetField] || 0) + 1;
      }
    });
    return Object.keys(fieldCounts).filter(field => fieldCounts[field] > 1);
  }, [mappings]);

  const unmappedRequired = useMemo(() => {
    const mappedFields = new Set(mappings.map(m => m.targetField).filter(Boolean));
    return targetFields.filter(f => f.required && !mappedFields.has(f.value));
  }, [mappings, targetFields]);

  return (
    <div className="space-y-4">
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <h3 className="text-sm font-medium text-blue-900 mb-2">Field Mapping</h3>
        <p className="text-sm text-blue-700">
          Map your CSV columns to the target fields. Fields marked with{' '}
          <span className="text-red-600 font-bold">*</span> are required.
        </p>
      </div>

      {unmappedRequired.length > 0 && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 flex items-start gap-3">
          <svg
            className="h-5 w-5 text-yellow-600 flex-shrink-0 mt-0.5"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
            />
          </svg>
          <div>
            <h4 className="text-sm font-medium text-yellow-800">Required Fields Missing</h4>
            <p className="text-sm text-yellow-700 mt-1">
              The following required fields are not mapped:{' '}
              {unmappedRequired.map(f => f.label).join(', ')}
            </p>
          </div>
        </div>
      )}

      {duplicates.length > 0 && (
        <div className="bg-orange-50 border border-orange-200 rounded-lg p-4 flex items-start gap-3">
          <svg
            className="h-5 w-5 text-orange-600 flex-shrink-0 mt-0.5"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <div>
            <h4 className="text-sm font-medium text-orange-800">Duplicate Mappings</h4>
            <p className="text-sm text-orange-700 mt-1">
              Multiple CSV columns are mapped to: {duplicates.map(d => 
                targetFields.find(f => f.value === d)?.label || d
              ).join(', ')}
            </p>
          </div>
        </div>
      )}

      <div className="border rounded-lg divide-y divide-gray-200">
        {mappings.map((mapping, idx) => {
          const selectedField = targetFields.find(f => f.value === mapping.targetField);
          const isDuplicate = duplicates.includes(mapping.targetField || '');

          return (
            <div
              key={idx}
              className={'p-4 ' + (isDuplicate ? 'bg-orange-50' : 'bg-white')}
            >
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 items-center">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    CSV Column
                  </label>
                  <div className="text-sm text-gray-900 font-mono bg-gray-100 px-3 py-2 rounded border border-gray-300">
                    {mapping.csvColumn}
                  </div>
                </div>

                <div>
                  <label 
                    htmlFor={'mapping-' + idx}
                    className="block text-sm font-medium text-gray-700 mb-1"
                  >
                    Maps To
                  </label>
                  <select
                    id={'mapping-' + idx}
                    value={mapping.targetField || ''}
                    onChange={(e) => handleMappingChange(
                      mapping.csvColumn,
                      e.target.value || null
                    )}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    aria-label={'Map ' + mapping.csvColumn + ' to target field'}
                  >
                    <option value="">-- Ignore column --</option>
                    {Object.entries(groupedFields).map(([group, fields]) => (
                      <optgroup key={group} label={group}>
                        {fields.map(field => (
                          <option key={field.value} value={field.value}>
                            {field.label}
                            {field.required ? ' *' : ''}
                          </option>
                        ))}
                      </optgroup>
                    ))}
                  </select>
                  {selectedField?.required && (
                    <p className="mt-1 text-xs text-gray-500">
                      <span className="text-red-600 font-bold">*</span> Required field
                    </p>
                  )}
                </div>
              </div>
            </div>
          );
        })}
      </div>

      <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
        <h4 className="text-sm font-medium text-gray-900 mb-2">Summary</h4>
        <div className="grid grid-cols-2 md:grid-cols-3 gap-4 text-sm">
          <div>
            <span className="text-gray-500">Total Columns:</span>{' '}
            <span className="font-medium text-gray-900">{csvHeaders.length}</span>
          </div>
          <div>
            <span className="text-gray-500">Mapped:</span>{' '}
            <span className="font-medium text-gray-900">
              {mappings.filter(m => m.targetField).length}
            </span>
          </div>
          <div>
            <span className="text-gray-500">Ignored:</span>{' '}
            <span className="font-medium text-gray-900">
              {mappings.filter(m => !m.targetField).length}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}
