import { useState, useCallback, useMemo } from 'react';

// Pagination configuration
interface PaginationConfig {
  initialPage?: number;
  initialPageSize?: number;
  pageSizeOptions?: number[];
}

// Pagination state and controls
interface UsePaginationReturn {
  // Current state
  currentPage: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  
  // Calculated values
  startItem: number;
  endItem: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  
  // Actions
  setPage: (page: number) => void;
  setPageSize: (size: number) => void;
  setTotalCount: (count: number) => void;
  nextPage: () => void;
  previousPage: () => void;
  firstPage: () => void;
  lastPage: () => void;
  reset: () => void;
  
  // For backend API calls
  getPaginationParams: () => { page: number; pageSize: number };
  
  // For components
  getPaginationProps: () => {
    currentPage: number;
    totalPages: number;
    pageSize: number;
    totalCount: number;
    onPageChange: (page: number) => void;
    onPageSizeChange: (size: number) => void;
  };
}

/**
 * Custom hook for managing pagination state and calculations
 * Provides all necessary data and controls for paginated data display
 */
export function usePagination(config: PaginationConfig = {}): UsePaginationReturn {
  const {
    initialPage = 1,
    initialPageSize = 20,
    pageSizeOptions = [10, 20, 50, 100]
  } = config;

  const [currentPage, setCurrentPage] = useState<number>(initialPage);
  const [pageSize, setCurrentPageSize] = useState<number>(initialPageSize);
  const [totalCount, setTotalCount] = useState<number>(0);

  // Calculated values
  const totalPages = useMemo(() => 
    Math.max(1, Math.ceil(totalCount / pageSize))
  , [totalCount, pageSize]);

  const startItem = useMemo(() => 
    totalCount === 0 ? 0 : (currentPage - 1) * pageSize + 1
  , [currentPage, pageSize, totalCount]);

  const endItem = useMemo(() => 
    Math.min(currentPage * pageSize, totalCount)
  , [currentPage, pageSize, totalCount]);

  const hasNextPage = useMemo(() => 
    currentPage < totalPages
  , [currentPage, totalPages]);

  const hasPreviousPage = useMemo(() => 
    currentPage > 1
  , [currentPage]);

  // Actions
  const setPage = useCallback((page: number) => {
    const validPage = Math.max(1, Math.min(page, totalPages));
    setCurrentPage(validPage);
  }, [totalPages]);

  const setPageSize = useCallback((size: number) => {
    // Ensure page size is valid
    const validSize = pageSizeOptions.includes(size) ? size : initialPageSize;
    setCurrentPageSize(validSize);
    
    // Recalculate current page to maintain position as much as possible
    const currentItem = (currentPage - 1) * pageSize + 1;
    const newPage = Math.max(1, Math.ceil(currentItem / validSize));
    setCurrentPage(newPage);
  }, [currentPage, pageSize, pageSizeOptions, initialPageSize]);

  const nextPage = useCallback(() => {
    if (hasNextPage) {
      setPage(currentPage + 1);
    }
  }, [currentPage, hasNextPage, setPage]);

  const previousPage = useCallback(() => {
    if (hasPreviousPage) {
      setPage(currentPage - 1);
    }
  }, [currentPage, hasPreviousPage, setPage]);

  const firstPage = useCallback(() => {
    setPage(1);
  }, [setPage]);

  const lastPage = useCallback(() => {
    setPage(totalPages);
  }, [setPage, totalPages]);

  const reset = useCallback(() => {
    setCurrentPage(initialPage);
    setCurrentPageSize(initialPageSize);
    setTotalCount(0);
  }, [initialPage, initialPageSize]);

  // Helper functions for API calls
  const getPaginationParams = useCallback(() => ({
    page: currentPage,
    pageSize: pageSize
  }), [currentPage, pageSize]);

  // Helper function for components
  const getPaginationProps = useCallback(() => ({
    currentPage,
    totalPages,
    pageSize,
    totalCount,
    onPageChange: setPage,
    onPageSizeChange: setPageSize
  }), [currentPage, totalPages, pageSize, totalCount, setPage, setPageSize]);

  return {
    // State
    currentPage,
    pageSize,
    totalCount,
    totalPages,
    
    // Calculated
    startItem,
    endItem,
    hasNextPage,
    hasPreviousPage,
    
    // Actions
    setPage,
    setPageSize,
    setTotalCount,
    nextPage,
    previousPage,
    firstPage,
    lastPage,
    reset,
    
    // Helpers
    getPaginationParams,
    getPaginationProps
  };
}