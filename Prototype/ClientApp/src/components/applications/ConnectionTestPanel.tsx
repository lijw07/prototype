// ConnectionTestPanel component - displays connection test results and statistics
// Follows SRP by focusing only on connection test display logic

import React from 'react';
import { TestTube, CheckCircle, XCircle, Clock, TrendingUp, AlertCircle } from 'lucide-react';
import type { ConnectionTestResult } from './hooks/useConnectionTest';

interface ConnectionTestPanelProps {
  testing: boolean;
  lastTest: ConnectionTestResult | null;
  error: string | null;
  hasResults: boolean;
  resultCount: number;
  onTestAll: () => void;
  onClearResults: () => void;
  onClearError: () => void;
  getTestStatistics: () => {
    total: number;
    successful: number;
    failed: number;
    successRate: number;
    averageResponseTime: number;
  };
}

export const ConnectionTestPanel: React.FC<ConnectionTestPanelProps> = ({
  testing,
  lastTest,
  error,
  hasResults,
  resultCount,
  onTestAll,
  onClearResults,
  onClearError,
  getTestStatistics
}) => {
  const stats = getTestStatistics();

  return (
    <div className="card border-0 shadow-sm mb-4">
      <div className="card-header bg-light d-flex justify-content-between align-items-center">
        <h6 className="mb-0 d-flex align-items-center">
          <TestTube size={18} className="me-2 text-primary" />
          Connection Testing
        </h6>
        <div className="d-flex gap-2">
          <button
            className="btn btn-sm btn-primary"
            onClick={onTestAll}
            disabled={testing}
          >
            {testing ? (
              <>
                <div className="spinner-border spinner-border-sm me-2" role="status" />
                Testing...
              </>
            ) : (
              <>
                <TestTube size={16} className="me-2" />
                Test All
              </>
            )}
          </button>
          {hasResults && (
            <button
              className="btn btn-sm btn-outline-secondary"
              onClick={onClearResults}
              disabled={testing}
            >
              Clear Results
            </button>
          )}
        </div>
      </div>

      <div className="card-body">
        {/* Error Display */}
        {error && (
          <div className="alert alert-danger d-flex align-items-center justify-content-between">
            <div className="d-flex align-items-center">
              <AlertCircle size={18} className="me-2 flex-shrink-0" />
              <span>{error}</span>
            </div>
            <button
              className="btn btn-sm btn-outline-danger"
              onClick={onClearError}
            >
              Dismiss
            </button>
          </div>
        )}

        {/* Test Statistics */}
        {hasResults && (
          <div className="row g-3 mb-4">
            <div className="col-6 col-md-3">
              <div className="card bg-primary text-white">
                <div className="card-body text-center">
                  <div className="h4 mb-1">{stats.total}</div>
                  <div className="small">Total Tests</div>
                </div>
              </div>
            </div>
            <div className="col-6 col-md-3">
              <div className="card bg-success text-white">
                <div className="card-body text-center">
                  <div className="h4 mb-1">{stats.successful}</div>
                  <div className="small">Successful</div>
                </div>
              </div>
            </div>
            <div className="col-6 col-md-3">
              <div className="card bg-danger text-white">
                <div className="card-body text-center">
                  <div className="h4 mb-1">{stats.failed}</div>
                  <div className="small">Failed</div>
                </div>
              </div>
            </div>
            <div className="col-6 col-md-3">
              <div className="card bg-info text-white">
                <div className="card-body text-center">
                  <div className="h4 mb-1">{stats.successRate.toFixed(1)}%</div>
                  <div className="small">Success Rate</div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Performance Metrics */}
        {hasResults && stats.averageResponseTime > 0 && (
          <div className="row g-3 mb-4">
            <div className="col-md-6">
              <div className="card border-0 bg-light">
                <div className="card-body">
                  <div className="d-flex align-items-center">
                    <Clock size={18} className="text-primary me-2" />
                    <div>
                      <div className="fw-semibold">Average Response Time</div>
                      <div className="text-muted">{stats.averageResponseTime}ms</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div className="col-md-6">
              <div className="card border-0 bg-light">
                <div className="card-body">
                  <div className="d-flex align-items-center">
                    <TrendingUp size={18} className="text-success me-2" />
                    <div>
                      <div className="fw-semibold">Connection Health</div>
                      <div className="text-muted">
                        {stats.successRate >= 90 ? 'Excellent' : 
                         stats.successRate >= 75 ? 'Good' : 
                         stats.successRate >= 50 ? 'Fair' : 'Poor'}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Last Test Result */}
        {lastTest && (
          <div className="card border-0 bg-light">
            <div className="card-body">
              <h6 className="card-title d-flex align-items-center mb-3">
                {lastTest.success ? (
                  <CheckCircle size={18} className="text-success me-2" />
                ) : (
                  <XCircle size={18} className="text-danger me-2" />
                )}
                Last Test Result
              </h6>

              <div className="row g-3">
                <div className="col-md-6">
                  <div className="mb-2">
                    <strong>Status:</strong>{' '}
                    <span className={`badge ${lastTest.success ? 'bg-success' : 'bg-danger'}`}>
                      {lastTest.success ? 'Success' : 'Failed'}
                    </span>
                  </div>
                  <div className="mb-2">
                    <strong>Message:</strong>{' '}
                    <span className="text-muted">{lastTest.message}</span>
                  </div>
                  <div className="mb-2">
                    <strong>Timestamp:</strong>{' '}
                    <span className="text-muted">
                      {new Date(lastTest.timestamp).toLocaleString()}
                    </span>
                  </div>
                </div>

                {lastTest.connectionDetails && (
                  <div className="col-md-6">
                    <div className="mb-2">
                      <strong>Host:</strong>{' '}
                      <span className="text-muted">
                        {lastTest.connectionDetails.host}:{lastTest.connectionDetails.port}
                      </span>
                    </div>
                    <div className="mb-2">
                      <strong>Database:</strong>{' '}
                      <span className="text-muted">{lastTest.connectionDetails.database}</span>
                    </div>
                    {lastTest.connectionDetails.responseTime && (
                      <div className="mb-2">
                        <strong>Response Time:</strong>{' '}
                        <span className="text-muted">{lastTest.connectionDetails.responseTime}ms</span>
                      </div>
                    )}
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {/* No Results State */}
        {!hasResults && !testing && !error && (
          <div className="text-center py-4">
            <TestTube size={48} className="text-muted mb-3" />
            <h6 className="text-muted">No connection tests performed yet</h6>
            <p className="text-muted">
              Click "Test All" to test connections for all applications, or use the test button on individual application cards.
            </p>
          </div>
        )}

        {/* Testing State */}
        {testing && (
          <div className="text-center py-4">
            <div className="spinner-border text-primary mb-3" role="status">
              <span className="visually-hidden">Testing connections...</span>
            </div>
            <h6 className="text-muted">Testing connections...</h6>
            <p className="text-muted">
              This may take a moment depending on the number of applications and network conditions.
            </p>
          </div>
        )}
      </div>
    </div>
  );
};

export default ConnectionTestPanel;