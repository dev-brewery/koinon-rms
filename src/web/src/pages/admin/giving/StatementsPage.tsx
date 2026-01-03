/**
 * Contribution Statements Page
 * Generate and manage tax-deductible giving statements
 */

import { useState } from 'react';
import { useStatements, useEligiblePeople, useGenerateStatement, useDownloadStatementPdf } from '@/hooks/useGiving';
import { Loading, EmptyState, ErrorState } from '@/components/ui';
import { useToast } from '@/contexts/ToastContext';

export function StatementsPage() {
  const toast = useToast();
  const currentYear = new Date().getFullYear();

  // Filter state
  const [startDate, setStartDate] = useState<string>(`${currentYear}-01-01`);
  const [endDate, setEndDate] = useState<string>(`${currentYear}-12-31`);
  const [minimumAmount, setMinimumAmount] = useState<string>('');
  
  // Pagination for statements list
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  
  // Selection state
  const [selectedPeople, setSelectedPeople] = useState<Set<string>>(new Set());
  const [showEligible, setShowEligible] = useState(false);

  // Queries and mutations
  const { data: statementsData, isLoading: statementsLoading, error: statementsError, refetch: refetchStatements } = useStatements(page, pageSize);
  const { data: eligiblePeople, isLoading: eligibleLoading, error: eligibleError, refetch: refetchEligible } = useEligiblePeople(
    startDate,
    endDate,
    minimumAmount ? parseFloat(minimumAmount) : undefined
  );
  const generateStatement = useGenerateStatement();
  const downloadPdf = useDownloadStatementPdf();

  const statements = statementsData?.data || [];
  const meta = statementsData?.meta;

  const handleFindEligible = () => {
    if (!startDate || !endDate) {
      toast.error('Date range required', 'Please select both start and end dates');
      return;
    }
    setShowEligible(true);
    refetchEligible();
  };

  const handleTogglePerson = (personIdKey: string) => {
    setSelectedPeople((prev) => {
      const next = new Set(prev);
      if (next.has(personIdKey)) {
        next.delete(personIdKey);
      } else {
        next.add(personIdKey);
      }
      return next;
    });
  };

  const handleSelectAll = () => {
    if (!eligiblePeople) return;
    setSelectedPeople(new Set(eligiblePeople.map((p) => p.personIdKey)));
  };

  const handleDeselectAll = () => {
    setSelectedPeople(new Set());
  };

  const handleGenerateSelected = async () => {
    if (selectedPeople.size === 0) {
      toast.error('No people selected', 'Please select at least one person');
      return;
    }

    const peopleArray = Array.from(selectedPeople);
    let successCount = 0;
    let errorCount = 0;

    for (const personIdKey of peopleArray) {
      try {
        await generateStatement.mutateAsync({
          personIdKey,
          startDate,
          endDate,
        });
        successCount++;
      } catch (error) {
        errorCount++;
      }
    }

    if (successCount > 0) {
      if (errorCount > 0) {
        toast.success(
          `Generated ${successCount} statement${successCount !== 1 ? 's' : ''}`,
          `${errorCount} failed to generate`
        );
      } else {
        toast.success(
          `Generated ${successCount} statement${successCount !== 1 ? 's' : ''}`,
          'Statements are ready to download'
        );
      }
      refetchStatements();
      setSelectedPeople(new Set());
    } else {
      toast.error('Statement generation failed', 'Please try again');
    }
  };

  const handleGenerateAll = async () => {
    if (!eligiblePeople || eligiblePeople.length === 0) {
      toast.error('No eligible people', 'Please find eligible people first');
      return;
    }

    const confirmed = window.confirm(
      `Generate statements for all ${eligiblePeople.length} eligible people?`
    );

    if (!confirmed) return;

    let successCount = 0;
    let errorCount = 0;

    for (const person of eligiblePeople) {
      try {
        await generateStatement.mutateAsync({
          personIdKey: person.personIdKey,
          startDate,
          endDate,
        });
        successCount++;
      } catch (error) {
        errorCount++;
      }
    }

    if (successCount > 0) {
      if (errorCount > 0) {
        toast.success(
          `Generated ${successCount} statement${successCount !== 1 ? 's' : ''}`,
          `${errorCount} failed to generate`
        );
      } else {
        toast.success(
          `Generated ${successCount} statement${successCount !== 1 ? 's' : ''}`,
          'Statements are ready to download'
        );
      }
      refetchStatements();
      setSelectedPeople(new Set());
    } else {
      toast.error('Statement generation failed', 'Please try again');
    }
  };

  const handleDownloadPdf = async (idKey: string) => {
    try {
      await downloadPdf.mutateAsync(idKey);
      toast.success('PDF downloaded', 'Statement has been downloaded');
    } catch (error) {
      toast.error('Download failed', 'Please try again');
    }
  };

  const handlePageSizeChange = (newSize: number) => {
    setPageSize(newSize);
    setPage(1);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Contribution Statements</h1>
        <p className="mt-2 text-gray-600">Generate tax-deductible giving statements for contributors</p>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Find Eligible Contributors</h2>
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div>
            <label htmlFor="startDate" className="block text-sm font-medium text-gray-700 mb-1">
              Start Date
            </label>
            <input
              type="date"
              id="startDate"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>

          <div>
            <label htmlFor="endDate" className="block text-sm font-medium text-gray-700 mb-1">
              End Date
            </label>
            <input
              type="date"
              id="endDate"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>

          <div>
            <label htmlFor="minimumAmount" className="block text-sm font-medium text-gray-700 mb-1">
              Minimum Amount
            </label>
            <input
              type="number"
              id="minimumAmount"
              value={minimumAmount}
              onChange={(e) => setMinimumAmount(e.target.value)}
              placeholder="Optional"
              step="0.01"
              min="0"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>

          <div className="flex items-end">
            <button
              onClick={handleFindEligible}
              disabled={!startDate || !endDate || eligibleLoading}
              className="w-full px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {eligibleLoading ? 'Finding...' : 'Find Eligible'}
            </button>
          </div>
        </div>
      </div>

      {/* Eligible People */}
      {showEligible && (
        <div className="bg-white rounded-lg border border-gray-200">
          <div className="p-4 border-b border-gray-200 flex items-center justify-between">
            <h2 className="text-lg font-semibold text-gray-900">
              Eligible Contributors
              {eligiblePeople && ` (${eligiblePeople.length})`}
            </h2>
            {eligiblePeople && eligiblePeople.length > 0 && (
              <div className="flex items-center gap-2">
                <button
                  onClick={handleSelectAll}
                  className="px-3 py-1 text-sm text-primary-600 hover:text-primary-700"
                >
                  Select All
                </button>
                <button
                  onClick={handleDeselectAll}
                  className="px-3 py-1 text-sm text-gray-600 hover:text-gray-700"
                >
                  Deselect All
                </button>
                <button
                  onClick={handleGenerateSelected}
                  disabled={selectedPeople.size === 0 || generateStatement.isPending}
                  className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Generate Selected ({selectedPeople.size})
                </button>
                <button
                  onClick={handleGenerateAll}
                  disabled={generateStatement.isPending}
                  className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Generate All
                </button>
              </div>
            )}
          </div>

          {eligibleLoading && (
            <div className="p-8">
              <Loading text="Finding eligible contributors..." />
            </div>
          )}

          {eligibleError && (
            <div className="p-8">
              <ErrorState
                title="Failed to load eligible people"
                message={eligibleError instanceof Error ? eligibleError.message : 'Unknown error'}
                onRetry={() => refetchEligible()}
              />
            </div>
          )}

          {!eligibleLoading && !eligibleError && eligiblePeople && (
            <>
              {eligiblePeople.length === 0 ? (
                <div className="p-8">
                  <EmptyState
                    icon={
                      <svg
                        className="w-12 h-12 text-gray-400"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                        aria-hidden="true"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                        />
                      </svg>
                    }
                    title="No eligible contributors"
                    description="No one made contributions during this period"
                  />
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-6 py-3 text-left">
                          <input
                            type="checkbox"
                            checked={eligiblePeople.length > 0 && selectedPeople.size === eligiblePeople.length}
                            onChange={(e) => e.target.checked ? handleSelectAll() : handleDeselectAll()}
                            className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                          />
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Name
                        </th>
                        <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Total Amount
                        </th>
                        <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Contributions
                        </th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      {eligiblePeople.map((person) => (
                        <tr key={person.personIdKey} className="hover:bg-gray-50">
                          <td className="px-6 py-4">
                            <input
                              type="checkbox"
                              checked={selectedPeople.has(person.personIdKey)}
                              onChange={() => handleTogglePerson(person.personIdKey)}
                              className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                            />
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                            {person.personName}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">
                            ${person.totalAmount.toFixed(2)}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">
                            {person.contributionCount}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </>
          )}
        </div>
      )}

      {/* Previously Generated Statements */}
      <div className="bg-white rounded-lg border border-gray-200">
        <div className="p-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900">Generated Statements</h2>
        </div>

        {statementsLoading && (
          <div className="p-8">
            <Loading text="Loading statements..." />
          </div>
        )}

        {statementsError && (
          <div className="p-8">
            <ErrorState
              title="Failed to load statements"
              message={statementsError instanceof Error ? statementsError.message : 'Unknown error'}
              onRetry={() => refetchStatements()}
            />
          </div>
        )}

        {!statementsLoading && !statementsError && (
          <>
            {statements.length === 0 ? (
              <div className="p-8">
                <EmptyState
                  icon={
                    <svg
                      className="w-12 h-12 text-gray-400"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                      aria-hidden="true"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                      />
                    </svg>
                  }
                  title="No statements yet"
                  description="Generated statements will appear here"
                />
              </div>
            ) : (
              <>
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Person
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Date Range
                        </th>
                        <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Total Amount
                        </th>
                        <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Contributions
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Generated
                        </th>
                        <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Actions
                        </th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      {statements.map((statement) => (
                        <tr key={statement.idKey} className="hover:bg-gray-50">
                          <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                            {statement.personName}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                            {new Date(statement.startDate).toLocaleDateString()} -{' '}
                            {new Date(statement.endDate).toLocaleDateString()}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">
                            ${statement.totalAmount.toFixed(2)}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">
                            {statement.contributionCount}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                            {new Date(statement.generatedDateTime).toLocaleDateString()}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                            <button
                              onClick={() => handleDownloadPdf(statement.idKey)}
                              disabled={downloadPdf.isPending}
                              className="text-primary-600 hover:text-primary-700 disabled:opacity-50"
                            >
                              Download PDF
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Pagination */}
                {meta && meta.totalPages > 1 && (
                  <div className="px-4 py-3 border-t border-gray-200 flex items-center justify-between">
                    <div className="flex items-center gap-2 text-sm text-gray-700">
                      <span>Show</span>
                      <select
                        value={pageSize}
                        onChange={(e) => handlePageSizeChange(Number(e.target.value))}
                        className="px-2 py-1 border border-gray-300 rounded focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                      >
                        <option value={25}>25</option>
                        <option value={50}>50</option>
                        <option value={100}>100</option>
                      </select>
                      <span>of {meta.totalCount} total</span>
                    </div>

                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => setPage((p) => Math.max(1, p - 1))}
                        disabled={page === 1}
                        className="px-3 py-1 border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        Previous
                      </button>
                      <span className="text-sm text-gray-700">
                        Page {page} of {meta.totalPages}
                      </span>
                      <button
                        onClick={() => setPage((p) => Math.min(meta.totalPages, p + 1))}
                        disabled={page === meta.totalPages}
                        className="px-3 py-1 border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        Next
                      </button>
                    </div>
                  </div>
                )}
              </>
            )}
          </>
        )}
      </div>
    </div>
  );
}
