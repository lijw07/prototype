// Migration Progress Tracker Component
// Follows SRP: Only responsible for tracking and displaying migration progress

import React, { useState, useEffect, useCallback } from 'react';
import { 
  Activity,
  Clock,
  CheckCircle,
  XCircle,
  AlertTriangle,
  Pause,
  Play,
  Square,
  RefreshCw,
  Download,
  Eye,
  Filter,
  TrendingUp,
  BarChart3,
  FileText,
  Users,
  Zap
} from 'lucide-react';
import { useSignalRProgress } from './hooks/useSignalRProgress';
import type { 
  MigrationSession, 
  MigrationError, 
  MigrationWarning,
  BaseProvisioningProps 
} from './types/provisioning.types';

interface MigrationProgressTrackerProps extends BaseProvisioningProps {
  sessionId?: string;
  showDetails?: boolean;
  autoRefresh?: boolean;
  refreshInterval?: number;
}

const MigrationProgressTracker: React.FC<MigrationProgressTrackerProps> = ({
  className = '',
  sessionId,
  showDetails = true,
  autoRefresh = true,
  refreshInterval = 5000,
  onError,
  onSuccess
}) => {
  const {
    isConnected,
    connectionError,
    activeJobs,
    completedJobs,
    failedJobs,
    subscribeToJob,
    unsubscribeFromJob,
    getJobProgress,
    getCompletedJob,
    getFailedJob,
    getJobsSummary,
    refreshConnection
  } = useSignalRProgress();

  const [activeSessions, setActiveSessions] = useState<Map<string, MigrationSession>>(new Map());
  const [selectedSession, setSelectedSession] = useState<string | null>(sessionId || null);
  const [showErrors, setShowErrors] = useState(false);
  const [showWarnings, setShowWarnings] = useState(false);
  const [filterLevel, setFilterLevel] = useState<'all' | 'critical' | 'warnings'>('all');

  // Mock session data (replace with actual API call)
  const mockSessions: MigrationSession[] = [
    {
      sessionId: 'session_1',
      jobId: 'job_123',
      sessionName: 'User Provisioning Migration - Batch 1',
      startedAt: new Date(Date.now() - 300000).toISOString(),
      status: 'in_progress',
      totalFiles: 5,
      processedFiles: 3,
      totalRecords: 2500,
      processedRecords: 1800,
      successfulRecords: 1650,
      failedRecords: 150,
      overallProgress: 72,
      currentOperation: 'Processing user roles',
      estimatedTimeRemaining: '5 minutes',
      errors: [
        {
          errorId: 'err_1',
          timestamp: new Date().toISOString(),
          errorType: 'validation',
          errorMessage: 'Invalid email format for user john.doe@',
          fileName: 'users_batch_1.csv',
          rowNumber: 156,
          column: 'email',
          isCritical: false
        }
      ],
      warnings: [
        {
          warningId: 'warn_1',
          timestamp: new Date().toISOString(),
          warningType: 'data_quality',
          warningMessage: 'Phone number format inconsistent',
          fileName: 'users_batch_1.csv',
          rowNumber: 234,
          suggestedAction: 'Standardize phone number format'
        }
      ]
    }
  ];

  // Initialize sessions
  useEffect(() => {
    const sessionsMap = new Map(mockSessions.map(s => [s.sessionId, s]));
    setActiveSessions(sessionsMap);
  }, []);

  // Subscribe to active jobs
  useEffect(() => {
    if (isConnected && activeSessions.size > 0) {
      activeSessions.forEach(session => {
        if (session.status === 'in_progress' || session.status === 'preparing') {
          subscribeToJob({
            jobId: session.jobId,
            onProgress: (progress) => {
              // Update session progress
              setActiveSessions(prev => {
                const updated = new Map(prev);
                const session = updated.get(progress.jobId);
                if (session) {
                  updated.set(progress.jobId, {
                    ...session,
                    overallProgress: progress.progressPercentage,
                    currentOperation: progress.status
                  });
                }
                return updated;
              });
            },
            onComplete: (result) => {
              if (onSuccess) {
                onSuccess(`Migration session completed: ${result.jobId}`);
              }
            },
            onError: (error) => {
              if (onError) {
                onError(`Migration session failed: ${error.error}`);
              }
            }
          });
        }
      });
    }

    return () => {
      // Cleanup subscriptions
      activeSessions.forEach(session => {
        unsubscribeFromJob(session.jobId);
      });
    };
  }, [isConnected, activeSessions, subscribeToJob, unsubscribeFromJob, onSuccess, onError]);

  // Auto-refresh
  useEffect(() => {
    if (!autoRefresh || !refreshInterval) return;

    const interval = setInterval(() => {
      // Refresh session data (replace with actual API call)
      console.log('Refreshing migration sessions...');
    }, refreshInterval);

    return () => clearInterval(interval);
  }, [autoRefresh, refreshInterval]);

  // Get status color
  const getStatusColor = (status: string) => {
    switch (status) {
      case 'preparing': return 'info';
      case 'in_progress': return 'primary';
      case 'paused': return 'warning';
      case 'completed': return 'success';
      case 'failed': return 'danger';
      case 'cancelled': return 'secondary';
      default: return 'secondary';
    }
  };

  // Get status icon
  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'preparing': return <Clock size={16} />;
      case 'in_progress': return <Activity size={16} />;
      case 'paused': return <Pause size={16} />;
      case 'completed': return <CheckCircle size={16} />;
      case 'failed': return <XCircle size={16} />;
      case 'cancelled': return <Square size={16} />;
      default: return <Clock size={16} />;
    }
  };

  // Format time remaining
  const formatTimeRemaining = (timeString?: string) => {
    if (!timeString) return 'Calculating...';
    return timeString;
  };

  // Format duration
  const formatDuration = (startTime: string, endTime?: string) => {
    const start = new Date(startTime);
    const end = endTime ? new Date(endTime) : new Date();
    const duration = Math.floor((end.getTime() - start.getTime()) / 1000);
    
    const hours = Math.floor(duration / 3600);
    const minutes = Math.floor((duration % 3600) / 60);
    const seconds = duration % 60;
    
    if (hours > 0) {
      return `${hours}h ${minutes}m ${seconds}s`;
    } else if (minutes > 0) {
      return `${minutes}m ${seconds}s`;
    } else {
      return `${seconds}s`;
    }
  };

  const jobsSummary = getJobsSummary();
  const selectedSessionData = selectedSession ? activeSessions.get(selectedSession) : null;

  return (
    <div className={`card border-0 rounded-4 shadow-sm ${className}`}>
      {/* Header */}
      <div className="card-header border-0 bg-transparent pt-4 px-4 pb-0">
        <div className="d-flex align-items-center justify-content-between mb-3">
          <div className="d-flex align-items-center">
            <div className="rounded-circle bg-primary bg-opacity-10 p-3 me-3">
              <Activity className="text-primary" size={24} />
            </div>
            <div>
              <h5 className="card-title mb-0 fw-bold text-dark">Migration Progress</h5>
              <p className="text-muted small mb-0">
                Real-time migration and bulk upload tracking
              </p>
            </div>
          </div>
          
          <div className="d-flex align-items-center gap-3">
            {/* Connection Status */}
            <div className="d-flex align-items-center">
              <div className={`rounded-circle p-1 me-2 ${isConnected ? 'bg-success' : 'bg-danger'}`}>
                <div className="rounded-circle bg-white" style={{width: '6px', height: '6px'}}></div>
              </div>
              <span className={`small ${isConnected ? 'text-success' : 'text-danger'}`}>
                {isConnected ? 'Live' : 'Disconnected'}
              </span>
            </div>
            
            {/* Refresh Button */}
            <button
              className="btn btn-outline-primary btn-sm rounded-3"
              onClick={refreshConnection}
              title="Refresh Connection"
            >
              <RefreshCw size={14} />
            </button>
          </div>
        </div>

        {/* Quick Stats */}
        <div className="row g-3">
          <div className="col-md-3">
            <div className="bg-primary bg-opacity-10 rounded-3 p-3 text-center">
              <div className="fw-bold text-primary h5 mb-1">{jobsSummary.active}</div>
              <div className="small text-muted">Active Sessions</div>
            </div>
          </div>
          <div className="col-md-3">
            <div className="bg-success bg-opacity-10 rounded-3 p-3 text-center">
              <div className="fw-bold text-success h5 mb-1">{jobsSummary.completed}</div>
              <div className="small text-muted">Completed</div>
            </div>
          </div>
          <div className="col-md-3">
            <div className="bg-danger bg-opacity-10 rounded-3 p-3 text-center">
              <div className="fw-bold text-danger h5 mb-1">{jobsSummary.failed}</div>
              <div className="small text-muted">Failed</div>
            </div>
          </div>
          <div className="col-md-3">
            <div className="bg-info bg-opacity-10 rounded-3 p-3 text-center">
              <div className="fw-bold text-info h5 mb-1">{jobsSummary.total}</div>
              <div className="small text-muted">Total Sessions</div>
            </div>
          </div>
        </div>
      </div>

      <div className="card-body p-4">
        {/* Active Sessions List */}
        {activeSessions.size > 0 ? (
          <div className="row">
            {/* Sessions List */}
            <div className="col-md-8">
              <h6 className="fw-semibold mb-3">Active Migration Sessions</h6>
              
              {Array.from(activeSessions.values()).map(session => (
                <div 
                  key={session.sessionId} 
                  className={`border rounded-3 p-3 mb-3 cursor-pointer ${
                    selectedSession === session.sessionId ? 'border-primary bg-primary bg-opacity-5' : 'border-light'
                  }`}
                  onClick={() => setSelectedSession(session.sessionId)}
                >
                  <div className="d-flex align-items-center justify-content-between mb-2">
                    <div className="d-flex align-items-center">
                      <div className={`text-${getStatusColor(session.status)} me-2`}>
                        {getStatusIcon(session.status)}
                      </div>
                      <div>
                        <div className="fw-semibold text-dark">{session.sessionName}</div>
                        <div className="small text-muted">
                          Started {formatDuration(session.startedAt)} ago
                        </div>
                      </div>
                    </div>
                    <span className={`badge bg-${getStatusColor(session.status)}`}>
                      {session.status.replace('_', ' ')}
                    </span>
                  </div>

                  {/* Progress Bar */}
                  <div className="mb-2">
                    <div className="d-flex justify-content-between align-items-center mb-1">
                      <span className="small text-muted">{session.currentOperation}</span>
                      <span className="small fw-semibold">{session.overallProgress}%</span>
                    </div>
                    <div className="progress" style={{height: '6px'}}>
                      <div 
                        className="progress-bar bg-primary" 
                        style={{width: `${session.overallProgress}%`}}
                      ></div>
                    </div>
                  </div>

                  {/* Quick Stats */}
                  <div className="row g-2 small text-muted">
                    <div className="col-6">
                      <FileText size={12} className="me-1" />
                      {session.processedFiles}/{session.totalFiles} files
                    </div>
                    <div className="col-6">
                      <Users size={12} className="me-1" />
                      {session.processedRecords.toLocaleString()}/{session.totalRecords.toLocaleString()} records
                    </div>
                    <div className="col-6">
                      <CheckCircle size={12} className="me-1 text-success" />
                      {session.successfulRecords.toLocaleString()} successful
                    </div>
                    <div className="col-6">
                      <XCircle size={12} className="me-1 text-danger" />
                      {session.failedRecords.toLocaleString()} failed
                    </div>
                  </div>

                  {session.estimatedTimeRemaining && session.status === 'in_progress' && (
                    <div className="mt-2 small text-muted">
                      <Clock size={12} className="me-1" />
                      Est. {formatTimeRemaining(session.estimatedTimeRemaining)} remaining
                    </div>
                  )}
                </div>
              ))}
            </div>

            {/* Session Details */}
            <div className="col-md-4">
              {selectedSessionData ? (
                <div className="bg-light rounded-3 p-3">
                  <h6 className="fw-semibold mb-3">Session Details</h6>
                  
                  <div className="mb-3">
                    <div className="small text-muted mb-1">Session ID</div>
                    <div className="small font-monospace">{selectedSessionData.sessionId}</div>
                  </div>

                  <div className="mb-3">
                    <div className="small text-muted mb-1">Job ID</div>
                    <div className="small font-monospace">{selectedSessionData.jobId}</div>
                  </div>

                  <div className="mb-3">
                    <div className="small text-muted mb-1">Status</div>
                    <span className={`badge bg-${getStatusColor(selectedSessionData.status)}`}>
                      {selectedSessionData.status.replace('_', ' ')}
                    </span>
                  </div>

                  <div className="mb-3">
                    <div className="small text-muted mb-1">Progress</div>
                    <div className="progress mb-1" style={{height: '8px'}}>
                      <div 
                        className="progress-bar bg-primary" 
                        style={{width: `${selectedSessionData.overallProgress}%`}}
                      ></div>
                    </div>
                    <div className="small">{selectedSessionData.overallProgress}% Complete</div>
                  </div>

                  <div className="mb-3">
                    <div className="small text-muted mb-1">Current Operation</div>
                    <div className="small">{selectedSessionData.currentOperation}</div>
                  </div>

                  {/* Errors and Warnings */}
                  {(selectedSessionData.errors.length > 0 || selectedSessionData.warnings.length > 0) && (
                    <div className="mb-3">
                      <div className="d-flex align-items-center justify-content-between mb-2">
                        <div className="small text-muted">Issues</div>
                        <div className="btn-group btn-group-sm">
                          <button
                            className={`btn ${showErrors ? 'btn-danger' : 'btn-outline-danger'} btn-sm`}
                            onClick={() => setShowErrors(!showErrors)}
                          >
                            {selectedSessionData.errors.length} Errors
                          </button>
                          <button
                            className={`btn ${showWarnings ? 'btn-warning' : 'btn-outline-warning'} btn-sm`}
                            onClick={() => setShowWarnings(!showWarnings)}
                          >
                            {selectedSessionData.warnings.length} Warnings
                          </button>
                        </div>
                      </div>

                      {/* Error List */}
                      {showErrors && selectedSessionData.errors.length > 0 && (
                        <div className="small">
                          {selectedSessionData.errors.slice(0, 3).map(error => (
                            <div key={error.errorId} className="border rounded-2 p-2 mb-1 bg-white">
                              <div className="d-flex align-items-start">
                                <AlertTriangle className="text-danger flex-shrink-0 me-2" size={12} />
                                <div className="flex-grow-1">
                                  <div className="fw-semibold">{error.errorMessage}</div>
                                  {error.fileName && (
                                    <div className="text-muted">
                                      {error.fileName}:{error.rowNumber}
                                    </div>
                                  )}
                                </div>
                              </div>
                            </div>
                          ))}
                          {selectedSessionData.errors.length > 3 && (
                            <div className="text-center">
                              <button className="btn btn-link btn-sm p-0">
                                View all {selectedSessionData.errors.length} errors
                              </button>
                            </div>
                          )}
                        </div>
                      )}

                      {/* Warning List */}
                      {showWarnings && selectedSessionData.warnings.length > 0 && (
                        <div className="small">
                          {selectedSessionData.warnings.slice(0, 3).map(warning => (
                            <div key={warning.warningId} className="border rounded-2 p-2 mb-1 bg-white">
                              <div className="d-flex align-items-start">
                                <AlertTriangle className="text-warning flex-shrink-0 me-2" size={12} />
                                <div className="flex-grow-1">
                                  <div className="fw-semibold">{warning.warningMessage}</div>
                                  {warning.suggestedAction && (
                                    <div className="text-muted">
                                      Suggestion: {warning.suggestedAction}
                                    </div>
                                  )}
                                </div>
                              </div>
                            </div>
                          ))}
                          {selectedSessionData.warnings.length > 3 && (
                            <div className="text-center">
                              <button className="btn btn-link btn-sm p-0">
                                View all {selectedSessionData.warnings.length} warnings
                              </button>
                            </div>
                          )}
                        </div>
                      )}
                    </div>
                  )}

                  {/* Action Buttons */}
                  <div className="d-flex gap-2">
                    <button className="btn btn-outline-primary btn-sm rounded-3 flex-grow-1">
                      <Eye size={12} className="me-1" />
                      Details
                    </button>
                    <button className="btn btn-outline-secondary btn-sm rounded-3">
                      <Download size={12} />
                    </button>
                  </div>
                </div>
              ) : (
                <div className="bg-light rounded-3 p-3 text-center">
                  <Activity size={32} className="text-muted opacity-50 mb-2" />
                  <div className="small text-muted">
                    Select a session to view details
                  </div>
                </div>
              )}
            </div>
          </div>
        ) : (
          <div className="text-center py-5">
            <Activity size={48} className="text-muted opacity-50 mb-3" />
            <h5 className="text-muted mb-2">No Active Migrations</h5>
            <p className="text-muted">
              Migration sessions will appear here when bulk uploads are in progress.
            </p>
          </div>
        )}

        {/* Connection Error */}
        {connectionError && (
          <div className="alert alert-warning mt-3" role="alert">
            <div className="d-flex align-items-center">
              <AlertTriangle size={16} className="me-2 flex-shrink-0" />
              <span>Connection issue: {connectionError}</span>
              <button
                className="btn btn-link btn-sm ms-auto"
                onClick={refreshConnection}
              >
                Retry
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default MigrationProgressTracker;