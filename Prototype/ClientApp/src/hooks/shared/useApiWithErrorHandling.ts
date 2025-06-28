import { useCallback } from 'react';
import { useApi, ApiResponse } from './useApi';
import { useNotifications } from './useNotifications';
import { ErrorHandler, ProcessedError, errorUtils } from '../../utils/errorHandling';

interface UseApiWithErrorHandlingOptions {
  immediate?: boolean;
  showSuccessNotification?: boolean | string;
  showErrorNotification?: boolean;
  onSuccess?: (data: any) => void;
  onError?: (error: ProcessedError) => void;
  dependencies?: any[];
  retryable?: boolean;
  maxRetries?: number;
}

/**
 * Enhanced API hook with integrated error handling and notifications
 * Provides automatic error processing, user notifications, and retry logic
 */
export function useApiWithErrorHandling<T = any>(
  apiFunction: (...args: any[]) => Promise<ApiResponse<T>>,
  options: UseApiWithErrorHandlingOptions = {}
) {
  const {
    immediate = false,
    showSuccessNotification = false,
    showErrorNotification = true,
    onSuccess,
    onError,
    dependencies = [],
    retryable = false,
    maxRetries = 3
  } = options;

  const notifications = useNotifications();

  // Enhanced success handler
  const handleSuccess = useCallback((data: any) => {
    if (showSuccessNotification) {
      const message = typeof showSuccessNotification === 'string' 
        ? showSuccessNotification 
        : 'Operation completed successfully';
      notifications.showSuccess(message);
    }
    onSuccess?.(data);
  }, [showSuccessNotification, notifications, onSuccess]);

  // Enhanced error handler
  const handleError = useCallback((error: any, attemptCount: number = 0) => {
    const processedError = ErrorHandler.processError(error);
    
    // Log the error
    ErrorHandler.logError(processedError, `API Call Attempt ${attemptCount + 1}`);
    
    // Show notification if enabled
    if (showErrorNotification) {
      const retryAction = retryable && ErrorHandler.shouldRetry(processedError, attemptCount) 
        ? {
            label: 'Retry',
            action: () => {
              // This will be handled by the retry logic below
            },
            style: 'primary' as const
          }
        : undefined;

      notifications.showProcessedError(processedError, {
        actions: retryAction ? [retryAction] : undefined
      });
    }
    
    onError?.(processedError);
    
    return processedError;
  }, [showErrorNotification, notifications, onError, retryable]);

  // Use the base API hook with enhanced handlers
  const apiHook = useApi(apiFunction, {
    immediate,
    onSuccess: handleSuccess,
    onError: handleError,
    dependencies
  });

  // Enhanced execute function with retry logic
  const executeWithRetry = useCallback(async (...args: any[]): Promise<ApiResponse<T> | null> => {
    let attemptCount = 0;
    let lastError: ProcessedError | null = null;

    while (attemptCount <= maxRetries) {
      try {
        const result = await apiHook.execute(...args);
        
        // If we get here and have a result, the call succeeded
        if (result) {
          return result;
        }
        
        // If result is null but no exception was thrown, treat as error
        if (attemptCount < maxRetries) {
          attemptCount++;
          await new Promise(resolve => setTimeout(resolve, 1000 * attemptCount)); // Exponential backoff
          continue;
        }
        
        return null;
      } catch (error) {
        const processedError = ErrorHandler.processError(error);
        lastError = processedError;
        
        // Check if we should retry
        if (retryable && ErrorHandler.shouldRetry(processedError, attemptCount) && attemptCount < maxRetries) {
          attemptCount++;
          
          // Show retry notification
          notifications.showInfo(`Retrying... (Attempt ${attemptCount + 1}/${maxRetries + 1})`, {
            duration: 2000
          });
          
          // Wait before retrying (exponential backoff)
          await new Promise(resolve => setTimeout(resolve, 1000 * attemptCount));
          continue;
        }
        
        // No more retries, handle the error
        handleError(error, attemptCount);
        throw processedError;
      }
    }

    // If we've exhausted retries
    if (lastError) {
      notifications.showError(`Failed after ${maxRetries + 1} attempts. Please try again later.`);
      throw lastError;
    }

    return null;
  }, [apiHook.execute, maxRetries, retryable, handleError, notifications]);

  // Optimistic update helper
  const executeOptimistic = useCallback(async (
    optimisticUpdate: () => void,
    rollback: () => void,
    ...args: any[]
  ) => {
    // Apply optimistic update
    optimisticUpdate();

    try {
      const result = await executeWithRetry(...args);
      return result;
    } catch (error) {
      // Rollback on error
      rollback();
      throw error;
    }
  }, [executeWithRetry]);

  // Background refresh (silent, no notifications)
  const refreshSilently = useCallback(async (...args: any[]) => {
    try {
      return await apiHook.execute(...args);
    } catch (error) {
      // Log but don't show notifications
      const processedError = ErrorHandler.processError(error);
      ErrorHandler.logError(processedError, 'Silent Refresh');
      return null;
    }
  }, [apiHook.execute]);

  return {
    ...apiHook,
    execute: retryable ? executeWithRetry : apiHook.execute,
    executeWithRetry,
    executeOptimistic,
    refreshSilently,
    
    // Error utilities
    isNetworkError: apiHook.error ? errorUtils.isNetworkError(ErrorHandler.processError(apiHook.error)) : false,
    requiresAuth: apiHook.error ? errorUtils.requiresAuth(ErrorHandler.processError(apiHook.error)) : false,
    isPermissionError: apiHook.error ? errorUtils.isPermissionError(ErrorHandler.processError(apiHook.error)) : false,
    
    // Retry helpers
    canRetry: retryable && apiHook.error ? ErrorHandler.shouldRetry(ErrorHandler.processError(apiHook.error)) : false,
    retry: () => executeWithRetry(),
    
    // Notification controls
    clearErrorNotifications: () => notifications.clearByType('error'),
    clearAllNotifications: () => notifications.clearAll()
  };
}

