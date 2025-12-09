/**
 * Communication analytics API service
 */

import { get } from '@/services/api/client';
import type { IdKey } from '@/services/api/types';

// ============================================================================
// Types
// ============================================================================

export interface CommunicationAnalyticsDto {
  idKey: IdKey;
  communicationType: string;
  totalRecipients: number;
  sent: number;
  delivered: number;
  failed: number;
  opened: number;
  clicked: number;
  openRate: number;
  clickRate: number;
  clickThroughRate: number;
  deliveryRate: number;
  statusBreakdown: RecipientStatusBreakdownDto;
  sentDateTime?: string;
}

export interface RecipientStatusBreakdownDto {
  pending: number;
  delivered: number;
  failed: number;
  opened: number;
}

export interface AnalyticsSummaryDto {
  totalCommunications: number;
  totalRecipients: number;
  totalDelivered: number;
  totalFailed: number;
  totalOpened: number;
  totalClicked: number;
  deliveryRate: number;
  openRate: number;
  clickRate: number;
  byType: ByTypeBreakdownDto;
  startDate: string;
  endDate: string;
}

export interface ByTypeBreakdownDto {
  email: TypeStatsDto;
  sms: TypeStatsDto;
}

export interface TypeStatsDto {
  count: number;
  recipients: number;
  delivered: number;
  opened: number;
  clicked: number;
}

// ============================================================================
// API Functions
// ============================================================================

/**
 * Get detailed analytics for a single communication
 */
export async function getCommunicationAnalytics(
  communicationIdKey: IdKey
): Promise<CommunicationAnalyticsDto> {
  return get<CommunicationAnalyticsDto>(
    `/communications/${communicationIdKey}/analytics`
  );
}

/**
 * Get aggregate analytics summary for a time period
 */
export async function getAnalyticsSummary(params: {
  startDate?: Date;
  endDate?: Date;
  type?: string;
}): Promise<AnalyticsSummaryDto> {
  const queryParams = new URLSearchParams();

  if (params.startDate) {
    queryParams.append('startDate', params.startDate.toISOString());
  }
  if (params.endDate) {
    queryParams.append('endDate', params.endDate.toISOString());
  }
  if (params.type) {
    queryParams.append('type', params.type);
  }

  const query = queryParams.toString();
  const endpoint = `/communications/analytics/summary${query ? `?${query}` : ''}`;

  return get<AnalyticsSummaryDto>(endpoint);
}
