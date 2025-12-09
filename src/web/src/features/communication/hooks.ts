/**
 * React hooks for communication analytics
 */

import { useQuery } from '@tanstack/react-query';
import type { IdKey } from '@/services/api/types';
import {
  getCommunicationAnalytics,
  getAnalyticsSummary,
  type CommunicationAnalyticsDto,
  type AnalyticsSummaryDto,
} from './api';

/**
 * Hook to fetch analytics for a single communication
 */
export function useCommunicationAnalytics(communicationIdKey: IdKey) {
  return useQuery<CommunicationAnalyticsDto>({
    queryKey: ['communication-analytics', communicationIdKey],
    queryFn: () => getCommunicationAnalytics(communicationIdKey),
    enabled: !!communicationIdKey,
  });
}

/**
 * Hook to fetch analytics summary for a time period
 */
export function useAnalyticsSummary(params: {
  startDate?: Date;
  endDate?: Date;
  type?: string;
}) {
  return useQuery<AnalyticsSummaryDto>({
    queryKey: ['analytics-summary', params],
    queryFn: () => getAnalyticsSummary(params),
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
  });
}
