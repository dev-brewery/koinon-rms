import { useState, useCallback } from 'react';
import { useDropzone } from 'react-dropzone';
import { Button } from '../ui/Button';
import { Card } from '../ui/Card';

export interface CsvUploaderProps {
  onFileSelected: (file: File, headers: string[], previewRows: string[][]) => void;
  maxSizeBytes?: number;
  isLoading?: boolean;
}

interface ParsedCsv {
  headers: string[];
  previewRows: string[][];
}

const DEFAULT_MAX_SIZE = 10 * 1024 * 1024; // 10MB

function parseCsvLine(line: string): string[] {
  const result: string[] = [];
  let current = '';
  let inQuotes = false;
  
  for (let i = 0; i < line.length; i++) {
    const char = line[i];
    const nextChar = line[i + 1];
    
    if (char === '"') {
      if (inQuotes && nextChar === '"') {
        current += '"';
        i++;
      } else {
        inQuotes = !inQuotes;
      }
    } else if (char === ',' && !inQuotes) {
      result.push(current.trim());
      current = '';
    } else {
      current += char;
    }
  }
  
  result.push(current.trim());
  return result;
}

function parseCsv(content: string): ParsedCsv {
  const lines = content.split(/\r?\n/).filter(line => line.trim());
  
  if (lines.length === 0) {
    throw new Error('CSV file is empty');
  }
  
  const headers = parseCsvLine(lines[0]);
  const previewRows = lines.slice(1, 6).map(line => parseCsvLine(line));
  
  return { headers, previewRows };
}

export function CsvUploader({ 
  onFileSelected, 
  maxSizeBytes = DEFAULT_MAX_SIZE,
  isLoading = false 
}: CsvUploaderProps) {
  const [preview, setPreview] = useState<ParsedCsv | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const onDrop = useCallback(async (acceptedFiles: File[]) => {
    setError(null);
    
    if (acceptedFiles.length === 0) {
      setError('Please select a valid CSV file');
      return;
    }
    
    const file = acceptedFiles[0];
    
    if (file.size > maxSizeBytes) {
      setError(`File size exceeds ${Math.round(maxSizeBytes / 1024 / 1024)}MB limit`);
      return;
    }
    
    try {
      const content = await file.text();
      const parsed = parseCsv(content);
      
      setPreview(parsed);
      setSelectedFile(file);
      onFileSelected(file, parsed.headers, parsed.previewRows);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to parse CSV file');
      setPreview(null);
      setSelectedFile(null);
    }
  }, [maxSizeBytes, onFileSelected]);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      'text/csv': ['.csv']
    },
    maxFiles: 1,
    disabled: isLoading
  });

  const handleClear = () => {
    setPreview(null);
    setSelectedFile(null);
    setError(null);
  };

  return (
    <div className="space-y-4">
      {!preview ? (
        <div
          {...getRootProps()}
          className={`
            border-2 border-dashed rounded-lg p-8 text-center cursor-pointer
            transition-colors duration-200
            ${isDragActive ? 'border-blue-500 bg-blue-50' : 'border-gray-300 hover:border-gray-400'}
            ${isLoading ? 'opacity-50 cursor-not-allowed' : ''}
          `}
        >
          <input {...getInputProps()} aria-label="CSV file upload" />
          
          <div className="space-y-2">
            <svg
              className="mx-auto h-12 w-12 text-gray-400"
              stroke="currentColor"
              fill="none"
              viewBox="0 0 48 48"
              aria-hidden="true"
            >
              <path
                d="M28 8H12a4 4 0 00-4 4v20m32-12v8m0 0v8a4 4 0 01-4 4H12a4 4 0 01-4-4v-4m32-4l-3.172-3.172a4 4 0 00-5.656 0L28 28M8 32l9.172-9.172a4 4 0 015.656 0L28 28m0 0l4 4m4-24h8m-4-4v8m-12 4h.02"
                strokeWidth={2}
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
            
            {isDragActive ? (
              <p className="text-blue-600 font-medium">Drop the CSV file here</p>
            ) : (
              <>
                <p className="text-gray-700 font-medium">
                  Drag and drop a CSV file here, or click to select
                </p>
                <p className="text-sm text-gray-500">
                  Maximum file size: {Math.round(maxSizeBytes / 1024 / 1024)}MB
                </p>
              </>
            )}
          </div>
        </div>
      ) : (
        <Card className="p-4">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <svg
                className="h-5 w-5 text-green-600"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <span className="font-medium text-gray-900">
                {selectedFile?.name}
              </span>
              <span className="text-sm text-gray-500">
                ({Math.round((selectedFile?.size || 0) / 1024)}KB)
              </span>
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={handleClear}
              disabled={isLoading}
            >
              Clear
            </Button>
          </div>

          <div className="border rounded-lg overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  {preview.headers.map((header, idx) => (
                    <th
                      key={idx}
                      className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                    >
                      {header}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {preview.previewRows.map((row, rowIdx) => (
                  <tr key={rowIdx} className="hover:bg-gray-50">
                    {row.map((cell, cellIdx) => (
                      <td
                        key={cellIdx}
                        className="px-4 py-3 text-sm text-gray-900 whitespace-nowrap"
                      >
                        {cell || <span className="text-gray-400 italic">empty</span>}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <p className="mt-2 text-xs text-gray-500">
            Showing first {preview.previewRows.length} rows of data
          </p>
        </Card>
      )}

      {error && (
        <div 
          className="bg-red-50 border border-red-200 rounded-lg p-4 flex items-start gap-3"
          role="alert"
        >
          <svg
            className="h-5 w-5 text-red-600 flex-shrink-0 mt-0.5"
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
            <h3 className="text-sm font-medium text-red-800">Upload Error</h3>
            <p className="text-sm text-red-700 mt-1">{error}</p>
          </div>
        </div>
      )}

      {isLoading && (
        <div className="flex items-center justify-center gap-2 text-gray-600">
          <svg
            className="animate-spin h-5 w-5"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            />
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            />
          </svg>
          <span className="text-sm">Processing...</span>
        </div>
      )}
    </div>
  );
}
