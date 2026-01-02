/**
 * Dashboard API service
 */

import { get } from './client';
import type { DashboardStatsDto, UpcomingScheduleDto } from '@/types';

// Re-export types for backwards compatibility
export type { DashboardStatsDto, UpcomingScheduleDto };

export async function getDashboardStats(): Promise<DashboardStatsDto> {
  const response = await get<{ data: DashboardStatsDto }>('/dashboard/stats');
  return response.data;
}
