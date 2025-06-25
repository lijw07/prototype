// Migration state persistence utilities

export interface MigrationState {
  status: 'idle' | 'processing' | 'completed' | 'error';
  progress: number;
  jobId: string | null;
  results: {
    successful: number;
    failed: number;
    errors: string[];
    processedFiles: number;
    totalFiles: number;
  } | null;
  startTime: string | null;
  endTime: string | null;
}

const MIGRATION_STATE_KEY = 'cams_migration_state';

export class MigrationStorage {
  static saveMigrationState(state: MigrationState): void {
    try {
      const stateToSave = {
        ...state,
        lastUpdated: new Date().toISOString()
      };
      localStorage.setItem(MIGRATION_STATE_KEY, JSON.stringify(stateToSave));
    } catch (error) {
      console.warn('❌ Failed to save migration state:', error);
    }
  }

  static loadMigrationState(): MigrationState | null {
    try {
      const stored = localStorage.getItem(MIGRATION_STATE_KEY);
      
      if (!stored) {
        return null;
      }

      const parsed = JSON.parse(stored);
      
      // Check if state is recent (within last hour)
      const lastUpdated = new Date(parsed.lastUpdated);
      const oneHourAgo = new Date(Date.now() - 60 * 60 * 1000);
      
      if (lastUpdated < oneHourAgo) {
        // State is too old, clear it
        MigrationStorage.clearMigrationState();
        return null;
      }

      const restoredState = {
        status: parsed.status || 'idle',
        progress: parsed.progress || 0,
        jobId: parsed.jobId || null,
        results: parsed.results || null,
        startTime: parsed.startTime || null,
        endTime: parsed.endTime || null
      };
      
      return restoredState;
    } catch (error) {
      console.warn('❌ Failed to load migration state:', error);
      return null;
    }
  }

  static clearMigrationState(): void {
    try {
      localStorage.removeItem(MIGRATION_STATE_KEY);
    } catch (error) {
      console.warn('Failed to clear migration state:', error);
    }
  }

  static updateMigrationProgress(progress: number, jobId?: string): void {
    const currentState = MigrationStorage.loadMigrationState();
    if (currentState) {
      MigrationStorage.saveMigrationState({
        ...currentState,
        progress,
        jobId: jobId || currentState.jobId
      });
    }
  }

  static updateMigrationStatus(status: MigrationState['status'], results?: MigrationState['results']): void {
    const currentState = MigrationStorage.loadMigrationState();
    const now = new Date().toISOString();
    
    MigrationStorage.saveMigrationState({
      ...currentState,
      status,
      results: results || currentState?.results || null,
      startTime: status === 'processing' ? now : currentState?.startTime || null,
      endTime: (status === 'completed' || status === 'error') ? now : null
    } as MigrationState);
  }

  static isAnyMigrationInProgress(): boolean {
    const state = MigrationStorage.loadMigrationState();
    return state?.status === 'processing';
  }

  static getMigrationProgress(): { status: string; progress: number } | null {
    const state = MigrationStorage.loadMigrationState();
    if (!state || state.status === 'idle') return null;
    
    return {
      status: state.status,
      progress: state.progress
    };
  }
}