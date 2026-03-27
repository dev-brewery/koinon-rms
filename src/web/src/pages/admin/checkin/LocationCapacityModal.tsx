/**
 * Location Capacity Modal
 * Edit soft/hard capacity thresholds for a location
 */

import { useState, useEffect } from 'react';
import { useLocation, useUpdateLocation } from '@/hooks/useLocations';
import { useToast } from '@/contexts/ToastContext';
import { Loading } from '@/components/ui';

interface LocationCapacityModalProps {
  locationIdKey: string;
  locationName: string;
  onClose: () => void;
}

export function LocationCapacityModal({
  locationIdKey,
  locationName,
  onClose,
}: LocationCapacityModalProps) {
  const toast = useToast();

  const { data: location, isLoading } = useLocation(locationIdKey);
  const updateLocation = useUpdateLocation();

  const [softThreshold, setSoftThreshold] = useState('');
  const [firmThreshold, setFirmThreshold] = useState('');
  const [overflowLocationIdKey, setOverflowLocationIdKey] = useState('');
  const [autoAssignOverflow, setAutoAssignOverflow] = useState(false);

  useEffect(() => {
    if (location) {
      setSoftThreshold(location.softRoomThreshold != null ? String(location.softRoomThreshold) : '');
      setFirmThreshold(location.firmRoomThreshold != null ? String(location.firmRoomThreshold) : '');
      setOverflowLocationIdKey(location.overflowLocationIdKey ?? '');
      setAutoAssignOverflow(location.autoAssignOverflow);
    }
  }, [location]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    try {
      await updateLocation.mutateAsync({
        idKey: locationIdKey,
        request: {
          softRoomThreshold: softThreshold ? Number(softThreshold) : undefined,
          firmRoomThreshold: firmThreshold ? Number(firmThreshold) : undefined,
          overflowLocationIdKey: overflowLocationIdKey || undefined,
          autoAssignOverflow,
        },
      });
      toast.success('Capacity updated', `Capacity settings for "${locationName}" have been saved.`);
      onClose();
    } catch {
      toast.error('Update failed', 'Could not save capacity settings. Please try again.');
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto" role="dialog" aria-modal="true" aria-labelledby="capacity-modal-title">
      <div className="fixed inset-0 bg-black bg-opacity-40" onClick={onClose} />

      <div className="relative min-h-screen flex items-center justify-center p-4">
        <div className="relative bg-white rounded-xl shadow-xl w-full max-w-md">
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
            <div>
              <h2 id="capacity-modal-title" className="text-lg font-semibold text-gray-900">
                Edit Capacity
              </h2>
              <p className="text-sm text-gray-500">{locationName}</p>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="p-1 text-gray-400 hover:text-gray-600 rounded-md"
              aria-label="Close"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          {isLoading ? (
            <div className="px-6 py-8">
              <Loading text="Loading location details..." />
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="px-6 py-5 space-y-5">
              <div className="grid grid-cols-2 gap-4">
                {/* Soft Threshold */}
                <div>
                  <label htmlFor="soft-threshold" className="block text-sm font-medium text-gray-700 mb-1">
                    Soft Threshold
                  </label>
                  <input
                    id="soft-threshold"
                    type="number"
                    min="0"
                    value={softThreshold}
                    onChange={(e) => setSoftThreshold(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm"
                    placeholder="None"
                  />
                  <p className="mt-1 text-xs text-gray-500">Warning threshold</p>
                </div>

                {/* Firm Threshold */}
                <div>
                  <label htmlFor="firm-threshold" className="block text-sm font-medium text-gray-700 mb-1">
                    Firm Threshold
                  </label>
                  <input
                    id="firm-threshold"
                    type="number"
                    min="0"
                    value={firmThreshold}
                    onChange={(e) => setFirmThreshold(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm"
                    placeholder="None"
                  />
                  <p className="mt-1 text-xs text-gray-500">Hard limit</p>
                </div>
              </div>

              {/* Auto-assign Overflow */}
              <div className="flex items-center gap-3">
                <button
                  type="button"
                  role="switch"
                  aria-checked={autoAssignOverflow}
                  onClick={() => setAutoAssignOverflow((v) => !v)}
                  className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 ${
                    autoAssignOverflow ? 'bg-primary-600' : 'bg-gray-200'
                  }`}
                >
                  <span
                    className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${
                      autoAssignOverflow ? 'translate-x-6' : 'translate-x-1'
                    }`}
                  />
                </button>
                <span className="text-sm font-medium text-gray-700">Auto-assign overflow</span>
              </div>

              {/* Footer */}
              <div className="flex items-center justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={onClose}
                  disabled={updateLocation.isPending}
                  className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors disabled:opacity-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={updateLocation.isPending}
                  className="px-4 py-2 text-sm font-medium text-white bg-primary-600 rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {updateLocation.isPending ? 'Saving...' : 'Save Changes'}
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
