// Auto Provisioning Panel Component
// Follows SRP: Only responsible for auto-provisioning configuration and management

import React, { useState, useEffect, useCallback } from 'react';
import { 
  Settings,
  Play,
  Pause,
  Clock,
  CheckCircle,
  AlertTriangle,
  RefreshCw,
  Zap,
  Calendar,
  Mail,
  Users,
  Shield,
  BookOpen,
  Save,
  Edit,
  Eye
} from 'lucide-react';
import { useProvisioning } from './hooks/useProvisioning';
import type { 
  AutoProvisioningProps, 
  AutoProvisioningConfig, 
  EligibilityCriteria,
  ProvisioningTemplate 
} from './types/provisioning.types';

const AutoProvisioningPanel: React.FC<AutoProvisioningProps> = ({
  className = '',
  config: initialConfig,
  onConfigUpdate,
  onError,
  onSuccess
}) => {
  const {
    templates: provisioningTemplates,
    loading,
    error,
    fetchTemplates: fetchProvisioningTemplates,
    autoProvision: triggerAutoProvisioning
  } = useProvisioning();

  // Mock auto provisioning config since the hook doesn't provide it yet
  const autoProvisioningConfig = null;
  const fetchAutoProvisioningConfig = () => {};
  const updateAutoProvisioningConfig = async (config: AutoProvisioningConfig) => ({ 
    success: true,
    message: 'Configuration updated successfully'
  });

  const [isEditing, setIsEditing] = useState(false);
  const [localConfig, setLocalConfig] = useState<AutoProvisioningConfig>({
    enabled: false,
    scheduleType: 'manual',
    scheduleTime: '02:00',
    batchSize: 50,
    retryFailedRequests: true,
    notifyOnCompletion: true,
    notificationEmails: [],
    eligibilityCriteria: {
      minimumRequestAge: 24,
      requiredDocuments: [],
      managerApprovalRequired: false,
      backgroundCheckRequired: false,
      trainingCompletionRequired: false
    }
  });
  const [selectedTemplate, setSelectedTemplate] = useState<string>('');
  const [newEmail, setNewEmail] = useState('');
  const [lastRun, setLastRun] = useState<string | null>(null);

  // Initialize local config
  useEffect(() => {
    if (initialConfig) {
      setLocalConfig(prev => ({ ...prev, ...initialConfig }));
    } else if (autoProvisioningConfig) {
      setLocalConfig(autoProvisioningConfig);
    }
  }, [initialConfig, autoProvisioningConfig]);

  // Fetch data on mount
  useEffect(() => {
    fetchAutoProvisioningConfig();
    fetchProvisioningTemplates();
  }, [fetchAutoProvisioningConfig, fetchProvisioningTemplates]);

  // Handle errors
  useEffect(() => {
    if (error && onError) {
      onError(error);
    }
  }, [error, onError]);

  // Handle config update
  const handleSaveConfig = useCallback(async () => {
    try {
      const result = await updateAutoProvisioningConfig(localConfig);
      if (result.success) {
        setIsEditing(false);
        if (onConfigUpdate) {
          onConfigUpdate(localConfig);
        }
        if (onSuccess) {
          onSuccess('Auto-provisioning configuration updated successfully');
        }
      } else {
        if (onError) {
          onError(result.message || 'Failed to update configuration');
        }
      }
    } catch (err: any) {
      if (onError) {
        onError(err.message || 'Error updating configuration');
      }
    }
  }, [localConfig, updateAutoProvisioningConfig, onConfigUpdate, onSuccess, onError]);

  // Handle manual trigger
  const handleTriggerProvisioning = useCallback(async () => {
    try {
      const result = await triggerAutoProvisioning(selectedTemplate || undefined);
      if (result.success) {
        setLastRun(new Date().toISOString());
        if (onSuccess) {
          onSuccess('Auto-provisioning triggered successfully');
        }
      } else {
        if (onError) {
          onError(result.message || 'Failed to trigger auto-provisioning');
        }
      }
    } catch (err: any) {
      if (onError) {
        onError(err.message || 'Error triggering auto-provisioning');
      }
    }
  }, [triggerAutoProvisioning, selectedTemplate, onSuccess, onError]);

  // Add notification email
  const handleAddEmail = useCallback(() => {
    if (newEmail && !localConfig.notificationEmails.includes(newEmail)) {
      setLocalConfig(prev => ({
        ...prev,
        notificationEmails: [...prev.notificationEmails, newEmail]
      }));
      setNewEmail('');
    }
  }, [newEmail, localConfig.notificationEmails]);

  // Remove notification email
  const handleRemoveEmail = useCallback((email: string) => {
    setLocalConfig(prev => ({
      ...prev,
      notificationEmails: prev.notificationEmails.filter(e => e !== email)
    }));
  }, []);

  // Get schedule display text
  const getScheduleDisplayText = (config: AutoProvisioningConfig) => {
    switch (config.scheduleType) {
      case 'manual':
        return 'Manual trigger only';
      case 'hourly':
        return 'Every hour';
      case 'daily':
        return `Daily at ${config.scheduleTime}`;
      case 'weekly':
        return `Weekly on Sunday at ${config.scheduleTime}`;
      default:
        return 'Unknown schedule';
    }
  };

  if (loading && !autoProvisioningConfig) {
    return (
      <div className={`card border-0 rounded-4 shadow-sm ${className}`}>
        <div className="card-body p-4">
          <div className="d-flex align-items-center justify-content-center py-5">
            <div className="spinner-border text-primary me-3" role="status">
              <span className="visually-hidden">Loading...</span>
            </div>
            <span className="text-muted">Loading auto-provisioning configuration...</span>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={`card border-0 rounded-4 shadow-sm ${className}`}>
      {/* Header */}
      <div className="card-header border-0 bg-transparent pt-4 px-4 pb-0">
        <div className="d-flex align-items-center justify-content-between mb-3">
          <div className="d-flex align-items-center">
            <div className="rounded-circle bg-info bg-opacity-10 p-3 me-3">
              <Zap className="text-info" size={24} />
            </div>
            <div>
              <h5 className="card-title mb-0 fw-bold text-dark">Auto Provisioning</h5>
              <p className="text-muted small mb-0">
                Automated user provisioning configuration and scheduling
              </p>
            </div>
          </div>
          
          <div className="d-flex align-items-center gap-2">
            {/* Status Badge */}
            <span className={`badge ${localConfig.enabled ? 'bg-success' : 'bg-secondary'}`}>
              {localConfig.enabled ? 'Enabled' : 'Disabled'}
            </span>
            
            {/* Edit/Save Toggle */}
            {isEditing ? (
              <div className="btn-group">
                <button
                  className="btn btn-outline-secondary btn-sm rounded-3"
                  onClick={() => {
                    setIsEditing(false);
                    // Reset to saved config
                    if (autoProvisioningConfig) {
                      setLocalConfig(autoProvisioningConfig);
                    }
                  }}
                >
                  Cancel
                </button>
                <button
                  className="btn btn-primary btn-sm rounded-3"
                  onClick={handleSaveConfig}
                  disabled={loading}
                >
                  <Save size={14} className="me-1" />
                  Save
                </button>
              </div>
            ) : (
              <button
                className="btn btn-outline-primary btn-sm rounded-3"
                onClick={() => setIsEditing(true)}
              >
                <Edit size={14} className="me-1" />
                Edit
              </button>
            )}
          </div>
        </div>
      </div>

      <div className="card-body p-4">
        {/* Basic Configuration */}
        <div className="row mb-4">
          <div className="col-md-6">
            <div className="bg-light rounded-3 p-3">
              <h6 className="fw-semibold mb-3 d-flex align-items-center">
                <Settings className="me-2 text-primary" size={18} />
                Basic Settings
              </h6>
              
              <div className="mb-3">
                <div className="form-check form-switch">
                  <input
                    className="form-check-input"
                    type="checkbox"
                    id="enableAutoProvisioning"
                    checked={localConfig.enabled}
                    onChange={(e) => setLocalConfig(prev => ({
                      ...prev,
                      enabled: e.target.checked
                    }))}
                    disabled={!isEditing}
                  />
                  <label className="form-check-label fw-semibold" htmlFor="enableAutoProvisioning">
                    Enable Auto Provisioning
                  </label>
                </div>
              </div>

              <div className="mb-3">
                <label className="form-label small fw-semibold">Schedule Type</label>
                <select
                  className="form-select form-select-sm rounded-3"
                  value={localConfig.scheduleType}
                  onChange={(e) => setLocalConfig(prev => ({
                    ...prev,
                    scheduleType: e.target.value as any
                  }))}
                  disabled={!isEditing}
                >
                  <option value="manual">Manual</option>
                  <option value="hourly">Hourly</option>
                  <option value="daily">Daily</option>
                  <option value="weekly">Weekly</option>
                </select>
              </div>

              {localConfig.scheduleType !== 'manual' && localConfig.scheduleType !== 'hourly' && (
                <div className="mb-3">
                  <label className="form-label small fw-semibold">Schedule Time</label>
                  <input
                    type="time"
                    className="form-control form-control-sm rounded-3"
                    value={localConfig.scheduleTime}
                    onChange={(e) => setLocalConfig(prev => ({
                      ...prev,
                      scheduleTime: e.target.value
                    }))}
                    disabled={!isEditing}
                  />
                </div>
              )}

              <div className="mb-3">
                <label className="form-label small fw-semibold">Batch Size</label>
                <input
                  type="number"
                  className="form-control form-control-sm rounded-3"
                  value={localConfig.batchSize}
                  onChange={(e) => setLocalConfig(prev => ({
                    ...prev,
                    batchSize: Number(e.target.value)
                  }))}
                  min="1"
                  max="500"
                  disabled={!isEditing}
                />
                <small className="text-muted">Number of requests to process per batch</small>
              </div>
            </div>
          </div>

          <div className="col-md-6">
            <div className="bg-light rounded-3 p-3">
              <h6 className="fw-semibold mb-3 d-flex align-items-center">
                <Clock className="me-2 text-info" size={18} />
                Schedule Information
              </h6>
              
              <div className="mb-3">
                <div className="d-flex align-items-center">
                  <Calendar className="text-muted me-2" size={16} />
                  <span className="small">
                    <strong>Current Schedule:</strong> {getScheduleDisplayText(localConfig)}
                  </span>
                </div>
              </div>

              {lastRun && (
                <div className="mb-3">
                  <div className="d-flex align-items-center">
                    <RefreshCw className="text-muted me-2" size={16} />
                    <span className="small">
                      <strong>Last Run:</strong> {new Date(lastRun).toLocaleString()}
                    </span>
                  </div>
                </div>
              )}

              <div className="mb-3">
                <div className="d-flex align-items-center">
                  <Users className="text-muted me-2" size={16} />
                  <span className="small">
                    <strong>Batch Size:</strong> {localConfig.batchSize} requests
                  </span>
                </div>
              </div>

              {/* Manual Trigger */}
              <div className="mt-4">
                <label className="form-label small fw-semibold">Manual Trigger</label>
                <div className="d-flex gap-2">
                  <select
                    className="form-select form-select-sm rounded-3 flex-grow-1"
                    value={selectedTemplate}
                    onChange={(e) => setSelectedTemplate(e.target.value)}
                  >
                    <option value="">All templates</option>
                    {provisioningTemplates.map(template => (
                      <option key={template.templateId} value={template.templateId}>
                        {template.templateName}
                      </option>
                    ))}
                  </select>
                  <button
                    className="btn btn-primary btn-sm rounded-3"
                    onClick={handleTriggerProvisioning}
                    disabled={loading || !localConfig.enabled}
                  >
                    <Play size={14} className="me-1" />
                    Run Now
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Advanced Options */}
        <div className="row mb-4">
          <div className="col-md-6">
            <div className="bg-light rounded-3 p-3">
              <h6 className="fw-semibold mb-3 d-flex align-items-center">
                <Shield className="me-2 text-warning" size={18} />
                Eligibility Criteria
              </h6>

              <div className="mb-3">
                <label className="form-label small fw-semibold">Minimum Request Age (hours)</label>
                <input
                  type="number"
                  className="form-control form-control-sm rounded-3"
                  value={localConfig.eligibilityCriteria.minimumRequestAge}
                  onChange={(e) => setLocalConfig(prev => ({
                    ...prev,
                    eligibilityCriteria: {
                      ...prev.eligibilityCriteria,
                      minimumRequestAge: Number(e.target.value)
                    }
                  }))}
                  min="0"
                  disabled={!isEditing}
                />
              </div>

              <div className="mb-3">
                <div className="form-check">
                  <input
                    className="form-check-input"
                    type="checkbox"
                    id="managerApprovalRequired"
                    checked={localConfig.eligibilityCriteria.managerApprovalRequired}
                    onChange={(e) => setLocalConfig(prev => ({
                      ...prev,
                      eligibilityCriteria: {
                        ...prev.eligibilityCriteria,
                        managerApprovalRequired: e.target.checked
                      }
                    }))}
                    disabled={!isEditing}
                  />
                  <label className="form-check-label small" htmlFor="managerApprovalRequired">
                    Manager approval required
                  </label>
                </div>
              </div>

              <div className="mb-3">
                <div className="form-check">
                  <input
                    className="form-check-input"
                    type="checkbox"
                    id="backgroundCheckRequired"
                    checked={localConfig.eligibilityCriteria.backgroundCheckRequired}
                    onChange={(e) => setLocalConfig(prev => ({
                      ...prev,
                      eligibilityCriteria: {
                        ...prev.eligibilityCriteria,
                        backgroundCheckRequired: e.target.checked
                      }
                    }))}
                    disabled={!isEditing}
                  />
                  <label className="form-check-label small" htmlFor="backgroundCheckRequired">
                    Background check required
                  </label>
                </div>
              </div>

              <div className="mb-3">
                <div className="form-check">
                  <input
                    className="form-check-input"
                    type="checkbox"
                    id="trainingCompletionRequired"
                    checked={localConfig.eligibilityCriteria.trainingCompletionRequired}
                    onChange={(e) => setLocalConfig(prev => ({
                      ...prev,
                      eligibilityCriteria: {
                        ...prev.eligibilityCriteria,
                        trainingCompletionRequired: e.target.checked
                      }
                    }))}
                    disabled={!isEditing}
                  />
                  <label className="form-check-label small" htmlFor="trainingCompletionRequired">
                    Training completion required
                  </label>
                </div>
              </div>
            </div>
          </div>

          <div className="col-md-6">
            <div className="bg-light rounded-3 p-3">
              <h6 className="fw-semibold mb-3 d-flex align-items-center">
                <Mail className="me-2 text-success" size={18} />
                Notifications
              </h6>

              <div className="mb-3">
                <div className="form-check">
                  <input
                    className="form-check-input"
                    type="checkbox"
                    id="retryFailedRequests"
                    checked={localConfig.retryFailedRequests}
                    onChange={(e) => setLocalConfig(prev => ({
                      ...prev,
                      retryFailedRequests: e.target.checked
                    }))}
                    disabled={!isEditing}
                  />
                  <label className="form-check-label small" htmlFor="retryFailedRequests">
                    Retry failed requests
                  </label>
                </div>
              </div>

              <div className="mb-3">
                <div className="form-check">
                  <input
                    className="form-check-input"
                    type="checkbox"
                    id="notifyOnCompletion"
                    checked={localConfig.notifyOnCompletion}
                    onChange={(e) => setLocalConfig(prev => ({
                      ...prev,
                      notifyOnCompletion: e.target.checked
                    }))}
                    disabled={!isEditing}
                  />
                  <label className="form-check-label small" htmlFor="notifyOnCompletion">
                    Notify on completion
                  </label>
                </div>
              </div>

              <div className="mb-3">
                <label className="form-label small fw-semibold">Notification Emails</label>
                
                {/* Email List */}
                {localConfig.notificationEmails.length > 0 && (
                  <div className="mb-2">
                    {localConfig.notificationEmails.map((email, index) => (
                      <div key={index} className="d-flex align-items-center justify-content-between bg-white rounded-2 p-2 mb-1">
                        <span className="small">{email}</span>
                        {isEditing && (
                          <button
                            className="btn btn-link btn-sm p-0 text-danger"
                            onClick={() => handleRemoveEmail(email)}
                          >
                            ×
                          </button>
                        )}
                      </div>
                    ))}
                  </div>
                )}

                {/* Add Email */}
                {isEditing && (
                  <div className="d-flex gap-2">
                    <input
                      type="email"
                      className="form-control form-control-sm rounded-3"
                      placeholder="Enter email address"
                      value={newEmail}
                      onChange={(e) => setNewEmail(e.target.value)}
                      onKeyPress={(e) => {
                        if (e.key === 'Enter') {
                          e.preventDefault();
                          handleAddEmail();
                        }
                      }}
                    />
                    <button
                      className="btn btn-outline-primary btn-sm rounded-3"
                      onClick={handleAddEmail}
                      disabled={!newEmail}
                    >
                      Add
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Error Display */}
        {error && (
          <div className="alert alert-danger" role="alert">
            <div className="d-flex align-items-center">
              <AlertTriangle size={16} className="me-2 flex-shrink-0" />
              <span>{error}</span>
            </div>
          </div>
        )}
      </div>

      {/* Loading overlay */}
      {loading && (
        <div className="position-absolute top-0 start-0 w-100 h-100 bg-white bg-opacity-75 d-flex align-items-center justify-content-center rounded-4">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Loading...</span>
          </div>
        </div>
      )}
    </div>
  );
};

export default AutoProvisioningPanel;