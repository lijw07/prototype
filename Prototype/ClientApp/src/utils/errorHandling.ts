/**
 * Centralized error handling utilities for consistent error management
 * Provides standardized error processing, logging, and user-friendly messages
 */

// Error types from backend
export interface ApiError {
  message: string;
  statusCode?: number;
  errors?: string[];
  details?: any;
}

// Frontend error categories
export enum ErrorCategory {
  NETWORK = 'NETWORK',
  VALIDATION = 'VALIDATION',
  AUTHENTICATION = 'AUTHENTICATION',
  AUTHORIZATION = 'AUTHORIZATION',
  NOT_FOUND = 'NOT_FOUND',
  SERVER_ERROR = 'SERVER_ERROR',
  CLIENT_ERROR = 'CLIENT_ERROR',
  UNKNOWN = 'UNKNOWN'
}

// Processed error for UI display
export interface ProcessedError {
  category: ErrorCategory;
  message: string;
  userMessage: string;
  statusCode?: number;
  details?: any;
  timestamp: Date;
  retryable: boolean;
}

export class ErrorHandler {
  private static readonly USER_FRIENDLY_MESSAGES = {
    [ErrorCategory.NETWORK]: 'Unable to connect to the server. Please check your internet connection and try again.',
    [ErrorCategory.VALIDATION]: 'Please check your input and try again.',
    [ErrorCategory.AUTHENTICATION]: 'Your session has expired. Please sign in again.',
    [ErrorCategory.AUTHORIZATION]: 'You do not have permission to perform this action.',
    [ErrorCategory.NOT_FOUND]: 'The requested resource was not found.',
    [ErrorCategory.SERVER_ERROR]: 'A server error occurred. Please try again later.',
    [ErrorCategory.CLIENT_ERROR]: 'There was an error with your request. Please try again.',
    [ErrorCategory.UNKNOWN]: 'An unexpected error occurred. Please try again.'
  };

  private static readonly RETRYABLE_ERRORS = [
    ErrorCategory.NETWORK,
    ErrorCategory.SERVER_ERROR
  ];

  /**
   * Process any error into a standardized format
   */
  static processError(error: any): ProcessedError {
    const timestamp = new Date();

    // Handle network errors
    if (error.name === 'NetworkError' || error.message?.includes('fetch')) {
      return {
        category: ErrorCategory.NETWORK,
        message: error.message || 'Network error',
        userMessage: this.USER_FRIENDLY_MESSAGES[ErrorCategory.NETWORK],
        timestamp,
        retryable: true
      };
    }

    // Handle API response errors
    if (error.response) {
      const { status, data } = error.response;
      const category = this.categorizeHttpError(status);
      
      return {
        category,
        message: data?.message || error.message || 'HTTP error',
        userMessage: data?.message || this.USER_FRIENDLY_MESSAGES[category],
        statusCode: status,
        details: data?.errors || data?.details,
        timestamp,
        retryable: this.RETRYABLE_ERRORS.includes(category)
      };
    }

    // Handle API errors from our backend structure
    if (error.success === false) {
      const category = this.categorizeApiError(error);
      
      return {
        category,
        message: error.message || 'API error',
        userMessage: error.message || this.USER_FRIENDLY_MESSAGES[category],
        statusCode: error.statusCode,
        details: error.errors,
        timestamp,
        retryable: this.RETRYABLE_ERRORS.includes(category)
      };
    }

    // Handle JavaScript errors
    if (error instanceof Error) {
      return {
        category: ErrorCategory.CLIENT_ERROR,
        message: error.message,
        userMessage: this.USER_FRIENDLY_MESSAGES[ErrorCategory.CLIENT_ERROR],
        timestamp,
        retryable: false
      };
    }

    // Handle string errors
    if (typeof error === 'string') {
      return {
        category: ErrorCategory.UNKNOWN,
        message: error,
        userMessage: error.length > 100 ? this.USER_FRIENDLY_MESSAGES[ErrorCategory.UNKNOWN] : error,
        timestamp,
        retryable: false
      };
    }

    // Unknown error type
    return {
      category: ErrorCategory.UNKNOWN,
      message: 'Unknown error occurred',
      userMessage: this.USER_FRIENDLY_MESSAGES[ErrorCategory.UNKNOWN],
      timestamp,
      retryable: false
    };
  }

  /**
   * Categorize HTTP status codes
   */
  private static categorizeHttpError(statusCode: number): ErrorCategory {
    if (statusCode >= 200 && statusCode < 300) {
      return ErrorCategory.UNKNOWN; // Shouldn't happen for errors
    }
    
    if (statusCode === 400) return ErrorCategory.VALIDATION;
    if (statusCode === 401) return ErrorCategory.AUTHENTICATION;
    if (statusCode === 403) return ErrorCategory.AUTHORIZATION;
    if (statusCode === 404) return ErrorCategory.NOT_FOUND;
    if (statusCode >= 400 && statusCode < 500) return ErrorCategory.CLIENT_ERROR;
    if (statusCode >= 500) return ErrorCategory.SERVER_ERROR;
    
    return ErrorCategory.UNKNOWN;
  }

