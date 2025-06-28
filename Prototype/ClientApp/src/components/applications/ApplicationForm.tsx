// ApplicationForm component - handles create/edit form UI
// Follows SRP by focusing only on form presentation logic

import React from 'react';
import { Eye, EyeOff, Save, X } from 'lucide-react';
import { DataSourceTypeEnum } from './hooks/useApplicationForm';
import type { ApplicationFormData } from './hooks/useApplicationForm';

interface ApplicationFormProps {
  show: boolean;
  onClose: () => void;
  onSubmit: (data: ApplicationFormData) => Promise<void>;
  formData: ApplicationFormData;
  errors: { [key: string]: string | undefined };
  isValid: boolean;
  isSubmitting: boolean;
  isEditing: boolean;
  showPasswords: { password: boolean; awsSecretAccessKey: boolean };
  onFieldChange: (fieldPath: string, value: any) => void;
  onTogglePasswordVisibility: (field: 'password' | 'awsSecretAccessKey') => void;
  getAuthenticationOptions: () => Array<{ value: string; label: string }>;
}

export const ApplicationForm: React.FC<ApplicationFormProps> = ({
  show,
  onClose,
  onSubmit,
  formData,
  errors,
  isValid,
  isSubmitting,
  isEditing,
  showPasswords,
  onFieldChange,
  onTogglePasswordVisibility,
  getAuthenticationOptions
}) => {
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isValid || isSubmitting) return;
    await onSubmit(formData);
  };

  const getDataSourceOptions = () => {
    return Object.entries(DataSourceTypeEnum).map(([value, label]) => ({
      value: parseInt(value),
      label
    }));
  };

  const authOptions = getAuthenticationOptions();

  if (!show) return null;

  return (
    <div className="modal fade show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
      <div className="modal-dialog modal-lg">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">
              {isEditing ? 'Edit Application' : 'Create New Application'}
            </h5>
            <button
              type="button"
              className="btn-close"
              onClick={onClose}
              disabled={isSubmitting}
            />
          </div>

          <form onSubmit={handleSubmit}>
            <div className="modal-body">
              <div className="row g-3">
                {/* Basic Information */}
                <div className="col-12">
                  <h6 className="border-bottom pb-2 mb-3">Basic Information</h6>
                </div>

                <div className="col-12">
                  <label className="form-label">Application Name *</label>
                  <input
                    type="text"
                    className={`form-control ${errors.applicationName ? 'is-invalid' : ''}`}
                    value={formData.applicationName}
                    onChange={(e) => onFieldChange('applicationName', e.target.value)}
                    placeholder="Enter application name"
                    disabled={isSubmitting}
                  />
                  {errors.applicationName && (
                    <div className="invalid-feedback">{errors.applicationName}</div>
                  )}
                </div>

                <div className="col-12">
                  <label className="form-label">Description *</label>
                  <textarea
                    className={`form-control ${errors.applicationDescription ? 'is-invalid' : ''}`}
                    rows={3}
                    value={formData.applicationDescription}
                    onChange={(e) => onFieldChange('applicationDescription', e.target.value)}
                    placeholder="Enter application description"
                    disabled={isSubmitting}
                  />
                  {errors.applicationDescription && (
                    <div className="invalid-feedback">{errors.applicationDescription}</div>
                  )}
                </div>

                <div className="col-12">
                  <label className="form-label">Data Source Type *</label>
                  <select
                    className="form-select"
                    value={formData.applicationDataSourceType}
                    onChange={(e) => onFieldChange('applicationDataSourceType', parseInt(e.target.value))}
                    disabled={isSubmitting}
                  >
                    {getDataSourceOptions().map(option => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </div>

                {/* Connection Information */}
                <div className="col-12 mt-4">
                  <h6 className="border-bottom pb-2 mb-3">Connection Information</h6>
                </div>

                <div className="col-md-8">
                  <label className="form-label">Host *</label>
                  <input
                    type="text"
                    className={`form-control ${errors['connection.host'] ? 'is-invalid' : ''}`}
                    value={formData.connection.host}
                    onChange={(e) => onFieldChange('connection.host', e.target.value)}
                    placeholder="localhost or IP address"
                    disabled={isSubmitting}
                  />
                  {errors['connection.host'] && (
                    <div className="invalid-feedback">{errors['connection.host']}</div>
                  )}
                </div>

                <div className="col-md-4">
                  <label className="form-label">Port *</label>
                  <input
                    type="number"
                    className={`form-control ${errors['connection.port'] ? 'is-invalid' : ''}`}
                    value={formData.connection.port}
                    onChange={(e) => onFieldChange('connection.port', e.target.value)}
                    placeholder="1433"
                    min="1"
                    max="65535"
                    disabled={isSubmitting}
                  />
                  {errors['connection.port'] && (
                    <div className="invalid-feedback">{errors['connection.port']}</div>
                  )}
                </div>

                <div className="col-12">
                  <label className="form-label">Database Name *</label>
                  <input
                    type="text"
                    className={`form-control ${errors['connection.databaseName'] ? 'is-invalid' : ''}`}
                    value={formData.connection.databaseName}
                    onChange={(e) => onFieldChange('connection.databaseName', e.target.value)}
                    placeholder="Database name"
                    disabled={isSubmitting}
                  />
                  {errors['connection.databaseName'] && (
                    <div className="invalid-feedback">{errors['connection.databaseName']}</div>
                  )}
                </div>

                {/* Authentication */}
                <div className="col-12 mt-4">
                  <h6 className="border-bottom pb-2 mb-3">Authentication</h6>
                </div>

                <div className="col-12">
                  <label className="form-label">Authentication Type *</label>
                  <select
                    className="form-select"
                    value={formData.connection.authenticationType}
                    onChange={(e) => onFieldChange('connection.authenticationType', e.target.value)}
                    disabled={isSubmitting}
                  >
                    {authOptions.map(option => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </div>

                {/* Username/Password fields - shown for UserPassword auth */}
                {formData.connection.authenticationType === 'UserPassword' && (
                  <>
                    <div className="col-md-6">
                      <label className="form-label">Username *</label>
                      <input
                        type="text"
                        className={`form-control ${errors['connection.username'] ? 'is-invalid' : ''}`}
                        value={formData.connection.username || ''}
                        onChange={(e) => onFieldChange('connection.username', e.target.value)}
                        placeholder="Username"
                        disabled={isSubmitting}
                      />
                      {errors['connection.username'] && (
                        <div className="invalid-feedback">{errors['connection.username']}</div>
                      )}
                    </div>

                    <div className="col-md-6">
                      <label className="form-label">Password *</label>
                      <div className="input-group">
                        <input
                          type={showPasswords.password ? 'text' : 'password'}
                          className={`form-control ${errors['connection.password'] ? 'is-invalid' : ''}`}
                          value={formData.connection.password || ''}
                          onChange={(e) => onFieldChange('connection.password', e.target.value)}
                          placeholder="Password"
                          disabled={isSubmitting}
                        />
                        <button
                          type="button"
                          className="btn btn-outline-secondary"
                          onClick={() => onTogglePasswordVisibility('password')}
                          disabled={isSubmitting}
                        >
                          {showPasswords.password ? <EyeOff size={16} /> : <Eye size={16} />}
                        </button>
                        {errors['connection.password'] && (
                          <div className="invalid-feedback">{errors['connection.password']}</div>
                        )}
                      </div>
                    </div>
                  </>
                )}

                {/* Additional fields for specific authentication types */}
                {formData.connection.authenticationType === 'AzureAdPassword' && (
                  <div className="col-12">
                    <label className="form-label">Principal</label>
                    <input
                      type="text"
                      className="form-control"
                      value={formData.connection.principal || ''}
                      onChange={(e) => onFieldChange('connection.principal', e.target.value)}
                      placeholder="Azure AD Principal"
                      disabled={isSubmitting}
                    />
                  </div>
                )}

                {/* MongoDB specific fields */}
                {formData.applicationDataSourceType === 3 && (
                  <div className="col-12">
                    <label className="form-label">Authentication Database</label>
                    <input
                      type="text"
                      className="form-control"
                      value={formData.connection.authenticationDatabase || ''}
                      onChange={(e) => onFieldChange('connection.authenticationDatabase', e.target.value)}
                      placeholder="Authentication database (optional)"
                      disabled={isSubmitting}
                    />
                  </div>
                )}

                {/* AWS specific fields */}
                {(formData.applicationDataSourceType === 20 || formData.applicationDataSourceType === 22 || formData.applicationDataSourceType === 23) && (
                  <>
                    <div className="col-md-6">
                      <label className="form-label">AWS Access Key ID</label>
                      <input
                        type="text"
                        className="form-control"
                        value={formData.connection.awsAccessKeyId || ''}
                        onChange={(e) => onFieldChange('connection.awsAccessKeyId', e.target.value)}
                        placeholder="AWS Access Key ID"
                        disabled={isSubmitting}
                      />
                    </div>

                    <div className="col-md-6">
                      <label className="form-label">AWS Secret Access Key</label>
                      <div className="input-group">
                        <input
                          type={showPasswords.awsSecretAccessKey ? 'text' : 'password'}
                          className="form-control"
                          value={formData.connection.awsSecretAccessKey || ''}
                          onChange={(e) => onFieldChange('connection.awsSecretAccessKey', e.target.value)}
                          placeholder="AWS Secret Access Key"
                          disabled={isSubmitting}
                        />
                        <button
                          type="button"
                          className="btn btn-outline-secondary"
                          onClick={() => onTogglePasswordVisibility('awsSecretAccessKey')}
                          disabled={isSubmitting}
                        >
                          {showPasswords.awsSecretAccessKey ? <EyeOff size={16} /> : <Eye size={16} />}
                        </button>
                      </div>
                    </div>

                    {formData.connection.awsRoleArn !== undefined && (
                      <div className="col-12">
                        <label className="form-label">AWS Role ARN</label>
                        <input
                          type="text"
                          className="form-control"
                          value={formData.connection.awsRoleArn || ''}
                          onChange={(e) => onFieldChange('connection.awsRoleArn', e.target.value)}
                          placeholder="arn:aws:iam::account:role/role-name"
                          disabled={isSubmitting}
                        />
                      </div>
                    )}
                  </>
                )}
              </div>
            </div>

            <div className="modal-footer">
              <button
                type="button"
                className="btn btn-secondary"
                onClick={onClose}
                disabled={isSubmitting}
              >
                <X size={16} className="me-2" />
                Cancel
              </button>
              <button
                type="submit"
                className="btn btn-primary"
                disabled={!isValid || isSubmitting}
              >
                {isSubmitting ? (
                  <div className="spinner-border spinner-border-sm me-2" role="status" />
                ) : (
                  <Save size={16} className="me-2" />
                )}
                {isSubmitting ? 'Saving...' : isEditing ? 'Update Application' : 'Create Application'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

export default ApplicationForm;