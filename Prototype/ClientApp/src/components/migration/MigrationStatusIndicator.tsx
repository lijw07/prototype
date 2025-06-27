import React from 'react';
import { RefreshCw, CheckCircle, AlertCircle, Upload } from 'lucide-react';
import { useMigration } from '../../contexts/MigrationContext';

interface MigrationStatusIndicatorProps {
  compact?: boolean;
}

export const MigrationStatusIndicator: React.FC<MigrationStatusIndicatorProps> = ({ compact = false }) => {
  const { migrationState } = useMigration();

  if (!migrationState || migrationState.status === 'idle') {
    return null;
  }

  const getStatusIcon = () => {
    switch (migrationState.status) {
      case 'processing':
        return <RefreshCw className="rotating" size={compact ? 14 : 18} />;
      case 'completed':
        return <CheckCircle size={compact ? 14 : 18} />;
      case 'error':
        return <AlertCircle size={compact ? 14 : 18} />;
      default:
        return <Upload size={compact ? 14 : 18} />;
    }
  };

  const getStatusColor = () => {
    switch (migrationState.status) {
      case 'processing':
        return 'text-warning';
      case 'completed':
        return 'text-success';
      case 'error':
        return 'text-danger';
      default:
        return 'text-secondary';
    }
  };

  const getStatusText = () => {
    switch (migrationState.status) {
      case 'processing':
        return compact ? 'Migration...' : 'Migration in Progress';
      case 'completed':
        return compact ? 'Completed' : 'Migration Completed';
      case 'error':
        return compact ? 'Failed' : 'Migration Failed';
      default:
        return 'Migration';
    }
  };

  const progress = migrationState.progress || 0;

  if (compact) {
    return (
      <div className="d-flex align-items-center">
        <span className={`me-2 ${getStatusColor()}`}>
          {getStatusIcon()}
        </span>
        <div className="flex-grow-1" style={{ minWidth: '60px' }}>
          <div className="d-flex align-items-center">
            <small className={`fw-semibold ${getStatusColor()}`} style={{ fontSize: '0.75rem' }}>
              {migrationState.status === 'processing' ? `${Math.round(progress)}%` : getStatusText()}
            </small>
          </div>
          {migrationState.status === 'processing' && (
            <div className="progress mt-1" style={{ height: '2px' }}>
              <div
                className="progress-bar progress-bar-striped progress-bar-animated bg-warning"
                style={{ width: `${progress}%` }}
              />
            </div>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="card border-0 shadow-sm mb-3">
      <div className="card-body p-3">
        <div className="d-flex align-items-center mb-2">
          <span className={`me-2 ${getStatusColor()}`}>
            {getStatusIcon()}
          </span>
          <h6 className={`mb-0 ${getStatusColor()}`}>
            {getStatusText()}
          </h6>
          {migrationState.status === 'processing' && (
            <span className="ms-auto badge bg-warning text-dark">
              {Math.round(progress)}%
            </span>
          )}
        </div>

        {migrationState.status === 'processing' && (
          <div className="progress mb-2" style={{ height: '6px' }}>
            <div
              className="progress-bar progress-bar-striped progress-bar-animated bg-warning"
              style={{ width: `${progress}%` }}
            />
          </div>
        )}

        {migrationState.results && (
          <div className="row g-2">
            <div className="col-6">
              <small className="text-muted d-block">Processed</small>
              <small className="fw-semibold text-success">
                {migrationState.results.successful}
              </small>
            </div>
            <div className="col-6">
              <small className="text-muted d-block">Failed</small>
              <small className="fw-semibold text-danger">
                {migrationState.results.failed}
              </small>
            </div>
          </div>
        )}

        {migrationState.startTime && (
          <small className="text-muted d-block mt-2">
            Started: {new Date(migrationState.startTime).toLocaleTimeString()}
          </small>
        )}
      </div>
    </div>
  );
};