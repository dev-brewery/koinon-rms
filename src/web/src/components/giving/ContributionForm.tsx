/**
 * Contribution Form Modal Component
 * Modal for adding or editing financial contributions
 */

import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { PersonLookup } from './PersonLookup';
import { FundSplitEditor } from './FundSplitEditor';
import type { PersonSummaryDto, DefinedValueDto } from '@/services/api/types';
import type {
  AddContributionRequest,
  UpdateContributionRequest,
  ContributionDto,
  FundDto,
  ContributionDetailRequest,
} from '@/types/giving';
import { cn } from '@/lib/utils';

export interface ContributionFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: AddContributionRequest | UpdateContributionRequest) => void;
  funds: FundDto[];
  transactionTypes: DefinedValueDto[];
  contribution?: ContributionDto;
  isSubmitting?: boolean;
}

export function ContributionForm({
  isOpen,
  onClose,
  onSubmit,
  funds,
  transactionTypes,
  contribution,
  isSubmitting = false,
}: ContributionFormProps) {
  // Form state
  const [selectedPerson, setSelectedPerson] = useState<PersonSummaryDto | null>(null);
  const [transactionDateTime, setTransactionDateTime] = useState('');
  const [transactionTypeValueIdKey, setTransactionTypeValueIdKey] = useState('');
  const [transactionCode, setTransactionCode] = useState('');
  const [summary, setSummary] = useState('');
  const [fundSplits, setFundSplits] = useState<ContributionDetailRequest[]>([]);
  const [validationError, setValidationError] = useState('');

  // Initialize form when contribution prop changes (edit mode)
  useEffect(() => {
    if (contribution) {
      // Pre-fill person (create pseudo-PersonSummaryDto from contribution data)
      if (contribution.personIdKey && contribution.personName) {
        setSelectedPerson({
          idKey: contribution.personIdKey,
          fullName: contribution.personName,
          email: undefined,
          photoUrl: undefined,
        } as PersonSummaryDto);
      } else {
        setSelectedPerson(null);
      }

      // Pre-fill transaction details
      setTransactionDateTime(formatDateTimeForInput(contribution.transactionDateTime));
      setTransactionTypeValueIdKey(contribution.transactionTypeValueIdKey);
      setTransactionCode(contribution.transactionCode || '');
      setSummary(contribution.summary || '');

      // Map contribution details to fund splits
      const splits: ContributionDetailRequest[] = contribution.details.map((detail) => ({
        fundIdKey: detail.fundIdKey,
        amount: detail.amount,
        summary: detail.summary || '',
      }));
      setFundSplits(splits);
    } else {
      // Reset form for add mode
      resetForm();
    }
  }, [contribution]);

  // Reset form when modal closes
  useEffect(() => {
    if (!isOpen) {
      resetForm();
      setValidationError('');
    }
  }, [isOpen]);

  const resetForm = () => {
    setSelectedPerson(null);
    setTransactionDateTime('');
    setTransactionTypeValueIdKey('');
    setTransactionCode('');
    setSummary('');
    setFundSplits([]);
  };

  // Convert ISO datetime string to input format (yyyy-MM-ddTHH:mm)
  const formatDateTimeForInput = (isoDateTime: string): string => {
    const date = new Date(isoDateTime);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  };

  const validateForm = (): boolean => {
    // Transaction date is required
    if (!transactionDateTime) {
      setValidationError('Transaction date is required');
      return false;
    }

    // Transaction type is required
    if (!transactionTypeValueIdKey) {
      setValidationError('Transaction type is required');
      return false;
    }

    // At least one fund with amount > 0 is required
    const validFunds = fundSplits.filter(
      (split) => split.fundIdKey && split.amount > 0
    );
    if (validFunds.length === 0) {
      setValidationError('At least one fund with amount greater than $0.00 is required');
      return false;
    }

    setValidationError('');
    return true;
  };

  const handleSubmit = () => {
    if (!validateForm()) {
      return;
    }

    // Filter out empty fund splits
    const validFundSplits = fundSplits.filter(
      (split) => split.fundIdKey && split.amount > 0
    );

    const formData: AddContributionRequest | UpdateContributionRequest = {
      personIdKey: selectedPerson?.idKey,
      transactionDateTime,
      transactionCode: transactionCode || undefined,
      transactionTypeValueIdKey,
      details: validFundSplits,
      summary: summary || undefined,
    };

    onSubmit(formData);
  };

  const handleClose = () => {
    if (!isSubmitting) {
      onClose();
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:p-0">
        {/* Backdrop */}
        <div
          className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
          onClick={handleClose}
        />

        {/* Modal */}
        <div className="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-4xl sm:w-full">
          <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div className="flex items-start justify-between mb-4">
              <h3 className="text-lg font-medium text-gray-900">
                {contribution ? 'Edit Contribution' : 'Add Contribution'}
              </h3>
              <button
                onClick={handleClose}
                disabled={isSubmitting}
                className="text-gray-400 hover:text-gray-500 disabled:cursor-not-allowed"
                aria-label="Close"
              >
                <svg
                  className="w-6 h-6"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>

            {/* Validation Error */}
            {validationError && (
              <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
                <p className="text-sm text-red-800">{validationError}</p>
              </div>
            )}

            {/* Form Fields */}
            <div className="space-y-6">
              {/* Person Lookup */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Contributor (optional)
                </label>
                <PersonLookup
                  value={selectedPerson}
                  onChange={setSelectedPerson}
                  disabled={isSubmitting}
                />
              </div>

              {/* Transaction Date and Type Row */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Transaction Date */}
                <div>
                  <Input
                    type="datetime-local"
                    label="Transaction Date"
                    value={transactionDateTime}
                    onChange={(e) => setTransactionDateTime(e.target.value)}
                    disabled={isSubmitting}
                    required
                  />
                </div>

                {/* Transaction Type */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Transaction Type <span className="text-red-500">*</span>
                  </label>
                  <select
                    value={transactionTypeValueIdKey}
                    onChange={(e) => setTransactionTypeValueIdKey(e.target.value)}
                    disabled={isSubmitting}
                    required
                    className={cn(
                      'w-full px-4 py-3 text-base border rounded-lg',
                      'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent',
                      'disabled:bg-gray-100 disabled:cursor-not-allowed',
                      'min-h-[48px] border-gray-300'
                    )}
                  >
                    <option value="">Select Type...</option>
                    {transactionTypes.map((type) => (
                      <option key={type.idKey} value={type.idKey}>
                        {type.value}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              {/* Transaction Code */}
              <div>
                <Input
                  type="text"
                  label="Transaction Code (optional)"
                  placeholder="e.g., Check #1234"
                  value={transactionCode}
                  onChange={(e) => setTransactionCode(e.target.value)}
                  disabled={isSubmitting}
                />
              </div>

              {/* Summary */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Summary (optional)
                </label>
                <textarea
                  placeholder="Add notes about this contribution..."
                  value={summary}
                  onChange={(e) => setSummary(e.target.value)}
                  disabled={isSubmitting}
                  rows={3}
                  className={cn(
                    'w-full px-4 py-3 text-base border rounded-lg',
                    'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent',
                    'disabled:bg-gray-100 disabled:cursor-not-allowed',
                    'border-gray-300 resize-vertical'
                  )}
                />
              </div>

              {/* Fund Splits */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Fund Allocations <span className="text-red-500">*</span>
                </label>
                <FundSplitEditor
                  value={fundSplits}
                  onChange={setFundSplits}
                  funds={funds}
                  disabled={isSubmitting}
                />
              </div>
            </div>
          </div>

          {/* Actions */}
          <div className="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse gap-3">
            <Button
              onClick={handleSubmit}
              disabled={isSubmitting}
              loading={isSubmitting}
              className="w-full sm:w-auto"
            >
              {contribution ? 'Update Contribution' : 'Add Contribution'}
            </Button>
            <Button
              variant="outline"
              onClick={handleClose}
              disabled={isSubmitting}
              className="w-full sm:w-auto mt-3 sm:mt-0"
            >
              Cancel
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
