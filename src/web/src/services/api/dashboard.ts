/**
 * Dashboard API service
 */

import { get } from './client';

export interface DashboardStats {
  totalPeople: number;
  totalFamilies: number;
  activeGroups: number;
  todayCheckIns: number;
  lastWeekCheckIns: number;
  activeSchedules: number;
  upcomingSchedules: UpcomingSchedule[];
}

export interface UpcomingSchedule {
  idKey: string;
  name: string;
  nextOccurrence: string;
  minutesUntilCheckIn: number;
}

export async function getDashboardStats(): Promise<DashboardStats> {
  const response = await get<{ data: DashboardStats }>('/dashboard/stats');
  return response.data;
}
