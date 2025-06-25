import React, { useState, useEffect } from 'react';
import { 
  Shield, 
  FileText, 
  AlertTriangle,
  CheckCircle,
  Clock,
  Download,
  Settings,
  BarChart3,
  RefreshCw,
  Award,
  Target,
  TrendingUp,
  Users,
  Database,
  Activity,
  Calendar
} from 'lucide-react';
import { complianceApi } from '../../services/api';

interface ComplianceOverview {
  summary: {
    overallComplianceScore: number;
    status: string;
    lastAssessment: string;
    criticalIssues: number;
  };
  scores: {
    auditTrail: number;
    accessManagement: number;
    securityControls: number;
    dataRetention: number;
    userVerification: number;
  };
  metrics: {
    auditLogsLast30Days: number;
    userActivityLogs: number;
    verifiedUsersPercentage: number;
    activeUsersLast90Days: number;
    dataRetentionCompliance: number;
  };
  frameworks: Array<{
    framework: string;
    score: number;
    status: string;
  }>;
  recommendations: string[];
}

interface PolicyViolation {
  type: string;
  severity: string;
  description: string;
  entity: string;
  detectedAt: string;
  status: string;
}

export default function ComplianceDashboard() {
  const [overview, setOverview] = useState<ComplianceOverview | null>(null);
  const [violations, setViolations] = useState<PolicyViolation[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('overview');
  const [lastUpdated, setLastUpdated] = useState<Date>(new Date());

  const fetchData = async () => {
    try {
      setLoading(true);
      const [overviewResponse, violationsResponse] = await Promise.all([
        complianceApi.getComplianceOverview(),
        complianceApi.getPolicyViolations(1, 20)
      ]);

      if (overviewResponse.success) {
        setOverview(overviewResponse.data);
      }
      
      if (violationsResponse.success) {
        setViolations(violationsResponse.data.violations);
      }
      
      setLastUpdated(new Date());
    } catch (error) {
      console.error('Failed to fetch compliance data:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, 10 * 60 * 1000); // Refresh every 10 minutes
    return () => clearInterval(interval);
  }, []);

  const getScoreColor = (score: number) => {
    if (score >= 90) return 'success';
    if (score >= 80) return 'info';
    if (score >= 70) return 'warning';
    return 'danger';
  };

  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case 'High': return 'danger';
      case 'Medium': return 'warning';
      default: return 'info';
    }
  };

  const generateReport = async () => {
    try {
      const response = await complianceApi.generateCustomReport({
        startDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
        endDate: new Date().toISOString(),
        framework: 'General',
        format: 'JSON',
        includeAuditTrail: true,
        includeUserActivity: true,
        includeSecurityEvents: true,
        includeViolations: true
      });
      
      if (response.success) {
        alert('Compliance report generated successfully');
      }
    } catch (error) {
      console.error('Report generation failed:', error);
      alert('Failed to generate report');
    }
  };

  if (loading && !overview) {
    return (
      <div className="min-vh-100 bg-light">
        <div className="container-fluid py-4">
          <div className="d-flex justify-content-center align-items-center" style={{ height: '50vh' }}>
            <div className="text-center">
              <div className="spinner-border text-primary mb-3" role="status">
                <span className="visually-hidden">Loading...</span>
              </div>
              <h5>Loading Compliance Dashboard...</h5>
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
                  Compliance Dashboard
                </h1>
                <p className="text-muted mb-0">
                  Monitor regulatory compliance and audit readiness
                </p>
              </div>
              <div className="text-end">
                <button 
                  onClick={fetchData}
                  className="btn btn-outline-primary btn-sm d-flex align-items-center me-2"
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

        {/* Navigation Tabs */}
        <div className="row mb-4">
          <div className="col-12">
            <ul className="nav nav-pills nav-fill bg-white rounded-4 shadow-sm p-2">
              <li className="nav-item">
                <button
                  className={`nav-link ${activeTab === 'overview' ? 'active' : ''}`}
                  onClick={() => setActiveTab('overview')}
                >
                  <BarChart3 size={16} className="me-2" />
                  Overview
                </button>
              </li>
              <li className="nav-item">
                <button
                  className={`nav-link ${activeTab === 'frameworks' ? 'active' : ''}`}
                  onClick={() => setActiveTab('frameworks')}
                >
                  <Award size={16} className="me-2" />
                  Frameworks
                </button>
              </li>
              <li className="nav-item">
                <button
                  className={`nav-link ${activeTab === 'violations' ? 'active' : ''}`}
                  onClick={() => setActiveTab('violations')}
                >
                  <AlertTriangle size={16} className="me-2" />
                  Violations
                </button>
              </li>
              <li className="nav-item">
                <button
                  className={`nav-link ${activeTab === 'reports' ? 'active' : ''}`}
                  onClick={() => setActiveTab('reports')}
                >
                  <FileText size={16} className="me-2" />
                  Reports
                </button>
              </li>
            </ul>
          </div>
        </div>

        {overview && (
          <>
            {/* Overview Tab */}
            {activeTab === 'overview' && (
              <>
                {/* Summary Cards */}
                <div className="row g-4 mb-4">
                  <div className="col-lg-3 col-md-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4 text-center">
                        <Target className={`text-${getScoreColor(overview.summary.overallComplianceScore)} mb-3`} size={40} />
                        <h2 className="fw-bold mb-1">{overview.summary.overallComplianceScore}%</h2>
                        <h6 className="text-muted mb-2">Overall Score</h6>
                        <span className={`badge bg-${getScoreColor(overview.summary.overallComplianceScore)} bg-opacity-10 text-${getScoreColor(overview.summary.overallComplianceScore)}`}>
                          {overview.summary.status}
                        </span>
                      </div>
                    </div>
                  </div>
                  
                  <div className="col-lg-3 col-md-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4 text-center">
                        <AlertTriangle className="text-danger mb-3" size={40} />
                        <h2 className="fw-bold mb-1">{overview.summary.criticalIssues}</h2>
                        <h6 className="text-muted mb-2">Critical Issues</h6>
                        <div className="small text-danger">
                          Requires attention
                        </div>
                      </div>
                    </div>
                  </div>
                  
                  <div className="col-lg-3 col-md-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4 text-center">
                        <Activity className="text-info mb-3" size={40} />
                        <h2 className="fw-bold mb-1">{overview.metrics.auditLogsLast30Days}</h2>
                        <h6 className="text-muted mb-2">Audit Logs</h6>
                        <div className="small text-muted">Last 30 days</div>
                      </div>
                    </div>
                  </div>
                  
                  <div className="col-lg-3 col-md-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4 text-center">
                        <Users className="text-success mb-3" size={40} />
                        <h2 className="fw-bold mb-1">{overview.metrics.verifiedUsersPercentage}%</h2>
                        <h6 className="text-muted mb-2">User Verification</h6>
                        <div className="small text-success">Compliance rate</div>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Compliance Scores */}
                <div className="row g-4 mb-4">
                  <div className="col-lg-8">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4">
                        <div className="d-flex align-items-center mb-4">
                          <BarChart3 className="text-primary me-3" size={24} />
                          <h5 className="card-title fw-bold mb-0">Compliance Scores</h5>
                        </div>
                        
                        <div className="space-y-4">
                          <div className="row g-3">
                            <div className="col-md-6">
                              <div className="d-flex justify-content-between align-items-center mb-2">
                                <span className="fw-semibold">Audit Trail</span>
                                <span className={`badge bg-${getScoreColor(overview.scores.auditTrail)} bg-opacity-10 text-${getScoreColor(overview.scores.auditTrail)}`}>
                                  {overview.scores.auditTrail}%
                                </span>
                              </div>
                              <div className="progress" style={{ height: '8px' }}>
                                <div 
                                  className={`progress-bar bg-${getScoreColor(overview.scores.auditTrail)}`}
                                  style={{ width: `${overview.scores.auditTrail}%` }}
                                ></div>
                              </div>
                            </div>
                            
                            <div className="col-md-6">
                              <div className="d-flex justify-content-between align-items-center mb-2">
                                <span className="fw-semibold">Access Management</span>
                                <span className={`badge bg-${getScoreColor(overview.scores.accessManagement)} bg-opacity-10 text-${getScoreColor(overview.scores.accessManagement)}`}>
                                  {overview.scores.accessManagement}%
                                </span>
                              </div>
                              <div className="progress" style={{ height: '8px' }}>
                                <div 
                                  className={`progress-bar bg-${getScoreColor(overview.scores.accessManagement)}`}
                                  style={{ width: `${overview.scores.accessManagement}%` }}
                                ></div>
                              </div>
                            </div>
                            
                            <div className="col-md-6">
                              <div className="d-flex justify-content-between align-items-center mb-2">
                                <span className="fw-semibold">Security Controls</span>
                                <span className={`badge bg-${getScoreColor(overview.scores.securityControls)} bg-opacity-10 text-${getScoreColor(overview.scores.securityControls)}`}>
                                  {overview.scores.securityControls}%
                                </span>
                              </div>
                              <div className="progress" style={{ height: '8px' }}>
                                <div 
                                  className={`progress-bar bg-${getScoreColor(overview.scores.securityControls)}`}
                                  style={{ width: `${overview.scores.securityControls}%` }}
                                ></div>
                              </div>
                            </div>
                            
                            <div className="col-md-6">
                              <div className="d-flex justify-content-between align-items-center mb-2">
                                <span className="fw-semibold">Data Retention</span>
                                <span className={`badge bg-${getScoreColor(overview.scores.dataRetention)} bg-opacity-10 text-${getScoreColor(overview.scores.dataRetention)}`}>
                                  {overview.scores.dataRetention}%
                                </span>
                              </div>
                              <div className="progress" style={{ height: '8px' }}>
                                <div 
                                  className={`progress-bar bg-${getScoreColor(overview.scores.dataRetention)}`}
                                  style={{ width: `${overview.scores.dataRetention}%` }}
                                ></div>
                              </div>
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
                          <TrendingUp className="text-primary me-3" size={24} />
                          <h5 className="card-title fw-bold mb-0">Recommendations</h5>
                        </div>
                        
                        <div className="space-y-3">
                          {overview.recommendations.slice(0, 4).map((recommendation, index) => (
                            <div key={index} className="d-flex align-items-start">
                              <CheckCircle className="text-primary me-2 mt-1 flex-shrink-0" size={14} />
                              <span className="small">{recommendation}</span>
                            </div>
                          ))}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </>
            )}

            {/* Frameworks Tab */}
            {activeTab === 'frameworks' && (
              <div className="row g-4">
                {overview.frameworks.map((framework, index) => (
                  <div key={index} className="col-lg-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4">
                        <div className="d-flex justify-content-between align-items-center mb-3">
                          <div>
                            <h6 className="fw-bold mb-1">{framework.framework}</h6>
                            <span className={`badge bg-${getScoreColor(framework.score)} bg-opacity-10 text-${getScoreColor(framework.score)}`}>
                              {framework.status}
                            </span>
                          </div>
                          <div className="text-end">
                            <div className={`h4 fw-bold text-${getScoreColor(framework.score)} mb-0`}>
                              {framework.score}%
                            </div>
                          </div>
                        </div>
                        
                        <div className="progress mb-3" style={{ height: '10px' }}>
                          <div 
                            className={`progress-bar bg-${getScoreColor(framework.score)}`}
                            style={{ width: `${framework.score}%` }}
                          ></div>
                        </div>
                        
                        <div className="d-flex justify-content-between align-items-center">
                          <small className="text-muted">Last assessed: Today</small>
                          <button className="btn btn-sm btn-outline-primary">
                            View Details
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}

            {/* Violations Tab */}
            {activeTab === 'violations' && (
              <div className="row">
                <div className="col-12">
                  <div className="card border-0 rounded-4 shadow-sm">
                    <div className="card-header bg-transparent border-0 p-4">
                      <h5 className="fw-bold mb-0">Policy Violations</h5>
                    </div>
                    <div className="card-body p-0">
                      <div className="table-responsive">
                        <table className="table mb-0">
                          <thead className="bg-light">
                            <tr>
                              <th className="border-0 px-4 py-3">Type</th>
                              <th className="border-0 px-4 py-3">Description</th>
                              <th className="border-0 px-4 py-3">Severity</th>
                              <th className="border-0 px-4 py-3">Detected</th>
                              <th className="border-0 px-4 py-3">Status</th>
                            </tr>
                          </thead>
                          <tbody>
                            {violations.map((violation, index) => (
                              <tr key={index}>
                                <td className="px-4 py-3">
                                  <span className="fw-semibold">{violation.type}</span>
                                </td>
                                <td className="px-4 py-3">
                                  <span className="small text-muted">{violation.description}</span>
                                </td>
                                <td className="px-4 py-3">
                                  <span className={`badge bg-${getSeverityColor(violation.severity)} bg-opacity-10 text-${getSeverityColor(violation.severity)}`}>
                                    {violation.severity}
                                  </span>
                                </td>
                                <td className="px-4 py-3">
                                  <span className="small text-muted">
                                    {new Date(violation.detectedAt).toLocaleDateString()}
                                  </span>
                                </td>
                                <td className="px-4 py-3">
                                  <span className="badge bg-warning bg-opacity-10 text-warning">
                                    {violation.status}
                                  </span>
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Reports Tab */}
            {activeTab === 'reports' && (
              <div className="row g-4">
                <div className="col-lg-8">
                  <div className="card border-0 rounded-4 shadow-sm h-100">
                    <div className="card-body p-4">
                      <div className="d-flex align-items-center mb-4">
                        <FileText className="text-primary me-3" size={24} />
                        <h5 className="card-title fw-bold mb-0">Generate Compliance Report</h5>
                      </div>
                      
                      <div className="row g-3 mb-4">
                        <div className="col-md-6">
                          <label className="form-label">Report Period</label>
                          <select className="form-select">
                            <option>Last 30 Days</option>
                            <option>Last 90 Days</option>
                            <option>Last 6 Months</option>
                            <option>Last Year</option>
                            <option>Custom Range</option>
                          </select>
                        </div>
                        
                        <div className="col-md-6">
                          <label className="form-label">Framework</label>
                          <select className="form-select">
                            <option>All Frameworks</option>
                            <option>SOX</option>
                            <option>GDPR</option>
                            <option>HIPAA</option>
                            <option>ISO27001</option>
                          </select>
                        </div>
                      </div>
                      
                      <div className="mb-4">
                        <label className="form-label">Include Sections</label>
                        <div className="row g-2">
                          <div className="col-md-6">
                            <div className="form-check">
                              <input className="form-check-input" type="checkbox" defaultChecked />
                              <label className="form-check-label">Audit Trail</label>
                            </div>
                          </div>
                          <div className="col-md-6">
                            <div className="form-check">
                              <input className="form-check-input" type="checkbox" defaultChecked />
                              <label className="form-check-label">User Activity</label>
                            </div>
                          </div>
                          <div className="col-md-6">
                            <div className="form-check">
                              <input className="form-check-input" type="checkbox" defaultChecked />
                              <label className="form-check-label">Security Events</label>
                            </div>
                          </div>
                          <div className="col-md-6">
                            <div className="form-check">
                              <input className="form-check-input" type="checkbox" defaultChecked />
                              <label className="form-check-label">Policy Violations</label>
                            </div>
                          </div>
                        </div>
                      </div>
                      
                      <button 
                        onClick={generateReport}
                        className="btn btn-primary d-flex align-items-center"
                      >
                        <Download className="me-2" size={16} />
                        Generate Report
                      </button>
                    </div>
                  </div>
                </div>

                <div className="col-lg-4">
                  <div className="card border-0 rounded-4 shadow-sm h-100">
                    <div className="card-body p-4">
                      <div className="d-flex align-items-center mb-4">
                        <Calendar className="text-primary me-3" size={24} />
                        <h5 className="card-title fw-bold mb-0">Recent Reports</h5>
                      </div>
                      
                      <div className="space-y-3">
                        <div className="border rounded-3 p-3">
                          <div className="d-flex justify-content-between align-items-center">
                            <div>
                              <div className="fw-semibold small">SOX Compliance Report</div>
                              <div className="text-muted small">Generated yesterday</div>
                            </div>
                            <button className="btn btn-sm btn-outline-primary">
                              <Download size={14} />
                            </button>
                          </div>
                        </div>
                        
                        <div className="border rounded-3 p-3">
                          <div className="d-flex justify-content-between align-items-center">
                            <div>
                              <div className="fw-semibold small">GDPR Audit Trail</div>
                              <div className="text-muted small">Generated 3 days ago</div>
                            </div>
                            <button className="btn btn-sm btn-outline-primary">
                              <Download size={14} />
                            </button>
                          </div>
                        </div>
                        
                        <div className="border rounded-3 p-3">
                          <div className="d-flex justify-content-between align-items-center">
                            <div>
                              <div className="fw-semibold small">Monthly Security Report</div>
                              <div className="text-muted small">Generated last week</div>
                            </div>
                            <button className="btn btn-sm btn-outline-primary">
                              <Download size={14} />
                            </button>
                          </div>
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
        
        .space-y-4 > * + * {
          margin-top: 1rem;
        }
      `}</style>
    </div>
  );
}