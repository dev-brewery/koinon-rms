/**
 * Locations Step
 * Step 3: Add check-in rooms for the campus
 */

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { useCreateLocation } from '@/hooks/useLocations';
import { useToast } from '@/contexts/ToastContext';

const roomSchema = z.object({
  name: z.string().min(1, 'Room name is required').max(100),
  capacity: z.string().optional(),
});

type RoomFormData = z.infer<typeof roomSchema>;

interface AddedRoom {
  idKey: string;
  name: string;
  capacity?: string;
}

interface LocationsStepProps {
  campusIdKey: string | null;
  campusName: string | null;
  onNext: (locationIdKeys: string[], locationNames: string[]) => void;
  onSkip: () => void;
  onBack: () => void;
}

const MAX_ROOMS = 3;

export function LocationsStep({ campusIdKey, campusName, onNext, onSkip, onBack }: LocationsStepProps) {
  const [addedRooms, setAddedRooms] = useState<AddedRoom[]>([]);
  const createLocation = useCreateLocation();
  const { error: showError } = useToast();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<RoomFormData>({
    resolver: zodResolver(roomSchema),
    defaultValues: { name: '', capacity: '' },
  });

  const onAddRoom = async (data: RoomFormData) => {
    try {
      const result = await createLocation.mutateAsync({
        name: data.name,
        campusIdKey: campusIdKey ?? undefined,
        softRoomThreshold: data.capacity ? parseInt(data.capacity, 10) : undefined,
      });
      setAddedRooms(prev => [...prev, { idKey: result.idKey, name: data.name, capacity: data.capacity }]);
      reset({ name: '', capacity: '' });
    } catch {
      showError('Error', 'Failed to create room. Please try again.');
    }
  };

  const handleNext = () => {
    onNext(
      addedRooms.map(r => r.idKey),
      addedRooms.map(r => r.name)
    );
  };

  const canAddMore = addedRooms.length < MAX_ROOMS;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <div className="flex items-center gap-3 mb-2">
          <div className="w-10 h-10 bg-purple-100 rounded-full flex items-center justify-center flex-shrink-0">
            <svg className="w-5 h-5 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
          </div>
          <h2 className="text-xl font-bold text-gray-900">Add Check-in Rooms</h2>
        </div>
        {campusName && (
          <p className="text-gray-600">
            Adding rooms to <span className="font-medium">{campusName}</span>. Add up to {MAX_ROOMS} rooms now
            — you can add more later from Settings.
          </p>
        )}
        {!campusName && (
          <p className="text-gray-600">
            Add up to {MAX_ROOMS} check-in rooms. You can add more later from Settings.
          </p>
        )}
      </div>

      {/* Added rooms list */}
      {addedRooms.length > 0 && (
        <div className="space-y-2">
          <p className="text-sm font-medium text-gray-700">Rooms added ({addedRooms.length}/{MAX_ROOMS}):</p>
          <ul className="space-y-2">
            {addedRooms.map(room => (
              <li key={room.idKey} className="flex items-center gap-3 bg-green-50 border border-green-200 rounded-lg px-4 py-3">
                <svg className="w-4 h-4 text-green-600 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
                <span className="text-sm font-medium text-gray-900">{room.name}</span>
                {room.capacity && (
                  <span className="text-sm text-gray-500">Capacity: {room.capacity}</span>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Add room form */}
      {canAddMore && (
        <form onSubmit={handleSubmit(onAddRoom)} className="space-y-3 border border-gray-200 rounded-lg p-4 bg-gray-50">
          <p className="text-sm font-medium text-gray-700">
            {addedRooms.length === 0 ? 'Add your first room:' : 'Add another room:'}
          </p>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div className="sm:col-span-2">
              <Input
                placeholder='e.g. Nursery, Preschool, Elementary'
                error={errors.name?.message}
                {...register('name')}
              />
            </div>
            <Input
              type="number"
              placeholder="Capacity (optional)"
              min={1}
              max={999}
              error={errors.capacity?.message}
              {...register('capacity')}
            />
          </div>
          <Button type="submit" variant="outline" size="sm" loading={createLocation.isPending}>
            Add Room
          </Button>
        </form>
      )}

      {!canAddMore && (
        <p className="text-sm text-gray-500 italic">
          Maximum of {MAX_ROOMS} rooms added. You can add more rooms from Settings after setup.
        </p>
      )}

      {/* Navigation */}
      <div className="flex items-center justify-between pt-2">
        <Button type="button" variant="outline" onClick={onBack}>
          Back
        </Button>
        <div className="flex items-center gap-3">
          {addedRooms.length === 0 && (
            <Button type="button" variant="ghost" onClick={onSkip}>
              Skip for now
            </Button>
          )}
          {addedRooms.length > 0 && (
            <Button type="button" onClick={handleNext}>
              Continue
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
