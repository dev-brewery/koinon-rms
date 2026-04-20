/**
 * RecipientBuilder Component
 * 3-tab recipient selection: Groups, Individuals, and By Filter
 */

import { useState } from 'react';
import { usePeople } from '@/hooks/usePeople';
import { useDebounce } from '@/hooks/useDebounce';
import { useDefinedTypeValues } from '@/hooks/useDefinedTypes';
import { useCampuses } from '@/hooks/useCampuses';
import type { GroupSummaryDto, PersonSummaryDto } from '@/services/api/types';

// GUID for the ConnectionStatus defined type (matches existing usage in PeopleListPage)
const CONNECTION_STATUS_GUID = '2e6540ea-63f0-40fe-be50-f2a84735e600';

type ActiveTab = 'groups' | 'individuals' | 'filter';

interface RecipientBuilderProps {
  groups: GroupSummaryDto[];
  selectedGroupIdKeys: string[];
  selectedPersons: PersonSummaryDto[];
  onGroupToggle: (idKey: string) => void;
  onAddPerson: (person: PersonSummaryDto) => void;
  onRemovePerson: (idKey: string) => void;
}

export function RecipientBuilder({
  groups,
  selectedGroupIdKeys,
  selectedPersons,
  onGroupToggle,
  onAddPerson,
  onRemovePerson,
}: RecipientBuilderProps) {
  const [activeTab, setActiveTab] = useState<ActiveTab>('groups');

  return (
    <div className="border border-gray-300 rounded-lg overflow-hidden">
      {/* Tab Bar */}
      <div className="flex border-b border-gray-300 bg-gray-50">
        <TabButton
          label="Groups"
          isActive={activeTab === 'groups'}
          onClick={() => setActiveTab('groups')}
          testId="recipient-tab-groups"
        />
        <TabButton
          label="Individuals"
          isActive={activeTab === 'individuals'}
          onClick={() => setActiveTab('individuals')}
          testId="recipient-tab-individuals"
        />
        <TabButton
          label="By Filter"
          isActive={activeTab === 'filter'}
          onClick={() => setActiveTab('filter')}
          testId="recipient-tab-filter"
        />
      </div>

      {/* Tab Panels */}
      <div className="p-4">
        {activeTab === 'groups' && (
          <GroupsTab
            groups={groups}
            selectedGroupIdKeys={selectedGroupIdKeys}
            onGroupToggle={onGroupToggle}
          />
        )}
        {activeTab === 'individuals' && (
          <IndividualsTab
            selectedPersons={selectedPersons}
            onAddPerson={onAddPerson}
            onRemovePerson={onRemovePerson}
          />
        )}
        {activeTab === 'filter' && (
          <FilterTab
            selectedPersons={selectedPersons}
            onAddPerson={onAddPerson}
          />
        )}
      </div>
    </div>
  );
}

// ============================================================================
// Internal: TabButton
// ============================================================================

interface TabButtonProps {
  label: string;
  isActive: boolean;
  onClick: () => void;
  testId: string;
}

function TabButton({ label, isActive, onClick, testId }: TabButtonProps) {
  return (
    <button
      type="button"
      data-testid={testId}
      onClick={onClick}
      className={[
        'flex-1 px-4 py-2 text-sm font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-inset focus:ring-primary-500',
        isActive
          ? 'bg-white text-primary-700 border-b-2 border-primary-600'
          : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100',
      ].join(' ')}
    >
      {label}
    </button>
  );
}

// ============================================================================
// Internal: GroupsTab
// ============================================================================

interface GroupsTabProps {
  groups: GroupSummaryDto[];
  selectedGroupIdKeys: string[];
  onGroupToggle: (idKey: string) => void;
}

function GroupsTab({ groups, selectedGroupIdKeys, onGroupToggle }: GroupsTabProps) {
  if (groups.length === 0) {
    return <p className="text-sm text-gray-500 text-center py-4">No groups available</p>;
  }

  return (
    <div className="max-h-56 overflow-y-auto divide-y divide-gray-100">
      {groups.map((group) => (
        <label
          key={group.idKey}
          className="flex items-center gap-3 py-2 px-1 hover:bg-gray-50 cursor-pointer"
        >
          <input
            type="checkbox"
            checked={selectedGroupIdKeys.includes(group.idKey)}
            onChange={() => onGroupToggle(group.idKey)}
            className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
          />
          <div className="flex-1 min-w-0">
            <div className="text-sm font-medium text-gray-900 truncate">{group.name}</div>
            <div className="text-xs text-gray-500">
              {group.memberCount} {group.memberCount === 1 ? 'member' : 'members'}
            </div>
          </div>
        </label>
      ))}
    </div>
  );
}

// ============================================================================
// Internal: IndividualsTab
// ============================================================================

interface IndividualsTabProps {
  selectedPersons: PersonSummaryDto[];
  onAddPerson: (person: PersonSummaryDto) => void;
  onRemovePerson: (idKey: string) => void;
}

