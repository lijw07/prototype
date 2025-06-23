import React, { useState, useEffect } from 'react';
import { 
  BarChart3, 
  TrendingUp, 
  TrendingDown,
  Users, 
  Database, 
  Shield, 
  Target,
  DollarSign,
  Activity,
  Award,
  Briefcase,
  RefreshCw,
  ArrowUpRight,
  ArrowDownRight,
  Calendar,
  PieChart,
  Globe,
  Zap,
  AlertCircle
} from 'lucide-react';
import { executiveDashboardApi } from '../../services/api';

interface ExecutiveOverview {
  summary: {
    totalUsers: number;
    totalApplications: number;
    securityScore: number;
    systemHealth: number;
    timeframe: string;
  };
  userMetrics: {
    total: number;
    verified: number;
    unverified: number;
    newUsersLast30Days: number;
    growthRate: number;
    adoptionRate: number;
  };
  applicationMetrics: {
    total: number;
    active: number;
    utilizationRate: number;
    totalConnections: number;
    averageConnectionsPerApp: number;
  };
  securityMetrics: {
    score: number;
    status: string;
    successfulLogins: number;
    failedLogins: number;
    securityEvents: number;
    complianceScore: number;
  };
  operationalMetrics: {
    systemHealth: number;
    averageUserSessions: number;
    totalRoles: number;
    dataIntegrity: number;
    uptime: number;
  };
  businessValue: {
    estimatedCostSavings: number;
    productivityGain: number;
    riskReduction: number;
    complianceReadiness: number;
  };
}

interface BusinessMetrics {
  accessControl: {
    totalAccessRequests: number;
    avgRequestsPerUser: number;
    rolesManaged: number;
    avgApplicationsPerUser: number;
  };
  adoption: {
    activeUsers: number;
    totalUsers: number;
    adoptionRate: number;
    engagementScore: number;
  };
  governance: {
    auditEvents: number;
    complianceScore: number;
    dataGovernanceScore: number;
    riskScore: number;
  };
  efficiency: {
    avgResponseTime: number;
    systemUtilization: number;
    automationRate: number;
    errorRate: number;
  };
}

interface GrowthTrends {
  monthlyTrends: Array<{
    month: string;
    monthName: string;
    usersAdded: number;
    applicationsAdded: number;
    totalActivity: number;
    cumulativeUsers: number;
    cumulativeApplications: number;
  }>;
  projections: {
    projectedUsers: number;
    projectedApplications: number;
    confidenceLevel: number;
  };
  insights: {
    insights: string[];
    recommendations: string[];
  };
}

