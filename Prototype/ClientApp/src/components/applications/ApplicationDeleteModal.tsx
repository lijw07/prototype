// ApplicationDeleteModal component - handles application deletion confirmation
// Follows SRP by focusing only on delete confirmation UI

import React from 'react';
import { Trash2, AlertTriangle } from 'lucide-react';
import type { Application } from '../../types/api.types';
import { getDataSourceTypeName } from './hooks/useApplicationForm';

interface ApplicationDeleteModalProps {
  show: boolean;
  application: Application | null;
  onClose: () => void;
  onConfirm: (application: Application) => Promise<void>;
  isDeleting: boolean;
}

export const ApplicationDeleteModal: React.FC<ApplicationDeleteModalProps> = ({
  show,
  application,
  onClose,
  onConfirm,
  isDeleting
}) => {
  const handleConfirm = async () => {
    if (!application || isDeleting) return;
    await onConfirm(application);
  };

  if (!show || !application) return null;

  const dataSourceTypeName = getDataSourceTypeName(application.applicationDataSourceType);

  return (
    <div className="modal fade show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header border-0">
            <h5 className="modal-title d-flex align-items-center text-danger">
              <AlertTriangle size={20} className="me-2" />
              Confirm Deletion
            </h5>
            <button
              type="button"
              className="btn-close"
              onClick={onClose}
              disabled={isDeleting}
            />
          </div>

          <div className="modal-body">
            <div className="alert alert-danger d-flex align-items-start">
              <Trash2 size={20} className="text-danger me-3 mt-1 flex-shrink-0" />
              <div>
                <h6 className="alert-heading mb-2">Warning: This action cannot be undone</h6>
                <p className="mb-0">
                  You are about to permanently delete this application and all its associated data.
                </p>
              </div>
            </div>

            <div className="card">
              <div className="card-body">
                <h6 className="card-title mb-3">Application Details</h6>
                
                <div className="row g-2">
                  <div className="col-4">
                    <strong>Name:</strong>
                  </div>
                  <div className="col-8">
                    {application.applicationName}
                  </div>

                  <div className="col-4">
                    <strong>Type:</strong>
                  </div>
                  <div className="col-8">
                    <span className="badge bg-primary">{dataSourceTypeName}</span>
                  </div>

                  <div className="col-4">
                    <strong>Host:</strong>
                  </div>
                  <div className="col-8">
                    {application.connection.host}:{application.connection.port}
                  </div>

                  <div className="col-4">
                    <strong>Database:</strong>
                  </div>
                  <div className="col-8">
                    {application.connection.databaseName}
                  </div>

                  <div className="col-4">
                    <strong>Auth Type:</strong>
                  </div>
                  <div className="col-8">
                    <span className="badge bg-secondary">
                      {application.connection.authenticationType}
                    </span>
                  </div>

                  {application.createdAt && (
                    <>
                      <div className="col-4">
                        <strong>Created:</strong>
                      </div>
                      <div className="col-8">
                        {new Date(application.createdAt).toLocaleString()}
                      </div>
                    </>
                  )}
                </div>

                <div className="mt-3">
                  <strong>Description:</strong>
                  <p className="text-muted mt-1 mb-0">
                    {application.applicationDescription}
                  </p>
                </div>
              </div>
            </div>

            <div className="mt-3">
              <h6 className="text-danger">What will be deleted:</h6>
              <ul className="text-muted">
                <li>Application configuration and settings</li>
                <li>Connection information (credentials will be removed)</li>
                <li>User access permissions for this application</li>
                <li>Associated logs and audit trails</li>
                <li>Any scheduled tasks or automation rules</li>
              </ul>
            </div>

            <div className="mt-3 p-3 bg-light rounded">
              <div className="form-check">
                <input 
                  className="form-check-input" 
                  type="checkbox" 
                  id="confirmDelete"
                  required
                />
                <label className="form-check-label fw-semibold" htmlFor="confirmDelete">
                  I understand that this action is permanent and cannot be undone
                </label>
              </div>
            </div>
          </div>

          <div className="modal-footer">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={onClose}
              disabled={isDeleting}
            >
              Cancel
            </button>
            <button
              type="button"
              className="btn btn-danger"
              onClick={handleConfirm}
              disabled={isDeleting}
            >
              {isDeleting ? (
                <>
                  <div className="spinner-border spinner-border-sm me-2" role="status" />
                  Deleting...
                </>
              ) : (
                <>
                  <Trash2 size={16} className="me-2" />
                  Delete Application
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ApplicationDeleteModal;