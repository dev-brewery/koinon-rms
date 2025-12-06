import React, { useState } from 'react';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';

export interface FamilySearchProps {
  onSearch: (name: string) => void;
  loading?: boolean;
}

/**
 * Search by name alternative to phone search
 */
export function FamilySearch({ onSearch, loading }: FamilySearchProps) {
  const [name, setName] = useState('');

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (name.trim().length >= 2) {
      onSearch(name.trim());
    }
  };

  return (
    <div className="max-w-2xl mx-auto">
      <div className="bg-white rounded-2xl shadow-xl p-8">
        <h2 className="text-3xl font-bold text-center mb-2 text-gray-900">
          Search by Name
        </h2>
        <p className="text-center text-gray-600 mb-8">
          Enter your last name or family name
        </p>

        <form onSubmit={handleSearch} className="space-y-6">
          <Input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Last name..."
            className="text-2xl py-6"
            autoFocus
          />

          <Button
            type="submit"
            disabled={name.trim().length < 2}
            loading={loading}
            size="lg"
            className="w-full text-xl"
          >
            Search
          </Button>

          <p className="text-center text-sm text-gray-500">
            Enter at least 2 characters to search
          </p>
        </form>
      </div>
    </div>
  );
}
