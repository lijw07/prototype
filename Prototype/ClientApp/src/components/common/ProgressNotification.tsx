import React, { useState } from 'react';
import {
  RefreshCw,
  CheckCircle,
  AlertCircle,
  X,
  Minimize2,
  Maximize2,
  Clock
} from 'lucide-react';

interface ProgressNotificationProps {
  isVisible: boolean;
  status: 'idle' | 'processing' | 'completed' | 'error';
  progress: number;
  title: string;
  description?: string;
  details?: {
    processedRecords?: number;
    totalRecords?: number;
    processedFiles?: number;
    totalFiles?: number;
    currentOperation?: string;
  };
  results?: {
    successful: number;
    failed: number;
    errors: string[];
    processedFiles: number;
    totalFiles: number;
  };
  onClose: () => void;
  onMinimize: () => void;
  onMaximize?: () => void;
  isMinimized?: boolean;
}

export default function ProgressNotification({
  isVisible,
  status,
  progress,
  title,
  description,
  details,
  results,
  onClose,
  onMinimize,
  onMaximize,
  isMinimized = false
}: ProgressNotificationProps) {
  if (!isVisible) return null;

  const getStatusColor = () => {
    switch (status) {
      case 'processing': return 'bg-warning';
      case 'completed': return 'bg-success';
      case 'error': return 'bg-danger';
      default: return 'bg-secondary';
    }
  };

  const getStatusIcon = () => {
    switch (status) {
      case 'processing': return <RefreshCw className="rotating" size={16} />;
      case 'completed': return <CheckCircle size={16} />;
      case 'error': return <AlertCircle size={16} />;
      default: return <Clock size={16} />;
    }
  };

  const getStatusText = () => {
    switch (status) {
      case 'processing': return 'In Progress';
      case 'completed': return 'Completed';
      case 'error': return 'Failed';
      default: return 'Idle';
    }
  };

  if (isMinimized) {
    return (
      <div 
        className="position-fixed bottom-0 start-0 w-100 p-3"
        style={{ zIndex: 1050 }}
      >
        <div className="container-fluid">
          <div 
            className={`card border-0 shadow-lg ${getStatusColor()} bg-opacity-10 border-start border-4 border-${getStatusColor().replace('bg-', '')}`}
            style={{ cursor: 'pointer' }}
            onClick={onMaximize}
          >
            <div className="card-body p-3">
              <div className="row align-items-center">
                <div className="col-auto">
                  <div className={`text-${getStatusColor().replace('bg-', '')}`}>
                    {getStatusIcon()}
                  </div>
                </div>
                <div className="col">
                  <div className="d-flex align-items-center justify-content-between">
                    <div>
                      <h6 className="mb-0 fw-semibold">{title}</h6>
                      <small className="text-muted">
                        {status === 'processing' ? `${Math.round(progress)}% complete` : getStatusText()}
                      </small>
                    </div>
                    {status === 'processing' && (
                      <div className="ms-3" style={{ width: '120px' }}>
                        <div className="progress" style={{ height: '4px' }}>
                          <div 
                            className={`progress-bar ${status === 'processing' ? 'progress-bar-striped progress-bar-animated' : ''}`}
                            style={{ width: `${progress}%` }}
                          ></div>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
                <div className="col-auto">
                  <button
                    className="btn btn-sm btn-outline-secondary"
                    onClick={(e) => {
                      e.stopPropagation();
                      onClose();
                    }}
                    title="Close notification"
                  >
                    <X size={14} />
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div 
      className="position-fixed bottom-0 end-0 p-4"
      style={{ zIndex: 1050, maxWidth: '400px', width: '100%' }}
    >
      <div className={`card border-0 shadow-lg ${getStatusColor()} bg-opacity-10`}>
        <div className="card-header bg-transparent border-0 pb-2">
          <div className="d-flex align-items-center justify-content-between">
            <div className="d-flex align-items-center">
              <div className={`me-2 text-${getStatusColor().replace('bg-', '')}`}>
                {getStatusIcon()}
              </div>
              <h6 className="mb-0 fw-semibold">{title}</h6>
            </div>
            <div className="d-flex gap-1">
              <button
                className="btn btn-sm btn-outline-secondary"
                onClick={onMinimize}
                title="Minimize to bottom bar"
              >
                <Minimize2 size={14} />
              </button>
              <button
                className="btn btn-sm btn-outline-secondary"
                onClick={onClose}
                title="Close notification"
              >
                <X size={14} />
              </button>
            </div>
          </div>
        </div>
        
        <div className="card-body pt-0">
          {description && (
            <p className="text-muted small mb-3">{description}</p>
          )}
          
          {status === 'processing' && (
            <div>
              <div className="progress mb-3" style={{ height: '8px' }}>
                <div 
                  className="progress-bar progress-bar-striped progress-bar-animated"
                  style={{ width: `${progress}%` }}
                ></div>
              </div>
              
              <div className="d-flex justify-content-between align-items-center mb-2">
                <small className="text-muted">
                  {details?.currentOperation || 'Processing...'} {Math.round(progress)}% complete
                </small>
                {details && (
                  <small className="text-muted">
                    {details.processedRecords}/{details.totalRecords} records
                  </small>
                )}
              </div>
              
              {details && (
                <div className="row g-2 mt-2">
                  <div className="col-6">
                    <div className="text-center p-2 bg-light rounded">
                      <div className="small fw-semibold">{details.processedRecords || 0}</div>
                      <div style={{ fontSize: '0.7rem' }} className="text-muted">Processed</div>
                    </div>
                  </div>
                  <div className="col-6">
                    <div className="text-center p-2 bg-light rounded">
                      <div className="small fw-semibold">{details.processedFiles || 0}/{details.totalFiles || 0}</div>
                      <div style={{ fontSize: '0.7rem' }} className="text-muted">Files</div>
                    </div>
                  </div>
                </div>
              )}
            </div>
          )}
          
          {status === 'completed' && results && (
            <div>
              <div className="row g-2 mb-3">
                <div className="col-6">
                  <div className="text-center p-2 bg-success bg-opacity-10 rounded">
                    <div className="fw-semibold text-success">{results.successful}</div>
                    <div className="small text-muted">Success</div>
                  </div>
                </div>
                <div className="col-6">
                  <div className="text-center p-2 bg-danger bg-opacity-10 rounded">
                    <div className="fw-semibold text-danger">{results.failed}</div>
                    <div className="small text-muted">Failed</div>
                  </div>
                </div>
              </div>
              
              {results.errors.length > 0 && (
                <div className="alert alert-warning py-2 mb-0">
                  <small className="fw-semibold">Errors:</small>
                  <ul className="mb-0 mt-1" style={{ fontSize: '0.75rem' }}>
                    {results.errors.slice(0, 2).map((error, index) => (
                      <li key={index}>{error}</li>
                    ))}
                    {results.errors.length > 2 && (
                      <li className="text-muted">... and {results.errors.length - 2} more</li>
                    )}
                  </ul>
                </div>
              )}
            </div>
          )}
          
          {status === 'error' && results && (
            <div>
              <div className="alert alert-danger py-2 mb-0">
                <small className="fw-semibold">Error Details:</small>
                <ul className="mb-0 mt-1" style={{ fontSize: '0.75rem' }}>
                  {results.errors.slice(0, 3).map((error, index) => (
                    <li key={index}>{error}</li>
                  ))}
                  {results.errors.length > 3 && (
                    <li className="text-muted">... and {results.errors.length - 3} more</li>
                  )}
                </ul>
              </div>
            </div>
          )}
        </div>
      </div>
      
      <style>{`
        .rotating {
          animation: spin 1s linear infinite;
        }
        
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
      `}</style>
    </div>
  );
}