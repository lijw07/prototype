import React, { createContext, useContext, useState, ReactNode } from 'react';

interface NotificationDetails {
  processedRecords?: number;
  totalRecords?: number;
  processedFiles?: number;
  totalFiles?: number;
  currentOperation?: string;
}

interface NotificationResults {
  successful: number;
  failed: number;
  errors: string[];
  processedFiles: number;
  totalFiles: number;
}

interface NotificationState {
  isVisible: boolean;
  isMinimized: boolean;
  status: 'idle' | 'processing' | 'completed' | 'error';
  progress: number;
  title: string;
  description?: string;
  details?: NotificationDetails;
  results?: NotificationResults;
}

interface NotificationContextType {
  notification: NotificationState;
  showNotification: (config: Partial<NotificationState>) => void;
  updateNotification: (updates: Partial<NotificationState>) => void;
  hideNotification: () => void;
  minimizeNotification: () => void;
  maximizeNotification: () => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

const initialState: NotificationState = {
  isVisible: false,
  isMinimized: false,
  status: 'idle',
  progress: 0,
  title: '',
  description: undefined,
  details: undefined,
  results: undefined
};

export function NotificationProvider({ children }: { children: ReactNode }) {
  const [notification, setNotification] = useState<NotificationState>(initialState);

  const showNotification = (config: Partial<NotificationState>) => {
    setNotification(prev => ({
      ...prev,
      ...config,
      isVisible: true,
      isMinimized: false
    }));
  };

  const updateNotification = (updates: Partial<NotificationState>) => {
    setNotification(prev => ({
      ...prev,
      ...updates
    }));
  };

  const hideNotification = () => {
    setNotification(prev => ({
      ...prev,
      isVisible: false,
      isMinimized: false
    }));
  };

  const minimizeNotification = () => {
    setNotification(prev => ({
      ...prev,
      isMinimized: true
    }));
  };

  const maximizeNotification = () => {
    setNotification(prev => ({
      ...prev,
      isMinimized: false
    }));
  };

  return (
    <NotificationContext.Provider
      value={{
        notification,
        showNotification,
        updateNotification,
        hideNotification,
        minimizeNotification,
        maximizeNotification
      }}
    >
      {children}
    </NotificationContext.Provider>
  );
}

export function useNotification() {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error('useNotification must be used within a NotificationProvider');
  }
  return context;
}