function IndividualsTab({ selectedPersons, onAddPerson, onRemovePerson }: IndividualsTabProps) {
  const [query, setQuery] = useState('');
  const debouncedQuery = useDebounce(query, 300);

  const selectedIdKeys = new Set(selectedPersons.map((p) => p.idKey));

  const { data: searchResult, isFetching } = usePeople(
    debouncedQuery.length >= 2 ? { q: debouncedQuery } : {}
  );
  const results = debouncedQuery.length >= 2 ? (searchResult?.data ?? []) : [];
  const unselectedResults = results.filter((p) => !selectedIdKeys.has(p.idKey));

  return (
    <div className="space-y-3">
      <input
        type="text"
        data-testid="person-search-input"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Search by name or email..."
        className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
      />

      {debouncedQuery.length >= 2 && (
        <div className="max-h-40 overflow-y-auto border border-gray-200 rounded-lg divide-y divide-gray-100">
          {isFetching && (
            <p className="text-sm text-gray-500 px-3 py-2">Searching...</p>
          )}
          {!isFetching && unselectedResults.length === 0 && (
            <p className="text-sm text-gray-500 px-3 py-2">No results</p>
          )}
          {!isFetching &&
            unselectedResults.map((person) => (
              <div key={person.idKey} className="flex items-center justify-between px-3 py-2 hover:bg-gray-50">
                <div className="min-w-0">
                  <div className="text-sm font-medium text-gray-900 truncate">{person.fullName}</div>
                  {person.email && (
                    <div className="text-xs text-gray-500 truncate">{person.email}</div>
                  )}
                </div>
                <button
                  type="button"
                  data-testid={`add-person-btn-${person.idKey}`}
                  onClick={() => onAddPerson(person)}
                  className="ml-3 shrink-0 px-2 py-1 text-xs font-medium text-primary-700 bg-primary-50 border border-primary-200 rounded hover:bg-primary-100 transition-colors"
                >
                  Add
                </button>
              </div>
            ))}
        </div>
      )}

      {selectedPersons.length > 0 && (
        <div>
          <p className="text-xs font-medium text-gray-600 mb-1">
            Added individually ({selectedPersons.length}):
          </p>
          <div className="max-h-32 overflow-y-auto space-y-1">
            {selectedPersons.map((person) => (
              <div key={person.idKey} className="flex items-center justify-between bg-gray-50 rounded px-3 py-1">
                <span className="text-sm text-gray-800 truncate">{person.fullName}</span>
                <button
                  type="button"
                  onClick={() => onRemovePerson(person.idKey)}
                  className="ml-2 shrink-0 text-gray-400 hover:text-red-600 transition-colors"
                  aria-label={`Remove ${person.fullName}`}
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

// ============================================================================
// Internal: FilterTab
// ============================================================================

interface FilterTabProps {
  selectedPersons: PersonSummaryDto[];
  onAddPerson: (person: PersonSummaryDto) => void;
}

function FilterTab({ selectedPersons, onAddPerson }: FilterTabProps) {
  const [connectionStatusId, setConnectionStatusId] = useState('');
  const [campusId, setCampusId] = useState('');

  const { data: connectionStatuses = [] } = useDefinedTypeValues(CONNECTION_STATUS_GUID);
  const { data: campuses = [] } = useCampuses();

  const filterParams = {
    ...(connectionStatusId ? { connectionStatusId } : {}),
    ...(campusId ? { campusId } : {}),
  };
  const hasFilter = connectionStatusId !== '' || campusId !== '';

  const { data: filterResult } = usePeople(hasFilter ? filterParams : {});
  const matchingPeople = hasFilter ? (filterResult?.data ?? []) : [];
  const matchCount = hasFilter ? (filterResult?.meta?.totalCount ?? matchingPeople.length) : 0;

  const selectedIdKeys = new Set(selectedPersons.map((p) => p.idKey));

  const handleAddFiltered = () => {
    for (const person of matchingPeople) {
      if (!selectedIdKeys.has(person.idKey)) {
        onAddPerson(person);
      }
    }
  };

  return (
    <div className="space-y-3">
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">
            Connection Status
          </label>
          <select
            data-testid="connection-status-filter"
            value={connectionStatusId}
            onChange={(e) => setConnectionStatusId(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          >
            <option value="">Any</option>
            {connectionStatuses.map((status) => (
              <option key={status.idKey} value={status.idKey}>
                {status.value}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">
            Campus
          </label>
          <select
            data-testid="campus-filter"
            value={campusId}
            onChange={(e) => setCampusId(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          >
            <option value="">Any</option>
            {campuses.map((campus) => (
              <option key={campus.idKey} value={campus.idKey}>
                {campus.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {hasFilter && (
        <p className="text-sm text-gray-600">
          {matchCount} {matchCount === 1 ? 'person' : 'people'} match the selected filters
        </p>
      )}

      <button
        type="button"
        data-testid="add-filtered-people-btn"
        onClick={handleAddFiltered}
        disabled={!hasFilter || matchingPeople.length === 0}
        className="w-full px-4 py-2 text-sm font-medium text-white bg-primary-600 rounded-lg hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
      >
        Add Filtered People
      </button>
    </div>
  );
}
