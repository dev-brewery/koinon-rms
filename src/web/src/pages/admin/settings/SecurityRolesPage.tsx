/**
 * Security Roles Admin Page
 *
 * Provides an admin UI for managing security roles, the claims associated
 * with each role (Allow / Deny), and the people assigned to each role.
 *
 * AC coverage:
 *   - AC1: Admin can view and create security roles (list + create modal)
 *   - AC2: Admin can assign claims (Allow / Deny) to a role
 *   - AC3: Admin can add and remove people from a role
 *   - AC4: All mutations invalidate the relevant React Query caches, and the
 *     runtime authorization check (SecurityClaimService) hits the DB on every
 *     request — no restart or cache bust required.
 */

import { useEffect, useState } from 'react';
import {
  useAddRoleMember,
  useAssignRoleClaim,
  useCreateSecurityRole,
  useDeleteSecurityRole,
  useRemoveRoleClaim,
  useRemoveRoleMember,
  useSecurityClaims,
  useSecurityRole,
  useSecurityRoles,
  useUpdateSecurityRole,
} from '@/hooks/useSecurityRoles';
import { peopleApi } from '@/services/api';
import { Loading, EmptyState, ErrorState } from '@/components/ui';
import { getErrorMessage } from '@/lib/errorMessages';
import type {
  RoleSecurityClaimDto,
  SecurityRoleDto,
} from '@/services/api/securityRoles';

