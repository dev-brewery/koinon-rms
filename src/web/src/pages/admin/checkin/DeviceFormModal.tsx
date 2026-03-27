/**
 * Device Form Modal
 * Create or edit a kiosk device
 */

import { useState, useEffect } from 'react';
import { useDevice, useCreateDevice, useUpdateDevice } from '@/hooks/useDevices';
import { useToast } from '@/contexts/ToastContext';
import { Loading } from '@/components/ui';

interface DeviceFormModalProps {
  deviceIdKey: string | null;
  onClose: () => void;
}

export function DeviceFormModal({ deviceIdKey, onClose }: DeviceFormModalProps) {
  const toast = useToast();
  const isEditing = !!deviceIdKey;

  const { data: device, isLoading } = useDevice(deviceIdKey ?? undefined);
  const createDevice = useCreateDevice();
  const updateDevice = useUpdateDevice();

  const [name, setName] = useState('');
  const [ipAddress, setIpAddress] = useState('');
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    if (device) {
      setName(device.name);
      setIpAddress(device.ipAddress ?? '');
      setIsActive(device.isActive);
    } else if (!deviceIdKey) {
      setName('');
      setIpAddress('');
      setIsActive(true);
    }
  }, [device, deviceIdKey]);

  const isPending = createDevice.isPending || updateDevice.isPending;

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    if (!name.trim()) return;

    try {
      if (isEditing && deviceIdKey) {
        await updateDevice.mutateAsync({
          idKey: deviceIdKey,
          request: {
            name: name.trim(),
            ipAddress: ipAddress.trim() || undefined,
            isActive,
          },
        });
        toast.success('Device updated', `"${name.trim()}" has been updated.`);
      } else {
        await createDevice.mutateAsync({
          name: name.trim(),
          ipAddress: ipAddress.trim() || undefined,
        });
        toast.success('Device created', `"${name.trim()}" has been registered.`);
      }
      onClose();
    } catch {
      toast.error(
        isEditing ? 'Update failed' : 'Create failed',
        'Could not save the device. Please try again.'
      );
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto" role="dialog" aria-modal="true" aria-labelledby="device-modal-title">
      <div className="fixed inset-0 bg-black bg-opacity-40" onClick={onClose} />

      <div className="relative min-h-screen flex items-center justify-center p-4">
        <div className="relative bg-white rounded-xl shadow-xl w-full max-w-md">
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
            <h2 id="device-modal-title" className="text-lg font-semibold text-gray-900">
              {isEditing ? 'Edit Device' : 'Add Device'}
            </h2>
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

          {isLoading && isEditing ? (
            <div className="px-6 py-8">
              <Loading text="Loading device details..." />
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="px-6 py-5 space-y-5">
              {/* Name */}
              <div>
                <label htmlFor="device-name" className="block text-sm font-medium text-gray-700 mb-1">
                  Device Name <span className="text-red-500">*</span>
                </label>
                <input
                  id="device-name"
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  required
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm"
                  placeholder="e.g. Main Lobby Kiosk"
                />
              </div>

              {/* IP Address */}
              <div>
                <label htmlFor="device-ip" className="block text-sm font-medium text-gray-700 mb-1">
                  IP Address
                </label>
                <input
                  id="device-ip"
                  type="text"
                  value={ipAddress}
                  onChange={(e) => setIpAddress(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm font-mono"
                  placeholder="e.g. 192.168.1.100"
                />
              </div>

              {/* Active Toggle (edit only) */}
              {isEditing && (
                <div className="flex items-center gap-3">
                  <button
                    type="button"
                    role="switch"
                    aria-checked={isActive}
                    onClick={() => setIsActive((v) => !v)}
                    className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 ${
                      isActive ? 'bg-primary-600' : 'bg-gray-200'
                    }`}
                  >
                    <span
                      className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${
                        isActive ? 'translate-x-6' : 'translate-x-1'
                      }`}
                    />
                  </button>
                  <span className="text-sm font-medium text-gray-700">Active</span>
                </div>
              )}

              {/* Footer */}
              <div className="flex items-center justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={onClose}
                  disabled={isPending}
                  className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors disabled:opacity-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isPending || !name.trim()}
                  className="px-4 py-2 text-sm font-medium text-white bg-primary-600 rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {isPending ? 'Saving...' : isEditing ? 'Save Changes' : 'Add Device'}
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
