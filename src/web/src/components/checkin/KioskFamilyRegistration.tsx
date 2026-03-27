import { useState } from 'react';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { registerFamily } from '@/services/api/checkin';
import type {
  CheckinFamilyDto,
  KioskChildRegistrationRequest,
} from '@/services/api/types';

export interface KioskFamilyRegistrationProps {
  onComplete: (family: CheckinFamilyDto) => void;
  onCancel: () => void;
  defaultPhone?: string;
}

type RegistrationStep = 'parent' | 'children' | 'review';

interface ChildDraft {
  id: string; // stable UUID for React key — never use array index
  firstName: string;
  lastName: string; // controlled; sent as undefined if empty
  birthMonth: string;
  birthDay: string;
  birthYear: string;
}

interface ParentDraft {
  firstName: string;
  lastName: string;
  phone: string;
}

interface ValidationErrors {
  parentFirstName?: string;
  parentLastName?: string;
  phone?: string;
  children?: Record<number, { firstName?: string }>;
}

// ─── helpers ────────────────────────────────────────────────────────────────

function formatPhone(value: string): string {
  if (value.length <= 3) return value;
  if (value.length <= 6) return `(${value.slice(0, 3)}) ${value.slice(3)}`;
  return `(${value.slice(0, 3)}) ${value.slice(3, 6)}-${value.slice(6)}`;
}

function emptyChild(defaultLastName: string): ChildDraft {
  return {
    id: crypto.randomUUID(),
    firstName: '',
    lastName: defaultLastName,
    birthMonth: '',
    birthDay: '',
    birthYear: '',
  };
}

function buildBirthDate(child: ChildDraft): string | undefined {
  if (!child.birthYear || !child.birthMonth || !child.birthDay) return undefined;
  const mm = child.birthMonth.padStart(2, '0');
  const dd = child.birthDay.padStart(2, '0');
  return `${child.birthYear}-${mm}-${dd}`;
}

function generateYears(): number[] {
  const currentYear = new Date().getFullYear();
  const years: number[] = [];
  for (let y = currentYear; y >= currentYear - 18; y--) {
    years.push(y);
  }
  return years;
}

const MONTHS = [
  { value: '1', label: 'January' },
  { value: '2', label: 'February' },
  { value: '3', label: 'March' },
  { value: '4', label: 'April' },
  { value: '5', label: 'May' },
  { value: '6', label: 'June' },
  { value: '7', label: 'July' },
  { value: '8', label: 'August' },
  { value: '9', label: 'September' },
  { value: '10', label: 'October' },
  { value: '11', label: 'November' },
  { value: '12', label: 'December' },
];

const DAYS = Array.from({ length: 31 }, (_, i) => String(i + 1));
const YEARS = generateYears();

// ─── sub-components ─────────────────────────────────────────────────────────

interface NumpadProps {
  value: string;
  maxLength: number;
  onChange: (value: string) => void;
}

function Numpad({ value, maxLength, onChange }: NumpadProps) {
  const handleDigit = (digit: string) => {
    if (value.length < maxLength) {
      onChange(value + digit);
    }
  };

  const digits = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '0'];

  return (
    <div className="grid grid-cols-3 gap-3">
      {digits.map((digit) => (
        <button
          key={digit}
          type="button"
          onClick={() => handleDigit(digit)}
          className="bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white text-3xl font-bold rounded-xl py-5 min-h-[72px] transition-colors focus:outline-none focus:ring-4 focus:ring-blue-300"
        >
          {digit}
        </button>
      ))}

      <button
        type="button"
        onClick={() => onChange('')}
        className="bg-gray-300 hover:bg-gray-400 active:bg-gray-500 text-gray-900 text-xl font-semibold rounded-xl py-5 min-h-[72px] transition-colors focus:outline-none focus:ring-4 focus:ring-gray-300"
      >
        Clear
      </button>

      <button
        type="button"
        onClick={() => onChange(value.slice(0, -1))}
        className="bg-gray-300 hover:bg-gray-400 active:bg-gray-500 text-gray-900 text-xl font-semibold rounded-xl py-5 min-h-[72px] transition-colors focus:outline-none focus:ring-4 focus:ring-gray-300"
      >
        ⌫
      </button>
    </div>
  );
}

