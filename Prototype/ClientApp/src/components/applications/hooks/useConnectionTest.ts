// Custom hook for connection testing logic
// Separates connection testing from UI components following SRP

import { useState, useCallback } from 'react';
import { applicationApi } from '../../../services/api';
import type { Application } from '../../../types/api.types';

interface ConnectionTestResult {
  success: boolean;
  message: string;
  timestamp: Date;
  applicationId?: string;
  connectionDetails?: {
    host: string;
    port: string;
    database: string;
    responseTime?: number;
  };
}

interface ConnectionTestState {
  testing: boolean;
  results: Map<string, ConnectionTestResult>;
  lastTest: ConnectionTestResult | null;
  error: string | null;
}

export const useConnectionTest = () => {
  const [state, setState] = useState<ConnectionTestState>({
    testing: false,
    results: new Map(),
    lastTest: null,
    error: null
  });

  // Test connection for a specific application
  const testConnection = useCallback(async (application: Application) => {
    try {
      setState(prev => ({
        ...prev,
        testing: true,
        error: null
      }));

      const startTime = Date.now();
      
      // Prepare connection data for testing
      const connectionData = {
        applicationId: application.applicationId,
        applicationDataSourceType: application.applicationDataSourceType,
        connection: application.connection
      };

      const response = await applicationApi.testConnection(connectionData);
      const endTime = Date.now();
      const responseTime = endTime - startTime;

      const result: ConnectionTestResult = {
        success: response.success || false,
        message: response.message || (response.success ? 'Connection successful' : 'Connection failed'),
        timestamp: new Date(),
        applicationId: application.applicationId,
        connectionDetails: {
          host: application.connection.host,
          port: application.connection.port,
          database: application.connection.databaseName,
          responseTime
        }
      };

      setState(prev => ({
        ...prev,
        lastTest: result,
        results: new Map(prev.results.set(application.applicationId, result))
      }));

      return result;
    } catch (err: any) {
      console.error('Error testing connection:', err);
      
      const errorResult: ConnectionTestResult = {
        success: false,
        message: err.message || 'Network error during connection test',
        timestamp: new Date(),
        applicationId: application.applicationId,
        connectionDetails: {
          host: application.connection.host,
          port: application.connection.port,
          database: application.connection.databaseName
        }
      };

      setState(prev => ({
        ...prev,
        error: err.message || 'Connection test failed',
        lastTest: errorResult,
        results: new Map(prev.results.set(application.applicationId, errorResult))
      }));

      return errorResult;
    } finally {
      setState(prev => ({ ...prev, testing: false }));
    }
  }, []);

  // Test multiple connections in batch
  const testMultipleConnections = useCallback(async (applications: Application[]) => {
    const results: ConnectionTestResult[] = [];
    
    for (const app of applications) {
      const result = await testConnection(app);
      results.push(result);
      
      // Small delay between tests to avoid overwhelming the server
      await new Promise(resolve => setTimeout(resolve, 500));
    }
    
    return results;
  }, [testConnection]);

  // Get test result for specific application
  const getTestResult = useCallback((applicationId: string): ConnectionTestResult | null => {
    return state.results.get(applicationId) || null;
  }, [state.results]);

  // Get all test results
  const getAllTestResults = useCallback((): ConnectionTestResult[] => {
    return Array.from(state.results.values());
  }, [state.results]);

  // Clear test results
  const clearTestResults = useCallback(() => {
    setState(prev => ({
      ...prev,
      results: new Map(),
      lastTest: null,
      error: null
    }));
  }, []);

  // Clear error
  const clearError = useCallback(() => {
    setState(prev => ({ ...prev, error: null }));
  }, []);

  // Get test statistics
  const getTestStatistics = useCallback(() => {
    const results = Array.from(state.results.values());
    const total = results.length;
    const successful = results.filter(r => r.success).length;
    const failed = total - successful;
    const averageResponseTime = results.length > 0 
      ? results.reduce((sum, r) => sum + (r.connectionDetails?.responseTime || 0), 0) / results.length
      : 0;

    return {
      total,
      successful,
      failed,
      successRate: total > 0 ? (successful / total) * 100 : 0,
      averageResponseTime: Math.round(averageResponseTime)
    };
  }, [state.results]);

  // Check if application has been tested recently
  const isRecentlyTested = useCallback((applicationId: string, withinMinutes: number = 5): boolean => {
    const result = state.results.get(applicationId);
    if (!result) return false;
    
    const timeDiff = Date.now() - result.timestamp.getTime();
    return timeDiff < (withinMinutes * 60 * 1000);
  }, [state.results]);

  return {
    // State
    testing: state.testing,
    lastTest: state.lastTest,
    error: state.error,
    
    // Actions
    testConnection,
    testMultipleConnections,
    getTestResult,
    getAllTestResults,
    clearTestResults,
    clearError,
    getTestStatistics,
    isRecentlyTested,

    // Computed values
    hasResults: state.results.size > 0,
    resultCount: state.results.size
  };
};

export type { ConnectionTestResult, ConnectionTestState };