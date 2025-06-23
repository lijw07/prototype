import React, { useState, useEffect } from 'react';
import { 
  Shield, 
  AlertTriangle, 
  Users, 
  Activity, 
  TrendingUp, 
  TrendingDown,
  Eye,
  MapPin,
  Clock,
  AlertCircle,
  CheckCircle2,
  Zap,
  RefreshCw
} from 'lucide-react';
import { securityDashboardApi } from '../../services/api';

interface SecurityOverview {
  riskScore: number;
  riskLevel: string;
  activeSessions: number;
  unverifiedUsers: number;
  failedLoginsToday: number;
  successfulLoginsToday: number;
  applicationChanges: number;
}

interface SecurityData {
  overview: SecurityOverview;
  threats: {
    suspiciousIps: Array<{IpAddress: string; Count: number}>;
    failedLoginsByIp: Array<{IpAddress: string; Count: number}>;
  };
  recentEvents: Array<{
    UserActivityLogId: string;
    ActionType: string;
    Description: string;
    Timestamp: string;
    IpAddress: string;
    UserId: string;
  }>;
  trends: {
    failedLoginsLast7Days: Array<{Date: string; Count: number}>;
    successfulLoginsLast7Days: Array<{Date: string; Count: number}>;
  };
}

export default function SecurityDashboard() {
  const [securityData, setSecurityData] = useState<SecurityData | null>(null);
  const [loading, setLoading] = useState(true);
  const [lastUpdated, setLastUpdated] = useState<Date>(new Date());

  const fetchSecurityData = async () => {
    try {
      setLoading(true);
      const response = await securityDashboardApi.getSecurityOverview();
      if (response.success && response.data) {
        setSecurityData(response.data);
        setLastUpdated(new Date());
      }
    } catch (error) {
      console.error('Failed to fetch security data:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSecurityData();
    
    // Auto-refresh every 5 minutes
    const interval = setInterval(fetchSecurityData, 5 * 60 * 1000);
    return () => clearInterval(interval);
  }, []);

  const getRiskColor = (riskLevel: string) => {
    switch (riskLevel) {
      case 'HIGH': return 'danger';
      case 'MEDIUM': return 'warning';
      case 'LOW': return 'info';
      default: return 'success';
    }
  };

  const getRiskIcon = (riskLevel: string) => {
    switch (riskLevel) {
      case 'HIGH': return AlertCircle;
      case 'MEDIUM': return AlertTriangle;
      case 'LOW': return Eye;
      default: return CheckCircle2;
    }
  };

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    return date.toLocaleDateString();
  };

  const getActionTypeColor = (actionType: string) => {
    switch (actionType) {
      case 'FailedLogin': return 'danger';
      case 'Login': return 'success';
      case 'ChangePassword': return 'warning';
      case 'ApplicationRemoved': return 'info';
      case 'RoleDeleted': return 'warning';
      default: return 'secondary';
    }
  };

  if (loading && !securityData) {
    return (
      <div className="min-vh-100 bg-light">
        <div className="container-fluid py-4">
          <div className="d-flex justify-content-center align-items-center" style={{ height: '50vh' }}>
            <div className="text-center">
              <div className="spinner-border text-primary mb-3" role="status">
                <span className="visually-hidden">Loading...</span>
              </div>
              <h5>Loading Security Dashboard...</h5>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-vh-100 bg-light">
      <div className="container-fluid py-4">
        {/* Header */}
        <div className="row mb-4">
          <div className="col-12">
            <div className="d-flex justify-content-between align-items-center">
              <div>
                <h1 className="display-6 fw-bold mb-2 d-flex align-items-center">
                  <Shield className="text-primary me-3" size={32} />
                  Security Dashboard
                </h1>
                <p className="text-muted mb-0">
                  Real-time security monitoring and threat detection
                </p>
              </div>
              <div className="text-end">
                <button 
                  onClick={fetchSecurityData}
                  className="btn btn-outline-primary btn-sm d-flex align-items-center"
                  disabled={loading}
                >
                  <RefreshCw className={`me-2 ${loading ? 'rotating' : ''}`} size={16} />
                  Refresh
                </button>
                <small className="text-muted d-block mt-1">
                  Last updated: {lastUpdated.toLocaleTimeString()}
                </small>
              </div>
            </div>
          </div>
        </div>

        {securityData && (
          <>
            {/* Risk Score & Key Metrics */}
            <div className="row g-4 mb-4">
              <div className="col-lg-3">
                <div className="card border-0 rounded-4 shadow-sm h-100">
                  <div className="card-body p-4 text-center">
                    {(() => {
                      const RiskIcon = getRiskIcon(securityData.overview.riskLevel);
                      const riskColor = getRiskColor(securityData.overview.riskLevel);
                      return (
                        <>
                          <RiskIcon className={`text-${riskColor} mb-3`} size={48} />
                          <h3 className={`text-${riskColor} fw-bold`}>{securityData.overview.riskScore}/100</h3>
                          <h6 className="fw-semibold mb-0">Risk Score</h6>
                          <span className={`badge bg-${riskColor} bg-opacity-10 text-${riskColor} mt-2`}>
                            {securityData.overview.riskLevel} RISK
                          </span>
                        </>
                      );
                    })()}
                  </div>
                </div>
              </div>

              <div className="col-lg-9">
                <div className="row g-3 h-100">
                  <div className="col-md-3">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-3 text-center">
                        <Users className="text-info mb-2" size={24} />
                        <h4 className="fw-bold mb-1">{securityData.overview.activeSessions}</h4>
                        <small className="text-muted">Active Sessions</small>
                      </div>
                    </div>
                  </div>
                  <div className="col-md-3">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-3 text-center">
                        <AlertTriangle className="text-warning mb-2" size={24} />
                        <h4 className="fw-bold mb-1">{securityData.overview.unverifiedUsers}</h4>
                        <small className="text-muted">Unverified Users</small>
                      </div>
                    </div>
                  </div>
                  <div className="col-md-3">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-3 text-center">
                        <AlertCircle className="text-danger mb-2" size={24} />
                        <h4 className="fw-bold mb-1">{securityData.overview.failedLoginsToday}</h4>
                        <small className="text-muted">Failed Logins (24h)</small>
                      </div>
                    </div>
                  </div>
                  <div className="col-md-3">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-3 text-center">
                        <CheckCircle2 className="text-success mb-2" size={24} />
                        <h4 className="fw-bold mb-1">{securityData.overview.successfulLoginsToday}</h4>
                        <small className="text-muted">Successful Logins (24h)</small>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Threats & Recent Events */}
            <div className="row g-4">
              {/* Suspicious Activity */}
              <div className="col-lg-6">
                <div className="card border-0 rounded-4 shadow-sm h-100">
                  <div className="card-body p-4">
                    <div className="d-flex align-items-center mb-4">
                      <MapPin className="text-danger me-3" size={24} />
                      <h5 className="card-title fw-bold mb-0">Suspicious IP Addresses</h5>
                    </div>
                    
                    {securityData.threats.suspiciousIps.length > 0 ? (
                      <div className="space-y-3">
                        {securityData.threats.suspiciousIps.map((ip, index) => (
                          <div key={index} className="d-flex justify-content-between align-items-center py-2 px-3 bg-light rounded-3">
                            <div className="d-flex align-items-center">
                              <AlertTriangle className="text-warning me-2" size={16} />
                              <code className="text-dark">{ip.IpAddress}</code>
                            </div>
                            <span className="badge bg-danger">{ip.Count} failed attempts</span>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <div className="text-center py-4 text-muted">
                        <CheckCircle2 size={32} className="mb-2 opacity-50" />
                        <div>No suspicious activity detected</div>
                      </div>
                    )}
                  </div>
                </div>
              </div>

              {/* Recent Security Events */}
              <div className="col-lg-6">
                <div className="card border-0 rounded-4 shadow-sm h-100">
                  <div className="card-body p-4">
                    <div className="d-flex align-items-center mb-4">
                      <Activity className="text-primary me-3" size={24} />
                      <h5 className="card-title fw-bold mb-0">Recent Security Events</h5>
                    </div>
                    
                    <div className="timeline" style={{ maxHeight: '300px', overflowY: 'auto' }}>
                      {securityData.recentEvents.map((event, index) => (
                        <div key={event.UserActivityLogId} className="d-flex align-items-start mb-3">
                          <div className={`rounded-circle bg-${getActionTypeColor(event.ActionType)} bg-opacity-10 p-2 me-3 flex-shrink-0`}>
                            <Zap className={`text-${getActionTypeColor(event.ActionType)}`} size={14} />
                          </div>
                          <div className="flex-grow-1">
                            <div className="fw-semibold small">{event.Description}</div>
                            <div className="text-muted small">
                              <Clock size={12} className="me-1" />
                              {formatTimestamp(event.Timestamp)}
                              {event.IpAddress && (
                                <>
                                  <span className="mx-2">•</span>
                                  <code className="small">{event.IpAddress}</code>
                                </>
                              )}
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </>
        )}
      </div>

      <style>{`
        .rotating {
          animation: spin 1s linear infinite;
        }
        
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
        
        .timeline .d-flex:last-child {
          margin-bottom: 0 !important;
        }
      `}</style>
    </div>
  );
}