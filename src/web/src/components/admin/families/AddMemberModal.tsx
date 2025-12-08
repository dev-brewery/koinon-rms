/**
 * Add Member Modal Component
 * Modal for searching and adding members to a family
 */

import { useState } from 'react';
import { Link } from 'react-router-dom';
import { usePeople } from '@/hooks/usePeople';
import type { PersonSummaryDto } from '@/services/api/types';

interface AddMemberModalProps {
  isOpen: boolean;
  onClose: () => void;
  onAdd: (personId: string, roleId: string) => void;
  adultRoleId: string;
  childRoleId: string;
  existingMemberIds: string[];
}

export function AddMemberModal({
  isOpen,
  onClose,
  onAdd,
  adultRoleId,
  childRoleId,
  existingMemberIds,
}: AddMemberModalProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedPerson, setSelectedPerson] = useState<PersonSummaryDto | null>(null);
  const [selectedRole, setSelectedRole] = useState(adultRoleId);

  const { data, isLoading } = usePeople({
    q: searchQuery || undefined,
    page: 1,
    pageSize: 10,
  });

  const people = data?.data || [];
  const filteredPeople = people.filter(
    (person) => !existingMemberIds.includes(person.idKey)
  );

  const handleAdd = () => {
    if (selectedPerson) {
      onAdd(selectedPerson.idKey, selectedRole);
      setSearchQuery('');
      setSelectedPerson(null);
      setSelectedRole(adultRoleId);
    }
  };

  const handleClose = () => {
    setSearchQuery('');
    setSelectedPerson(null);
    setSelectedRole(adultRoleId);
    onClose();
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
        <div className="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div className="flex items-start justify-between mb-4">
              <h3 className="text-lg font-medium text-gray-900">Add Family Member</h3>
              <button
                onClick={handleClose}
                className="text-gray-400 hover:text-gray-500"
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

            {/* Search Input */}
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Search for Person
              </label>
              <div className="relative">
                <input
                  type="text"
                  placeholder="Search by name, email, or phone..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                />
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <svg
                    className="w-5 h-5 text-gray-400"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                    />
                  </svg>
                </div>
              </div>
            </div>

            {/* Person Results */}
            {searchQuery && (
              <div className="mb-4 max-h-60 overflow-y-auto border border-gray-200 rounded-lg">
                {isLoading ? (
                  <div className="p-4 text-center text-gray-500">Searching...</div>
                ) : filteredPeople.length === 0 ? (
                  <div className="p-4 text-center text-gray-500">
                    No people found
                  </div>
                ) : (
                  <div className="divide-y divide-gray-200">
                    {filteredPeople.map((person) => (
                      <button
                        key={person.idKey}
                        onClick={() => {
                          setSelectedPerson(person);
                          setSearchQuery('');
                        }}
                        className="w-full p-3 hover:bg-gray-50 text-left transition-colors"
                      >
                        <div className="flex items-center gap-3">
                          {person.photoUrl ? (
                            <img
                              src={person.photoUrl}
                              alt={person.fullName}
                              className="w-10 h-10 rounded-full object-cover"
                            />
                          ) : (
                            <div className="w-10 h-10 rounded-full bg-gray-200 flex items-center justify-center">
                              <svg
                                className="w-5 h-5 text-gray-400"
                                fill="none"
                                stroke="currentColor"
                                viewBox="0 0 24 24"
                                aria-hidden="true"
                              >
                                <path
                                  strokeLinecap="round"
                                  strokeLinejoin="round"
                                  strokeWidth={2}
                                  d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                                />
                              </svg>
                            </div>
                          )}
                          <div className="flex-1 min-w-0">
                            <div className="text-sm font-medium text-gray-900 truncate">
                              {person.fullName}
                            </div>
                            {person.email && (
                              <div className="text-xs text-gray-500 truncate">
                                {person.email}
                              </div>
                            )}
                          </div>
                        </div>
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* Selected Person */}
            {selectedPerson && (
              <div className="mb-4 p-3 bg-blue-50 border border-blue-200 rounded-lg">
                <div className="flex items-center gap-3">
                  {selectedPerson.photoUrl ? (
                    <img
                      src={selectedPerson.photoUrl}
                      alt={selectedPerson.fullName}
                      className="w-10 h-10 rounded-full object-cover"
                    />
                  ) : (
                    <div className="w-10 h-10 rounded-full bg-gray-200 flex items-center justify-center">
                      <svg
                        className="w-5 h-5 text-gray-400"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                        aria-hidden="true"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                        />
                      </svg>
                    </div>
                  )}
                  <div className="flex-1 min-w-0">
                    <div className="text-sm font-medium text-gray-900">
                      {selectedPerson.fullName}
                    </div>
                    {selectedPerson.email && (
                      <div className="text-xs text-gray-600">{selectedPerson.email}</div>
                    )}
                  </div>
                  <button
                    onClick={() => setSelectedPerson(null)}
                    className="text-gray-400 hover:text-gray-600"
                    aria-label="Clear selection"
                  >
                    <svg
                      className="w-5 h-5"
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
              </div>
            )}

            {/* Role Selection */}
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Family Role
              </label>
              <select
                value={selectedRole}
                onChange={(e) => setSelectedRole(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              >
                <option value={adultRoleId}>Adult</option>
                <option value={childRoleId}>Child</option>
              </select>
            </div>

            {/* Create New Person Link */}
            <div className="mb-4">
              <Link
                to="/admin/people/new"
                className="text-sm text-primary-600 hover:text-primary-700 font-medium"
              >
                + Create New Person
              </Link>
            </div>
          </div>

          {/* Actions */}
          <div className="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse gap-3">
            <button
              onClick={handleAdd}
              disabled={!selectedPerson}
              className="w-full sm:w-auto px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              Add Member
            </button>
            <button
              onClick={handleClose}
              className="w-full sm:w-auto mt-3 sm:mt-0 px-4 py-2 bg-white border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
