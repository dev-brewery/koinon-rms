import { ImportStatus } from '@/types/import';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';

export interface ImportProgressProps {
  progress: number;
  processedRows: number;
  totalRows: number;
  successCount: number;
  errorCount: number;
  elapsedSeconds: number;
  status: ImportStatus;
  onCancel?: () => void;
}

export function ImportProgress({
  progress,
  processedRows,
  totalRows,
  successCount,
  errorCount,
  elapsedSeconds,
  status,
  onCancel,
}: ImportProgressProps) {
  const formatTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return mins + ':' + secs.toString().padStart(2, '0');
  };

  const getStatusMessage = () => {
    switch (status) {
      case 'completed':
        return (
          <div className="flex items-center gap-2 text-green-600 text-lg font-semibold">
            <span className="text-2xl">✓</span>
            Import completed successfully
          </div>
        );
      case 'failed':
        return (
          <div className="flex items-center gap-2 text-red-600 text-lg font-semibold">
            <span className="text-2xl">✗</span>
            Import failed
          </div>
        );
      case 'cancelled':
        return (
          <div className="flex items-center gap-2 text-yellow-600 text-lg font-semibold">
            <span className="text-2xl">⚠</span>
            Import cancelled by user
          </div>
        );
      case 'importing':
        return (
          <div className="text-lg font-semibold text-gray-900">Importing People...</div>
        );
    }
  };

  const isCompleted = status === 'completed' || status === 'failed' || status === 'cancelled';

  return (
    <Card className="p-6">
      <div className="space-y-6" role="status" aria-live="polite" aria-atomic="true">
        {/* Status Message */}
        {getStatusMessage()}

        {/* Progress Bar */}
        <div className="space-y-2">
          <div className="relative w-full h-8 bg-gray-200 rounded-lg overflow-hidden">
            <div
              className={cn(
                'absolute top-0 left-0 h-full transition-all duration-300 ease-out',
                status === 'completed'
                  ? 'bg-green-600'
                  : status === 'failed'
                  ? 'bg-red-600'
                  : status === 'cancelled'
                  ? 'bg-yellow-600'
                  : 'bg-blue-600'
              )}
              style={{ width: Math.min(100, Math.max(0, progress)) + '%' }}
              role="progressbar"
              aria-valuenow={progress}
              aria-valuemin={0}
              aria-valuemax={100}
              aria-label="Import progress"
            />
            <div className="absolute inset-0 flex items-center justify-center">
              <span className="text-sm font-semibold text-gray-900 mix-blend-difference">
                {Math.round(progress)}%
              </span>
            </div>
          </div>
        </div>

        {/* Stats */}
        <div className="space-y-2 text-sm">
          <div className="flex items-center justify-between">
            <span className="text-gray-700 font-medium">Processed:</span>
            <span className="text-gray-900 font-semibold">
              {processedRows} of {totalRows} rows
            </span>
          </div>

          <div className="flex items-center justify-between">
            <span className="text-gray-700 font-medium">Results:</span>
            <div className="flex items-center gap-4">
              <span className="text-green-600 font-semibold">✓ Success: {successCount}</span>
              <span className="text-red-600 font-semibold">✗ Errors: {errorCount}</span>
            </div>
          </div>

          <div className="flex items-center justify-between">
            <span className="text-gray-700 font-medium">Elapsed:</span>
            <span className="text-gray-900 font-semibold">{formatTime(elapsedSeconds)}</span>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex justify-center pt-4 border-t border-gray-200">
          {status === 'importing' && onCancel && (
            <Button onClick={onCancel} variant="outline" size="md">
              Cancel Import
            </Button>
          )}

          {isCompleted && errorCount > 0 && (
            <Button
              onClick={() => {
                const errorReport = 'Import Error Report\n\nTotal Errors: ' + errorCount + '\nSuccessful: ' + successCount + '\nTotal Processed: ' + processedRows + '\n\nPlease review the import logs for details.';
                const blob = new Blob([errorReport], { type: 'text/plain' });
                const url = URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = url;
                link.download = 'import-errors-' + new Date().toISOString().slice(0, 10) + '.txt';
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                URL.revokeObjectURL(url);
              }}
              variant="outline"
              size="md"
            >
              Download Error Report
            </Button>
          )}
        </div>
      </div>
    </Card>
  );
}
