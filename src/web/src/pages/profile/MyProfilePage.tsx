/**
 * MyProfilePage
 * Self-service member profile portal with tabs for Profile, Family, and Involvement
 */

import { useState } from 'react';
import { useMyProfile, useMyFamily, useMyInvolvement } from '@/hooks/useProfile';
import { ProfileEditor, FamilySection, InvolvementSummary } from '@/components/profile';

type TabType = 'profile' | 'family' | 'involvement';

export function MyProfilePage() {
  const [activeTab, setActiveTab] = useState<TabType>('profile');

  const {
    data: profile,
    isLoading: isLoadingProfile,
    error: profileError,
  } = useMyProfile();

  const {
    data: family,
    isLoading: isLoadingFamily,
    error: familyError,
  } = useMyFamily();

  const {
    data: involvement,
    isLoading: isLoadingInvolvement,
    error: involvementError,
  } = useMyInvolvement();

  const tabs = [
    { id: 'profile' as TabType, label: 'Profile', icon: 'user' },
    { id: 'family' as TabType, label: 'Family', icon: 'users' },
    { id: 'involvement' as TabType, label: 'Involvement', icon: 'calendar' },
  ];

  const renderTabIcon = (icon: string) => {
    switch (icon) {
      case 'user':
        return (
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
            />
          </svg>
        );
      case 'users':
        return (
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
            />
          </svg>
        );
      case 'calendar':
        return (
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
            />
          </svg>
        );
      default:
        return null;
    }
  };

  const renderContent = () => {
    switch (activeTab) {
      case 'profile':
        if (isLoadingProfile) {
          return <LoadingSkeleton />;
        }
        if (profileError) {
          return <ErrorState message="Failed to load profile" />;
        }
        if (!profile) {
          return <ErrorState message="Profile not found" />;
        }
        return <ProfileEditor profile={profile} />;

      case 'family':
        if (isLoadingFamily) {
          return <LoadingSkeleton />;
        }
        if (familyError) {
          return <ErrorState message="Failed to load family members" />;
        }
        return <FamilySection members={family || []} />;

      case 'involvement':
        if (isLoadingInvolvement) {
          return <LoadingSkeleton />;
        }
        if (involvementError) {
          return <ErrorState message="Failed to load involvement data" />;
        }
        if (!involvement) {
          return <ErrorState message="Involvement data not found" />;
        }
        return <InvolvementSummary involvement={involvement} />;

      default:
        return null;
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-6xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900">My Profile</h1>
          <p className="mt-2 text-gray-600">Manage your profile, family, and involvement</p>
        </div>

        {/* Tabs */}
        <div className="bg-white rounded-lg border border-gray-200 mb-6">
          <div className="border-b border-gray-200">
            <nav className="flex -mb-px overflow-x-auto" aria-label="Tabs">
              {tabs.map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`
                    flex items-center gap-2 px-6 py-4 text-sm font-medium whitespace-nowrap
                    border-b-2 transition-colors
                    ${
                      activeTab === tab.id
                        ? 'border-primary-600 text-primary-600'
                        : 'border-transparent text-gray-600 hover:text-gray-900 hover:border-gray-300'
                    }
                  `}
                >
                  {renderTabIcon(tab.icon)}
                  {tab.label}
                </button>
              ))}
            </nav>
          </div>
        </div>

        {/* Content */}
        <div>{renderContent()}</div>
      </div>
    </div>
  );
}

function LoadingSkeleton() {
  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <div className="animate-pulse space-y-4">
        <div className="flex items-center gap-4">
          <div className="w-16 h-16 bg-gray-200 rounded-full" />
          <div className="flex-1 space-y-2">
            <div className="h-6 bg-gray-200 rounded w-1/3" />
            <div className="h-4 bg-gray-200 rounded w-1/4" />
          </div>
        </div>
        <div className="space-y-3">
          <div className="h-4 bg-gray-200 rounded w-full" />
          <div className="h-4 bg-gray-200 rounded w-5/6" />
          <div className="h-4 bg-gray-200 rounded w-4/6" />
        </div>
      </div>
    </div>
  );
}

function ErrorState({ message }: { message: string }) {
  return (
    <div className="bg-white rounded-lg border border-red-200 p-6">
      <div className="flex items-center gap-3 text-red-800">
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
          />
        </svg>
        <p className="font-medium">{message}</p>
      </div>
    </div>
  );
}