interface FieldErrorProps {
  message?: string;
}

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-red-600 text-base mt-1">{message}</p>;
}

// ─── main component ──────────────────────────────────────────────────────────

export function KioskFamilyRegistration({
  onComplete,
  onCancel,
  defaultPhone = '',
}: KioskFamilyRegistrationProps) {
  const [regStep, setRegStep] = useState<RegistrationStep>('parent');

  const [parent, setParent] = useState<ParentDraft>({
    firstName: '',
    lastName: '',
    // Strip any non-digit characters from a pre-filled phone so numpad state
    // is consistent with the 10-digit raw format we maintain internally.
    phone: defaultPhone.replace(/\D/g, '').slice(0, 10),
  });

  const [children, setChildren] = useState<ChildDraft[]>([emptyChild('')]);
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);

  // ── parent step validation ──────────────────────────────────────────────

  function validateParent(): ValidationErrors {
    const errs: ValidationErrors = {};
    if (!parent.firstName.trim()) {
      errs.parentFirstName = 'First name is required.';
    } else if (parent.firstName.trim().length > 100) {
      errs.parentFirstName = 'First name must be 100 characters or fewer.';
    }
    if (!parent.lastName.trim()) {
      errs.parentLastName = 'Last name is required.';
    } else if (parent.lastName.trim().length > 100) {
      errs.parentLastName = 'Last name must be 100 characters or fewer.';
    }
    if (parent.phone.length !== 10) {
      errs.phone = 'Please enter a 10-digit phone number.';
    }
    return errs;
  }

  function handleParentNext() {
    const errs = validateParent();
    if (Object.keys(errs).length > 0) {
      setErrors(errs);
      return;
    }
    setErrors({});
    // Pre-fill child last names with parent's last name when first advancing
    setChildren((prev) =>
      prev.map((c) => (c.lastName === '' ? { ...c, lastName: parent.lastName.trim() } : c))
    );
    setRegStep('children');
  }

  // ── children step validation ────────────────────────────────────────────

  function validateChildren(): ValidationErrors {
    const childErrors: Record<number, { firstName?: string }> = {};
    children.forEach((child, idx) => {
      if (!child.firstName.trim()) {
        childErrors[idx] = { firstName: 'First name is required.' };
      } else if (child.firstName.trim().length > 100) {
        childErrors[idx] = { firstName: 'First name must be 100 characters or fewer.' };
      }
    });
    return Object.keys(childErrors).length > 0 ? { children: childErrors } : {};
  }

  function handleChildrenNext() {
    const errs = validateChildren();
    if (Object.keys(errs).length > 0) {
      setErrors(errs);
      return;
    }
    setErrors({});
    setRegStep('review');
  }

  function handleAddChild() {
    setChildren((prev) => [...prev, emptyChild(parent.lastName.trim())]);
  }

  function handleRemoveChild(id: string) {
    setChildren((prev) => prev.filter((c) => c.id !== id));
    // Clear all child validation errors after removal — indices are recomputed
    // on the next validation pass so stale index keys are not meaningful.
    setErrors((prev) => (prev.children ? { ...prev, children: {} } : prev));
  }

  function updateChild(id: string, patch: Partial<ChildDraft>) {
    setChildren((prev) => prev.map((c) => (c.id === id ? { ...c, ...patch } : c)));
  }

  // ── submit ──────────────────────────────────────────────────────────────

  async function handleSubmit() {
    setApiError(null);
    setSubmitting(true);

    const childPayloads: KioskChildRegistrationRequest[] = children.map((c) => ({
      firstName: c.firstName.trim(),
      lastName: c.lastName.trim() || undefined,
      birthDate: buildBirthDate(c),
    }));

    try {
      const family = await registerFamily({
        parentFirstName: parent.firstName.trim(),
        parentLastName: parent.lastName.trim(),
        phoneNumber: parent.phone,
        children: childPayloads,
      });
      onComplete(family);
    } catch (err) {
      setApiError(
        err instanceof Error
          ? err.message
          : 'Registration failed. Please try again or see a volunteer for help.'
      );
      setSubmitting(false);
    }
  }

  // ── render helpers ──────────────────────────────────────────────────────

  function renderStepIndicator() {
    const steps: { key: RegistrationStep; label: string }[] = [
      { key: 'parent', label: 'Parent Info' },
      { key: 'children', label: 'Children' },
      { key: 'review', label: 'Review' },
    ];
    const currentIndex = steps.findIndex((s) => s.key === regStep);
    return (
      <div className="flex items-center justify-center gap-2 mb-8">
        {steps.map((s, i) => (
          <div key={s.key} className="flex items-center gap-2">
            <div
              className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold ${
                i <= currentIndex
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-200 text-gray-500'
              }`}
            >
              {i + 1}
            </div>
            <span
              className={`text-sm font-medium hidden sm:inline ${
                i <= currentIndex ? 'text-blue-700' : 'text-gray-400'
              }`}
            >
              {s.label}
            </span>
            {i < steps.length - 1 && (
              <div
                className={`w-8 h-0.5 ${
                  i < currentIndex ? 'bg-blue-600' : 'bg-gray-200'
                }`}
              />
            )}
          </div>
        ))}
      </div>
    );
  }

  // ── step: parent ────────────────────────────────────────────────────────

  function renderParentStep() {
    return (
      <div className="bg-white rounded-2xl shadow-xl p-8 max-w-2xl mx-auto">
        <h2 className="text-3xl font-bold text-center mb-2 text-gray-900">
          Parent Information
        </h2>
        <p className="text-center text-gray-600 mb-8">
          Tell us about the parent or guardian
        </p>

        {/* First Name */}
        <div className="mb-6">
          <label className="block text-2xl font-semibold text-gray-800 mb-2">
            First Name
          </label>
          <input
            type="text"
            value={parent.firstName}
            onChange={(e) => setParent((p) => ({ ...p, firstName: e.target.value }))}
            placeholder="First name..."
            className="w-full border-2 border-gray-300 rounded-lg px-4 text-2xl text-gray-900 min-h-[56px] focus:outline-none focus:border-blue-500"
          />
          <FieldError message={errors.parentFirstName} />
        </div>

        {/* Last Name */}
        <div className="mb-6">
          <label className="block text-2xl font-semibold text-gray-800 mb-2">
            Last Name
          </label>
          <input
            type="text"
            value={parent.lastName}
            onChange={(e) => setParent((p) => ({ ...p, lastName: e.target.value }))}
            placeholder="Last name..."
            className="w-full border-2 border-gray-300 rounded-lg px-4 text-2xl text-gray-900 min-h-[56px] focus:outline-none focus:border-blue-500"
          />
          <FieldError message={errors.parentLastName} />
        </div>

        {/* Phone */}
        <div className="mb-8">
          <label className="block text-2xl font-semibold text-gray-800 mb-2">
            Phone Number
          </label>
          <div className="bg-gray-100 rounded-lg p-4 text-center mb-4">
            <p className="text-4xl font-mono font-bold text-gray-900 min-h-[3rem]">
              {parent.phone ? formatPhone(parent.phone) : '\u00A0'}
            </p>
          </div>
          <Numpad
            value={parent.phone}
            maxLength={10}
            onChange={(val) => setParent((p) => ({ ...p, phone: val }))}
          />
          <FieldError message={errors.phone} />
        </div>

        <div className="flex gap-4">
          <Button
            type="button"
            variant="outline"
            size="lg"
            onClick={onCancel}
            className="flex-1 text-xl"
          >
            Back to Search
          </Button>
          <Button
            type="button"
            size="lg"
            onClick={handleParentNext}
            className="flex-1 text-xl"
          >
            Next: Children
          </Button>
        </div>
      </div>
    );
  }

  // ── step: children ──────────────────────────────────────────────────────

  function renderChildrenStep() {
    return (
      <div className="bg-white rounded-2xl shadow-xl p-8 max-w-2xl mx-auto">
        <h2 className="text-3xl font-bold text-center mb-2 text-gray-900">
          Children
        </h2>
        <p className="text-center text-gray-600 mb-8">
          Add each child you would like to register
        </p>

        <div className="space-y-6 mb-6">
          {children.map((child, idx) => (
            <ChildEntry
              key={child.id}
              index={idx}
              child={child}
              showRemove={children.length > 1}
              firstNameError={errors.children?.[idx]?.firstName}
              onChange={(patch) => updateChild(child.id, patch)}
              onRemove={() => handleRemoveChild(child.id)}
            />
          ))}
        </div>

        {/* Add Child */}
        <Button
          type="button"
          variant="outline"
          size="lg"
          onClick={handleAddChild}
          className="w-full text-xl mb-8"
        >
          + Add Another Child
        </Button>

        <div className="flex gap-4">
          <Button
            type="button"
            variant="outline"
            size="lg"
            onClick={() => { setErrors({}); setRegStep('parent'); }}
            className="flex-1 text-xl"
          >
            Back
          </Button>
          <Button
            type="button"
            size="lg"
            onClick={handleChildrenNext}
            className="flex-1 text-xl"
          >
            Review
          </Button>
        </div>
      </div>
    );
  }

  // ── step: review ────────────────────────────────────────────────────────

  function renderReviewStep() {
    return (
      <div className="bg-white rounded-2xl shadow-xl p-8 max-w-2xl mx-auto">
        <h2 className="text-3xl font-bold text-center mb-2 text-gray-900">
          Review &amp; Register
        </h2>
        <p className="text-center text-gray-600 mb-8">
          Please confirm the information below
        </p>

        {/* Parent summary */}
        <Card className="mb-4 bg-gray-50">
          <p className="text-xl font-bold text-gray-700 mb-2">Parent / Guardian</p>
          <p className="text-2xl text-gray-900">
            {parent.firstName.trim()} {parent.lastName.trim()}
          </p>
          <p className="text-xl text-gray-600">{formatPhone(parent.phone)}</p>
        </Card>

        {/* Children summary */}
        <Card className="mb-8 bg-gray-50">
          <p className="text-xl font-bold text-gray-700 mb-3">
            {children.length === 1 ? 'Child' : 'Children'}
          </p>
          <ul className="space-y-3">
            {children.map((child, idx) => {
              const lastName = child.lastName.trim() || parent.lastName.trim();
              const birthDate = buildBirthDate(child);
              return (
                <li key={child.id} className="flex items-start gap-2">
                  <span className="mt-1 w-6 h-6 bg-blue-100 text-blue-700 rounded-full text-sm font-bold flex items-center justify-center flex-shrink-0">
                    {idx + 1}
                  </span>
                  <div>
                    <p className="text-xl text-gray-900 font-medium">
                      {child.firstName.trim()} {lastName}
                    </p>
                    {birthDate && (
                      <p className="text-gray-500">
                        {/*
                         * Append T00:00:00 (local midnight) before parsing.
                         * Without it, Date() treats a bare YYYY-MM-DD as UTC
                         * midnight, which shifts the displayed date one day
                         * earlier for users in US time zones (UTC-5 to UTC-8).
                         */}
                        Born: {new Date(birthDate + 'T00:00:00').toLocaleDateString()}
                      </p>
                    )}
                  </div>
                </li>
              );
            })}
          </ul>
        </Card>

        {/* API error */}
        {apiError && (
          <Card className="bg-red-50 border border-red-200 mb-6">
            <p className="text-red-800 font-medium text-center">{apiError}</p>
          </Card>
        )}

        <div className="flex gap-4">
          <Button
            type="button"
            variant="outline"
            size="lg"
            onClick={() => { setApiError(null); setRegStep('children'); }}
            className="flex-1 text-xl"
            disabled={submitting}
          >
            Back
          </Button>
          <Button
            type="button"
            size="lg"
            loading={submitting}
            onClick={handleSubmit}
            className="flex-1 text-xl"
          >
            {submitting ? 'Registering...' : 'Complete Registration'}
          </Button>
        </div>
      </div>
    );
  }

  // ── render ──────────────────────────────────────────────────────────────

  return (
    <div className="max-w-2xl mx-auto">
      {renderStepIndicator()}
      {regStep === 'parent' && renderParentStep()}
      {regStep === 'children' && renderChildrenStep()}
      {regStep === 'review' && renderReviewStep()}
    </div>
  );
}

// ─── ChildEntry ──────────────────────────────────────────────────────────────

interface ChildEntryProps {
  index: number;
  child: ChildDraft;
  showRemove: boolean;
  firstNameError?: string;
  onChange: (patch: Partial<ChildDraft>) => void;
  onRemove: () => void;
}

function ChildEntry({
  index,
  child,
  showRemove,
  firstNameError,
  onChange,
  onRemove,
}: ChildEntryProps) {
  return (
    <div className="border-2 border-gray-200 rounded-xl p-6">
      <div className="flex items-center justify-between mb-4">
        <p className="text-xl font-semibold text-gray-800">Child {index + 1}</p>
        {showRemove && (
          <button
            type="button"
            onClick={onRemove}
            className="text-red-600 hover:text-red-800 text-base font-medium min-h-[44px] px-3"
          >
            Remove
          </button>
        )}
      </div>

      {/* First Name */}
      <div className="mb-4">
        <label className="block text-xl font-semibold text-gray-700 mb-1">
          First Name <span className="text-red-500">*</span>
        </label>
        <input
          type="text"
          value={child.firstName}
          onChange={(e) => onChange({ firstName: e.target.value })}
          placeholder="First name..."
          className="w-full border-2 border-gray-300 rounded-lg px-4 text-xl text-gray-900 min-h-[56px] focus:outline-none focus:border-blue-500"
        />
        <FieldError message={firstNameError} />
      </div>

      {/* Last Name */}
      <div className="mb-4">
        <label className="block text-xl font-semibold text-gray-700 mb-1">
          Last Name <span className="text-gray-400 text-base font-normal">(optional — defaults to parent's)</span>
        </label>
        <input
          type="text"
          value={child.lastName}
          onChange={(e) => onChange({ lastName: e.target.value })}
          placeholder="Last name..."
          className="w-full border-2 border-gray-300 rounded-lg px-4 text-xl text-gray-900 min-h-[56px] focus:outline-none focus:border-blue-500"
        />
      </div>

      {/* Birth Date */}
      <div>
        <label className="block text-xl font-semibold text-gray-700 mb-1">
          Birth Date <span className="text-gray-400 text-base font-normal">(optional)</span>
        </label>
        <div className="grid grid-cols-3 gap-3">
          <select
            value={child.birthMonth}
            onChange={(e) => onChange({ birthMonth: e.target.value })}
            className="border-2 border-gray-300 rounded-lg px-2 text-lg text-gray-900 min-h-[56px] focus:outline-none focus:border-blue-500 bg-white"
          >
            <option value="">Month</option>
            {MONTHS.map((m) => (
              <option key={m.value} value={m.value}>
                {m.label}
              </option>
            ))}
          </select>

          <select
            value={child.birthDay}
            onChange={(e) => onChange({ birthDay: e.target.value })}
            className="border-2 border-gray-300 rounded-lg px-2 text-lg text-gray-900 min-h-[56px] focus:outline-none focus:border-blue-500 bg-white"
          >
            <option value="">Day</option>
            {DAYS.map((d) => (
              <option key={d} value={d}>
                {d}
              </option>
            ))}
          </select>

          <select
            value={child.birthYear}
            onChange={(e) => onChange({ birthYear: e.target.value })}
            className="border-2 border-gray-300 rounded-lg px-2 text-lg text-gray-900 min-h-[56px] focus:outline-none focus:border-blue-500 bg-white"
          >
            <option value="">Year</option>
            {YEARS.map((y) => (
              <option key={y} value={String(y)}>
                {y}
              </option>
            ))}
          </select>
        </div>
      </div>
    </div>
  );
}
