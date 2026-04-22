import type { CapacityPillColor } from '@/services/api/checkinOperations';

interface CapacityPillProps {
  color: CapacityPillColor;
  checkedInCount: number;
  capacity?: number;
  percentFull: number;
  isOpen: boolean;
}

const PILL_CLASSES: Record<CapacityPillColor, string> = {
  green: 'bg-green-100 text-green-800 border-green-300',
  yellow: 'bg-yellow-100 text-yellow-800 border-yellow-300',
  red: 'bg-red-100 text-red-800 border-red-300',
  grey: 'bg-gray-100 text-gray-600 border-gray-300',
};

export function CapacityPill({
  color,
  checkedInCount,
  capacity,
  percentFull,
  isOpen,
}: CapacityPillProps) {
  const label = !isOpen
    ? 'Closed'
    : capacity
    ? `${checkedInCount} / ${capacity} (${percentFull}%)`
    : `${checkedInCount} checked in`;

  return (
    <span
      data-testid="checkin-ops-capacity-pill"
      data-color={color}
      className={`inline-flex items-center rounded-full border px-3 py-1 text-sm font-semibold ${PILL_CLASSES[color]}`}
    >
      {label}
    </span>
  );
}