function Badge({
  children,
  variant = 'default',
}: {
  children: React.ReactNode;
  variant?: 'default' | 'system' | 'active' | 'inactive';
}) {
  const classes: Record<string, string> = {
    default: 'bg-gray-100 text-gray-800',
    system: 'bg-indigo-100 text-indigo-800',
    active: 'bg-green-100 text-green-800',
    inactive: 'bg-amber-100 text-amber-800',
  };
  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${classes[variant]}`}
    >
      {children}
    </span>
  );
}

export function SecurityRolesPage() {
  const { data: roles = [], isLoading, error, refetch } = useSecurityRoles();
  const [selectedIdKey, setSelectedIdKey] = useState<string | null>(null);
  const [isCreating, setIsCreating] = useState(false);

  const deleteMutation = useDeleteSecurityRole();

  const handleRowClick = (role: SecurityRoleDto) => {
    setSelectedIdKey(role.idKey);
  };

  const handleDelete = async (role: SecurityRoleDto) => {
    if (role.isSystemRole) {
      return;
    }
    if (
      !window.confirm(
        `Delete role "${role.name}"? This will remove all of its members and claim assignments.`
      )
    ) {
      return;
    }
    try {
      await deleteMutation.mutateAsync(role.idKey);
      if (selectedIdKey === role.idKey) {
        setSelectedIdKey(null);
      }
    } catch (err) {
      console.error('Failed to delete role', err);
    }
  };

  return (
    <div className="space-y-6" data-testid="security-roles-page">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Security Roles</h1>
          <p className="mt-2 text-gray-600">
            Manage roles and the permissions granted to their members.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setIsCreating(true)}
          className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
          data-testid="security-roles-create"
        >
          Create Role
        </button>
      </div>

      {isLoading && (
        <div className="bg-white rounded-lg border border-gray-200">
          <Loading text="Loading security roles..." />
        </div>
      )}

      {error && (
        <div className="bg-white rounded-lg border border-gray-200">
          <ErrorState
            title="Failed to load security roles"
            message={
              getErrorMessage(error).message +
              ' You may not have the security:manage permission.'
            }
            onRetry={() => refetch()}
          />
        </div>
      )}

      {!isLoading && !error && roles.length === 0 && (
        <div className="bg-white rounded-lg border border-gray-200">
          <EmptyState
            title="No security roles yet"
            description="Security roles group permissions that can be assigned to people. Create a role (e.g., 'Check-in Staff') and assign claims and members."
            action={{
              label: 'Create Role',
              onClick: () => setIsCreating(true),
            }}
          />
        </div>
      )}

      {!isLoading && !error && roles.length > 0 && (
        <div
          className="bg-white rounded-lg border border-gray-200 overflow-hidden"
          data-testid="security-roles-list"
        >
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Name
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Description
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Claims
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Members
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Status
                </th>
                <th className="px-6 py-3" />
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">

              {roles.map((role) => (
                <tr
                  key={role.idKey}
                  data-testid="security-role-row"
                  data-role-idkey={role.idKey}
                  data-role-name={role.name}
                  className="hover:bg-gray-50 cursor-pointer"
                  onClick={() => handleRowClick(role)}
                >
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {role.name}
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-600">
                    {role.description || (
                      <span className="text-gray-400 italic">(no description)</span>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-700">
                    {role.claimCount}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-700">
                    {role.memberCount}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm">
                    <div className="flex items-center gap-1">
                      {role.isActive ? (
                        <Badge variant="active">Active</Badge>
                      ) : (
                        <Badge variant="inactive">Inactive</Badge>
                      )}
                      {role.isSystemRole && <Badge variant="system">System</Badge>}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm">
                    <button
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        void handleDelete(role);
                      }}
                      disabled={role.isSystemRole}
                      title={
                        role.isSystemRole
                          ? 'System roles cannot be deleted'
                          : 'Delete role'
                      }
                      className="text-red-600 hover:text-red-800 disabled:text-gray-300 disabled:cursor-not-allowed"
                      data-testid="security-role-delete"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {isCreating && (
        <CreateRoleModal
          onClose={() => setIsCreating(false)}
          onCreated={(idKey) => {
            setIsCreating(false);
            setSelectedIdKey(idKey);
          }}
        />
      )}

      {selectedIdKey && (
        <RoleDetailDrawer
          idKey={selectedIdKey}
          onClose={() => setSelectedIdKey(null)}
        />
      )}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Create Role Modal
// ---------------------------------------------------------------------------

function CreateRoleModal({
  onClose,
  onCreated,
}: {
  onClose: () => void;
  onCreated: (idKey: string) => void;
}) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [error, setError] = useState<string | null>(null);
  const createMutation = useCreateSecurityRole();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!name.trim()) {
      setError('Name is required');
      return;
    }
    try {
      const role = await createMutation.mutateAsync({
        name: name.trim(),
        description: description.trim() || null,
        isActive: true,
      });
      onCreated(role.idKey);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create role');
    }
  };

  return (
    <div
      className="fixed inset-0 bg-black/40 flex items-center justify-center z-50"
      role="dialog"
      aria-modal="true"
      data-testid="security-role-create-modal"
    >
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full">
        <form onSubmit={handleSubmit}>
          <div className="px-6 py-4 border-b border-gray-200">
            <h2 className="text-lg font-semibold text-gray-900">Create Security Role</h2>
          </div>
          <div className="px-6 py-4 space-y-4">
            <div>
              <label
                htmlFor="role-name"
                className="block text-sm font-medium text-gray-700 mb-1"
              >
                Name <span className="text-red-500">*</span>
              </label>
              <input
                id="role-name"
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                maxLength={100}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-primary-500"
                data-testid="security-role-create-name"
              />
            </div>
            <div>
              <label
                htmlFor="role-description"
                className="block text-sm font-medium text-gray-700 mb-1"
              >
                Description
              </label>
              <textarea
                id="role-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                maxLength={500}
                rows={3}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-primary-500"
                data-testid="security-role-create-description"
              />
            </div>
            {error && (
              <div className="text-sm text-red-600" role="alert">
                {error}
              </div>
            )}
          </div>
          <div className="px-6 py-4 border-t border-gray-200 flex justify-end gap-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              data-testid="security-role-create-cancel"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={createMutation.isPending}
              className="px-4 py-2 bg-primary-600 text-white rounded-md hover:bg-primary-700 disabled:bg-gray-300"
              data-testid="security-role-create-submit"
            >
              {createMutation.isPending ? 'Creating…' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Role Detail Drawer (Claims + Members)
// ---------------------------------------------------------------------------

function RoleDetailDrawer({
  idKey,
  onClose,
}: {
  idKey: string;
  onClose: () => void;
}) {
  const { data: detail, isLoading } = useSecurityRole(idKey);
  const { data: allClaims = [] } = useSecurityClaims();
  const updateMutation = useUpdateSecurityRole();
  const assignClaimMutation = useAssignRoleClaim();
  const removeClaimMutation = useRemoveRoleClaim();
  const addMemberMutation = useAddRoleMember();
  const removeMemberMutation = useRemoveRoleMember();

  const [editName, setEditName] = useState('');
  const [editDescription, setEditDescription] = useState('');
  const [selectedClaimIdKey, setSelectedClaimIdKey] = useState('');
  const [selectedAllowOrDeny, setSelectedAllowOrDeny] = useState<'A' | 'D'>('A');
  const [personQuery, setPersonQuery] = useState('');
  const [personResults, setPersonResults] = useState<
    Array<{ idKey: string; fullName: string; email: string | null }>
  >([]);
  const [memberExpires, setMemberExpires] = useState('');
  const [searchingPeople, setSearchingPeople] = useState(false);

  // Populate edit form when detail loads or changes.
  useEffect(() => {
    if (detail) {
      setEditName(detail.name);
      setEditDescription(detail.description ?? '');
    }
  }, [detail]);

  const saveEdits = async () => {
    if (!detail) return;
    if (!editName.trim()) return;
    await updateMutation.mutateAsync({
      idKey: detail.idKey,
      request: {
        name: editName.trim(),
        description: editDescription.trim() || null,
        isActive: detail.isActive,
      },
    });
  };

  const assignClaim = async () => {
    if (!detail || !selectedClaimIdKey) return;
    await assignClaimMutation.mutateAsync({
      idKey: detail.idKey,
      request: { claimIdKey: selectedClaimIdKey, allowOrDeny: selectedAllowOrDeny },
    });
    setSelectedClaimIdKey('');
  };

  const removeClaim = async (claim: RoleSecurityClaimDto) => {
    if (!detail) return;
    await removeClaimMutation.mutateAsync({
      idKey: detail.idKey,
      claimIdKey: claim.claimIdKey,
    });
  };

  const searchPeople = async () => {
    if (!personQuery.trim()) return;
    setSearchingPeople(true);
    try {
      const result = await peopleApi.searchPeople({ q: personQuery, pageSize: 20 });
      setPersonResults(
        (result.data ?? []).map((p) => ({
          idKey: p.idKey,
          fullName: p.fullName ?? `${p.firstName} ${p.lastName}`.trim(),
          email: p.email ?? null,
        }))
      );
    } finally {
      setSearchingPeople(false);
    }
  };

  const addMember = async (person: { idKey: string }) => {
    if (!detail) return;
    await addMemberMutation.mutateAsync({
      idKey: detail.idKey,
      request: {
        personIdKey: person.idKey,
        expiresDateTime: memberExpires ? new Date(memberExpires).toISOString() : null,
      },
    });
  };

  const removeMember = async (personIdKey: string) => {
    if (!detail) return;
    await removeMemberMutation.mutateAsync({ idKey: detail.idKey, personIdKey });
  };

  return (
    <div
      className="fixed inset-0 z-40 flex"
      role="dialog"
      aria-modal="true"
      data-testid="security-role-detail"
      data-role-idkey={idKey}
    >
      <div className="flex-1 bg-black/40" onClick={onClose} />
      <div className="w-full max-w-2xl bg-white shadow-xl h-full flex flex-col">
        <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
          <h2 className="text-xl font-semibold text-gray-900">
            {detail ? detail.name : 'Loading…'}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="text-gray-500 hover:text-gray-700"
            aria-label="Close"
            data-testid="security-role-detail-close"
          >
            ✕
          </button>
        </div>

        <div className="flex-1 overflow-y-auto px-6 py-4 space-y-6">
          {isLoading || !detail ? (
            <div className="text-gray-500">Loading role details…</div>
          ) : (
            <>
              {/* Edit name/description */}
              <section>
                <h3 className="text-sm font-medium text-gray-700 mb-2">Role Info</h3>
                <div className="space-y-2">
                  <input
                    type="text"
                    value={editName}
                    onChange={(e) => setEditName(e.target.value)}
                    disabled={detail.isSystemRole}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md disabled:bg-gray-100"
                    data-testid="security-role-detail-name"
                  />
                  <textarea
                    value={editDescription}
                    onChange={(e) => setEditDescription(e.target.value)}
                    rows={2}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md"
                    data-testid="security-role-detail-description"
                  />
                  <div className="flex items-center justify-between text-sm">
                    <div className="text-gray-500">
                      {detail.isSystemRole && (
                        <span className="text-indigo-700">System role — name is read-only</span>
                      )}
                    </div>
                    <button
                      type="button"
                      onClick={() => void saveEdits()}
                      disabled={updateMutation.isPending}
                      className="px-3 py-1 bg-primary-600 text-white rounded-md hover:bg-primary-700 disabled:bg-gray-300"
                      data-testid="security-role-detail-save"
                    >
                      {updateMutation.isPending ? 'Saving…' : 'Save'}
                    </button>
                  </div>
                </div>
              </section>

              {/* Claims */}
              <section data-testid="security-role-claims-section">
                <h3 className="text-sm font-medium text-gray-700 mb-2">
                  Claims ({detail.claims.length})
                </h3>
                <div className="flex items-center gap-2 mb-3">
                  <select
                    value={selectedClaimIdKey}
                    onChange={(e) => setSelectedClaimIdKey(e.target.value)}
                    className="flex-1 px-3 py-2 border border-gray-300 rounded-md"
                    data-testid="security-role-claim-picker"
                  >
                    <option value="">— Select a claim —</option>
                    {allClaims.map((c) => (
                      <option key={c.idKey} value={c.idKey}>
                        {c.claimType}:{c.claimValue}
                        {c.description ? ` — ${c.description}` : ''}
                      </option>
                    ))}
                  </select>
                  <select
                    value={selectedAllowOrDeny}
                    onChange={(e) =>
                      setSelectedAllowOrDeny(e.target.value as 'A' | 'D')
                    }
                    className="px-2 py-2 border border-gray-300 rounded-md"
                    data-testid="security-role-claim-decision"
                  >
                    <option value="A">Allow</option>
                    <option value="D">Deny</option>
                  </select>
                  <button
                    type="button"
                    onClick={() => void assignClaim()}
                    disabled={!selectedClaimIdKey || assignClaimMutation.isPending}
                    className="px-3 py-2 bg-primary-600 text-white rounded-md hover:bg-primary-700 disabled:bg-gray-300"
                    data-testid="security-role-claim-assign"
                  >
                    Assign
                  </button>
                </div>
                <ul className="divide-y divide-gray-200 border border-gray-200 rounded-md">
                  {detail.claims.length === 0 && (
                    <li className="px-3 py-2 text-sm text-gray-500">No claims assigned.</li>
                  )}
                  {detail.claims.map((claim) => (
                    <li
                      key={claim.claimIdKey}
                      className="px-3 py-2 flex items-center justify-between"
                      data-testid="security-role-claim-row"
                      data-claim-idkey={claim.claimIdKey}
                      data-claim-type={claim.claimType}
                      data-claim-value={claim.claimValue}
                    >
                      <div>
                        <div className="text-sm font-medium text-gray-900">
                          {claim.claimType}:{claim.claimValue}
                        </div>
                        {claim.description && (
                          <div className="text-xs text-gray-500">{claim.description}</div>
                        )}
                      </div>
                      <div className="flex items-center gap-2">
                        <Badge variant={claim.allowOrDeny === 'A' ? 'active' : 'inactive'}>
                          {claim.allowOrDeny === 'A' ? 'Allow' : 'Deny'}
                        </Badge>
                        <button
                          type="button"
                          onClick={() => void removeClaim(claim)}
                          className="text-xs text-red-600 hover:text-red-800"
                          data-testid="security-role-claim-remove"
                        >
                          Remove
                        </button>
                      </div>
                    </li>
                  ))}
                </ul>
              </section>

              {/* Members */}
              <section data-testid="security-role-members-section">
                <h3 className="text-sm font-medium text-gray-700 mb-2">
                  Members ({detail.members.length})
                </h3>
                <div className="flex items-center gap-2 mb-2">
                  <input
                    type="text"
                    value={personQuery}
                    onChange={(e) => setPersonQuery(e.target.value)}
                    placeholder="Search people by name or email"
                    className="flex-1 px-3 py-2 border border-gray-300 rounded-md"
                    data-testid="security-role-member-picker"
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') {
                        e.preventDefault();
                        void searchPeople();
                      }
                    }}
                  />
                  <input
                    type="datetime-local"
                    value={memberExpires}
                    onChange={(e) => setMemberExpires(e.target.value)}
                    className="px-2 py-2 border border-gray-300 rounded-md text-sm"
                    data-testid="security-role-member-expires"
                    title="Optional expiration"
                  />
                  <button
                    type="button"
                    onClick={() => void searchPeople()}
                    disabled={searchingPeople}
                    className="px-3 py-2 bg-gray-200 text-gray-800 rounded-md hover:bg-gray-300"
                    data-testid="security-role-member-search"
                  >
                    Search
                  </button>
                </div>
                {personResults.length > 0 && (
                  <ul
                    className="divide-y divide-gray-200 border border-gray-200 rounded-md mb-3 max-h-40 overflow-y-auto"
                    data-testid="security-role-member-results"
                  >
                    {personResults.map((person) => (
                      <li
                        key={person.idKey}
                        className="px-3 py-2 flex items-center justify-between"
                        data-testid="security-role-member-result"
                        data-person-idkey={person.idKey}
                      >
                        <div>
                          <div className="text-sm font-medium text-gray-900">
                            {person.fullName}
                          </div>
                          {person.email && (
                            <div className="text-xs text-gray-500">{person.email}</div>
                          )}
                        </div>
                        <button
                          type="button"
                          onClick={() => void addMember(person)}
                          disabled={addMemberMutation.isPending}
                          className="text-xs px-2 py-1 bg-primary-600 text-white rounded hover:bg-primary-700 disabled:bg-gray-300"
                          data-testid="security-role-member-add"
                        >
                          Add
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
                <ul className="divide-y divide-gray-200 border border-gray-200 rounded-md">
                  {detail.members.length === 0 && (
                    <li className="px-3 py-2 text-sm text-gray-500">No members assigned.</li>
                  )}
                  {detail.members.map((member) => (
                    <li
                      key={member.personIdKey}
                      className="px-3 py-2 flex items-center justify-between"
                      data-testid="security-role-member-row"
                      data-person-idkey={member.personIdKey}
                    >
                      <div>
                        <div className="text-sm font-medium text-gray-900">
                          {member.personName}
                        </div>
                        {member.email && (
                          <div className="text-xs text-gray-500">{member.email}</div>
                        )}
                        {member.expiresDateTime && (
                          <div className="text-xs text-amber-600">
                            Expires {new Date(member.expiresDateTime).toLocaleString()}
                          </div>
                        )}
                      </div>
                      <button
                        type="button"
                        onClick={() => void removeMember(member.personIdKey)}
                        className="text-xs text-red-600 hover:text-red-800"
                        data-testid="security-role-member-remove"
                      >
                        Remove
                      </button>
                    </li>
                  ))}
                </ul>
              </section>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

export default SecurityRolesPage;
