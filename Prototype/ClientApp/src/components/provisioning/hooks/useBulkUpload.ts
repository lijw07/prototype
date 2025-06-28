// Custom hook for bulk upload functionality
// Handles file upload, validation, and progress tracking with SignalR integration

import { useState, useCallback, useRef } from 'react';
import { userProvisioningApi } from '../../../services/api';
import { progressService } from '../../../services/signalr';
import type { BulkUploadProgress, BulkUploadJobStart, BulkUploadJobComplete, BulkUploadJobError } from '../../../types/api.types';

interface UploadedFile {
  file: File;
  id: string;
  name: string;
  size: string;
  type: string;
  preview?: any[];
  isValid: boolean;
  errors: string[];
}

interface BulkUploadState {
  files: UploadedFile[];
  isUploading: boolean;
  progress: number;
  status: string;
  currentJobId: string | null;
  results: any[];
  errors: string[];
  uploadHistory: any[];
}

export interface UploadOptions {
  strategy: 'core' | 'multiple' | 'progress' | 'queue';
  detectTableTypes: boolean;
  validateOnly: boolean;
  autoProvision: boolean;
}

export const useBulkUpload = () => {
  const [state, setState] = useState<BulkUploadState>({
    files: [],
    isUploading: false,
    progress: 0,
    status: 'idle',
    currentJobId: null,
    results: [],
    errors: [],
    uploadHistory: []
  });

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const progressCallbacksRef = useRef<{
    onProgress?: (progress: BulkUploadProgress) => void;
    onComplete?: (result: BulkUploadJobComplete) => void;
    onError?: (error: BulkUploadJobError) => void;
  }>({});

  // Add files to upload queue with validation
  const addFiles = useCallback((newFiles: FileList | File[]) => {
    const fileArray = Array.from(newFiles);
    const validExtensions = ['.csv', '.xlsx', '.xls', '.json', '.xml'];
    
    const processedFiles: UploadedFile[] = fileArray.map(file => {
      const extension = file.name.toLowerCase().substring(file.name.lastIndexOf('.'));
      const isValidType = validExtensions.includes(extension);
      const isValidSize = file.size <= 50 * 1024 * 1024; // 50MB limit
      
      const errors: string[] = [];
      if (!isValidType) {
        errors.push(`Invalid file type. Supported: ${validExtensions.join(', ')}`);
      }
      if (!isValidSize) {
        errors.push('File size exceeds 50MB limit');
      }

      return {
        file,
        id: `file_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
        name: file.name,
        size: `${(file.size / 1024 / 1024).toFixed(2)} MB`,
        type: extension,
        isValid: isValidType && isValidSize,
        errors,
        preview: [] // Will be populated during file parsing
      };
    });

    setState(prev => ({
      ...prev,
      files: [...prev.files, ...processedFiles],
      errors: []
    }));

    return processedFiles;
  }, []);

  // Remove file from upload queue
  const removeFile = useCallback((fileId: string) => {
    setState(prev => ({
      ...prev,
      files: prev.files.filter(f => f.id !== fileId)
    }));
  }, []);

  // Clear all files
  const clearFiles = useCallback(() => {
    setState(prev => ({
      ...prev,
      files: [],
      errors: []
    }));
  }, []);

  // Setup SignalR progress tracking
  const setupProgressTracking = useCallback((jobId: string) => {
    const handleJobStarted = (jobStart: BulkUploadJobStart) => {
      if (jobStart.jobId === jobId) {
        setState(prev => ({
          ...prev,
          status: `Started processing ${jobStart.totalFiles} files`,
          progress: 0
        }));
      }
    };

    const handleProgressUpdate = (progress: BulkUploadProgress) => {
      if (progress.jobId === jobId) {
        setState(prev => ({
          ...prev,
          progress: progress.progressPercentage,
          status: progress.status,
          errors: progress.errors || []
        }));

        // Call external progress callback if provided
        progressCallbacksRef.current.onProgress?.(progress);
      }
    };

    const handleJobCompleted = (result: BulkUploadJobComplete) => {
      if (result.jobId === jobId) {
        setState(prev => ({
          ...prev,
          isUploading: false,
          progress: 100,
          status: result.success ? 'Completed successfully' : 'Completed with errors',
          results: result.data || [],
          currentJobId: null
        }));

        // Leave SignalR progress group
        progressService.leaveProgressGroup(jobId);

        // Call external completion callback if provided
        progressCallbacksRef.current.onComplete?.(result);
      }
    };

    const handleJobError = (jobError: BulkUploadJobError) => {
      if (jobError.jobId === jobId) {
        setState(prev => ({
          ...prev,
          isUploading: false,
          status: 'Failed',
          errors: [jobError.error],
          currentJobId: null
        }));

        // Leave SignalR progress group
        progressService.leaveProgressGroup(jobId);

        // Call external error callback if provided
        progressCallbacksRef.current.onError?.(jobError);
      }
    };

    // Setup SignalR event listeners
    progressService.onJobStarted(handleJobStarted);
    progressService.onProgressUpdate(handleProgressUpdate);
    progressService.onJobCompleted(handleJobCompleted);
    progressService.onJobError(handleJobError);

    // Join SignalR progress group
    progressService.joinProgressGroup(jobId);

    // Cleanup function
    return () => {
      progressService.offJobStarted(handleJobStarted);
      progressService.offProgressUpdate(handleProgressUpdate);
      progressService.offJobCompleted(handleJobCompleted);
      progressService.offJobError(handleJobError);
      progressService.leaveProgressGroup(jobId);
    };
  }, []);

  // Start bulk upload with specified strategy
  const startUpload = useCallback(async (options: UploadOptions = {
    strategy: 'progress',
    detectTableTypes: true,
    validateOnly: false,
    autoProvision: false
  }) => {
    if (state.files.length === 0) {
      setError('No files selected for upload');
      return { success: false, message: 'No files selected' };
    }

    const validFiles = state.files.filter(f => f.isValid);
    if (validFiles.length === 0) {
      setError('No valid files found for upload');
      return { success: false, message: 'No valid files found' };
    }

    try {
      setLoading(true);
      setError(null);
      
      // Generate job ID for tracking
      const jobId = progressService.generateJobId();
      
      setState(prev => ({
        ...prev,
        isUploading: true,
        progress: 0,
        status: 'Preparing upload...',
        currentJobId: jobId,
        results: [],
        errors: []
      }));

      // Setup progress tracking before starting upload
      const cleanupProgress = setupProgressTracking(jobId);

      // Prepare FormData
      const formData = new FormData();
      validFiles.forEach(fileData => {
        formData.append('files', fileData.file);
      });
      
      // Add options
      formData.append('jobId', jobId);
      formData.append('detectTableTypes', options.detectTableTypes.toString());
      formData.append('validateOnly', options.validateOnly.toString());
      formData.append('autoProvision', options.autoProvision.toString());

      let response: any;
      
      // Choose upload strategy based on options
      switch (options.strategy) {
        case 'core':
          response = await userProvisioningApi.bulkProvisionUsers(formData);
          break;
        case 'multiple':
          response = await userProvisioningApi.bulkProvisionMultipleFiles(formData);
          break;
        case 'progress':
          response = await userProvisioningApi.bulkProvisionWithProgress(formData);
          break;
        case 'queue':
          response = await userProvisioningApi.bulkProvisionWithQueue(formData);
          break;
        default:
          throw new Error('Invalid upload strategy');
      }

      if (response.success) {
        // For strategies without real-time progress, mark as completed
        if (options.strategy === 'core') {
          setState(prev => ({
            ...prev,
            isUploading: false,
            progress: 100,
            status: 'Upload completed',
            results: response.data || [],
            currentJobId: null
          }));
          cleanupProgress();
        }
        // For other strategies, progress will be handled by SignalR

        return {
          success: true,
          message: 'Upload started successfully',
          jobId,
          data: response.data
        };
      } else {
        setState(prev => ({
          ...prev,
          isUploading: false,
          status: 'Upload failed',
          errors: ['Upload failed'],
          currentJobId: null
        }));
        cleanupProgress();
        
        return {
          success: false,
          message: response.message || 'Upload failed'
        };
      }
    } catch (err: any) {
      console.error('Error during bulk upload:', err);
      const errorMessage = err.message || 'Network error during upload';
      
      setState(prev => ({
        ...prev,
        isUploading: false,
        status: 'Upload failed',
        errors: [errorMessage],
        currentJobId: null
      }));
      
      setError(errorMessage);
      
      return {
        success: false,
        message: errorMessage
      };
    } finally {
      setLoading(false);
    }
  }, [state.files, setupProgressTracking]);

  // Cancel current upload job
  const cancelUpload = useCallback(async () => {
    if (!state.currentJobId) {
      return { success: false, message: 'No active upload to cancel' };
    }

    try {
      const response = await userProvisioningApi.cancelQueue(state.currentJobId);
      
      if (response.success) {
        setState(prev => ({
          ...prev,
          isUploading: false,
          status: 'Upload cancelled',
          currentJobId: null
        }));

        // Leave SignalR progress group
        if (state.currentJobId) {
          progressService.leaveProgressGroup(state.currentJobId);
        }

        return {
          success: true,
          message: 'Upload cancelled successfully'
        };
      } else {
        return {
          success: false,
          message: response.message || 'Failed to cancel upload'
        };
      }
    } catch (err: any) {
      console.error('Error cancelling upload:', err);
      return {
        success: false,
        message: err.message || 'Network error while cancelling'
      };
    }
  }, [state.currentJobId]);

  // Set progress callbacks
  const setProgressCallbacks = useCallback((callbacks: {
    onProgress?: (progress: BulkUploadProgress) => void;
    onComplete?: (result: BulkUploadJobComplete) => void;
    onError?: (error: BulkUploadJobError) => void;
  }) => {
    progressCallbacksRef.current = callbacks;
  }, []);

  // Get upload status for a specific job
  const getJobStatus = useCallback(async (jobId: string) => {
    try {
      const response = await userProvisioningApi.getQueueStatus(jobId);
      return response;
    } catch (err: any) {
      console.error('Error getting job status:', err);
      return {
        success: false,
        message: err.message || 'Failed to get job status'
      };
    }
  }, []);

  // Clear error state
  const clearError = useCallback(() => {
    setError(null);
    setState(prev => ({ ...prev, errors: [] }));
  }, []);

  // Reset upload state
  const resetUpload = useCallback(() => {
    // Cancel any active upload first
    if (state.currentJobId && state.isUploading) {
      cancelUpload();
    }

    setState({
      files: [],
      isUploading: false,
      progress: 0,
      status: 'idle',
      currentJobId: null,
      results: [],
      errors: [],
      uploadHistory: []
    });
    
    setError(null);
  }, [state.currentJobId, state.isUploading, cancelUpload]);

  return {
    // State
    ...state,
    loading,
    error,

    // Actions
    addFiles,
    removeFile,
    clearFiles,
    startUpload,
    cancelUpload,
    setProgressCallbacks,
    getJobStatus,
    clearError,
    resetUpload
  };
};

export type { UploadedFile, BulkUploadState };