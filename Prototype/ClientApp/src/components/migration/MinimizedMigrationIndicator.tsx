import React from 'react';
import { RefreshCw, CheckCircle, AlertCircle, Upload } from 'lucide-react';
import { useMigration } from '../../context/MigrationContext';

interface MinimizedMigrationIndicatorProps {
  onExpand: () => void;
}

export const MinimizedMigrationIndicator: React.FC<MinimizedMigrationIndicatorProps> = ({ 
  onExpand 
}) => {
  const { migrationState } = useMigration();

  // Don't show if no migration state or idle
  if (!migrationState || migrationState.status === 'idle') {
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

  const progress = migrationState.progress || 0;
  const statusColor = getStatusColor();

  return (
    <div 
      className="position-fixed"
      style={{ 
        bottom: '20px', 
        right: '20px', 
        zIndex: 1050,
        cursor: 'pointer'
      }}
      onClick={onExpand}
      title="Click to view migration details"
    >
      <div className={`badge bg-${statusColor} d-flex align-items-center p-3 shadow-lg`} 
           style={{ borderRadius: '12px' }}>
        <span className="me-2">
          {getStatusIcon()}
        </span>
        {migrationState.status === 'processing' && (
          <span className="fw-bold">
            {Math.round(progress)}%
          </span>
        )}
        {migrationState.status === 'completed' && (
          <span className="fw-bold">
            Done
          </span>
        )}
        {migrationState.status === 'error' && (
          <span className="fw-bold">
            Error
          </span>
        )}
      </div>
    </div>
  );
};