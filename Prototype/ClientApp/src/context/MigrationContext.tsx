import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { MigrationState, MigrationStorage } from '../utils/migrationStorage';

interface MigrationContextType {
  migrationState: MigrationState | null;
  updateMigrationState: (state: Partial<MigrationState>) => void;
  clearMigrationState: () => void;
  isAnyMigrationInProgress: () => boolean;
}

const MigrationContext = createContext<MigrationContextType | undefined>(undefined);

export const useMigration = () => {
  const context = useContext(MigrationContext);
  if (context === undefined) {
    throw new Error('useMigration must be used within a MigrationProvider');
  }
  return context;
};

interface MigrationProviderProps {
  children: ReactNode;
}

export const MigrationProvider: React.FC<MigrationProviderProps> = ({ children }) => {
  const [migrationState, setMigrationState] = useState<MigrationState | null>(null);

  // Load state from localStorage on mount
  useEffect(() => {
    const savedState = MigrationStorage.loadMigrationState();
    if (savedState) {
      console.log('🔄 Restoring migration session:', savedState.status, savedState.progress + '%');
      setMigrationState(savedState);
    }
  }, []);

  const updateMigrationState = (updates: Partial<MigrationState>) => {
    setMigrationState(current => {
      const newState = current ? { ...current, ...updates } : {
        status: 'idle' as const,
        progress: 0,
        jobId: null,
        results: null,
        startTime: null,
        endTime: null,
        ...updates
      };
      
      // Save to localStorage
      MigrationStorage.saveMigrationState(newState);
      return newState;
    });
  };

  const clearMigrationState = () => {
    setMigrationState(null);
    MigrationStorage.clearMigrationState();
  };

  const isAnyMigrationInProgress = () => {
    return migrationState?.status === 'processing';
  };

  const value: MigrationContextType = {
    migrationState,
    updateMigrationState,
    clearMigrationState,
    isAnyMigrationInProgress
  };

  return (
    <MigrationContext.Provider value={value}>
      {children}
    </MigrationContext.Provider>
  );
};