/**
 * SecuritySettingsSection
 * Password change and two-factor authentication management
 */

import { useState } from 'react';
import {
  useChangePassword,
  useTwoFactorStatus,
  useSetupTwoFactor,
  useVerifyTwoFactor,
  useDisableTwoFactor,
} from '@/hooks/useSettings';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Loading } from '@/components/ui/Loading';
import { ErrorState } from '@/components/ui/ErrorState';

export function SecuritySettingsSection() {
  const { data: twoFactorStatus, isLoading: isLoadingStatus, error: statusError } = useTwoFactorStatus();
  const changePassword = useChangePassword();
  const setupTwoFactor = useSetupTwoFactor();
  const verifyTwoFactor = useVerifyTwoFactor();
  const disableTwoFactor = useDisableTwoFactor();

  // Password change state
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordError, setPasswordError] = useState('');

  // 2FA state
  const [qrCodeUrl, setQrCodeUrl] = useState('');
  const [manualKey, setManualKey] = useState('');
  const [twoFactorCode, setTwoFactorCode] = useState('');
  const [showSetup, setShowSetup] = useState(false);

  const handlePasswordChange = async (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordError('');

    if (newPassword !== confirmPassword) {
      setPasswordError('New passwords do not match');
      return;
    }

    if (newPassword.length < 8) {
      setPasswordError('Password must be at least 8 characters');
      return;
    }

    try {
      await changePassword.mutateAsync({
        currentPassword,
        newPassword,
        confirmPassword,
      });
      // Reset form on success
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (error) {
      setPasswordError('Failed to change password. Please check your current password.');
    }
  };

  const handleSetupTwoFactor = async () => {
    try {
      const setup = await setupTwoFactor.mutateAsync();
      setQrCodeUrl(setup.qrCodeUri);
      setManualKey(setup.secretKey);
      setShowSetup(true);
    } catch (error) {
      // Error handled by mutation
    }
  };

  const handleVerifyTwoFactor = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      await verifyTwoFactor.mutateAsync(twoFactorCode);
      setShowSetup(false);
      setTwoFactorCode('');
      setQrCodeUrl('');
      setManualKey('');
    } catch (error) {
      // Error handled by mutation
    }
  };

  const handleDisableTwoFactor = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      await disableTwoFactor.mutateAsync(twoFactorCode);
      setTwoFactorCode('');
    } catch (error) {
      // Error handled by mutation
    }
  };

  if (isLoadingStatus) {
    return <Loading />;
  }

  if (statusError) {
    return <ErrorState title="Error" message="Failed to load security settings" />;
  }

  return (
    <div className="space-y-6">
      {/* Password Change */}
      <Card>
        <form onSubmit={handlePasswordChange} className="space-y-4">
          <h3 className="text-lg font-semibold text-gray-900 mb-2">Change Password</h3>
          <p className="text-sm text-gray-600 mb-4">
            Update your password to keep your account secure
          </p>

          <Input
            label="Current Password"
            type="password"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            required
            autoComplete="current-password"
          />

          <Input
            label="New Password"
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            required
            autoComplete="new-password"
          />

          <Input
            label="Confirm New Password"
            type="password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            required
            autoComplete="new-password"
            error={passwordError}
          />

          <Button
            type="submit"
            variant="primary"
            loading={changePassword.isPending}
          >
            Change Password
          </Button>

          {changePassword.isSuccess && (
            <p className="text-sm text-green-600">
              Password changed successfully!
            </p>
          )}
        </form>
      </Card>

      {/* Two-Factor Authentication */}
      <Card>
        <div className="space-y-4">
          <div>
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Two-Factor Authentication</h3>
            <p className="text-sm text-gray-600">
              Add an extra layer of security to your account
            </p>
          </div>

          {twoFactorStatus?.isEnabled ? (
            // 2FA is enabled - show disable form
            <div className="space-y-4">
              <div className="flex items-center gap-3 p-3 bg-green-50 border border-green-200 rounded-lg">
                <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                </svg>
                <span className="text-sm font-medium text-green-800">
                  Two-factor authentication is enabled
                </span>
              </div>

              <form onSubmit={handleDisableTwoFactor} className="space-y-4">
                <Input
                  label="Enter code from authenticator app to disable"
                  type="text"
                  value={twoFactorCode}
                  onChange={(e) => setTwoFactorCode(e.target.value)}
                  placeholder="000000"
                  maxLength={6}
                  required
                />

                <Button
                  type="submit"
                  variant="outline"
                  loading={disableTwoFactor.isPending}
                >
                  Disable Two-Factor Authentication
                </Button>

                {disableTwoFactor.isError && (
                  <p className="text-sm text-red-600">
                    Invalid code. Please try again.
                  </p>
                )}
              </form>
            </div>
          ) : showSetup ? (
            // Show setup QR code
            <div className="space-y-4">
              <div className="p-4 bg-gray-50 rounded-lg">
                <p className="text-sm text-gray-700 mb-3">
                  Scan this QR code with your authenticator app:
                </p>
                {qrCodeUrl && (
                  <img src={qrCodeUrl} alt="2FA QR Code" className="mx-auto border border-gray-300 rounded" />
                )}
                <p className="text-xs text-gray-600 mt-3">
                  Or enter this key manually: <code className="bg-white px-2 py-1 rounded">{manualKey}</code>
                </p>
              </div>

              <form onSubmit={handleVerifyTwoFactor} className="space-y-4">
                <Input
                  label="Enter the code from your authenticator app"
                  type="text"
                  value={twoFactorCode}
                  onChange={(e) => setTwoFactorCode(e.target.value)}
                  placeholder="000000"
                  maxLength={6}
                  required
                />

                <div className="flex gap-3">
                  <Button
                    type="submit"
                    variant="primary"
                    loading={verifyTwoFactor.isPending}
                  >
                    Verify and Enable
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => {
                      setShowSetup(false);
                      setTwoFactorCode('');
                      setQrCodeUrl('');
                      setManualKey('');
                    }}
                  >
                    Cancel
                  </Button>
                </div>

                {verifyTwoFactor.isError && (
                  <p className="text-sm text-red-600">
                    Invalid code. Please try again.
                  </p>
                )}
              </form>
            </div>
          ) : (
            // 2FA is disabled - show enable button
            <div className="space-y-4">
              <p className="text-sm text-gray-600">
                Two-factor authentication is not enabled. Enable it to add an extra layer of security.
              </p>

              <Button
                onClick={handleSetupTwoFactor}
                variant="primary"
                loading={setupTwoFactor.isPending}
              >
                Enable Two-Factor Authentication
              </Button>

              {setupTwoFactor.isError && (
                <p className="text-sm text-red-600">
                  Failed to setup two-factor authentication. Please try again.
                </p>
              )}
            </div>
          )}
        </div>
      </Card>
    </div>
  );
}
