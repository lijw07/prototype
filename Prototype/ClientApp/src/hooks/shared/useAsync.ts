import { useState, useEffect, useCallback, useRef } from 'react';

// Async operation status
type AsyncStatus = 'idle' | 'pending' | 'resolved' | 'rejected';

// Async state
interface AsyncState<T> {
  data: T | null;
  error: Error | null;
  status: AsyncStatus;
  loading: boolean;
}

// Async actions
interface AsyncActions<T> {
  execute: (...args: any[]) => Promise<T>;
  reset: () => void;
  setData: (data: T) => void;
  setError: (error: Error) => void;
}

/**
 * Custom hook for managing async operations
 * Provides loading states, error handling, and cancellation support
 */
export function useAsync<T = any>(
  asyncFunction?: (...args: any[]) => Promise<T>,
  immediate: boolean = false
): AsyncState<T> & AsyncActions<T> {
  const [state, setState] = useState<AsyncState<T>>({
    data: null,
    error: null,
    status: 'idle',
    loading: false
  });

  const cancelRef = useRef<boolean>(false);

  // Execute async function
  const execute = useCallback(async (...args: any[]): Promise<T> => {
    if (!asyncFunction) {
      throw new Error('No async function provided');
    }

    cancelRef.current = false;
    
    setState(prev => ({
      ...prev,
      status: 'pending',
      loading: true,
      error: null
    }));

    try {
      const data = await asyncFunction(...args);
      
      if (!cancelRef.current) {
        setState(prev => ({
          ...prev,
          data,
          status: 'resolved',
          loading: false,
          error: null
        }));
      }
      
      return data;
    } catch (error) {
      if (!cancelRef.current) {
        const err = error instanceof Error ? error : new Error(String(error));
        setState(prev => ({
          ...prev,
          error: err,
          status: 'rejected',
          loading: false
        }));
      }
      throw error;
    }
  }, [asyncFunction]);

  // Reset state
  const reset = useCallback(() => {
    cancelRef.current = true;
    setState({
      data: null,
      error: null,
      status: 'idle',
      loading: false
    });
  }, []);

  // Set data manually
  const setData = useCallback((data: T) => {
    setState(prev => ({
      ...prev,
      data,
      status: 'resolved',
      loading: false,
      error: null
    }));
  }, []);

  // Set error manually
  const setError = useCallback((error: Error) => {
    setState(prev => ({
      ...prev,
      error,
      status: 'rejected',
      loading: false
    }));
  }, []);

  // Execute immediately if requested
  useEffect(() => {
    if (immediate && asyncFunction) {
      execute();
    }
  }, [immediate, execute, asyncFunction]);

  // Cancel on unmount
  useEffect(() => {
    return () => {
      cancelRef.current = true;
    };
  }, []);

  return {
    ...state,
    execute,
    reset,
    setData,
    setError
  };
}

/**
 * Hook for managing multiple async operations
 * Useful for components that need to track several API calls
 */
export function useAsyncOperations<T extends Record<string, any>>() {
  const [operations, setOperations] = useState<
    Record<string, AsyncState<any>>
  >({});

  const setOperationState = useCallback((
    key: string, 
    state: Partial<AsyncState<any>>
  ) => {
    setOperations(prev => ({
      ...prev,
      [key]: { ...prev[key], ...state }
    }));
  }, []);

  const startOperation = useCallback((key: string) => {
    setOperationState(key, {
      status: 'pending',
      loading: true,
      error: null
    });
  }, [setOperationState]);

  const resolveOperation = useCallback((key: string, data: any) => {
    setOperationState(key, {
      data,
      status: 'resolved',
      loading: false,
      error: null
    });
  }, [setOperationState]);

  const rejectOperation = useCallback((key: string, error: Error) => {
    setOperationState(key, {
      error,
      status: 'rejected',
      loading: false
    });
  }, [setOperationState]);

  const resetOperation = useCallback((key: string) => {
    setOperationState(key, {
      data: null,
      error: null,
      status: 'idle',
      loading: false
    });
  }, [setOperationState]);

  const isAnyLoading = useCallback(() => {
    return Object.values(operations).some(op => op.loading);
  }, [operations]);

  const hasAnyError = useCallback(() => {
    return Object.values(operations).some(op => op.error);
  }, [operations]);

  const getOperation = useCallback((key: string) => {
    return operations[key] || {
      data: null,
      error: null,
      status: 'idle' as AsyncStatus,
      loading: false
    };
  }, [operations]);

  return {
    operations,
    startOperation,
    resolveOperation,
    rejectOperation,
    resetOperation,
    isAnyLoading,
    hasAnyError,
    getOperation
  };
}