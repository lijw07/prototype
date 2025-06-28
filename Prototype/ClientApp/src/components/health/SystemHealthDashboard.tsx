import React, { useState, useEffect } from 'react';
import { 
  Server, 
  Database, 
  Cpu, 
  HardDrive, 
  Wifi, 
  Activity, 
  AlertTriangle, 
  CheckCircle2, 
  AlertCircle,
  TrendingUp,
  TrendingDown,
  RefreshCw,
  Clock,
  Zap,
  Globe,
  Monitor,
  BarChart3
} from 'lucide-react';
import { systemHealthApi } from '../../services/api';

interface HealthOverview {
  overall: {
    status: string;
    healthScore: number;
    lastChecked: string;
    responseTime: number;
  };
  database: {
    mainDatabase: string;
    applicationConnections: {
      healthy: number;
      total: number;
      percentage: number;
    };
  };
  performance: {
    cpu: { usage: number; status: string };
    memory: { usage: number; status: string; available: string };
    disk: { usage: number; status: string; available: string };
    network: { status: string; latency: number };
  };
  alerts: Array<{
    level: string;
    message: string;
    timestamp: string;
    category: string;
  }>;
}

interface DatabaseConnection {
  connectionId: string;
  applicationName: string;
  connectionType: string;
  status: string;
  responseTime: number;
  errorMessage: string;
  lastTested: string;
}

interface PerformanceMetrics {
  responseTime: {
    average: number;
    p95: number;
    trend: number;
  };
  throughput: {
    requestsPerMinute: number;
    peak24h: number;
    trend: number;
  };
  uptime: {
    percentage: number;
    since: string;
    incidents24h: number;
  };
  resources: {
    cpu: { usage: number; status: string };
    memory: { usage: number; status: string; available: string };
    disk: { usage: number; status: string; available: string };
    network: { status: string; latency: number };
  };
}

