import { useState, useEffect, useCallback } from 'react';

// Generic API response structure that matches backend patterns
export interface ApiResponse<T = any> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
  statusCode?: number;
}

// Hook configuration options
interface UseApiOptions {
  immediate?: boolean; // Whether to call immediately on mount
  onSuccess?: (data: any) => void;
  onError?: (error: any) => void;
  dependencies?: any[]; // Dependencies that trigger refetch
}

// Hook return value
interface UseApiReturn<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
  execute: (...args: any[]) => Promise<ApiResponse<T> | null>;
  reset: () => void;
}

/**
 * Custom hook for making API calls with standardized loading/error states
 * Supports the backend ApiResponse<T> structure used throughout the application
 */
export function useApi<T = any>(
  apiFunction: (...args: any[]) => Promise<ApiResponse<T>>,
  options: UseApiOptions = {}
): UseApiReturn<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const { immediate = false, onSuccess, onError, dependencies = [] } = options;

  const execute = useCallback(async (...args: any[]): Promise<ApiResponse<T> | null> => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await apiFunction(...args);
      
      if (response.success && response.data !== undefined) {
        setData(response.data);
        onSuccess?.(response.data);
      } else {
        const errorMessage = response.message || 'An error occurred';
        setError(errorMessage);
        onError?.(errorMessage);
      }
      
      return response;
    } catch (err: any) {
      const errorMessage = err.message || 'Network error occurred';
      setError(errorMessage);
      onError?.(err);
      return null;
    } finally {
      setLoading(false);
    }
  }, [apiFunction, onSuccess, onError]);

  const reset = useCallback(() => {
    setData(null);
    setLoading(false);
    setError(null);
  }, []);

  // Execute immediately if requested
  useEffect(() => {
    if (immediate) {
      execute();
    }
  }, [immediate, execute, ...dependencies]);

  return {
    data,
    loading,
    error,
    execute,
    reset
  };
}