import React, { useState, useEffect } from 'react';
import { Shield, RefreshCw } from 'lucide-react';
import { securityDashboardApi } from '../../services/api';
import SecurityOverviewSection from './SecurityOverviewSection';
import SecurityMetrics from './SecurityMetrics';
import SuspiciousActivity from './SuspiciousActivity';
import RecentSecurityEvents from './RecentSecurityEvents';

interface LocalSecurityOverview {
  riskScore: number;
  riskLevel: string;
  activeSessions: number;
  unverifiedUsers: number;
  failedLoginsToday: number;
  successfulLoginsToday: number;
  applicationChanges: number;
}

interface SecurityData {
  overview: LocalSecurityOverview;
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
        setSecurityData(response.data as unknown as SecurityData);
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
                <SecurityOverviewSection overview={securityData.overview} />
              </div>

              <div className="col-lg-9">
                <SecurityMetrics overview={securityData.overview} />
              </div>
            </div>

            {/* Threats & Recent Events */}
            <div className="row g-4">
              {/* Suspicious Activity */}
              <div className="col-lg-6">
                <SuspiciousActivity suspiciousIps={securityData.threats.suspiciousIps} />
              </div>

              {/* Recent Security Events */}
              <div className="col-lg-6">
                <RecentSecurityEvents recentEvents={securityData.recentEvents} />
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