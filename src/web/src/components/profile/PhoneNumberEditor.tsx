/**
 * PhoneNumberEditor
 * Edit phone numbers with add/edit/remove functionality
 */

import { useState } from 'react';
import type { PhoneNumberDto, PhoneNumberRequestDto } from '@/types/profile';

interface PhoneNumberEditorProps {
  phoneNumbers: PhoneNumberDto[];
  onChange: (phoneNumbers: PhoneNumberRequestDto[]) => void;
  disabled?: boolean;
}

interface EditablePhoneNumber extends PhoneNumberRequestDto {
  tempId?: string;
  isNew?: boolean;
}

export function PhoneNumberEditor({
  phoneNumbers,
  onChange,
  disabled = false,
}: PhoneNumberEditorProps) {
  const [editableNumbers, setEditableNumbers] = useState<EditablePhoneNumber[]>(
    phoneNumbers.map((p) => ({
      idKey: p.idKey,  // Preserve idKey from existing phones
      number: p.number,
      extension: p.extension,
      // Preserve existing phone type but don't allow editing (no DefinedValues API yet)
      phoneTypeIdKey: p.phoneType?.idKey,
      isMessagingEnabled: p.isMessagingEnabled,
      isUnlisted: p.isUnlisted,
    }))
  );

  const handleAdd = () => {
    const newNumber: EditablePhoneNumber = {
      // No idKey for new phones - backend will create new record
      number: '',
      isMessagingEnabled: true,
      isNew: true,
      tempId: Date.now().toString(),
    };
    const updated = [...editableNumbers, newNumber];
    setEditableNumbers(updated);
  };

  const handleRemove = (index: number) => {
    const updated = editableNumbers.filter((_, i) => i !== index);
    setEditableNumbers(updated);
    onChange(updated);
  };

  const handleChange = (index: number, field: keyof EditablePhoneNumber, value: string | boolean) => {
    const updated = [...editableNumbers];
    updated[index] = { ...updated[index], [field]: value };
    setEditableNumbers(updated);
    onChange(updated);
  };

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <label className="block text-sm font-medium text-gray-700">Phone Numbers</label>
        <button
          type="button"
          onClick={handleAdd}
          disabled={disabled}
          className="text-sm text-primary-600 hover:text-primary-700 font-medium disabled:opacity-50"
        >
          + Add Phone
        </button>
      </div>

      {editableNumbers.length === 0 ? (
        <div className="text-sm text-gray-500 italic">No phone numbers</div>
      ) : (
        <div className="space-y-3">
          {editableNumbers.map((phone, index) => (
            <div
              key={phone.tempId || index}
              className="border border-gray-200 rounded-lg p-3 space-y-2"
            >
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">
                  Number
                </label>
                <input
                  type="tel"
                  value={phone.number}
                  onChange={(e) => handleChange(index, 'number', e.target.value)}
                  disabled={disabled}
                  placeholder="(555) 123-4567"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-100"
                />
              </div>

              <div className="flex items-center justify-between">
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={phone.isMessagingEnabled || false}
                    onChange={(e) => handleChange(index, 'isMessagingEnabled', e.target.checked)}
                    disabled={disabled}
                    className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                  />
                  <span className="text-sm text-gray-700">Enable SMS</span>
                </label>

                <button
                  type="button"
                  onClick={() => handleRemove(index)}
                  disabled={disabled}
                  className="text-sm text-red-600 hover:text-red-700 font-medium disabled:opacity-50"
                >
                  Remove
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
