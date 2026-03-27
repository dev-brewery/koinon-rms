/**
 * Campus Step
 * Step 2: Create the first campus
 */

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { useCreateCampus } from '@/hooks/useCampuses';
import { useToast } from '@/contexts/ToastContext';

const campusSchema = z.object({
  name: z.string().min(1, 'Campus name is required').max(100),
  street1: z.string().max(200).optional(),
  city: z.string().max(100).optional(),
  state: z.string().max(50).optional(),
  postalCode: z.string().max(20).optional(),
});

type CampusFormData = z.infer<typeof campusSchema>;

interface CampusStepProps {
  onNext: (campusIdKey: string, campusName: string) => void;
  onSkip: () => void;
  onBack: () => void;
}

export function CampusStep({ onNext, onSkip, onBack }: CampusStepProps) {
  const createCampus = useCreateCampus();
  const { error: showError } = useToast();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CampusFormData>({
    resolver: zodResolver(campusSchema),
    defaultValues: {
      name: '',
      street1: '',
      city: '',
      state: '',
      postalCode: '',
    },
  });

  const onSubmit = async (data: CampusFormData) => {
    try {
      const result = await createCampus.mutateAsync({
        name: data.name,
      });
      onNext(result.idKey, result.name);
    } catch {
      showError('Error', 'Failed to create campus. Please try again.');
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <div className="flex items-center gap-3 mb-2">
          <div className="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center flex-shrink-0">
            <svg className="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
            </svg>
          </div>
          <h2 className="text-xl font-bold text-gray-900">Create Your Campus</h2>
        </div>
        <p className="text-gray-600">
          A campus represents your physical church location. You can add more campuses later from Settings.
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <Input
          label="Campus Name"
          placeholder="e.g. Main Campus, North Campus"
          error={errors.name?.message}
          {...register('name')}
        />

        <div>
          <p className="text-sm font-medium text-gray-700 mb-3">Address (optional)</p>
          <div className="space-y-3">
            <Input
              placeholder="Street address"
              error={errors.street1?.message}
              {...register('street1')}
            />
            <div className="grid grid-cols-2 gap-3">
              <Input
                placeholder="City"
                error={errors.city?.message}
                {...register('city')}
              />
              <Input
                placeholder="State"
                error={errors.state?.message}
                {...register('state')}
              />
            </div>
            <Input
              placeholder="ZIP / Postal code"
              error={errors.postalCode?.message}
              {...register('postalCode')}
            />
          </div>
        </div>

        <div className="flex items-center justify-between pt-4">
          <Button type="button" variant="outline" onClick={onBack}>
            Back
          </Button>
          <div className="flex items-center gap-3">
            <Button type="button" variant="ghost" onClick={onSkip}>
              Skip for now
            </Button>
            <Button type="submit" loading={createCampus.isPending}>
              Create Campus
            </Button>
          </div>
        </div>
      </form>
    </div>
  );
}