  /**
   * Categorize API errors from our backend
   */
  private static categorizeApiError(error: any): ErrorCategory {
    if (error.statusCode) {
      return this.categorizeHttpError(error.statusCode);
    }

    // Check error message patterns
    const message = error.message?.toLowerCase() || '';
    
    if (message.includes('validation') || message.includes('invalid')) {
      return ErrorCategory.VALIDATION;
    }
    
    if (message.includes('unauthorized') || message.includes('token')) {
      return ErrorCategory.AUTHENTICATION;
    }
    
    if (message.includes('forbidden') || message.includes('permission')) {
      return ErrorCategory.AUTHORIZATION;
    }
    
    if (message.includes('not found')) {
      return ErrorCategory.NOT_FOUND;
    }

    return ErrorCategory.UNKNOWN;
  }

  /**
   * Log error for debugging (development) or monitoring (production)
   */
  static logError(error: ProcessedError, context?: string): void {
    const logData = {
      category: error.category,
      message: error.message,
      statusCode: error.statusCode,
      details: error.details,
      timestamp: error.timestamp,
      context,
      userAgent: navigator.userAgent,
      url: window.location.href
    };

    if (process.env.NODE_ENV === 'development') {
      console.error('Error occurred:', logData);
    } else {
      // In production, send to monitoring service
      // this.sendToMonitoring(logData);
    }
  }

  /**
   * Check if error should trigger a retry
   */
  static shouldRetry(error: ProcessedError, attemptCount: number = 0): boolean {
    return error.retryable && attemptCount < 3;
  }

  /**
   * Get appropriate user notification type
   */
  static getNotificationType(error: ProcessedError): 'error' | 'warning' | 'info' {
    switch (error.category) {
      case ErrorCategory.VALIDATION:
        return 'warning';
      case ErrorCategory.NETWORK:
        return 'info';
      default:
        return 'error';
    }
  }

  /**
   * Format validation errors for display
   */
  static formatValidationErrors(errors: string[] | Record<string, string>): string {
    if (Array.isArray(errors)) {
      return errors.join(', ');
    }

    return Object.values(errors).join(', ');
  }

  /**
   * Create error from validation result
   */
  static fromValidationErrors(errors: Record<string, string>): ProcessedError {
    return {
      category: ErrorCategory.VALIDATION,
      message: this.formatValidationErrors(errors),
      userMessage: 'Please correct the following errors: ' + this.formatValidationErrors(errors),
      timestamp: new Date(),
      retryable: false,
      details: errors
    };
  }
}

// Error boundary error handler
export const handleComponentError = (error: Error, errorInfo: any): void => {
  const processedError = ErrorHandler.processError(error);
  ErrorHandler.logError(processedError, 'Component Error Boundary');
  
  // Additional component-specific logging
  console.error('Component stack trace:', errorInfo.componentStack);
};

// Global error handler for unhandled errors
export const setupGlobalErrorHandling = (): void => {
  // Handle unhandled promise rejections
  window.addEventListener('unhandledrejection', (event) => {
    const processedError = ErrorHandler.processError(event.reason);
    ErrorHandler.logError(processedError, 'Unhandled Promise Rejection');
    
    // Prevent default browser error logging
    event.preventDefault();
  });

  // Handle uncaught JavaScript errors
  window.addEventListener('error', (event) => {
    const processedError = ErrorHandler.processError(event.error || event.message);
    ErrorHandler.logError(processedError, 'Uncaught JavaScript Error');
  });
};

// Utility functions for common error scenarios
export const errorUtils = {
  /**
   * Wrap async functions with error handling
   */
  withErrorHandling: <T extends (...args: any[]) => Promise<any>>(
    fn: T,
    onError?: (error: ProcessedError) => void
  ): T => {
    return (async (...args: any[]) => {
      try {
        return await fn(...args);
      } catch (error) {
        const processedError = ErrorHandler.processError(error);
        ErrorHandler.logError(processedError, fn.name);
        
        if (onError) {
          onError(processedError);
        } else {
          throw processedError;
        }
      }
    }) as T;
  },

  /**
   * Create user-friendly error messages for common scenarios
   */
  createUserError: (message: string, category: ErrorCategory = ErrorCategory.CLIENT_ERROR): ProcessedError => {
    return {
      category,
      message,
      userMessage: message,
      timestamp: new Date(),
      retryable: ErrorHandler['RETRYABLE_ERRORS'].includes(category)
    };
  },

  /**
   * Check if error is a network connectivity issue
   */
  isNetworkError: (error: ProcessedError): boolean => {
    return error.category === ErrorCategory.NETWORK;
  },

  /**
   * Check if error requires user authentication
   */
  requiresAuth: (error: ProcessedError): boolean => {
    return error.category === ErrorCategory.AUTHENTICATION;
  },

  /**
   * Check if error is due to insufficient permissions
   */
  isPermissionError: (error: ProcessedError): boolean => {
    return error.category === ErrorCategory.AUTHORIZATION;
  }
};