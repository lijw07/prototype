import { useState, useEffect, useCallback } from 'react';

/**
 * Custom hook for managing localStorage with React state synchronization
 * Provides type-safe storage with automatic serialization/deserialization
 */
export function useLocalStorage<T>(
  key: string,
  initialValue: T
): [T, (value: T | ((prev: T) => T)) => void, () => void] {
  
  // Get initial value from localStorage or use provided initial value
  const [storedValue, setStoredValue] = useState<T>(() => {
    try {
      const item = window.localStorage.getItem(key);
      return item ? JSON.parse(item) : initialValue;
    } catch (error) {
      console.warn(`Error reading localStorage key "${key}":`, error);
      return initialValue;
    }
  });

  // Update localStorage and state
  const setValue = useCallback((value: T | ((prev: T) => T)) => {
    try {
      // Allow function updater pattern like useState
      const valueToStore = value instanceof Function ? value(storedValue) : value;
      
      setStoredValue(valueToStore);
      
      // Save to localStorage
      if (valueToStore === undefined) {
        window.localStorage.removeItem(key);
      } else {
        window.localStorage.setItem(key, JSON.stringify(valueToStore));
      }
    } catch (error) {
      console.error(`Error setting localStorage key "${key}":`, error);
    }
  }, [key, storedValue]);

  // Remove from localStorage and reset to initial value
  const removeValue = useCallback(() => {
    try {
      window.localStorage.removeItem(key);
      setStoredValue(initialValue);
    } catch (error) {
      console.error(`Error removing localStorage key "${key}":`, error);
    }
  }, [key, initialValue]);

  // Listen for storage changes from other tabs/windows
  useEffect(() => {
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === key && e.newValue !== null) {
        try {
          setStoredValue(JSON.parse(e.newValue));
        } catch (error) {
          console.warn(`Error parsing localStorage value for key "${key}":`, error);
        }
      }
    };

    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
  }, [key]);

  return [storedValue, setValue, removeValue];
}

/**
 * Hook for managing user preferences in localStorage
 * Common settings like theme, language, page size preferences, etc.
 */
export function useUserPreferences() {
  const [preferences, setPreferences, clearPreferences] = useLocalStorage('userPreferences', {
    theme: 'light',
    language: 'en',
    defaultPageSize: 20,
    dashboardLayout: 'grid',
    notifications: {
      email: true,
      push: true,
      marketing: false
    }
  });

  const updatePreference = useCallback((key: string, value: any) => {
    setPreferences(prev => ({
      ...prev,
      [key]: value
    }));
  }, [setPreferences]);

  const resetPreferences = useCallback(() => {
    clearPreferences();
  }, [clearPreferences]);

  return {
    preferences,
    updatePreference,
    resetPreferences,
    setPreferences
  };
}