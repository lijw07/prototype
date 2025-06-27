// Custom hook for applications business logic
// Separates applications CRUD logic from UI components following SRP

import { useState, useCallback } from 'react';
import { applicationApi } from '../../../services/api';
import type { ApiResponse, PaginatedResponse, Application } from '../../../types/api.types';

interface ApplicationsOverview {
  totalApplications: number;
  totalConnections: number;
  successfulConnections: number;
  failedConnections: number;
  lastUpdated?: string;
}

interface ApplicationsState {
  applications: Application[];
  allApplications: Application[];
  loading: boolean;
  error: string | null;
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
  filters: {
    searchTerm: string;
    connectionType: string;
    authType: string;
    sortOrder: 'newest' | 'oldest';
  };
}

export const useApplications = () => {
  const [state, setState] = useState<ApplicationsState>({
    applications: [],
    allApplications: [],
    loading: false,
    error: null,
    pagination: {
      page: 1,
      pageSize: 4,
      totalCount: 0,
      totalPages: 0
    },
    filters: {
      searchTerm: '',
      connectionType: 'all',
      authType: 'all',
      sortOrder: 'newest'
    }
  });

  const [overview, setOverview] = useState<ApplicationsOverview | null>(null);

  // Fetch applications with pagination
  const fetchApplications = useCallback(async (page: number = 1, pageSize: number = 4) => {
    try {
      setState(prev => ({ ...prev, loading: true, error: null }));
      
      const response = await applicationApi.getApplications(page, pageSize);
      
      if (response.success && response.data?.data) {
        setState(prev => ({
          ...prev,
          applications: response.data?.data || [],
          pagination: {
            page: response.data?.page || page,
            pageSize: response.data?.pageSize || pageSize,
            totalCount: response.data?.totalCount || 0,
            totalPages: response.data?.totalPages || 0
          }
        }));
      } else {
        setState(prev => ({
          ...prev,
          error: 'Failed to fetch applications',
          applications: []
        }));
      }
    } catch (err: any) {
      console.error('Error fetching applications:', err);
      setState(prev => ({
        ...prev,
        error: err.message || 'Network error occurred',
        applications: []
      }));
    } finally {
      setState(prev => ({ ...prev, loading: false }));
    }
  }, []);

  // Fetch all applications for client-side operations
  const fetchAllApplications = useCallback(async () => {
    try {
      const response = await applicationApi.getApplications(1, 1000); // Large enough to get all
      if (response.success && response.data?.data) {
        setState(prev => ({
          ...prev,
          allApplications: response.data?.data || []
        }));
        return response.data?.data || [];
      }
      return [];
    } catch (err: any) {
      console.error('Error fetching all applications:', err);
      return [];
    }
  }, []);

  // Create new application
  const createApplication = useCallback(async (applicationData: Partial<Application>) => {
    try {
      setState(prev => ({ ...prev, loading: true, error: null }));
      
      const response = await applicationApi.createApplication(applicationData);
      
      if (response.success) {
        // Refresh applications list
        await fetchApplications(state.pagination.page, state.pagination.pageSize);
        await fetchAllApplications();
        
        return {
          success: true,
          message: 'Application created successfully',
          data: response.data
        };
      } else {
        setState(prev => ({
          ...prev,
          error: 'Failed to create application'
        }));
        return {
          success: false,
          message: 'Failed to create application'
        };
      }
    } catch (err: any) {
      console.error('Error creating application:', err);
      const errorMessage = err.message || 'Network error while creating application';
      setState(prev => ({ ...prev, error: errorMessage }));
      return {
        success: false,
        message: errorMessage
      };
    } finally {
      setState(prev => ({ ...prev, loading: false }));
    }
  }, [state.pagination.page, state.pagination.pageSize, fetchApplications, fetchAllApplications]);

  // Update existing application
  const updateApplication = useCallback(async (applicationId: string, applicationData: Partial<Application>) => {
    try {
      setState(prev => ({ ...prev, loading: true, error: null }));
      
      const response = await applicationApi.updateApplication(applicationId, applicationData);
      
      if (response.success) {
        // Refresh applications list
        await fetchApplications(state.pagination.page, state.pagination.pageSize);
        await fetchAllApplications();
        
        return {
          success: true,
          message: 'Application updated successfully',
          data: response.data
        };
      } else {
        setState(prev => ({
          ...prev,
          error: 'Failed to update application'
        }));
        return {
          success: false,
          message: 'Failed to update application'
        };
      }
    } catch (err: any) {
      console.error('Error updating application:', err);
      const errorMessage = err.message || 'Network error while updating application';
      setState(prev => ({ ...prev, error: errorMessage }));
      return {
        success: false,
        message: errorMessage
      };
    } finally {
      setState(prev => ({ ...prev, loading: false }));
    }
  }, [state.pagination.page, state.pagination.pageSize, fetchApplications, fetchAllApplications]);

  // Delete application
  const deleteApplication = useCallback(async (applicationId: string) => {
    try {
      setState(prev => ({ ...prev, loading: true, error: null }));
      
      const response = await applicationApi.deleteApplication(applicationId);
      
      if (response.success) {
        // Refresh applications list
        await fetchApplications(state.pagination.page, state.pagination.pageSize);
        await fetchAllApplications();
        
        return {
          success: true,
          message: 'Application deleted successfully'
        };
      } else {
        setState(prev => ({
          ...prev,
          error: 'Failed to delete application'
        }));
        return {
          success: false,
          message: 'Failed to delete application'
        };
      }
    } catch (err: any) {
      console.error('Error deleting application:', err);
      const errorMessage = err.message || 'Network error while deleting application';
      setState(prev => ({ ...prev, error: errorMessage }));
      return {
        success: false,
        message: errorMessage
      };
    } finally {
      setState(prev => ({ ...prev, loading: false }));
    }
  }, [state.pagination.page, state.pagination.pageSize, fetchApplications, fetchAllApplications]);

  // Update filters
  const updateFilters = useCallback((newFilters: Partial<ApplicationsState['filters']>) => {
    setState(prev => ({
      ...prev,
      filters: {
        ...prev.filters,
        ...newFilters
      }
    }));
  }, []);

  // Change page
  const changePage = useCallback((newPage: number) => {
    setState(prev => ({
      ...prev,
      pagination: {
        ...prev.pagination,
        page: newPage
      }
    }));
    fetchApplications(newPage, state.pagination.pageSize);
  }, [fetchApplications, state.pagination.pageSize]);

  // Change page size
  const changePageSize = useCallback((newPageSize: number) => {
    setState(prev => ({
      ...prev,
      pagination: {
        ...prev.pagination,
        pageSize: newPageSize,
        page: 1 // Reset to first page when changing page size
      }
    }));
    fetchApplications(1, newPageSize);
  }, [fetchApplications]);

  // Calculate filtered applications for client-side operations
  const getFilteredApplications = useCallback(() => {
    const { searchTerm, connectionType, authType, sortOrder } = state.filters;
    const sourceApplications = state.allApplications.length > 0 ? state.allApplications : state.applications;
    
    let filtered = sourceApplications.filter(app => {
      const matchesSearch = searchTerm === '' || 
        app.applicationName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        app.applicationDescription.toLowerCase().includes(searchTerm.toLowerCase());
      
      const matchesConnectionType = connectionType === 'all' || 
        app.applicationDataSourceType.toString() === connectionType;
      
      const matchesAuthType = authType === 'all' || 
        app.connection.authenticationType === authType;
      
      return matchesSearch && matchesConnectionType && matchesAuthType;
    });

    // Sort applications
    filtered.sort((a, b) => {
      const dateA = new Date(a.createdAt || '').getTime();
      const dateB = new Date(b.createdAt || '').getTime();
      return sortOrder === 'newest' ? dateB - dateA : dateA - dateB;
    });

    return filtered;
  }, [state.allApplications, state.applications, state.filters]);

  // Clear error
  const clearError = useCallback(() => {
    setState(prev => ({ ...prev, error: null }));
  }, []);

  // Generate overview data
  const generateOverview = useCallback(() => {
    const apps = state.allApplications.length > 0 ? state.allApplications : state.applications;
    const overview: ApplicationsOverview = {
      totalApplications: apps.length,
      totalConnections: apps.length,
      successfulConnections: 0, // Would need connection status from backend
      failedConnections: 0, // Would need connection status from backend
      lastUpdated: new Date().toISOString()
    };
    setOverview(overview);
    return overview;
  }, [state.allApplications, state.applications]);

  return {
    // State
    applications: state.applications,
    allApplications: state.allApplications,
    loading: state.loading,
    error: state.error,
    pagination: state.pagination,
    filters: state.filters,
    overview,

    // Actions
    fetchApplications,
    fetchAllApplications,
    createApplication,
    updateApplication,
    deleteApplication,
    updateFilters,
    changePage,
    changePageSize,
    getFilteredApplications,
    generateOverview,
    clearError
  };
};

export type { ApplicationsOverview, ApplicationsState };