import React, { useState, useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { RefreshCw, CheckCircle, AlertCircle, Upload, X, Maximize2, Minimize2 } from 'lucide-react';
import { useMigration } from '../../context/MigrationContext';
import { MinimizedMigrationIndicator } from './MinimizedMigrationIndicator';

interface GlobalMigrationIndicatorProps {
  onNavigateToMigration?: () => void;
}

export const GlobalMigrationIndicator: React.FC<GlobalMigrationIndicatorProps> = ({ 
  onNavigateToMigration 
}) => {
  const { migrationState, setShouldNavigateToBulkTab } = useMigration();
  const location = useLocation();
  const [isExpanded, setIsExpanded] = useState(true);
  const [isVisible, setIsVisible] = useState(true);

  // Check if we're on the user provisioning page
  const isOnProvisioningPage = location.pathname === '/user-provisioning';

  // Don't show anything if:
  // 1. No migration state or idle
  // 2. User is on User Provisioning page (regardless of tab - they can see progress on bulk operations tab)
  const shouldHideAll = !migrationState || 
                        migrationState.status === 'idle' || 
                        isOnProvisioningPage;
  
  // Show minimized indicator if user closed the expanded popup but migration is still active
  const shouldShowMinimized = !shouldHideAll && !isVisible && !isExpanded;

  // Debug logging
  console.log('🔍 GlobalMigrationIndicator state:', {
    migrationState: migrationState?.status,
    isOnProvisioningPage,
    isVisible,
    isExpanded,
    shouldHideAll,
    shouldShowMinimized,
    pathname: location.pathname
  });

  // Show minimized indicator if needed
  if (shouldShowMinimized) {
    return (
      <MinimizedMigrationIndicator 
        onExpand={() => {
          setIsExpanded(true);
          setIsVisible(true);
        }} 
      />
    );
  }

  // Hide all if conditions met
  if (shouldHideAll || !isVisible || !isExpanded) {
    return null;
  }

  const getStatusIcon = () => {
    switch (migrationState.status) {
      case 'processing':
        return <RefreshCw className="rotating" size={16} />;
      case 'completed':
        return <CheckCircle size={16} />;
      case 'error':
        return <AlertCircle size={16} />;
      default:
        return <Upload size={16} />;
    }
  };

  const getStatusColor = () => {
    switch (migrationState.status) {
      case 'processing':
        return 'warning';
      case 'completed':
        return 'success';
      case 'error':
        return 'danger';
      default:
        return 'secondary';
    }
  };

  const getStatusText = () => {
    switch (migrationState.status) {
      case 'processing':
        return 'Migration in Progress';
      case 'completed':
        return 'Migration Completed';
      case 'error':
        return 'Migration Failed';
      default:
        return 'Migration';
    }
  };

  const progress = migrationState.progress || 0;
  const statusColor = getStatusColor();

  const handleClose = () => {
    setIsVisible(false);
    setIsExpanded(false);
  };

  const handleMinimize = () => {
    setIsExpanded(false);
  };

  const handleNavigate = () => {
    // Set flag to navigate to bulk tab
    setShouldNavigateToBulkTab(true);
    
    if (onNavigateToMigration) {
      onNavigateToMigration();
    } else {
      // Default navigation - you can implement this based on your routing needs
      window.location.href = '/user-provisioning';
    }
  };


  return (
    <div 
      className="position-fixed shadow-lg animate__animated animate__slideInUp"
      style={{ 
        bottom: '20px', 
        right: '20px', 
        zIndex: 1050,
        width: '340px'
      }}
    >
      <div className={`card border-0 bg-white shadow-lg border border-${statusColor}`} 
           style={{ borderWidth: '2px !important' }}>
        <div className="card-body p-3">
          {/* Header */}
          <div className="d-flex align-items-center justify-content-between mb-3">
            <div className="d-flex align-items-center">
              <span className={`text-${statusColor} me-2`}>
                {getStatusIcon()}
              </span>
              <h6 className={`mb-0 text-${statusColor} fw-bold`}>
                {getStatusText()}
              </h6>
            </div>
            <div className="d-flex align-items-center gap-1">
              <button
                onClick={handleMinimize}
                className={`btn btn-sm btn-outline-${statusColor} p-1`}
                style={{ width: '24px', height: '24px' }}
                title="Minimize"
              >
                <Minimize2 size={12} />
              </button>
              <button
                onClick={handleClose}
                className={`btn btn-sm btn-outline-${statusColor} p-1`}
                style={{ width: '24px', height: '24px' }}
                title="Close"
              >
                <X size={12} />
              </button>
            </div>
          </div>

          {/* Progress Section */}
          {migrationState.status === 'processing' && (
            <div className="mb-3">
              <div className="d-flex justify-content-between align-items-center mb-2">
                <span className="fw-semibold text-dark">Migration Progress</span>
                <span className={`badge bg-${statusColor} fs-6 px-2 py-1`}>
                  {Math.round(progress)}%
                </span>
              </div>
              <div className="progress mb-2" style={{ height: '8px' }}>
                <div
                  className={`progress-bar progress-bar-striped progress-bar-animated bg-${statusColor}`}
                  style={{ width: `${progress}%` }}
                />
              </div>
              <div className="d-flex justify-content-between">
                <small className="text-muted">0%</small>
                <small className="text-muted">100%</small>
              </div>
            </div>
          )}

          {/* Results Section for completed/error states */}
          {migrationState.results && migrationState.status !== 'processing' && (
            <div className="row g-2 mb-3">
              <div className="col-6">
                <div className="text-center p-2 bg-success bg-opacity-10 rounded border border-success">
                  <div className="fw-bold text-success">
                    {migrationState.results.successful}
                  </div>
                  <div className="small text-success">Successful</div>
                </div>
              </div>
              <div className="col-6">
                <div className="text-center p-2 bg-danger bg-opacity-10 rounded border border-danger">
                  <div className="fw-bold text-danger">
                    {migrationState.results.failed}
                  </div>
                  <div className="small text-danger">Failed</div>
                </div>
              </div>
            </div>
          )}

          {/* Processing details for active migration */}
          {migrationState.status === 'processing' && migrationState.results && (
            <div className="row g-2 mb-3">
              <div className="col-6">
                <div className="small text-muted">Processed</div>
                <div className="fw-semibold text-success">
                  {migrationState.results.successful}
                </div>
              </div>
              <div className="col-6">
                <div className="small text-muted">Failed</div>
                <div className="fw-semibold text-danger">
                  {migrationState.results.failed}
                </div>
              </div>
            </div>
          )}

          {/* Footer */}
          <div className="d-flex justify-content-between align-items-center">
            {migrationState.startTime && (
              <small className="text-muted">
                {new Date(migrationState.startTime).toLocaleTimeString()}
              </small>
            )}
            <button
              onClick={handleNavigate}
              className={`btn btn-sm btn-${statusColor} fw-semibold`}
            >
              View Details
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};