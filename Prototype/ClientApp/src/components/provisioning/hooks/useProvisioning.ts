// Custom hook for user provisioning business logic
// Separates provisioning logic from UI components following SRP

import { useState, useCallback } from 'react';
import { userProvisioningApi } from '../../../services/api';
import type { ApiResponse, PaginatedResponse } from '../../../types/api.types';

interface ProvisioningOverview {
  totalPendingRequests: number;
  totalProvisionedUsers: number;
  totalFailedRequests: number;
  lastProvisioningRun?: string;
  autoProvisioningEnabled: boolean;
}

interface PendingRequest {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  requestedRole: string;
  requestDate: string;
  status: 'pending' | 'approved' | 'rejected';
  requestedBy?: string;
}

interface ProvisioningTemplate {
  templateId: string;
  templateName: string;
  roleId: string;
  roleName: string;
  defaultApplications: string[];
  autoApprovalEnabled: boolean;
}

export const useProvisioning = () => {
  const [overview, setOverview] = useState<ProvisioningOverview | null>(null);
  const [pendingRequests, setPendingRequests] = useState<PendingRequest[]>([]);
  const [templates, setTemplates] = useState<ProvisioningTemplate[]>([]);
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 1
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Fetch provisioning overview data
  const fetchOverview = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await userProvisioningApi.getProvisioningOverview();
      
      if (response.success && response.data) {
        setOverview(response.data);
      } else {
        setError('Failed to fetch provisioning overview');
      }
    } catch (err: any) {
      console.error('Error fetching provisioning overview:', err);
      setError(err.message || 'Network error occurred');
    } finally {
      setLoading(false);
    }
  }, []);

  // Fetch pending provisioning requests with pagination
  const fetchPendingRequests = useCallback(async (page: number = 1, pageSize: number = 10) => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await userProvisioningApi.getPendingRequests(page, pageSize);
      
      if (response.success && response.data?.data) {
        setPendingRequests(response.data.data);
        setPagination({
          page: response.data.page || page,
          pageSize: response.data.pageSize || pageSize,
          totalCount: response.data.totalCount || 0,
          totalPages: response.data.totalPages || 1
        });
      } else {
        setError('Failed to fetch pending requests');
        setPendingRequests([]);
      }
    } catch (err: any) {
      console.error('Error fetching pending requests:', err);
      setError(err.message || 'Network error occurred');
      setPendingRequests([]);
    } finally {
      setLoading(false);
    }
  }, []);

  // Fetch provisioning templates
  const fetchTemplates = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await userProvisioningApi.getProvisioningTemplates();
      
      if (response.success && response.data) {
        setTemplates(response.data);
      } else {
        setError('Failed to fetch provisioning templates');
        setTemplates([]);
      }
    } catch (err: any) {
      console.error('Error fetching provisioning templates:', err);
      setError(err.message || 'Network error occurred');
      setTemplates([]);
    } finally {
      setLoading(false);
    }
  }, []);

  // Auto-provision eligible users
  const autoProvision = useCallback(async (templateId?: string) => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await userProvisioningApi.autoProvisionUsers(
        templateId ? { templateId } : {}
      );
      
      if (response.success) {
        // Refresh data after successful auto-provisioning
        await Promise.all([
          fetchOverview(),
          fetchPendingRequests(pagination.page, pagination.pageSize)
        ]);
        
        return {
          success: true,
          message: 'Auto-provisioning completed successfully',
          data: response.data
        };
      } else {
        setError('Auto-provisioning failed');
        return {
          success: false,
          message: 'Auto-provisioning failed'
        };
      }
    } catch (err: any) {
      console.error('Error during auto-provisioning:', err);
      const errorMessage = err.message || 'Network error during auto-provisioning';
      setError(errorMessage);
      return {
        success: false,
        message: errorMessage
      };
    } finally {
      setLoading(false);
    }
  }, [pagination.page, pagination.pageSize, fetchOverview, fetchPendingRequests]);

  // Manually approve/reject pending request
  const updateRequestStatus = useCallback(async (
    requestId: string, 
    status: 'approved' | 'rejected',
    reason?: string
  ) => {
    try {
      setLoading(true);
      setError(null);
      
      // Note: This endpoint might need to be implemented in the backend
      // Based on the analysis, this functionality might be part of the auto-provisioning
      const response = await userProvisioningApi.autoProvisionUsers({
        requestId,
        action: status,
        reason
      });
      
      if (response.success) {
        // Refresh pending requests after status update
        await fetchPendingRequests(pagination.page, pagination.pageSize);
        
        return {
          success: true,
          message: `Request ${status} successfully`
        };
      } else {
        setError(`Failed to ${status} request`);
        return {
          success: false,
          message: `Failed to ${status} request`
        };
      }
    } catch (err: any) {
      console.error(`Error ${status} request:`, err);
      const errorMessage = err.message || `Network error while ${status} request`;
      setError(errorMessage);
      return {
        success: false,
        message: errorMessage
      };
    } finally {
      setLoading(false);
    }
  }, [pagination.page, pagination.pageSize, fetchPendingRequests]);

  // Clear error state
  const clearError = useCallback(() => {
    setError(null);
  }, []);

  // Change page for pagination
  const changePage = useCallback((newPage: number) => {
    if (newPage >= 1 && newPage <= pagination.totalPages) {
      fetchPendingRequests(newPage, pagination.pageSize);
    }
  }, [pagination.totalPages, pagination.pageSize, fetchPendingRequests]);

  // Change page size for pagination
  const changePageSize = useCallback((newPageSize: number) => {
    fetchPendingRequests(1, newPageSize); // Reset to first page with new size
  }, [fetchPendingRequests]);

  return {
    // State
    overview,
    pendingRequests,
    templates,
    pagination,
    loading,
    error,

    // Actions
    fetchOverview,
    fetchPendingRequests,
    fetchTemplates,
    autoProvision,
    updateRequestStatus,
    clearError,
    changePage,
    changePageSize
  };
};

export type { ProvisioningOverview, PendingRequest, ProvisioningTemplate };