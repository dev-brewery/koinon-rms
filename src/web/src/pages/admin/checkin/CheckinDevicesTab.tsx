/**
 * Check-in Devices Tab
 * Manage kiosk devices — list, create, edit, delete, generate token
 */

import { useState } from 'react';
import { useDevices, useDeleteDevice, useGenerateKioskToken } from '@/hooks/useDevices';
import { useToast } from '@/contexts/ToastContext';
import { Loading, EmptyState, ErrorState, ConfirmDialog } from '@/components/ui';
import type { DeviceSummaryDto } from '@/services/api/types';
import { DeviceFormModal } from './DeviceFormModal';

export function CheckinDevicesTab() {
  const toast = useToast();
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingDevice, setEditingDevice] = useState<DeviceSummaryDto | null>(null);
  const [deletingDeviceIdKey, setDeletingDeviceIdKey] = useState<string | null>(null);
  const [generatedToken, setGeneratedToken] = useState<string | null>(null);

  const { data, isLoading, error, refetch } = useDevices();
  const deleteDevice = useDeleteDevice();
  const generateToken = useGenerateKioskToken();

  const devices = data ?? [];

  const handleEdit = (device: DeviceSummaryDto) => {
    setEditingDevice(device);
    setIsFormOpen(true);
  };

  const handleCreate = () => {
    setEditingDevice(null);
    setIsFormOpen(true);
  };

  const handleFormClose = () => {
    setIsFormOpen(false);
    setEditingDevice(null);
  };

  const handleDeleteConfirm = async () => {
    if (!deletingDeviceIdKey) return;
    try {
      await deleteDevice.mutateAsync(deletingDeviceIdKey);
      toast.success('Device deleted', 'The kiosk device has been removed.');
    } catch {
      toast.error('Delete failed', 'Could not delete the device. Please try again.');
    } finally {
      setDeletingDeviceIdKey(null);
    }
  };

  const handleGenerateToken = async (device: DeviceSummaryDto) => {
    try {
      const result = await generateToken.mutateAsync(device.idKey);
      setGeneratedToken(result.token);
    } catch {
      toast.error('Token generation failed', 'Could not generate a kiosk token. Please try again.');
    }
  };

  if (isLoading) {
    return <Loading text="Loading devices..." />;
  }

  if (error) {
    return (
      <ErrorState
        title="Failed to load devices"
        message={error instanceof Error ? error.message : 'Unknown error'}
        onRetry={() => refetch()}
      />
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-gray-600">
          {devices.length} {devices.length === 1 ? 'device' : 'devices'} registered
        </p>
        <button
          onClick={handleCreate}
          className="px-4 py-2 bg-primary-600 text-white text-sm font-medium rounded-lg hover:bg-primary-700 transition-colors"
        >
          Add Device
        </button>
      </div>

      {devices.length === 0 ? (
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
                d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
              />
            </svg>
          }
          title="No devices registered"
          description="Add your first kiosk device to enable check-in"
          action={{ label: 'Add Device', onClick: handleCreate }}
        />
      ) : (
        <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Device
                </th>
                <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Type
                </th>
                <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Campus
                </th>
                <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  IP Address
                </th>
                <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Status
                </th>
                <th scope="col" className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-100">
              {devices.map((device) => (
                <DeviceRow
                  key={device.idKey}
                  device={device}
                  isGeneratingToken={generateToken.isPending && generateToken.variables === device.idKey}
                  onEdit={() => handleEdit(device)}
                  onDelete={() => setDeletingDeviceIdKey(device.idKey)}
                  onGenerateToken={() => handleGenerateToken(device)}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}

      {isFormOpen && (
        <DeviceFormModal
          deviceIdKey={editingDevice?.idKey ?? null}
          onClose={handleFormClose}
        />
      )}

      <ConfirmDialog
        isOpen={!!deletingDeviceIdKey}
        title="Delete Device"
        description="Are you sure you want to delete this kiosk device? Any active kiosk sessions using this device will stop working."
        confirmLabel="Delete"
        variant="danger"
        onConfirm={handleDeleteConfirm}
        onClose={() => setDeletingDeviceIdKey(null)}
      />

      {/* Token display dialog */}
      {generatedToken && (
        <div className="fixed inset-0 z-50 overflow-y-auto" role="dialog" aria-modal="true" aria-labelledby="token-dialog-title">
          <div className="fixed inset-0 bg-black bg-opacity-40" onClick={() => setGeneratedToken(null)} />
          <div className="relative min-h-screen flex items-center justify-center p-4">
            <div className="relative bg-white rounded-xl shadow-xl w-full max-w-md">
              <div className="px-6 py-4 border-b border-gray-200">
                <h2 id="token-dialog-title" className="text-lg font-semibold text-gray-900">
                  Kiosk Token Generated
                </h2>
              </div>
              <div className="px-6 py-5 space-y-4">
                <p className="text-sm text-gray-600">
                  Copy this token and enter it on the kiosk device to complete setup. The token will only be shown once.
                </p>
                <div className="bg-gray-50 rounded-lg p-4 font-mono text-sm text-gray-900 break-all border border-gray-200">
                  {generatedToken}
                </div>
                <div className="flex justify-end">
                  <button
                    type="button"
                    onClick={() => {
                      navigator.clipboard.writeText(generatedToken).catch(() => undefined);
                      toast.success('Copied', 'Token copied to clipboard.');
                    }}
                    className="mr-3 px-4 py-2 text-sm font-medium text-primary-600 border border-primary-300 rounded-lg hover:bg-primary-50 transition-colors"
                  >
                    Copy Token
                  </button>
                  <button
                    type="button"
                    onClick={() => setGeneratedToken(null)}
                    className="px-4 py-2 text-sm font-medium text-white bg-primary-600 rounded-lg hover:bg-primary-700 transition-colors"
                  >
                    Done
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ============================================================================
// Device Row
// ============================================================================

interface DeviceRowProps {
  device: DeviceSummaryDto;
  isGeneratingToken: boolean;
  onEdit: () => void;
  onDelete: () => void;
  onGenerateToken: () => void;
}

function DeviceRow({ device, isGeneratingToken, onEdit, onDelete, onGenerateToken }: DeviceRowProps) {
  return (
    <tr className="hover:bg-gray-50">
      <td className="px-6 py-4 whitespace-nowrap">
        <span className="text-sm font-medium text-gray-900">{device.name}</span>
      </td>
      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
        {device.deviceTypeName ?? <span className="text-gray-400">—</span>}
      </td>
      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
        {device.campusName ?? <span className="text-gray-400">—</span>}
      </td>
      <td className="px-6 py-4 whitespace-nowrap">
        {device.ipAddress ? (
          <span className="font-mono text-sm text-gray-700">{device.ipAddress}</span>
        ) : (
          <span className="text-gray-400 text-sm">—</span>
        )}
      </td>
      <td className="px-6 py-4 whitespace-nowrap">
        <span
          className={`inline-flex px-2 py-0.5 text-xs font-medium rounded-full ${
            device.isActive
              ? 'bg-green-100 text-green-800'
              : 'bg-gray-100 text-gray-600'
          }`}
        >
          {device.isActive ? 'Active' : 'Inactive'}
        </span>
      </td>
      <td className="px-6 py-4 whitespace-nowrap text-right">
        <div className="flex items-center justify-end gap-3">
          <button
            onClick={onGenerateToken}
            disabled={isGeneratingToken}
            className="text-sm font-medium text-gray-600 hover:text-gray-700 disabled:opacity-50"
          >
            {isGeneratingToken ? 'Generating...' : 'Token'}
          </button>
          <button
            onClick={onEdit}
            className="text-sm font-medium text-primary-600 hover:text-primary-700"
          >
            Edit
          </button>
          <button
            onClick={onDelete}
            className="text-sm font-medium text-red-600 hover:text-red-700"
          >
            Delete
          </button>
        </div>
      </td>
    </tr>
  );
}