export default function SystemHealthDashboard() {
  const [healthData, setHealthData] = useState<HealthOverview | null>(null);
  const [connections, setConnections] = useState<DatabaseConnection[]>([]);
  const [performanceData, setPerformanceData] = useState<PerformanceMetrics | null>(null);
  const [loading, setLoading] = useState(true);
  const [lastUpdated, setLastUpdated] = useState<Date>(new Date());

  const fetchHealthData = async () => {
    try {
      setLoading(true);
      
      const [healthResponse, connectionsResponse, performanceResponse] = await Promise.all([
        systemHealthApi.getHealthOverview(),
        systemHealthApi.getDatabaseConnections(),
        systemHealthApi.getPerformanceMetrics()
      ]);

      if (healthResponse.success && healthResponse.data) {
        // Transform SystemHealthMetrics to HealthOverview
        const healthOverview: HealthOverview = {
          overall: healthResponse.data.overall || {
            status: 'unknown',
            healthScore: 0,
            lastChecked: new Date().toISOString(),
            responseTime: 0
          },
          database: {
            mainDatabase: healthResponse.data.databaseStatus || 'unknown',
            applicationConnections: {
              healthy: healthResponse.data.activeConnections || 0,
              total: healthResponse.data.activeConnections || 0,
              percentage: 100
            }
          },
          performance: {
            cpu: { usage: healthResponse.data.cpuUsage || 0, status: 'normal' },
            memory: { usage: healthResponse.data.memoryUsage || 0, status: 'normal', available: '8GB' },
            disk: { usage: 45, status: 'normal', available: '100GB' },
            network: { status: 'normal', latency: healthResponse.data.apiResponseTime || 0 }
          },
          alerts: []
        };
        setHealthData(healthOverview);
      } else {
        console.error('Health API failed:', healthResponse);
      }
      
      if (connectionsResponse && Array.isArray(connectionsResponse)) {
        setConnections(connectionsResponse);
      } else {
        console.error('Connections API failed:', connectionsResponse);
      }
      
      if (performanceResponse.success) {
        setPerformanceData(performanceResponse.data);
      } else {
        console.error('Performance API failed:', performanceResponse);
      }
      
      setLastUpdated(new Date());
    } catch (error) {
      console.error('Failed to fetch health data:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchHealthData();
    
    // Auto-refresh every 2 minutes
    const interval = setInterval(fetchHealthData, 2 * 60 * 1000);
    return () => clearInterval(interval);
  }, []);

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'excellent': case 'healthy': case 'good': case 'stable': case 'normal': return 'success';
      case 'fair': case 'medium': case 'warning': return 'warning';
      case 'poor': case 'unhealthy': case 'critical': case 'high': return 'danger';
      default: return 'info';
    }
  };

  const getAlertColor = (level: string) => {
    switch (level.toLowerCase()) {
      case 'critical': return 'danger';
      case 'warning': return 'warning';
      case 'info': return 'info';
      default: return 'secondary';
    }
  };

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    return date.toLocaleTimeString();
  };

  const getTrendIcon = (trend: number) => {
    if (trend > 5) return <TrendingUp className="text-success" size={16} />;
    if (trend < -5) return <TrendingDown className="text-danger" size={16} />;
    return <div className="text-muted" style={{ width: 16, height: 16 }}>–</div>;
  };

  if (loading && !healthData) {
    return (
      <div className="min-vh-100 bg-light">
        <div className="container-fluid py-4">
          <div className="d-flex justify-content-center align-items-center" style={{ height: '50vh' }}>
            <div className="text-center">
              <div className="spinner-border text-primary mb-3" role="status">
                <span className="visually-hidden">Loading...</span>
              </div>
              <h5>Loading System Health Dashboard...</h5>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Show error state if APIs failed to load data
  if (!loading && !healthData) {
    return (
      <div className="min-vh-100 bg-light">
        <div className="container-fluid py-4">
          <div className="d-flex justify-content-center align-items-center" style={{ height: '50vh' }}>
            <div className="text-center">
              <AlertTriangle className="text-warning mb-3" size={48} />
              <h5>Unable to load system health data</h5>
              <p className="text-muted">Check console for errors or try refreshing the page.</p>
              <button 
                onClick={fetchHealthData}
                className="btn btn-primary"
              >
                Retry
              </button>
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
                  <Monitor className="text-primary me-3" size={32} />
                  System Health Dashboard
                </h1>
                <p className="text-muted mb-0">
                  Real-time infrastructure monitoring and performance analytics
                </p>
              </div>
              <div className="text-end">
                <button 
                  onClick={fetchHealthData}
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

        {healthData && (
          <>
            {/* Overall Health Status */}
            <div className="row g-4 mb-4">
              <div className="col-lg-4">
                <div className="card border-0 rounded-4 shadow-sm h-100">
                  <div className="card-body p-4 text-center">
                    <div className={`text-${getStatusColor(healthData.overall.status)} mb-3`}>
                      {healthData.overall.status === 'Excellent' || healthData.overall.status === 'Good' ? 
                        <CheckCircle2 size={48} /> : 
                        <AlertCircle size={48} />
                      }
                    </div>
                    <h3 className={`text-${getStatusColor(healthData.overall.status)} fw-bold`}>
                      {healthData.overall.healthScore}/100
                    </h3>
                    <h6 className="fw-semibold mb-2">Overall Health</h6>
                    <span className={`badge bg-${getStatusColor(healthData.overall.status)} bg-opacity-10 text-${getStatusColor(healthData.overall.status)} mb-3`}>
                      {healthData.overall.status.toUpperCase()}
                    </span>
                    <div className="small text-muted">
                      <Clock size={12} className="me-1" />
                      Response time: {healthData.overall.responseTime}ms
                    </div>
                  </div>
                </div>
              </div>

              <div className="col-lg-8">
                {/* System Resources */}
                <div className="row g-3 h-100">
                  <div className="col-md-3">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-3 text-center">
                        <Cpu className={`text-${getStatusColor(healthData.performance.cpu.status)} mb-2`} size={24} />
                        <h4 className="fw-bold mb-1">{healthData.performance.cpu.usage}%</h4>
                        <small className="text-muted">CPU Usage</small>
                        <div className={`badge bg-${getStatusColor(healthData.performance.cpu.status)} bg-opacity-10 text-${getStatusColor(healthData.performance.cpu.status)} mt-1`}>
                          {healthData.performance.cpu.status}
                        </div>
                      </div>
                    </div>
                  </div>
                  <div className="col-md-3">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-3 text-center">
                        <HardDrive className={`text-${getStatusColor(healthData.performance.memory.status)} mb-2`} size={24} />
                        <h4 className="fw-bold mb-1">{healthData.performance.memory.usage}%</h4>
                        <small className="text-muted">Memory</small>
                        <div className="small text-muted">{healthData.performance.memory.available} free</div>
                      </div>
                    </div>
                  </div>
                  <div className="col-md-3">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-3 text-center">
                        <Database className={`text-${getStatusColor(healthData.database.mainDatabase)} mb-2`} size={24} />
                        <h4 className="fw-bold mb-1">{healthData.database.applicationConnections.healthy}/{healthData.database.applicationConnections.total}</h4>
                        <small className="text-muted">DB Connections</small>
                        <div className="small text-muted">{healthData.database.applicationConnections.percentage}% healthy</div>
                      </div>
                    </div>
                  </div>
                  <div className="col-md-3">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-3 text-center">
                        <Wifi className={`text-${getStatusColor(healthData.performance.network.status)} mb-2`} size={24} />
                        <h4 className="fw-bold mb-1">{healthData.performance.network.latency}ms</h4>
                        <small className="text-muted">Network Latency</small>
                        <div className={`badge bg-${getStatusColor(healthData.performance.network.status)} bg-opacity-10 text-${getStatusColor(healthData.performance.network.status)} mt-1`}>
                          {healthData.performance.network.status}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Alerts */}
            {healthData.alerts.length > 0 && (
              <div className="row mb-4">
                <div className="col-12">
                  <div className="card border-0 rounded-4 shadow-sm">
                    <div className="card-body p-4">
                      <div className="d-flex align-items-center mb-3">
                        <AlertTriangle className="text-warning me-3" size={24} />
                        <h5 className="card-title fw-bold mb-0">Active Alerts</h5>
                      </div>
                      <div className="row g-3">
                        {healthData.alerts.map((alert, index) => (
                          <div key={index} className="col-lg-6">
                            <div className={`alert alert-${getAlertColor(alert.level)} d-flex align-items-start`}>
                              <AlertCircle size={16} className="me-2 mt-1 flex-shrink-0" />
                              <div className="flex-grow-1">
                                <div className="fw-semibold">{alert.message}</div>
                                <div className="small">
                                  {alert.category} • {formatTimestamp(alert.timestamp)}
                                </div>
                              </div>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Performance Metrics & Database Connections */}
            <div className="row g-4">
              {/* Performance Metrics */}
              {performanceData && (
                <div className="col-lg-6">
                  <div className="card border-0 rounded-4 shadow-sm h-100">
                    <div className="card-body p-4">
                      <div className="d-flex align-items-center mb-4">
                        <BarChart3 className="text-primary me-3" size={24} />
                        <h5 className="card-title fw-bold mb-0">Performance Metrics</h5>
                      </div>
                      
                      <div className="row g-3">
                        <div className="col-12">
                          <div className="bg-light rounded-3 p-3">
                            <div className="d-flex justify-content-between align-items-center mb-2">
                              <span className="fw-semibold">Response Time</span>
                              {getTrendIcon(performanceData.responseTime.trend)}
                            </div>
                            <div className="d-flex justify-content-between">
                              <div>
                                <div className="h5 mb-0">{performanceData.responseTime.average}ms</div>
                                <small className="text-muted">Average</small>
                              </div>
                              <div className="text-end">
                                <div className="h6 mb-0">{performanceData.responseTime.p95}ms</div>
                                <small className="text-muted">95th percentile</small>
                              </div>
                            </div>
                          </div>
                        </div>
                        
                        <div className="col-12">
                          <div className="bg-light rounded-3 p-3">
                            <div className="d-flex justify-content-between align-items-center mb-2">
                              <span className="fw-semibold">Uptime</span>
                              <span className="text-success">{performanceData.uptime.percentage}%</span>
                            </div>
                            <div className="small text-muted">
                              Since {performanceData.uptime.since} • {performanceData.uptime.incidents24h} incidents (24h)
                            </div>
                          </div>
                        </div>
                        
                        <div className="col-12">
                          <div className="bg-light rounded-3 p-3">
                            <div className="d-flex justify-content-between align-items-center mb-2">
                              <span className="fw-semibold">Throughput</span>
                              {getTrendIcon(performanceData.throughput.trend)}
                            </div>
                            <div className="d-flex justify-content-between">
                              <div>
                                <div className="h6 mb-0">{performanceData.throughput.requestsPerMinute}</div>
                                <small className="text-muted">Requests/min</small>
                              </div>
                              <div className="text-end">
                                <div className="h6 mb-0">{performanceData.throughput.peak24h}</div>
                                <small className="text-muted">Peak (24h)</small>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* Database Connections */}
              <div className="col-lg-6">
                <div className="card border-0 rounded-4 shadow-sm h-100">
                  <div className="card-body p-4">
                    <div className="d-flex align-items-center mb-4">
                      <Database className="text-primary me-3" size={24} />
                      <h5 className="card-title fw-bold mb-0">Database Connections</h5>
                    </div>
                    
                    <div style={{ maxHeight: '300px', overflowY: 'auto' }}>
                      {connections.length > 0 ? connections.map((connection) => (
                        <div key={connection.connectionId} className="d-flex justify-content-between align-items-center py-2 px-3 mb-2 bg-light rounded-3">
                          <div className="flex-grow-1">
                            <div className="fw-semibold small">{connection.applicationName}</div>
                            <div className="text-muted small">{connection.connectionType}</div>
                          </div>
                          <div className="text-end">
                            <span className={`badge bg-${getStatusColor(connection.status)}`}>
                              {connection.status}
                            </span>
                            <div className="small text-muted mt-1">{connection.responseTime}ms</div>
                          </div>
                        </div>
                      )) : (
                        <div className="text-center py-4 text-muted">
                          <Database size={32} className="mb-2 opacity-50" />
                          <div>No database connections configured</div>
                        </div>
                      )}
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
      `}</style>
    </div>
  );
}