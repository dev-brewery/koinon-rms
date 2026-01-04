/**
 * Import History Page
 * Displays past import jobs with filtering and error reporting
 */

import { Link } from 'react-router-dom';
import { useImportJobs } from '@/hooks/useImportJobs';
import { downloadImportErrors } from '@/services/api/import';
import { useToast } from '@/contexts/ToastContext';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Loading, EmptyState, ErrorState } from '@/components/ui';

function getStatusBadge(status: string) {
  const styles: Record<string, string> = {
    Completed: 'bg-green-100 text-green-800',
    Failed: 'bg-red-100 text-red-800',
    Processing: 'bg-blue-100 text-blue-800',
    Pending: 'bg-yellow-100 text-yellow-800',
    Cancelled: 'bg-gray-100 text-gray-800',
  };

  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
        styles[status] || 'bg-gray-100 text-gray-800'
      }`}
    >
      {status}
    </span>
  );
}

function formatDate(dateString: string | undefined) {
  if (!dateString) return 'N/A';
  return new Date(dateString).toLocaleString();
}

export function ImportHistoryPage() {
  const toast = useToast();
  const { jobs, isLoading, error, typeFilter, setTypeFilter, refetch } =
    useImportJobs();

  const handleDownloadErrors = async (idKey: string, fileName: string) => {
    try {
      const blob = await downloadImportErrors(idKey);
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `errors-${fileName}`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch (err) {
      toast.error(
        'Download Failed',
        err instanceof Error ? err.message : 'Failed to download error report'
      );
    }
  };

  if (error) {
    return <ErrorState title="Error" message="Failed to load import history" onRetry={refetch} />;
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Import History</h1>
          <p className="mt-2 text-gray-600">
            View past import jobs and download error reports
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Link
            to="/admin/import/people"
            className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
          >
            Import People
          </Link>
          <Link
            to="/admin/import/families"
            className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
          >
            Import Families
          </Link>
        </div>
      </div>

      {/* Filters */}
      <Card className="p-4">
        <div className="flex items-center gap-4">
          <label htmlFor="type-filter" className="text-sm font-medium text-gray-700">
            Filter by type:
          </label>
          <select
            id="type-filter"
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          >
            <option value="all">All Types</option>
            <option value="People">People</option>
            <option value="Families">Families</option>
            <option value="Attendance">Attendance</option>
            <option value="Giving">Giving</option>
          </select>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Refresh
          </Button>
        </div>
      </Card>

      {/* Table */}
      {isLoading ? (
        <Loading text="Loading import history..." />
      ) : jobs.length === 0 ? (
        <EmptyState
          title="No imports found"
          description={
            typeFilter === 'all'
              ? 'No import jobs have been run yet'
              : `No ${typeFilter} imports found`
          }
        />
      ) : (
        <Card>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    File Name
                  </th>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Type
                  </th>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Status
                  </th>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Results
                  </th>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Date
                  </th>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {jobs.map((job) => (
                  <tr key={job.idKey} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                      {job.fileName}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-700">
                      {job.importType}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {getStatusBadge(job.status)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-700">
                      <div className="flex flex-col gap-1">
                        <div>
                          <span className="text-gray-600">Total:</span>{' '}
                          {job.totalRows}
                        </div>
                        <div>
                          <span className="text-green-600">Success:</span>{' '}
                          {job.successCount}
                        </div>
                        {job.errorCount > 0 && (
                          <div>
                            <span className="text-red-600">Errors:</span>{' '}
                            {job.errorCount}
                          </div>
                        )}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-700">
                      {formatDate(job.completedAt || job.startedAt)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm">
                      {job.errorCount > 0 && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() =>
                            handleDownloadErrors(job.idKey, job.fileName)
                          }
                        >
                          Download Errors
                        </Button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}
    </div>
  );
}
