/**
 * Audit log hooks using TanStack Query
 */

import { useQuery, useMutation } from '@tanstack/react-query';
import * as auditLogApi from '@/services/api/auditLogApi';
import type {
  AuditLogSearchParams,
  AuditLogExportParams,
} from '@/services/api/types';

/**
 * Search audit logs with filters and pagination
 */
export function useAuditLogs(params: AuditLogSearchParams = {}) {
  return useQuery({
    queryKey: ['audit-logs', params],
    queryFn: () => auditLogApi.searchAuditLogs(params),
    staleTime: 2 * 60 * 1000, // 2 minutes - audit logs don't change often
  });
}

/**
 * Get audit history for a specific entity
 */
export function useEntityAuditHistory(entityType?: string, idKey?: string) {
  return useQuery({
    queryKey: ['audit-logs', entityType, idKey],
    queryFn: () => auditLogApi.getEntityAuditHistory(entityType!, idKey!),
    enabled: !!entityType && !!idKey,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

/**
 * Export audit logs to file
 * Returns mutation that triggers download
 */
export function useExportAuditLogs() {
  return useMutation({
    mutationFn: (params: AuditLogExportParams) => auditLogApi.exportAuditLogs(params),
    onSuccess: (blob, variables) => {
      // Create download link
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      
      // Determine file extension based on format
      const format = variables.format || 'Csv';
      const extension = format === 'Csv' ? 'csv' : format === 'Json' ? 'json' : 'xlsx';
      const timestamp = new Date().toISOString().split('T')[0];
      
      link.download = `audit-logs-${timestamp}.${extension}`;
      document.body.appendChild(link);
      link.click();
      
      // Cleanup
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    },
  });
}
