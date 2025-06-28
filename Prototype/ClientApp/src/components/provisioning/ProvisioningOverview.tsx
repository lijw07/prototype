// Provisioning Overview Component - Dashboard metrics and statistics
// Follows SRP: Only responsible for displaying provisioning metrics

import React, { useEffect, useState } from 'react';
import { 
  Users, 
  CheckCircle, 
  Clock, 
  AlertTriangle, 
  TrendingUp, 
  RefreshCw,
  Activity,
  Settings
} from 'lucide-react';
import { useProvisioning } from './hooks/useProvisioning';
import type { ProvisioningOverviewProps } from './types/provisioning.types';

const ProvisioningOverview: React.FC<ProvisioningOverviewProps> = ({
  className = '',
  refreshInterval = 30000, // 30 seconds
  showTrends = true,
  showRecentActivity = true,
  onError,
  onSuccess
}) => {
  const {
    overview,
    loading,
    error,
    fetchOverview
  } = useProvisioning();

  const [lastRefresh, setLastRefresh] = useState<Date | null>(null);
  const [autoRefresh, setAutoRefresh] = useState(true);

  // Initial data fetch
  useEffect(() => {
    fetchOverview();
  }, [fetchOverview]);

  // Auto-refresh functionality
  useEffect(() => {
    if (!autoRefresh || !refreshInterval) return;

    const interval = setInterval(() => {
      fetchOverview();
      setLastRefresh(new Date());
    }, refreshInterval);

    return () => clearInterval(interval);
  }, [autoRefresh, refreshInterval, fetchOverview]);

  // Handle errors
  useEffect(() => {
    if (error && onError) {
      onError(error);
    }
  }, [error, onError]);

  // Manual refresh handler
  const handleRefresh = async () => {
    await fetchOverview();
    setLastRefresh(new Date());
    if (onSuccess) {
      onSuccess('Overview refreshed successfully');
    }
  };

  // Format time since last refresh
  const formatLastRefresh = () => {
    if (!lastRefresh) return 'Never';
    const now = new Date();
    const diff = Math.floor((now.getTime() - lastRefresh.getTime()) / 1000);
    
    if (diff < 60) return `${diff}s ago`;
    if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
    return `${Math.floor(diff / 3600)}h ago`;
  };

  if (loading && !overview) {
    return (
      <div className={`card border-0 rounded-4 shadow-sm ${className}`}>
        <div className="card-body p-4">
          <div className="d-flex align-items-center justify-content-center py-5">
            <div className="spinner-border text-primary me-3" role="status">
              <span className="visually-hidden">Loading...</span>
            </div>
            <span className="text-muted">Loading provisioning overview...</span>
          </div>
        </div>
      </div>
    );
  }

  if (error && !overview) {
    return (
      <div className={`card border-0 rounded-4 shadow-sm ${className}`}>
        <div className="card-body p-4">
          <div className="text-center py-5">
            <AlertTriangle size={48} className="text-warning mb-3" />
            <h5 className="text-dark mb-2">Unable to load overview</h5>
            <p className="text-muted mb-4">{error}</p>
            <button 
              className="btn btn-primary rounded-3"
              onClick={handleRefresh}
              disabled={loading}
            >
              <RefreshCw size={16} className="me-2" />
              Try Again
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={`card border-0 rounded-4 shadow-sm ${className}`}>
      {/* Header */}
      <div className="card-header border-0 bg-transparent pt-4 px-4 pb-0">
        <div className="d-flex align-items-center justify-content-between">
          <div className="d-flex align-items-center">
            <div className="rounded-circle bg-primary bg-opacity-10 p-3 me-3">
              <Users className="text-primary" size={24} />
            </div>
            <div>
              <h5 className="card-title mb-0 fw-bold text-dark">Provisioning Overview</h5>
              <p className="text-muted small mb-0">Real-time provisioning metrics and status</p>
            </div>
          </div>
          
          <div className="d-flex align-items-center gap-3">
            {/* Auto-refresh toggle */}
            <div className="form-check form-switch">
              <input 
                className="form-check-input" 
                type="checkbox" 
                id="autoRefreshToggle"
                checked={autoRefresh}
                onChange={(e) => setAutoRefresh(e.target.checked)}
              />
              <label className="form-check-label small text-muted" htmlFor="autoRefreshToggle">
                Auto-refresh
              </label>
            </div>
            
            {/* Manual refresh button */}
            <button 
              className="btn btn-outline-primary btn-sm rounded-3"
              onClick={handleRefresh}
              disabled={loading}
              title="Refresh data"
            >
              <RefreshCw size={16} className={loading ? 'spin' : ''} />
            </button>
          </div>
        </div>
        
        {/* Last refresh info */}
        {lastRefresh && (
          <div className="text-end">
            <small className="text-muted">
              Last updated: {formatLastRefresh()}
            </small>
          </div>
        )}
      </div>

      <div className="card-body p-4">
        {overview ? (
          <>
            {/* Key Metrics Row */}
            <div className="row g-4 mb-4">
              {/* Pending Requests */}
              <div className="col-md-3">
                <div className="bg-warning bg-opacity-10 rounded-4 p-4 h-100">
                  <div className="d-flex align-items-center justify-content-between mb-2">
                    <Clock className="text-warning" size={24} />
                    <span className="badge bg-warning text-dark">Pending</span>
                  </div>
                  <h3 className="fw-bold text-dark mb-1">
                    {overview.totalPendingRequests.toLocaleString()}
                  </h3>
                  <p className="small text-muted mb-0">Provisioning Requests</p>
                </div>
              </div>

              {/* Provisioned Users */}
              <div className="col-md-3">
                <div className="bg-success bg-opacity-10 rounded-4 p-4 h-100">
                  <div className="d-flex align-items-center justify-content-between mb-2">
                    <CheckCircle className="text-success" size={24} />
                    <span className="badge bg-success">Completed</span>
                  </div>
                  <h3 className="fw-bold text-dark mb-1">
                    {overview.totalProvisionedUsers.toLocaleString()}
                  </h3>
                  <p className="small text-muted mb-0">Users Provisioned</p>
                </div>
              </div>

              {/* Failed Requests */}
              <div className="col-md-3">
                <div className="bg-danger bg-opacity-10 rounded-4 p-4 h-100">
                  <div className="d-flex align-items-center justify-content-between mb-2">
                    <AlertTriangle className="text-danger" size={24} />
                    <span className="badge bg-danger">Failed</span>
                  </div>
                  <h3 className="fw-bold text-dark mb-1">
                    {overview.totalFailedRequests.toLocaleString()}
                  </h3>
                  <p className="small text-muted mb-0">Failed Requests</p>
                </div>
              </div>

              {/* Auto Provisioning Status */}
              <div className="col-md-3">
                <div className="bg-info bg-opacity-10 rounded-4 p-4 h-100">
                  <div className="d-flex align-items-center justify-content-between mb-2">
                    <Settings className="text-info" size={24} />
                    <span className={`badge ${overview.autoProvisioningEnabled ? 'bg-success' : 'bg-secondary'}`}>
                      {overview.autoProvisioningEnabled ? 'Enabled' : 'Disabled'}
                    </span>
                  </div>
                  <h3 className="fw-bold text-dark mb-1">
                    {overview.autoProvisioningEnabled ? 'Active' : 'Inactive'}
                  </h3>
                  <p className="small text-muted mb-0">Auto Provisioning</p>
                </div>
              </div>
            </div>

            {/* Last Provisioning Run */}
            {overview.lastProvisioningRun && (
              <div className="row mb-4">
                <div className="col-12">
                  <div className="bg-light rounded-4 p-4">
                    <div className="d-flex align-items-center">
                      <Activity className="text-primary me-3" size={20} />
                      <div>
                        <h6 className="fw-semibold mb-1 text-dark">Last Provisioning Run</h6>
                        <p className="text-muted small mb-0">
                          {new Date(overview.lastProvisioningRun).toLocaleString()}
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Success Rate Indicator */}
            <div className="row">
              <div className="col-12">
                <div className="bg-light rounded-4 p-4">
                  <h6 className="fw-semibold mb-3 text-dark d-flex align-items-center">
                    <TrendingUp className="text-primary me-2" size={20} />
                    Provisioning Success Rate
                  </h6>
                  
                  {(() => {
                    const total = overview.totalProvisionedUsers + overview.totalFailedRequests;
                    const successRate = total > 0 ? (overview.totalProvisionedUsers / total) * 100 : 0;
                    
                    return (
                      <div>
                        <div className="d-flex justify-content-between align-items-center mb-2">
                          <span className="text-muted small">Success Rate</span>
                          <span className="fw-semibold text-dark">{successRate.toFixed(1)}%</span>
                        </div>
                        <div className="progress" style={{ height: '8px' }}>
                          <div 
                            className={`progress-bar ${
                              successRate >= 90 ? 'bg-success' : 
                              successRate >= 75 ? 'bg-warning' : 'bg-danger'
                            }`}
                            style={{ width: `${successRate}%` }}
                          ></div>
                        </div>
                        <div className="d-flex justify-content-between mt-2">
                          <small className="text-muted">
                            {overview.totalProvisionedUsers} successful
                          </small>
                          <small className="text-muted">
                            {overview.totalFailedRequests} failed
                          </small>
                        </div>
                      </div>
                    );
                  })()}
                </div>
              </div>
            </div>
          </>
        ) : (
          <div className="text-center py-5">
            <Users size={48} className="text-muted opacity-50 mb-3" />
            <h5 className="text-muted mb-2">No Data Available</h5>
            <p className="text-muted mb-4">Provisioning overview data is not available.</p>
            <button 
              className="btn btn-primary rounded-3"
              onClick={handleRefresh}
              disabled={loading}
            >
              <RefreshCw size={16} className="me-2" />
              Load Data
            </button>
          </div>
        )}
      </div>

      {/* Loading overlay */}
      {loading && overview && (
        <div className="position-absolute top-0 start-0 w-100 h-100 bg-white bg-opacity-75 d-flex align-items-center justify-content-center rounded-4">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Updating...</span>
          </div>
        </div>
      )}
    </div>
  );
};

export default ProvisioningOverview;