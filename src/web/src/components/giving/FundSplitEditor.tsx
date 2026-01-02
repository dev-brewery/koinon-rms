/**
 * FundSplitEditor Component
 * Allows editing multiple fund splits for a contribution
 */

import { useState, useEffect } from 'react';
import { ContributionDetailRequest, FundDto } from '@/types/giving';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { cn } from '@/lib/utils';

export interface FundSplitEditorProps {
  value: ContributionDetailRequest[];
  onChange: (details: ContributionDetailRequest[]) => void;
  funds: FundDto[];
  disabled?: boolean;
}

export function FundSplitEditor({
  value,
  onChange,
  funds,
  disabled = false
}: FundSplitEditorProps) {
  const [splits, setSplits] = useState<ContributionDetailRequest[]>(value);

  // Initialize with one empty row if value is empty
  useEffect(() => {
    if (value.length === 0) {
      const emptyRow: ContributionDetailRequest = {
        fundIdKey: '',
        amount: 0,
        summary: ''
      };
      setSplits([emptyRow]);
      onChange([emptyRow]);
    } else {
      setSplits(value);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [value]);

  const handleFieldChange = (
    index: number,
    field: keyof ContributionDetailRequest,
    fieldValue: string | number
  ) => {
    const updatedSplits = splits.map((split, i) => {
      if (i === index) {
        return { ...split, [field]: fieldValue };
      }
      return split;
    });
    setSplits(updatedSplits);
    onChange(updatedSplits);
  };

  const handleAddRow = () => {
    const newRow: ContributionDetailRequest = {
      fundIdKey: '',
      amount: 0,
      summary: ''
    };
    const updatedSplits = [...splits, newRow];
    setSplits(updatedSplits);
    onChange(updatedSplits);
  };

  const handleRemoveRow = (index: number) => {
    if (splits.length <= 1) return;
    const updatedSplits = splits.filter((_, i) => i !== index);
    setSplits(updatedSplits);
    onChange(updatedSplits);
  };

  const totalAmount = splits.reduce((sum, split) => sum + (Number(split.amount) || 0), 0);

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  };

  return (
    <div className="space-y-4">
      {/* Fund split rows */}
      <div className="space-y-3">
        {splits.map((split, index) => (
          <div
            key={index}
            className="grid grid-cols-1 md:grid-cols-12 gap-3 p-4 border border-gray-200 rounded-lg bg-gray-50"
          >
            {/* Fund dropdown */}
            <div className="md:col-span-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Fund {splits.length > 1 ? `#${index + 1}` : ''}
              </label>
              <select
                value={split.fundIdKey}
                onChange={(e) => handleFieldChange(index, 'fundIdKey', e.target.value)}
                disabled={disabled}
                className={cn(
                  'w-full px-4 py-3 text-base border rounded-lg',
                  'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent',
                  'disabled:bg-gray-100 disabled:cursor-not-allowed',
                  'min-h-[48px] border-gray-300'
                )}
                aria-label={`Fund selection ${index + 1}`}
              >
                <option value="">Select Fund...</option>
                {funds
                  .filter(fund => fund.isActive)
                  .map(fund => (
                    <option key={fund.idKey} value={fund.idKey}>
                      {fund.publicName || fund.name}
                    </option>
                  ))}
              </select>
            </div>

            {/* Amount input */}
            <div className="md:col-span-3">
              <Input
                type="number"
                step="0.01"
                min="0"
                label="Amount"
                value={split.amount}
                onChange={(e) => handleFieldChange(index, 'amount', parseFloat(e.target.value) || 0)}
                disabled={disabled}
                aria-label={`Amount for split ${index + 1}`}
              />
            </div>

            {/* Summary input */}
            <div className="md:col-span-4">
              <Input
                type="text"
                label="Note (optional)"
                value={split.summary || ''}
                onChange={(e) => handleFieldChange(index, 'summary', e.target.value)}
                disabled={disabled}
                placeholder="e.g., Memorial gift"
                aria-label={`Note for split ${index + 1}`}
              />
            </div>

            {/* Remove button */}
            <div className="md:col-span-1 flex items-end">
              {!disabled && splits.length > 1 && (
                <button
                  type="button"
                  onClick={() => handleRemoveRow(index)}
                  className="w-full md:w-auto px-3 py-3 text-red-600 hover:bg-red-50 rounded-lg transition-colors focus:outline-none focus:ring-2 focus:ring-red-500 min-h-[48px]"
                  aria-label={`Remove split ${index + 1}`}
                >
                  <svg
                    className="w-5 h-5 mx-auto"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M6 18L18 6M6 6l12 12"
                    />
                  </svg>
                </button>
              )}
            </div>
          </div>
        ))}
      </div>

      {/* Add Fund button */}
      {!disabled && (
        <Button
          type="button"
          variant="outline"
          onClick={handleAddRow}
          className="w-full md:w-auto"
        >
          <svg
            className="w-5 h-5 mr-2 inline-block"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 4v16m8-8H4"
            />
          </svg>
          Add Fund
        </Button>
      )}

      {/* Total amount display */}
      <div className="flex justify-end pt-4 border-t border-gray-200">
        <div className="text-right">
          <div className="text-sm text-gray-600 mb-1">Total Amount</div>
          <div className="text-2xl font-bold text-gray-900">
            {formatCurrency(totalAmount)}
          </div>
        </div>
      </div>
    </div>
  );
}
