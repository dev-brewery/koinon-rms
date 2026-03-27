/**
 * Check-in Configuration Page
 * Three-tab admin UI: Areas, Locations, Devices
 */

import { useState } from 'react';
import { CheckinAreasTab } from './CheckinAreasTab';
import { CheckinLocationsTab } from './CheckinLocationsTab';
import { CheckinDevicesTab } from './CheckinDevicesTab';

type TabId = 'areas' | 'locations' | 'devices';

interface Tab {
  id: TabId;
  label: string;
}

const TABS: Tab[] = [
  { id: 'areas', label: 'Areas' },
  { id: 'locations', label: 'Locations' },
  { id: 'devices', label: 'Devices' },
];

export function CheckinConfigPage() {
  const [activeTab, setActiveTab] = useState<TabId>('areas');

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Check-in Setup</h1>
        <p className="mt-2 text-gray-600">
          Configure check-in areas, location capacities, and kiosk devices
        </p>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex gap-1" aria-label="Check-in configuration tabs">
          {TABS.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
                activeTab === tab.id
                  ? 'border-primary-600 text-primary-700'
                  : 'border-transparent text-gray-600 hover:text-gray-800 hover:border-gray-300'
              }`}
              aria-current={activeTab === tab.id ? 'page' : undefined}
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      {/* Tab Content */}
      {activeTab === 'areas' && <CheckinAreasTab />}
      {activeTab === 'locations' && <CheckinLocationsTab />}
      {activeTab === 'devices' && <CheckinDevicesTab />}
    </div>
  );
}
