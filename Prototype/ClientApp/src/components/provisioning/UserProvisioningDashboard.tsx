// User Provisioning Dashboard - Main Container Component
// Follows SRP: Only responsible for layout and coordination of provisioning components

import React, { useState, useCallback, useEffect } from 'react';
import { 
  Users, 
  Settings, 
  Upload, 
  Activity,
  Bell,
  AlertTriangle,
  CheckCircle,
  RefreshCw,
  Maximize2,
  Minimize2,
  Grid3X3,
  List
} from 'lucide-react';

// Import decomposed components
import ProvisioningOverview from './ProvisioningOverview';
import PendingRequestsTable from './PendingRequestsTable';
import BulkUploadForm from './BulkUploadForm';
import AutoProvisioningPanel from './AutoProvisioningPanel';
import MigrationProgressTracker from './MigrationProgressTracker';

// Import hooks
import { useProvisioning } from './hooks/useProvisioning';
import { useSignalRProgress } from './hooks/useSignalRProgress';

interface UserProvisioningDashboardProps {
  className?: string;
  defaultView?: 'grid' | 'list';
  enableNotifications?: boolean;
}

const UserProvisioningDashboard: React.FC<UserProvisioningDashboardProps> = ({
  className = '',
  defaultView = 'grid',
  enableNotifications = true
}) => {
  const [viewMode, setViewMode] = useState<'grid' | 'list'>(defaultView);
  const [expandedSection, setExpandedSection] = useState<string | null>(null);
  const [notifications, setNotifications] = useState<Array<{
    id: string;
    type: 'success' | 'error' | 'warning' | 'info';
    message: string;
    timestamp: Date;
    autoHide?: boolean;
  }>>([]);

  const {
    overview,
    loading: provisioningLoading,
    error: provisioningError,
    fetchOverview
  } = useProvisioning();

  const {
    isConnected,
    connectionError,
    getJobsSummary
  } = useSignalRProgress();

  // Add notification
  const addNotification = useCallback((type: 'success' | 'error' | 'warning' | 'info', message: string, autoHide = true) => {
    const notification = {
      id: `notification_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      type,
      message,
      timestamp: new Date(),
      autoHide
    };

    setNotifications(prev => [notification, ...prev.slice(0, 4)]); // Keep only 5 notifications

    if (autoHide) {
      setTimeout(() => {
        setNotifications(prev => prev.filter(n => n.id !== notification.id));
      }, 5000);
    }
  }, []);

  // Remove notification
  const removeNotification = useCallback((id: string) => {
    setNotifications(prev => prev.filter(n => n.id !== id));
  }, []);

  // Handle errors from child components
  const handleError = useCallback((error: string) => {
    addNotification('error', error);
  }, [addNotification]);

  // Handle success messages from child components
  const handleSuccess = useCallback((message: string) => {
    addNotification('success', message);
  }, [addNotification]);

  // Handle section expansion
  const handleSectionToggle = useCallback((sectionId: string) => {
    setExpandedSection(current => current === sectionId ? null : sectionId);
  }, []);

  // Refresh all data
  const handleRefreshAll = useCallback(async () => {
    try {
      await fetchOverview();
      addNotification('success', 'Dashboard data refreshed successfully');
    } catch (error: any) {
      addNotification('error', error.message || 'Failed to refresh dashboard data');
    }
  }, [fetchOverview, addNotification]);

  // Monitor connection status
  useEffect(() => {
    if (connectionError) {
      addNotification('warning', `Real-time connection: ${connectionError}`, false);
    } else if (isConnected) {
      // Remove connection warnings when reconnected
      setNotifications(prev => prev.filter(n => !n.message.includes('Real-time connection')));
    }
  }, [isConnected, connectionError, addNotification]);

  // Get notification icon
  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'success': return <CheckCircle size={16} className="text-success" />;
      case 'error': return <AlertTriangle size={16} className="text-danger" />;
      case 'warning': return <AlertTriangle size={16} className="text-warning" />;
      case 'info': return <Bell size={16} className="text-info" />;
      default: return <Bell size={16} className="text-secondary" />;
    }
  };

  const jobsSummary = getJobsSummary();

  return (
    <div className={`user-provisioning-dashboard ${className}`}>
      {/* Header */}
      <div className="d-flex align-items-center justify-content-between mb-4">
        <div className="d-flex align-items-center">
          <div className="rounded-circle bg-primary bg-opacity-10 p-3 me-3">
            <Users className="text-primary" size={32} />
          </div>
          <div>
            <h3 className="fw-bold text-dark mb-0">User Provisioning Dashboard</h3>
            <p className="text-muted mb-0">
              Comprehensive user provisioning management and automation
            </p>
          </div>
        </div>

        <div className="d-flex align-items-center gap-3">
          {/* Connection Status */}
          <div className="d-flex align-items-center">
            <div className={`rounded-circle p-1 me-2 ${isConnected ? 'bg-success' : 'bg-warning'}`}>
              <div className="rounded-circle bg-white" style={{width: '8px', height: '8px'}}></div>
            </div>
            <span className={`small ${isConnected ? 'text-success' : 'text-warning'}`}>
              {isConnected ? 'Real-time' : 'Limited'}
            </span>
          </div>

          {/* View Mode Toggle */}
          <div className="btn-group btn-group-sm">
            <button
              className={`btn ${viewMode === 'grid' ? 'btn-primary' : 'btn-outline-primary'} rounded-start-3`}
              onClick={() => setViewMode('grid')}
            >
              <Grid3X3 size={14} />
            </button>
            <button
              className={`btn ${viewMode === 'list' ? 'btn-primary' : 'btn-outline-primary'} rounded-end-3`}
              onClick={() => setViewMode('list')}
            >
              <List size={14} />
            </button>
          </div>

          {/* Refresh Button */}
          <button
            className="btn btn-outline-primary rounded-3"
            onClick={handleRefreshAll}
            disabled={provisioningLoading}
            title="Refresh All Data"
          >
            <RefreshCw size={16} className={provisioningLoading ? 'spin' : ''} />
          </button>
        </div>
      </div>

      {/* Notifications */}
      {enableNotifications && notifications.length > 0 && (
        <div className="position-fixed top-0 end-0 p-3" style={{zIndex: 1050}}>
          {notifications.map(notification => (
            <div
              key={notification.id}
              className={`alert alert-${notification.type} alert-dismissible fade show mb-2`}
              style={{minWidth: '300px'}}
            >
              <div className="d-flex align-items-center">
                {getNotificationIcon(notification.type)}
                <div className="ms-2 flex-grow-1">
                  <div className="fw-semibold">{notification.message}</div>
                  <div className="small opacity-75">
                    {notification.timestamp.toLocaleTimeString()}
                  </div>
                </div>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => removeNotification(notification.id)}
                ></button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Dashboard Content */}
      {viewMode === 'grid' ? (
        <div className="row g-4">
          {/* Overview Section */}
          <div className="col-12">
            <ProvisioningOverview
              onError={handleError}
              onSuccess={handleSuccess}
              refreshInterval={30000}
            />
          </div>

          {/* Pending Requests and Auto Provisioning */}
          <div className="col-lg-8">
            <div className={`position-relative ${expandedSection === 'pending' ? 'col-12' : ''}`}>
              <div className="position-absolute top-0 end-0 m-3" style={{zIndex: 10}}>
                <button
                  className="btn btn-outline-secondary btn-sm rounded-circle"
                  onClick={() => handleSectionToggle('pending')}
                  title={expandedSection === 'pending' ? 'Minimize' : 'Expand'}
                >
                  {expandedSection === 'pending' ? <Minimize2 size={14} /> : <Maximize2 size={14} />}
                </button>
              </div>
              <PendingRequestsTable
                onError={handleError}
                onSuccess={handleSuccess}
                pageSize={10}
                onRequestAction={(requestId, action) => {
                  addNotification('info', `Request ${requestId} ${action}`);
                }}
              />
            </div>
          </div>

          <div className="col-lg-4">
            <div className={`position-relative ${expandedSection === 'auto' ? 'col-12' : ''}`}>
              <div className="position-absolute top-0 end-0 m-3" style={{zIndex: 10}}>
                <button
                  className="btn btn-outline-secondary btn-sm rounded-circle"
                  onClick={() => handleSectionToggle('auto')}
                  title={expandedSection === 'auto' ? 'Minimize' : 'Expand'}
                >
                  {expandedSection === 'auto' ? <Minimize2 size={14} /> : <Maximize2 size={14} />}
                </button>
              </div>
              <AutoProvisioningPanel
                onError={handleError}
                onSuccess={handleSuccess}
                onConfigUpdate={(config) => {
                  addNotification('success', 'Auto-provisioning configuration updated');
                }}
              />
            </div>
          </div>

          {/* Bulk Upload and Migration Progress */}
          <div className="col-lg-6">
            <div className={`position-relative ${expandedSection === 'upload' ? 'col-12' : ''}`}>
              <div className="position-absolute top-0 end-0 m-3" style={{zIndex: 10}}>
                <button
                  className="btn btn-outline-secondary btn-sm rounded-circle"
                  onClick={() => handleSectionToggle('upload')}
                  title={expandedSection === 'upload' ? 'Minimize' : 'Expand'}
                >
                  {expandedSection === 'upload' ? <Minimize2 size={14} /> : <Maximize2 size={14} />}
                </button>
              </div>
              <BulkUploadForm
                onError={handleError}
                onSuccess={handleSuccess}
                onUploadComplete={(result) => {
                  addNotification('success', `Upload completed: ${result.jobId}`);
                }}
                onProgressUpdate={(progress) => {
                  // Progress updates handled by SignalR hook
                }}
                maxFiles={5}
                maxFileSize={50}
              />
            </div>
          </div>

          <div className="col-lg-6">
            <div className={`position-relative ${expandedSection === 'progress' ? 'col-12' : ''}`}>
              <div className="position-absolute top-0 end-0 m-3" style={{zIndex: 10}}>
                <button
                  className="btn btn-outline-secondary btn-sm rounded-circle"
                  onClick={() => handleSectionToggle('progress')}
                  title={expandedSection === 'progress' ? 'Minimize' : 'Expand'}
                >
                  {expandedSection === 'progress' ? <Minimize2 size={14} /> : <Maximize2 size={14} />}
                </button>
              </div>
              <MigrationProgressTracker
                onError={handleError}
                onSuccess={handleSuccess}
                autoRefresh={true}
                refreshInterval={5000}
              />
            </div>
          </div>
        </div>
      ) : (
        // List View
        <div className="row g-4">
          <div className="col-12">
            <div className="accordion" id="provisioningAccordion">
              {/* Overview */}
              <div className="accordion-item border-0 shadow-sm rounded-4 mb-3">
                <h2 className="accordion-header">
                  <button
                    className="accordion-button bg-transparent border-0 fw-semibold"
                    type="button"
                    data-bs-toggle="collapse"
                    data-bs-target="#overviewCollapse"
                  >
                    <Users className="me-2 text-primary" size={20} />
                    Provisioning Overview
                  </button>
                </h2>
                <div id="overviewCollapse" className="accordion-collapse collapse show">
                  <div className="accordion-body p-0">
                    <ProvisioningOverview
                      onError={handleError}
                      onSuccess={handleSuccess}
                      className="border-0 shadow-none"
                    />
                  </div>
                </div>
              </div>

              {/* Pending Requests */}
              <div className="accordion-item border-0 shadow-sm rounded-4 mb-3">
                <h2 className="accordion-header">
                  <button
                    className="accordion-button collapsed bg-transparent border-0 fw-semibold"
                    type="button"
                    data-bs-toggle="collapse"
                    data-bs-target="#pendingCollapse"
                  >
                    <Bell className="me-2 text-warning" size={20} />
                    Pending Requests
                  </button>
                </h2>
                <div id="pendingCollapse" className="accordion-collapse collapse">
                  <div className="accordion-body p-0">
                    <PendingRequestsTable
                      onError={handleError}
                      onSuccess={handleSuccess}
                      className="border-0 shadow-none"
                    />
                  </div>
                </div>
              </div>

              {/* Bulk Upload */}
              <div className="accordion-item border-0 shadow-sm rounded-4 mb-3">
                <h2 className="accordion-header">
                  <button
                    className="accordion-button collapsed bg-transparent border-0 fw-semibold"
                    type="button"
                    data-bs-toggle="collapse"
                    data-bs-target="#uploadCollapse"
                  >
                    <Upload className="me-2 text-primary" size={20} />
                    Bulk Upload
                  </button>
                </h2>
                <div id="uploadCollapse" className="accordion-collapse collapse">
                  <div className="accordion-body p-0">
                    <BulkUploadForm
                      onError={handleError}
                      onSuccess={handleSuccess}
                      className="border-0 shadow-none"
                    />
                  </div>
                </div>
              </div>

              {/* Auto Provisioning */}
              <div className="accordion-item border-0 shadow-sm rounded-4 mb-3">
                <h2 className="accordion-header">
                  <button
                    className="accordion-button collapsed bg-transparent border-0 fw-semibold"
                    type="button"
                    data-bs-toggle="collapse"
                    data-bs-target="#autoCollapse"
                  >
                    <Settings className="me-2 text-info" size={20} />
                    Auto Provisioning
                  </button>
                </h2>
                <div id="autoCollapse" className="accordion-collapse collapse">
                  <div className="accordion-body p-0">
                    <AutoProvisioningPanel
                      onError={handleError}
                      onSuccess={handleSuccess}
                      className="border-0 shadow-none"
                    />
                  </div>
                </div>
              </div>

              {/* Migration Progress */}
              <div className="accordion-item border-0 shadow-sm rounded-4 mb-3">
                <h2 className="accordion-header">
                  <button
                    className="accordion-button collapsed bg-transparent border-0 fw-semibold"
                    type="button"
                    data-bs-toggle="collapse"
                    data-bs-target="#progressCollapse"
                  >
                    <Activity className="me-2 text-success" size={20} />
                    Migration Progress {jobsSummary.active > 0 && (
                      <span className="badge bg-primary ms-2">{jobsSummary.active}</span>
                    )}
                  </button>
                </h2>
                <div id="progressCollapse" className="accordion-collapse collapse">
                  <div className="accordion-body p-0">
                    <MigrationProgressTracker
                      onError={handleError}
                      onSuccess={handleSuccess}
                      className="border-0 shadow-none"
                    />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Loading Overlay */}
      {provisioningLoading && (
        <div className="position-fixed top-0 start-0 w-100 h-100 bg-white bg-opacity-75 d-flex align-items-center justify-content-center" style={{zIndex: 9999}}>
          <div className="text-center">
            <div className="spinner-border text-primary mb-3" role="status">
              <span className="visually-hidden">Loading...</span>
            </div>
            <div className="fw-semibold text-dark">Loading dashboard...</div>
          </div>
        </div>
      )}
    </div>
  );
};

export default UserProvisioningDashboard;