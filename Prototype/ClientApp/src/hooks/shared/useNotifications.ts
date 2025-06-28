import { useState, useCallback, useRef, useEffect } from 'react';
import { ProcessedError, ErrorHandler } from '../../utils/errorHandling';

// Notification types
export type NotificationType = 'success' | 'error' | 'warning' | 'info';

// Notification interface
export interface Notification {
  id: string;
  type: NotificationType;
  title?: string;
  message: string;
  duration?: number;
  persistent?: boolean;
  actions?: Array<{
    label: string;
    action: () => void;
    style?: 'primary' | 'secondary';
  }>;
  timestamp: Date;
}

// Notification options
interface NotificationOptions {
  title?: string;
  duration?: number;
  persistent?: boolean;
  actions?: Array<{
    label: string;
    action: () => void;
    style?: 'primary' | 'secondary';
  }>;
}

/**
 * Custom hook for managing notifications and error messages
 * Provides centralized notification management with auto-dismiss and actions
 */
export function useNotifications() {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const timeoutRefs = useRef<Map<string, NodeJS.Timeout>>(new Map());

  // Generate unique ID for notifications
  const generateId = useCallback(() => {
    return Date.now().toString(36) + Math.random().toString(36).substr(2);
  }, []);

  // Clear timeout for a notification
  const clearNotificationTimeout = useCallback((id: string) => {
    const timeout = timeoutRefs.current.get(id);
    if (timeout) {
      clearTimeout(timeout);
      timeoutRefs.current.delete(id);
    }
  }, []);

  // Remove a notification
  const removeNotification = useCallback((id: string) => {
    clearNotificationTimeout(id);
    setNotifications(prev => prev.filter(n => n.id !== id));
  }, [clearNotificationTimeout]);

  // Add a notification with auto-dismiss
  const addNotification = useCallback((
    type: NotificationType,
    message: string,
    options: NotificationOptions = {}
  ) => {
    const {
      title,
      duration = type === 'error' ? 8000 : 5000,
      persistent = false,
      actions
    } = options;

    const id = generateId();
    const notification: Notification = {
      id,
      type,
      title,
      message,
      duration,
      persistent,
      actions,
      timestamp: new Date()
    };

    setNotifications(prev => [notification, ...prev]);

    // Auto-dismiss if not persistent
    if (!persistent && duration > 0) {
      const timeout = setTimeout(() => {
        removeNotification(id);
      }, duration);
      
      timeoutRefs.current.set(id, timeout);
    }

    return id;
  }, [generateId, removeNotification]);

  // Specific notification methods
  const showSuccess = useCallback((message: string, options?: NotificationOptions) => {
    return addNotification('success', message, options);
  }, [addNotification]);

  const showError = useCallback((message: string, options?: NotificationOptions) => {
    return addNotification('error', message, { duration: 8000, ...options });
  }, [addNotification]);

  const showWarning = useCallback((message: string, options?: NotificationOptions) => {
    return addNotification('warning', message, options);
  }, [addNotification]);

  const showInfo = useCallback((message: string, options?: NotificationOptions) => {
    return addNotification('info', message, options);
  }, [addNotification]);

  // Show error from ProcessedError
  const showProcessedError = useCallback((error: ProcessedError, options?: NotificationOptions) => {
    const notificationType = ErrorHandler.getNotificationType(error);
    
    const errorOptions: NotificationOptions = {
      title: `${error.category} Error`,
      duration: error.retryable ? 6000 : 8000,
      ...options
    };

    // Add retry action for retryable errors
    if (error.retryable && !errorOptions.actions) {
      errorOptions.actions = [
        {
          label: 'Retry',
          action: () => {
            // This would be provided by the calling component
            console.log('Retry action triggered');
          },
          style: 'primary'
        }
      ];
    }

    return addNotification(notificationType, error.userMessage, errorOptions);
  }, [addNotification]);

  // Clear all notifications
  const clearAll = useCallback(() => {
    // Clear all timeouts
    timeoutRefs.current.forEach(timeout => clearTimeout(timeout));
    timeoutRefs.current.clear();
    
    setNotifications([]);
  }, []);

  // Clear notifications by type
  const clearByType = useCallback((type: NotificationType) => {
    setNotifications(prev => {
      const toRemove = prev.filter(n => n.type === type);
      toRemove.forEach(n => clearNotificationTimeout(n.id));
      return prev.filter(n => n.type !== type);
    });
  }, [clearNotificationTimeout]);

  // Update notification
  const updateNotification = useCallback((id: string, updates: Partial<Notification>) => {
    setNotifications(prev => 
      prev.map(n => n.id === id ? { ...n, ...updates } : n)
    );
  }, []);

  // Get notifications by type
  const getNotificationsByType = useCallback((type: NotificationType) => {
    return notifications.filter(n => n.type === type);
  }, [notifications]);

  // Check if there are any notifications of a specific type
  const hasNotificationType = useCallback((type: NotificationType) => {
    return notifications.some(n => n.type === type);
  }, [notifications]);

  // Cleanup timeouts on unmount
  useEffect(() => {
    return () => {
      timeoutRefs.current.forEach(timeout => clearTimeout(timeout));
      timeoutRefs.current.clear();
    };
  }, []);

  return {
    // State
    notifications,
    
    // Actions
    addNotification,
    removeNotification,
    updateNotification,
    clearAll,
    clearByType,
    
    // Specific methods
    showSuccess,
    showError,
    showWarning,
    showInfo,
    showProcessedError,
    
    // Queries
    getNotificationsByType,
    hasNotificationType,
    
    // Computed
    hasErrors: hasNotificationType('error'),
    hasWarnings: hasNotificationType('warning'),
    count: notifications.length
  };
}

/**
 * Hook for managing form-specific error states
 * Integrates with form validation and submission
 */
export function useFormNotifications() {
  const notifications = useNotifications();
  
  const showFieldError = useCallback((field: string, error: string) => {
    return notifications.showError(`${field}: ${error}`, {
      title: 'Validation Error',
      duration: 5000
    });
  }, [notifications]);

  const showValidationErrors = useCallback((errors: Record<string, string>) => {
    const errorMessages = Object.entries(errors)
      .map(([field, error]) => `${field}: ${error}`)
      .join('\n');
    
    return notifications.showError(errorMessages, {
      title: 'Please correct the following errors:',
      duration: 8000,
      persistent: true
    });
  }, [notifications]);

  const showSubmissionSuccess = useCallback((message: string = 'Form submitted successfully') => {
    // Clear any existing form errors
    notifications.clearByType('error');
    
    return notifications.showSuccess(message, {
      duration: 4000
    });
  }, [notifications]);

  const showSubmissionError = useCallback((error: ProcessedError | string) => {
    if (typeof error === 'string') {
      return notifications.showError(error, {
        title: 'Submission Failed',
        duration: 8000
      });
    }
    
    return notifications.showProcessedError(error, {
      title: 'Submission Failed'
    });
  }, [notifications]);

  return {
    ...notifications,
    showFieldError,
    showValidationErrors,
    showSubmissionSuccess,
    showSubmissionError
  };
}