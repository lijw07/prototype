// Custom hook for SignalR progress tracking
// Centralizes real-time communication logic following SRP

import { useState, useEffect, useCallback, useRef } from 'react';
import { progressService } from '../../../services/signalr';
import type { 
  BulkUploadProgress, 
  BulkUploadJobStart, 
  BulkUploadJobComplete, 
  BulkUploadJobError 
} from '../../../types/api.types';

interface ProgressState {
  isConnected: boolean;
  connectionError: string | null;
  activeJobs: Map<string, BulkUploadProgress>;
  completedJobs: Map<string, BulkUploadJobComplete>;
  failedJobs: Map<string, BulkUploadJobError>;
}

interface JobSubscription {
  jobId: string;
  onProgress?: (progress: BulkUploadProgress) => void;
  onComplete?: (result: BulkUploadJobComplete) => void;
  onError?: (error: BulkUploadJobError) => void;
  onStart?: (start: BulkUploadJobStart) => void;
}

export const useSignalRProgress = () => {
  const [state, setState] = useState<ProgressState>({
    isConnected: false,
    connectionError: null,
    activeJobs: new Map(),
    completedJobs: new Map(),
    failedJobs: new Map()
  });

  const subscriptionsRef = useRef<Map<string, JobSubscription>>(new Map());

  // Initialize SignalR connection
  useEffect(() => {
    const initializeConnection = async () => {
      try {
        await progressService.ensureConnection();
        setState(prev => ({
          ...prev,
          isConnected: true,
          connectionError: null
        }));
      } catch (error: any) {
        console.error('SignalR connection failed:', error);
        setState(prev => ({
          ...prev,
          isConnected: false,
          connectionError: error.message || 'Connection failed'
        }));
      }
    };

    initializeConnection();

    // Cleanup on unmount
    return () => {
      // Clean up all subscriptions
      subscriptionsRef.current.clear();
    };
  }, []);

  // Handle job started event
  const handleJobStarted = useCallback((jobStart: BulkUploadJobStart) => {
    const subscription = subscriptionsRef.current.get(jobStart.jobId);
    if (subscription?.onStart) {
      subscription.onStart(jobStart);
    }

    console.log(`Job ${jobStart.jobId} started:`, jobStart);
  }, []);

  // Handle progress update event
  const handleProgressUpdate = useCallback((progress: BulkUploadProgress) => {
    setState(prev => {
      const newActiveJobs = new Map(prev.activeJobs);
      newActiveJobs.set(progress.jobId, progress);
      
      return {
        ...prev,
        activeJobs: newActiveJobs
      };
    });

    const subscription = subscriptionsRef.current.get(progress.jobId);
    if (subscription?.onProgress) {
      subscription.onProgress(progress);
    }

    console.log(`Job ${progress.jobId} progress:`, progress);
  }, []);

  // Handle job completed event
  const handleJobCompleted = useCallback((result: BulkUploadJobComplete) => {
    setState(prev => {
      const newActiveJobs = new Map(prev.activeJobs);
      const newCompletedJobs = new Map(prev.completedJobs);
      
      // Move from active to completed
      newActiveJobs.delete(result.jobId);
      newCompletedJobs.set(result.jobId, result);
      
      return {
        ...prev,
        activeJobs: newActiveJobs,
        completedJobs: newCompletedJobs
      };
    });

    const subscription = subscriptionsRef.current.get(result.jobId);
    if (subscription?.onComplete) {
      subscription.onComplete(result);
    }

    // Auto-unsubscribe after completion
    unsubscribeFromJob(result.jobId);

    console.log(`Job ${result.jobId} completed:`, result);
  }, []);

  // Handle job error event
  const handleJobError = useCallback((jobError: BulkUploadJobError) => {
    setState(prev => {
      const newActiveJobs = new Map(prev.activeJobs);
      const newFailedJobs = new Map(prev.failedJobs);
      
      // Move from active to failed
      newActiveJobs.delete(jobError.jobId);
      newFailedJobs.set(jobError.jobId, jobError);
      
      return {
        ...prev,
        activeJobs: newActiveJobs,
        failedJobs: newFailedJobs
      };
    });

    const subscription = subscriptionsRef.current.get(jobError.jobId);
    if (subscription?.onError) {
      subscription.onError(jobError);
    }

    // Auto-unsubscribe after error
    unsubscribeFromJob(jobError.jobId);

    console.error(`Job ${jobError.jobId} failed:`, jobError);
  }, []);

  // Setup SignalR event listeners
  useEffect(() => {
    if (state.isConnected) {
      progressService.onJobStarted(handleJobStarted);
      progressService.onProgressUpdate(handleProgressUpdate);
      progressService.onJobCompleted(handleJobCompleted);
      progressService.onJobError(handleJobError);

      return () => {
        progressService.offJobStarted(handleJobStarted);
        progressService.offProgressUpdate(handleProgressUpdate);
        progressService.offJobCompleted(handleJobCompleted);
        progressService.offJobError(handleJobError);
      };
    }
  }, [state.isConnected, handleJobStarted, handleProgressUpdate, handleJobCompleted, handleJobError]);

  // Subscribe to a specific job's progress
  const subscribeToJob = useCallback(async (subscription: JobSubscription) => {
    if (!state.isConnected) {
      throw new Error('SignalR not connected. Cannot subscribe to job progress.');
    }

    try {
      // Join the progress group for this job
      await progressService.joinProgressGroup(subscription.jobId);
      
      // Store the subscription
      subscriptionsRef.current.set(subscription.jobId, subscription);

      console.log(`Subscribed to job: ${subscription.jobId}`);
      
      return {
        success: true,
        message: `Subscribed to job ${subscription.jobId}`
      };
    } catch (error: any) {
      console.error(`Failed to subscribe to job ${subscription.jobId}:`, error);
      return {
        success: false,
        message: error.message || 'Failed to subscribe to job progress'
      };
    }
  }, [state.isConnected]);

  // Unsubscribe from a specific job's progress
  const unsubscribeFromJob = useCallback(async (jobId: string) => {
    try {
      // Leave the progress group for this job
      await progressService.leaveProgressGroup(jobId);
      
      // Remove the subscription
      subscriptionsRef.current.delete(jobId);

      console.log(`Unsubscribed from job: ${jobId}`);
      
      return {
        success: true,
        message: `Unsubscribed from job ${jobId}`
      };
    } catch (error: any) {
      console.error(`Failed to unsubscribe from job ${jobId}:`, error);
      return {
        success: false,
        message: error.message || 'Failed to unsubscribe from job progress'
      };
    }
  }, []);

  // Get current progress for a specific job
  const getJobProgress = useCallback((jobId: string): BulkUploadProgress | null => {
    return state.activeJobs.get(jobId) || null;
  }, [state.activeJobs]);

  // Get completed job result
  const getCompletedJob = useCallback((jobId: string): BulkUploadJobComplete | null => {
    return state.completedJobs.get(jobId) || null;
  }, [state.completedJobs]);

  // Get failed job error
  const getFailedJob = useCallback((jobId: string): BulkUploadJobError | null => {
    return state.failedJobs.get(jobId) || null;
  }, [state.failedJobs]);

  // Check if a job is currently active
  const isJobActive = useCallback((jobId: string): boolean => {
    return state.activeJobs.has(jobId);
  }, [state.activeJobs]);

  // Get all active job IDs
  const getActiveJobIds = useCallback((): string[] => {
    return Array.from(state.activeJobs.keys());
  }, [state.activeJobs]);

  // Refresh connection if disconnected
  const refreshConnection = useCallback(async () => {
    try {
      setState(prev => ({
        ...prev,
        connectionError: null
      }));

      await progressService.refreshConnection();
      
      setState(prev => ({
        ...prev,
        isConnected: true,
        connectionError: null
      }));

      return {
        success: true,
        message: 'Connection refreshed successfully'
      };
    } catch (error: any) {
      console.error('Failed to refresh SignalR connection:', error);
      
      setState(prev => ({
        ...prev,
        isConnected: false,
        connectionError: error.message || 'Failed to refresh connection'
      }));

      return {
        success: false,
        message: error.message || 'Failed to refresh connection'
      };
    }
  }, []);

  // Clear completed and failed jobs history
  const clearJobHistory = useCallback(() => {
    setState(prev => ({
      ...prev,
      completedJobs: new Map(),
      failedJobs: new Map()
    }));
  }, []);

  // Get summary of all jobs
  const getJobsSummary = useCallback(() => {
    return {
      active: state.activeJobs.size,
      completed: state.completedJobs.size,
      failed: state.failedJobs.size,
      total: state.activeJobs.size + state.completedJobs.size + state.failedJobs.size
    };
  }, [state.activeJobs.size, state.completedJobs.size, state.failedJobs.size]);

  return {
    // Connection state
    isConnected: state.isConnected,
    connectionError: state.connectionError,

    // Job management
    subscribeToJob,
    unsubscribeFromJob,
    
    // Job status queries
    getJobProgress,
    getCompletedJob,
    getFailedJob,
    isJobActive,
    getActiveJobIds,
    getJobsSummary,

    // Utility functions
    refreshConnection,
    clearJobHistory,

    // Raw state for advanced usage
    activeJobs: state.activeJobs,
    completedJobs: state.completedJobs,
    failedJobs: state.failedJobs
  };
};

export type { ProgressState, JobSubscription };