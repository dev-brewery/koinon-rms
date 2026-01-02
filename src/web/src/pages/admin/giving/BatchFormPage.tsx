/**
 * Batch Form Page
 * Create a new financial batch
 */

import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate, Link } from 'react-router-dom';
import { batchFormSchema, type BatchFormData } from '@/schemas/batch.schema';
import { useCreateBatch, useCampuses } from '@/hooks/useGiving';
import { useToast } from '@/contexts/ToastContext';
import type { CreateBatchRequest } from '@/types/giving';

export function BatchFormPage() {
  const navigate = useNavigate();
  const { success, error: showError } = useToast();

  const { data: campuses, isLoading: isCampusesLoading } = useCampuses();
  const createBatch = useCreateBatch();

  const {
    control,
    handleSubmit,
    register,
    formState: { errors, isSubmitting },
  } = useForm<BatchFormData>({
    resolver: zodResolver(batchFormSchema),
    defaultValues: {
      name: '',
      batchDate: new Date().toISOString().split('T')[0],
      controlAmount: undefined,
      controlItemCount: undefined,
      campusIdKey: '',
      note: '',
    },
  });

  const onSubmit = async (data: BatchFormData) => {
    try {
      // Map form data to API request
      const request: CreateBatchRequest = {
        name: data.name,
        batchDate: data.batchDate,
        controlAmount: data.controlAmount,
        controlItemCount: data.controlItemCount,
        campusIdKey: data.campusIdKey || undefined,
        note: data.note || undefined,
      };

      const response = await createBatch.mutateAsync(request);
      success('Success', 'Batch created successfully');
      navigate(`/admin/giving/${response.idKey}`);
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to create batch';
      showError('Error', errorMessage);
    }
  };

  const isPending = isSubmitting || createBatch.isPending;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link
          to="/admin/giving"
          className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100 transition-colors"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
        </Link>
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Create Batch</h1>
          <p className="mt-1 text-gray-600">Create a new financial batch for contributions</p>
        </div>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main Form */}
          <div className="lg:col-span-2 space-y-6">
            {/* Basic Info */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Basic Information</h2>

              <div className="space-y-4">
                {/* Name */}
                <div>
                  <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
                    Name <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    id="name"
                    {...register('name')}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                    placeholder="e.g., Sunday Morning Offering - 01/02/2026"
                  />
                  {errors.name && (
                    <p className="text-sm text-red-600 mt-1">{errors.name.message}</p>
                  )}
                </div>

                {/* Batch Date */}
                <div>
                  <label htmlFor="batchDate" className="block text-sm font-medium text-gray-700 mb-1">
                    Batch Date <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="date"
                    id="batchDate"
                    {...register('batchDate')}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  />
                  {errors.batchDate && (
                    <p className="text-sm text-red-600 mt-1">{errors.batchDate.message}</p>
                  )}
                  <p className="mt-1 text-xs text-gray-500">
                    The date this batch was collected (cannot be in the future)
                  </p>
                </div>

                {/* Campus */}
                <div>
                  <label htmlFor="campusIdKey" className="block text-sm font-medium text-gray-700 mb-1">
                    Campus
                  </label>
                  <Controller
                    name="campusIdKey"
                    control={control}
                    render={({ field }) => (
                      <select
                        {...field}
                        id="campusIdKey"
                        disabled={isCampusesLoading}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        <option value="">All Campuses</option>
                        {campuses?.map((campus) => (
                          <option key={campus.idKey} value={campus.idKey}>
                            {campus.name}
                          </option>
                        ))}
                      </select>
                    )}
                  />
                  {errors.campusIdKey && (
                    <p className="text-sm text-red-600 mt-1">{errors.campusIdKey.message}</p>
                  )}
                </div>
              </div>
            </div>

            {/* Control Amounts */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Control Amounts</h2>
              <p className="text-sm text-gray-600 mb-4">
                Optional control amounts for batch reconciliation
              </p>

              <div className="grid grid-cols-2 gap-4">
                {/* Control Amount */}
                <div>
                  <label htmlFor="controlAmount" className="block text-sm font-medium text-gray-700 mb-1">
                    Control Amount
                  </label>
                  <div className="relative">
                    <span className="absolute left-3 top-2 text-gray-500">$</span>
                    <input
                      type="number"
                      id="controlAmount"
                      step="0.01"
                      min="0"
                      {...register('controlAmount', {
                        setValueAs: (value) => (value === '' ? undefined : parseFloat(value)),
                      })}
                      className="w-full pl-7 pr-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                      placeholder="0.00"
                    />
                  </div>
                  {errors.controlAmount && (
                    <p className="text-sm text-red-600 mt-1">{errors.controlAmount.message}</p>
                  )}
                  <p className="mt-1 text-xs text-gray-500">
                    Expected total amount
                  </p>
                </div>

                {/* Control Item Count */}
                <div>
                  <label htmlFor="controlItemCount" className="block text-sm font-medium text-gray-700 mb-1">
                    Control Item Count
                  </label>
                  <input
                    type="number"
                    id="controlItemCount"
                    min="0"
                    {...register('controlItemCount', {
                      setValueAs: (value) => (value === '' ? undefined : parseInt(value, 10)),
                    })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                    placeholder="0"
                  />
                  {errors.controlItemCount && (
                    <p className="text-sm text-red-600 mt-1">{errors.controlItemCount.message}</p>
                  )}
                  <p className="mt-1 text-xs text-gray-500">
                    Expected number of contributions
                  </p>
                </div>
              </div>
            </div>

            {/* Notes */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Notes</h2>

              <div>
                <label htmlFor="note" className="block text-sm font-medium text-gray-700 mb-1">
                  Internal Notes
                </label>
                <textarea
                  id="note"
                  {...register('note')}
                  rows={4}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  placeholder="Optional notes about this batch"
                />
                {errors.note && (
                  <p className="text-sm text-red-600 mt-1">{errors.note.message}</p>
                )}
              </div>
            </div>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Actions */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <div className="space-y-3">
                <button
                  type="submit"
                  disabled={isPending}
                  className="w-full px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {isPending ? 'Creating...' : 'Create Batch'}
                </button>

                <Link
                  to="/admin/giving"
                  className="block w-full px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors text-center"
                >
                  Cancel
                </Link>
              </div>
            </div>

            {/* Help Text */}
            <div className="bg-blue-50 rounded-lg border border-blue-200 p-4">
              <h3 className="text-sm font-semibold text-blue-900 mb-2">Next Steps</h3>
              <p className="text-sm text-blue-800">
                After creating the batch, you'll be able to add individual contributions and reconcile the totals.
              </p>
            </div>
          </div>
        </div>
      </form>
    </div>
  );
}
