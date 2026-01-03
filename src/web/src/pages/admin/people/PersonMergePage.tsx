/**
 * Person Merge Page
 * Wrapper page for the merge wizard
 */

import { useSearchParams } from 'react-router-dom';
import { PersonMergeWizard } from '@/components/admin/people/PersonMergeWizard';

export function PersonMergePage() {
  const [searchParams] = useSearchParams();
  const person1 = searchParams.get('person1');
  const person2 = searchParams.get('person2');

  if (!person1 || !person2) {
    return (
      <div className="space-y-6">
        <h1 className="text-3xl font-bold text-gray-900">Merge People</h1>
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <p className="text-yellow-800">
            Missing person IDs. Please select two people to merge.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold text-gray-900">Merge People</h1>
      <PersonMergeWizard person1IdKey={person1} person2IdKey={person2} />
    </div>
  );
}
