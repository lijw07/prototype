// ApplicationsList component - displays paginated grid of application cards
// Follows SRP by focusing only on application display logic

import React from 'react';
import { Database, TestTube, Trash2, Edit } from 'lucide-react';
import type { Application } from '../../types/api.types';
import { getDataSourceTypeName } from './hooks/useApplicationForm';

interface ApplicationsListProps {
  applications: Application[];
  loading: boolean;
  onEdit: (application: Application) => void;
  onDelete: (application: Application) => void;
  onTestConnection: (application: Application) => void;
  getTestResult: (applicationId: string) => any;
  testing: boolean;
}

export const ApplicationsList: React.FC<ApplicationsListProps> = ({
  applications,
  loading,
  onEdit,
  onDelete,
  onTestConnection,
  getTestResult,
  testing
}) => {
  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '200px' }}>
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading applications...</span>
        </div>
      </div>
    );
  }

  if (applications.length === 0) {
    return (
      <div className="text-center py-5">
        <Database size={48} className="text-muted mb-3" />
        <h5 className="text-muted">No applications found</h5>
        <p className="text-muted">Create your first application to get started.</p>
      </div>
    );
  }

  return (
    <div className="row g-4">
      {applications.map((app) => {
        const testResult = getTestResult(app.applicationId);
        const dataSourceTypeName = getDataSourceTypeName(app.applicationDataSourceType);
        
        return (
          <div key={app.applicationId} className="col-12 col-md-6 col-lg-4 col-xl-3">
            <div className="card h-100 border-0 shadow-sm">
              <div className="card-header bg-light border-0 d-flex align-items-center justify-content-between">
                <div className="d-flex align-items-center">
                  <Database size={20} className="text-primary me-2" />
                  <span className="fw-semibold text-truncate" title={app.applicationName}>
                    {app.applicationName}
                  </span>
                </div>
                <div className="dropdown">
                  <button 
                    className="btn btn-sm btn-outline-secondary dropdown-toggle" 
                    type="button" 
                    data-bs-toggle="dropdown"
                  >
                    Actions
                  </button>
                  <ul className="dropdown-menu">
                    <li>
                      <button 
                        className="dropdown-item d-flex align-items-center"
                        onClick={() => onEdit(app)}
                      >
                        <Edit size={16} className="me-2" />
                        Edit
                      </button>
                    </li>
                    <li>
                      <button 
                        className="dropdown-item d-flex align-items-center"
                        onClick={() => onTestConnection(app)}
                        disabled={testing}
                      >
                        <TestTube size={16} className="me-2" />
                        Test Connection
                      </button>
                    </li>
                    <li><hr className="dropdown-divider" /></li>
                    <li>
                      <button 
                        className="dropdown-item text-danger d-flex align-items-center"
                        onClick={() => onDelete(app)}
                      >
                        <Trash2 size={16} className="me-2" />
                        Delete
                      </button>
                    </li>
                  </ul>
                </div>
              </div>

              <div className="card-body">
                <p className="card-text text-muted small mb-3" title={app.applicationDescription}>
                  {app.applicationDescription.length > 100 
                    ? `${app.applicationDescription.substring(0, 100)}...`
                    : app.applicationDescription
                  }
                </p>

                <div className="mb-3">
                  <div className="d-flex justify-content-between align-items-center mb-2">
                    <span className="text-muted small">Type:</span>
                    <span className="badge bg-primary">{dataSourceTypeName}</span>
                  </div>
                  <div className="d-flex justify-content-between align-items-center mb-2">
                    <span className="text-muted small">Host:</span>
                    <span className="small text-truncate" title={app.connection.host}>
                      {app.connection.host}:{app.connection.port}
                    </span>
                  </div>
                  <div className="d-flex justify-content-between align-items-center mb-2">
                    <span className="text-muted small">Database:</span>
                    <span className="small text-truncate" title={app.connection.databaseName}>
                      {app.connection.databaseName}
                    </span>
                  </div>
                  <div className="d-flex justify-content-between align-items-center">
                    <span className="text-muted small">Auth:</span>
                    <span className="badge bg-secondary small">
                      {app.connection.authenticationType}
                    </span>
                  </div>
                </div>

                {/* Connection Test Status */}
                {testResult && (
                  <div className={`alert ${testResult.success ? 'alert-success' : 'alert-danger'} alert-sm p-2 mb-2`}>
                    <div className="d-flex align-items-center">
                      {testResult.success ? (
                        <div className="text-success me-2">✓</div>
                      ) : (
                        <div className="text-danger me-2">✗</div>
                      )}
                      <div className="flex-grow-1">
                        <div className="small fw-semibold">
                          {testResult.success ? 'Connection Successful' : 'Connection Failed'}
                        </div>
                        <div className="text-muted" style={{ fontSize: '0.75rem' }}>
                          {new Date(testResult.timestamp).toLocaleString()}
                        </div>
                      </div>
                    </div>
                  </div>
                )}
              </div>

              <div className="card-footer bg-transparent border-0 pt-0">
                <div className="d-flex justify-content-between align-items-center text-muted">
                  <small>
                    Created: {app.createdAt ? new Date(app.createdAt).toLocaleDateString() : 'Unknown'}
                  </small>
                  {app.updatedAt && (
                    <small>
                      Updated: {new Date(app.updatedAt).toLocaleDateString()}
                    </small>
                  )}
                </div>
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
};

export default ApplicationsList;