export default function ExecutiveDashboard() {
  const [executiveData, setExecutiveData] = useState<ExecutiveOverview | null>(null);
  const [businessData, setBusinessData] = useState<BusinessMetrics | null>(null);
  const [growthData, setGrowthData] = useState<GrowthTrends | null>(null);
  const [loading, setLoading] = useState(true);
  const [lastUpdated, setLastUpdated] = useState<Date>(new Date());

  const fetchExecutiveData = async () => {
    try {
      setLoading(true);
      console.log('Fetching executive data...');
      
      const [overviewResponse, businessResponse, growthResponse] = await Promise.all([
        executiveDashboardApi.getExecutiveOverview(),
        executiveDashboardApi.getBusinessMetrics(),
        executiveDashboardApi.getGrowthTrends(6)
      ]);

      console.log('Executive overview response:', overviewResponse);
      console.log('Business metrics response:', businessResponse);
      console.log('Growth trends response:', growthResponse);

      if (overviewResponse.success) {
        setExecutiveData(overviewResponse.data);
      } else {
        console.error('Executive overview API failed:', overviewResponse);
      }
      
      if (businessResponse.success) {
        setBusinessData(businessResponse.data);
      } else {
        console.error('Business metrics API failed:', businessResponse);
      }
      
      if (growthResponse.success) {
        setGrowthData(growthResponse.data);
      } else {
        console.error('Growth trends API failed:', growthResponse);
      }
      
      setLastUpdated(new Date());
    } catch (error) {
      console.error('Failed to fetch executive data:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchExecutiveData();
    
    // Auto-refresh every 10 minutes
    const interval = setInterval(fetchExecutiveData, 10 * 60 * 1000);
    return () => clearInterval(interval);
  }, []);

  const getScoreColor = (score: number, reverse: boolean = false) => {
    if (reverse) {
      if (score <= 25) return 'success';
      if (score <= 50) return 'warning';
      return 'danger';
    }
    
    if (score >= 90) return 'success';
    if (score >= 75) return 'info';
    if (score >= 60) return 'warning';
    return 'danger';
  };

  const getTrendIcon = (value: number, size: number = 16) => {
    if (value > 0) return <ArrowUpRight className="text-success" size={size} />;
    if (value < 0) return <ArrowDownRight className="text-danger" size={size} />;
    return <div className="text-muted" style={{ width: size, height: size }}>–</div>;
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(amount);
  };

  const formatPercentage = (value: number) => {
    return `${value.toFixed(1)}%`;
  };

  if (loading && !executiveData) {
    return (
      <div className="min-vh-100 bg-light">
        <div className="container-fluid py-4">
          <div className="d-flex justify-content-center align-items-center" style={{ height: '50vh' }}>
            <div className="text-center">
              <div className="spinner-border text-primary mb-3" role="status">
                <span className="visually-hidden">Loading...</span>
              </div>
              <h5>Loading Executive Dashboard...</h5>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Show error state if APIs failed to load data
  if (!loading && !executiveData) {
    return (
      <div className="min-vh-100 bg-light">
        <div className="container-fluid py-4">
          <div className="d-flex justify-content-center align-items-center" style={{ height: '50vh' }}>
            <div className="text-center">
              <AlertCircle className="text-warning mb-3" size={48} />
              <h5>Unable to load executive dashboard data</h5>
              <p className="text-muted">Check console for errors or try refreshing the page.</p>
              <button 
                onClick={fetchExecutiveData}
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
                  <Briefcase className="text-primary me-3" size={32} />
                  Executive Dashboard
                </h1>
                <p className="text-muted mb-0">
                  Strategic insights and key performance indicators for leadership
                </p>
              </div>
              <div className="text-end">
                <button 
                  onClick={fetchExecutiveData}
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

        {executiveData && (
          <>
            {/* Executive Summary Cards */}
            <div className="row g-4 mb-4">
              <div className="col-lg-3 col-md-6">
                <div className="card border-0 rounded-4 shadow-sm h-100">
                  <div className="card-body p-4 text-center">
                    <Users className="text-primary mb-3" size={40} />
                    <h2 className="fw-bold mb-1">{executiveData.summary.totalUsers.toLocaleString()}</h2>
                    <h6 className="text-muted mb-2">Total Users</h6>
                    <div className="d-flex align-items-center justify-content-center">
                      {getTrendIcon(executiveData.userMetrics.growthRate)}
                      <span className="small ms-1">{formatPercentage(executiveData.userMetrics.growthRate)} growth</span>
                    </div>
                  </div>
                </div>
              </div>
              
              <div className="col-lg-3 col-md-6">
                <div className="card border-0 rounded-4 shadow-sm h-100">
                  <div className="card-body p-4 text-center">
                    <Database className="text-success mb-3" size={40} />
                    <h2 className="fw-bold mb-1">{executiveData.summary.totalApplications}</h2>
                    <h6 className="text-muted mb-2">Applications</h6>
                    <div className="small text-success">
                      {formatPercentage(executiveData.applicationMetrics.utilizationRate)} utilization
                    </div>
                  </div>
                </div>
              </div>
              
              <div className="col-lg-3 col-md-6">
                <div className="card border-0 rounded-4 shadow-sm h-100">
                  <div className="card-body p-4 text-center">
                    <Shield className={`text-${getScoreColor(executiveData.summary.securityScore)} mb-3`} size={40} />
                    <h2 className="fw-bold mb-1">{executiveData.summary.securityScore}/100</h2>
                    <h6 className="text-muted mb-2">Security Score</h6>
                    <span className={`badge bg-${getScoreColor(executiveData.summary.securityScore)} bg-opacity-10 text-${getScoreColor(executiveData.summary.securityScore)}`}>
                      {executiveData.securityMetrics.status}
                    </span>
                  </div>
                </div>
              </div>
              
              <div className="col-lg-3 col-md-6">
                <div className="card border-0 rounded-4 shadow-sm h-100">
                  <div className="card-body p-4 text-center">
                    <Activity className={`text-${getScoreColor(executiveData.summary.systemHealth)} mb-3`} size={40} />
                    <h2 className="fw-bold mb-1">{formatPercentage(executiveData.summary.systemHealth)}</h2>
                    <h6 className="text-muted mb-2">System Health</h6>
                    <div className="small text-muted">
                      {formatPercentage(executiveData.operationalMetrics.uptime)} uptime
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Business Value & ROI */}
            <div className="row g-4 mb-4">
              <div className="col-lg-8">
                <div className="card border-0 rounded-4 shadow-sm h-100">
                  <div className="card-body p-4">
                    <div className="d-flex align-items-center mb-4">
                      <DollarSign className="text-success me-3" size={24} />
                      <h5 className="card-title fw-bold mb-0">Business Value & ROI</h5>
                    </div>
                    
                    <div className="row g-4">
                      <div className="col-md-6">
                        <div className="bg-light rounded-3 p-3">
                          <div className="d-flex justify-content-between align-items-center mb-2">
                            <span className="fw-semibold">Cost Savings</span>
                            <TrendingUp className="text-success" size={16} />
                          </div>
                          <div className="h4 text-success mb-1">
                            {formatCurrency(executiveData.businessValue.estimatedCostSavings)}
                          </div>
                          <div className="small text-muted">Monthly estimate</div>
                        </div>
                      </div>
                      
                      <div className="col-md-6">
                        <div className="bg-light rounded-3 p-3">
                          <div className="d-flex justify-content-between align-items-center mb-2">
                            <span className="fw-semibold">Productivity Gain</span>
                            <TrendingUp className="text-info" size={16} />
                          </div>
                          <div className="h4 text-info mb-1">
                            {formatCurrency(executiveData.businessValue.productivityGain)}
                          </div>
                          <div className="small text-muted">Time savings value</div>
                        </div>
                      </div>
                      
                      <div className="col-md-6">
                        <div className="bg-light rounded-3 p-3">
                          <div className="d-flex justify-content-between align-items-center mb-2">
                            <span className="fw-semibold">Risk Reduction</span>
                            <Shield className="text-warning" size={16} />
                          </div>
                          <div className="h4 text-warning mb-1">
                            {formatPercentage(executiveData.businessValue.riskReduction)}
                          </div>
                          <div className="small text-muted">Security improvement</div>
                        </div>
                      </div>
                      
                      <div className="col-md-6">
                        <div className="bg-light rounded-3 p-3">
                          <div className="d-flex justify-content-between align-items-center mb-2">
                            <span className="fw-semibold">Compliance</span>
                            <Award className="text-primary" size={16} />
                          </div>
                          <div className="h4 text-primary mb-1">
                            {formatPercentage(executiveData.businessValue.complianceReadiness)}
                          </div>
                          <div className="small text-muted">Readiness score</div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div className="col-lg-4">
                <div className="card border-0 rounded-4 shadow-sm h-100">
                  <div className="card-body p-4">
                    <div className="d-flex align-items-center mb-4">
                      <Target className="text-primary me-3" size={24} />
                      <h5 className="card-title fw-bold mb-0">Key Metrics</h5>
                    </div>
                    
                    <div className="space-y-3">
                      <div className="d-flex justify-content-between align-items-center py-2">
                        <span className="small fw-semibold">User Adoption</span>
                        <span className="fw-bold text-success">
                          {formatPercentage(executiveData.userMetrics.adoptionRate)}
                        </span>
                      </div>
                      
                      <div className="d-flex justify-content-between align-items-center py-2">
                        <span className="small fw-semibold">App Utilization</span>
                        <span className="fw-bold text-info">
                          {formatPercentage(executiveData.applicationMetrics.utilizationRate)}
                        </span>
                      </div>
                      
                      <div className="d-flex justify-content-between align-items-center py-2">
                        <span className="small fw-semibold">Data Integrity</span>
                        <span className="fw-bold text-success">
                          {formatPercentage(executiveData.operationalMetrics.dataIntegrity)}
                        </span>
                      </div>
                      
                      <div className="d-flex justify-content-between align-items-center py-2">
                        <span className="small fw-semibold">Avg Sessions/User</span>
                        <span className="fw-bold text-primary">
                          {executiveData.operationalMetrics.averageUserSessions}
                        </span>
                      </div>
                      
                      {businessData && (
                        <div className="d-flex justify-content-between align-items-center py-2">
                          <span className="small fw-semibold">Engagement</span>
                          <span className="fw-bold text-warning">
                            {formatPercentage(businessData.adoption.engagementScore)}
                          </span>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Growth Trends & Insights */}
            {growthData && (
              <div className="row g-4 mb-4">
                <div className="col-lg-8">
                  <div className="card border-0 rounded-4 shadow-sm h-100">
                    <div className="card-body p-4">
                      <div className="d-flex align-items-center mb-4">
                        <BarChart3 className="text-primary me-3" size={24} />
                        <h5 className="card-title fw-bold mb-0">Growth Trends</h5>
                      </div>
                      
                      <div className="row g-3 mb-4">
                        <div className="col-md-4">
                          <div className="text-center p-3 bg-light rounded-3">
                            <div className="h5 fw-bold text-primary mb-1">
                              {growthData.projections.projectedUsers}
                            </div>
                            <div className="small text-muted">Projected Users</div>
                            <div className="small text-success">
                              {formatPercentage(growthData.projections.confidenceLevel)} confidence
                            </div>
                          </div>
                        </div>
                        
                        <div className="col-md-4">
                          <div className="text-center p-3 bg-light rounded-3">
                            <div className="h5 fw-bold text-success mb-1">
                              {growthData.projections.projectedApplications}
                            </div>
                            <div className="small text-muted">Projected Apps</div>
                            <div className="small text-info">Next 6 months</div>
                          </div>
                        </div>
                        
                        <div className="col-md-4">
                          <div className="text-center p-3 bg-light rounded-3">
                            <div className="h5 fw-bold text-warning mb-1">
                              {executiveData.userMetrics.newUsersLast30Days}
                            </div>
                            <div className="small text-muted">New Users</div>
                            <div className="small text-success">Last 30 days</div>
                          </div>
                        </div>
                      </div>
                      
                      <div className="bg-light rounded-3 p-3">
                        <h6 className="fw-semibold mb-2">Recent Growth Highlights</h6>
                        <div className="row g-2">
                          {growthData.monthlyTrends.slice(-3).map((trend, index) => (
                            <div key={index} className="col-md-4">
                              <div className="small">
                                <div className="fw-semibold">{trend.monthName}</div>
                                <div className="text-muted">
                                  +{trend.usersAdded} users, +{trend.applicationsAdded} apps
                                </div>
                              </div>
                            </div>
                          ))}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="col-lg-4">
                  <div className="card border-0 rounded-4 shadow-sm h-100">
                    <div className="card-body p-4">
                      <div className="d-flex align-items-center mb-4">
                        <Globe className="text-primary me-3" size={24} />
                        <h5 className="card-title fw-bold mb-0">Strategic Insights</h5>
                      </div>
                      
                      <div className="mb-4">
                        <h6 className="fw-semibold mb-2 small text-uppercase">Key Insights</h6>
                        {growthData.insights.insights.slice(0, 3).map((insight, index) => (
                          <div key={index} className="d-flex align-items-start mb-2">
                            <Zap className="text-warning me-2 mt-1 flex-shrink-0" size={14} />
                            <span className="small">{insight}</span>
                          </div>
                        ))}
                      </div>
                      
                      <div>
                        <h6 className="fw-semibold mb-2 small text-uppercase">Recommendations</h6>
                        {growthData.insights.recommendations.slice(0, 3).map((recommendation, index) => (
                          <div key={index} className="d-flex align-items-start mb-2">
                            <ArrowUpRight className="text-success me-2 mt-1 flex-shrink-0" size={14} />
                            <span className="small">{recommendation}</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Operational Excellence */}
            {businessData && (
              <div className="row g-4">
                <div className="col-lg-6">
                  <div className="card border-0 rounded-4 shadow-sm h-100">
                    <div className="card-body p-4">
                      <div className="d-flex align-items-center mb-4">
                        <PieChart className="text-primary me-3" size={24} />
                        <h5 className="card-title fw-bold mb-0">Operational Excellence</h5>
                      </div>
                      
                      <div className="row g-3">
                        <div className="col-6">
                          <div className="text-center">
                            <div className="h4 fw-bold text-primary mb-1">
                              {businessData.efficiency.avgResponseTime}ms
                            </div>
                            <div className="small text-muted">Avg Response Time</div>
                          </div>
                        </div>
                        
                        <div className="col-6">
                          <div className="text-center">
                            <div className="h4 fw-bold text-success mb-1">
                              {formatPercentage(businessData.efficiency.systemUtilization)}
                            </div>
                            <div className="small text-muted">System Utilization</div>
                          </div>
                        </div>
                        
                        <div className="col-6">
                          <div className="text-center">
                            <div className="h4 fw-bold text-info mb-1">
                              {formatPercentage(businessData.efficiency.automationRate)}
                            </div>
                            <div className="small text-muted">Automation Rate</div>
                          </div>
                        </div>
                        
                        <div className="col-6">
                          <div className="text-center">
                            <div className="h4 fw-bold text-warning mb-1">
                              {formatPercentage(businessData.efficiency.errorRate)}
                            </div>
                            <div className="small text-muted">Error Rate</div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="col-lg-6">
                  <div className="card border-0 rounded-4 shadow-sm h-100">
                    <div className="card-body p-4">
                      <div className="d-flex align-items-center mb-4">
                        <Award className="text-primary me-3" size={24} />
                        <h5 className="card-title fw-bold mb-0">Governance & Compliance</h5>
                      </div>
                      
                      <div className="space-y-3">
                        <div className="d-flex justify-content-between align-items-center py-2">
                          <span className="fw-semibold">Compliance Score</span>
                          <span className={`badge bg-${getScoreColor(businessData.governance.complianceScore)} bg-opacity-10 text-${getScoreColor(businessData.governance.complianceScore)}`}>
                            {formatPercentage(businessData.governance.complianceScore)}
                          </span>
                        </div>
                        
                        <div className="d-flex justify-content-between align-items-center py-2">
                          <span className="fw-semibold">Data Governance</span>
                          <span className={`badge bg-${getScoreColor(businessData.governance.dataGovernanceScore)} bg-opacity-10 text-${getScoreColor(businessData.governance.dataGovernanceScore)}`}>
                            {formatPercentage(businessData.governance.dataGovernanceScore)}
                          </span>
                        </div>
                        
                        <div className="d-flex justify-content-between align-items-center py-2">
                          <span className="fw-semibold">Risk Score</span>
                          <span className={`badge bg-${getScoreColor(businessData.governance.riskScore, true)} bg-opacity-10 text-${getScoreColor(businessData.governance.riskScore, true)}`}>
                            {businessData.governance.riskScore}/100
                          </span>
                        </div>
                        
                        <div className="d-flex justify-content-between align-items-center py-2">
                          <span className="fw-semibold">Audit Events (30d)</span>
                          <span className="fw-bold text-info">
                            {businessData.governance.auditEvents}
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}
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
        
        .space-y-3 > * + * {
          margin-top: 0.75rem;
        }
      `}</style>
    </div>
  );
}