/**
 * Specialized hook for form submissions with validation
 */
export function useFormSubmission<T = any>(
  submitFunction: (data: any) => Promise<ApiResponse<T>>,
  options: {
    successMessage?: string;
    onSuccess?: (data: any) => void;
    onValidationError?: (errors: Record<string, string>) => void;
  } = {}
) {
  const {
    successMessage = 'Form submitted successfully',
    onSuccess,
    onValidationError
  } = options;

  return useApiWithErrorHandling(submitFunction, {
    showSuccessNotification: successMessage,
    showErrorNotification: true,
    onSuccess: (data) => {
      onSuccess?.(data);
    },
    onError: (error) => {
      // Handle validation errors specially
      if (error.category === 'VALIDATION' && error.details) {
        onValidationError?.(error.details);
      }
    }
  });
}

/**
 * Hook for paginated data with error handling
 */
export function usePaginatedApiWithErrorHandling<T = any>(
  apiFunction: (page: number, pageSize: number, ...args: any[]) => Promise<ApiResponse<{
    data: T[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  }>>,
  options: UseApiWithErrorHandlingOptions & {
    initialPage?: number;
    initialPageSize?: number;
  } = {}
) {
  const {
    initialPage = 1,
    initialPageSize = 20,
    ...apiOptions
  } = options;

  const api = useApiWithErrorHandling(apiFunction, {
    ...apiOptions,
    immediate: true
  });

  const loadPage = useCallback((page: number, pageSize: number = initialPageSize) => {
    return api.execute(page, pageSize);
  }, [api.execute, initialPageSize]);

  return {
    ...api,
    loadPage,
    data: api.data?.data || [],
    pagination: {
      page: api.data?.page || initialPage,
      pageSize: api.data?.pageSize || initialPageSize,
      totalCount: api.data?.totalCount || 0,
      totalPages: api.data?.totalPages || 0
    }
  };
}