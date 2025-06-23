import React, { useState, useEffect } from 'react';
import { 
  Users, 
  UserPlus, 
  Clock, 
  CheckCircle,
  AlertCircle,
  Settings,
  Zap,
  FileText,
  Upload,
  Download,
  RefreshCw,
  ArrowUpRight,
  ArrowDownRight,
  Target,
  TrendingUp,
  BarChart3
} from 'lucide-react';
import { userProvisioningApi } from '../../services/api';

interface ProvisioningOverview {
  summary: {
    totalUsers: number;
    pendingUsers: number;
    recentlyProvisioned: number;
    provisioningEfficiency: number;
  };
  userMetrics: {
    total: number;
    verified: number;
    pending: number;
    accessGranted: number;
    rolesAssigned: number;
  };
  applicationAccess: {
    totalApplications: number;
    usersWithAccess: number;
    averageAppsPerUser: number;
    accessCoverage: number;
  };
  efficiency: {
    avgProvisioningTime: number;
    autoProvisioningRate: number;
    pendingBacklog: number;
    throughput: number;
  };
}

interface PendingRequest {
  temporaryUserId: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  role: string;
  createdAt: string;
  daysWaiting: number;
  priority: string;
}

export default function UserProvisioning() {
  const [overview, setOverview] = useState<ProvisioningOverview | null>(null);
  const [pendingRequests, setPendingRequests] = useState<PendingRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('overview');
  const [lastUpdated, setLastUpdated] = useState<Date>(new Date());

  const fetchData = async () => {
    try {
      setLoading(true);
      const [overviewResponse, requestsResponse] = await Promise.all([
        userProvisioningApi.getProvisioningOverview(),
        userProvisioningApi.getPendingRequests(1, 20)
      ]);

      if (overviewResponse.success) {
        setOverview(overviewResponse.data);
      }
      
      if (requestsResponse.success) {
        setPendingRequests(requestsResponse.data.requests);
      }
      
      setLastUpdated(new Date());
    } catch (error) {
      console.error('Failed to fetch provisioning data:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, 5 * 60 * 1000); // Refresh every 5 minutes
    return () => clearInterval(interval);
  }, []);

  const handleAutoProvision = async () => {
    try {
      const response = await userProvisioningApi.autoProvisionUsers({
        criteria: { maxDaysWaiting: 7 },
        maxUsers: 10,
        applyDefaultAccess: true
      });
      
      if (response.success) {
        await fetchData();
        alert(`Auto-provisioned ${response.data.processedCount} users`);
      }
    } catch (error) {
      console.error('Auto-provisioning failed:', error);
      alert('Auto-provisioning failed');
    }
  };

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'High': return 'danger';
      case 'Medium': return 'warning';
      default: return 'success';
    }
  };

  const getTrendIcon = (value: number, size: number = 16) => {
    if (value > 0) return <ArrowUpRight className="text-success" size={size} />;
    if (value < 0) return <ArrowDownRight className="text-danger" size={size} />;
    return <div className="text-muted" style={{ width: size, height: size }}>–</div>;
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
              <h5>Loading User Provisioning...</h5>
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
                  <UserPlus className="text-primary me-3" size={32} />
                  User Provisioning
                </h1>
                <p className="text-muted mb-0">
                  Automated user lifecycle management and access provisioning
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
                  className={`nav-link ${activeTab === 'requests' ? 'active' : ''}`}
                  onClick={() => setActiveTab('requests')}
                >
                  <Clock size={16} className="me-2" />
                  Pending Requests
                </button>
              </li>
              <li className="nav-item">
                <button
                  className={`nav-link ${activeTab === 'automation' ? 'active' : ''}`}
                  onClick={() => setActiveTab('automation')}
                >
                  <Zap size={16} className="me-2" />
                  Automation
                </button>
              </li>
              <li className="nav-item">
                <button
                  className={`nav-link ${activeTab === 'bulk' ? 'active' : ''}`}
                  onClick={() => setActiveTab('bulk')}
                >
                  <Upload size={16} className="me-2" />
                  Bulk Operations
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
                        <Users className="text-primary mb-3" size={40} />
                        <h2 className="fw-bold mb-1">{overview.summary.totalUsers}</h2>
                        <h6 className="text-muted mb-2">Total Users</h6>
                        <div className="d-flex align-items-center justify-content-center">
                          {getTrendIcon(5)}
                          <span className="small ms-1 text-success">5% growth</span>
                        </div>
                      </div>
                    </div>
                  </div>
                  
                  <div className="col-lg-3 col-md-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4 text-center">
                        <Clock className="text-warning mb-3" size={40} />
                        <h2 className="fw-bold mb-1">{overview.summary.pendingUsers}</h2>
                        <h6 className="text-muted mb-2">Pending Users</h6>
                        <div className="small text-warning">
                          Awaiting provisioning
                        </div>
                      </div>
                    </div>
                  </div>
                  
                  <div className="col-lg-3 col-md-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4 text-center">
                        <CheckCircle className="text-success mb-3" size={40} />
                        <h2 className="fw-bold mb-1">{overview.summary.recentlyProvisioned}</h2>
                        <h6 className="text-muted mb-2">Recent Provisions</h6>
                        <div className="small text-muted">Last 7 days</div>
                      </div>
                    </div>
                  </div>
                  
                  <div className="col-lg-3 col-md-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4 text-center">
                        <Target className="text-info mb-3" size={40} />
                        <h2 className="fw-bold mb-1">{overview.summary.provisioningEfficiency}%</h2>
                        <h6 className="text-muted mb-2">Automation Rate</h6>
                        <div className="small text-info">Efficiency score</div>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Detailed Metrics */}
                <div className="row g-4 mb-4">
                  <div className="col-lg-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4">
                        <div className="d-flex align-items-center mb-4">
                          <Users className="text-primary me-3" size={24} />
                          <h5 className="card-title fw-bold mb-0">User Metrics</h5>
                        </div>
                        
                        <div className="row g-3">
                          <div className="col-6">
                            <div className="text-center p-3 bg-light rounded-3">
                              <div className="h4 fw-bold text-primary mb-1">
                                {overview.userMetrics.verified}
                              </div>
                              <div className="small text-muted">Verified Users</div>
                            </div>
                          </div>
                          
                          <div className="col-6">
                            <div className="text-center p-3 bg-light rounded-3">
                              <div className="h4 fw-bold text-success mb-1">
                                {overview.userMetrics.accessGranted}
                              </div>
                              <div className="small text-muted">With Access</div>
                            </div>
                          </div>
                          
                          <div className="col-6">
                            <div className="text-center p-3 bg-light rounded-3">
                              <div className="h4 fw-bold text-info mb-1">
                                {overview.userMetrics.rolesAssigned}
                              </div>
                              <div className="small text-muted">Roles Assigned</div>
                            </div>
                          </div>
                          
                          <div className="col-6">
                            <div className="text-center p-3 bg-light rounded-3">
                              <div className="h4 fw-bold text-warning mb-1">
                                {overview.userMetrics.pending}
                              </div>
                              <div className="small text-muted">Pending</div>
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
                          <TrendingUp className="text-primary me-3" size={24} />
                          <h5 className="card-title fw-bold mb-0">Efficiency Metrics</h5>
                        </div>
                        
                        <div className="space-y-3">
                          <div className="d-flex justify-content-between align-items-center py-2">
                            <span className="fw-semibold">Avg Provisioning Time</span>
                            <span className="fw-bold text-primary">
                              {overview.efficiency.avgProvisioningTime}h
                            </span>
                          </div>
                          
                          <div className="d-flex justify-content-between align-items-center py-2">
                            <span className="fw-semibold">Auto-Provisioning Rate</span>
                            <span className="fw-bold text-success">
                              {overview.efficiency.autoProvisioningRate}%
                            </span>
                          </div>
                          
                          <div className="d-flex justify-content-between align-items-center py-2">
                            <span className="fw-semibold">Pending Backlog</span>
                            <span className="fw-bold text-warning">
                              {overview.efficiency.pendingBacklog}
                            </span>
                          </div>
                          
                          <div className="d-flex justify-content-between align-items-center py-2">
                            <span className="fw-semibold">Weekly Throughput</span>
                            <span className="fw-bold text-info">
                              {overview.efficiency.throughput}
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </>
            )}

            {/* Pending Requests Tab */}
            {activeTab === 'requests' && (
              <div className="row">
                <div className="col-12">
                  <div className="card border-0 rounded-4 shadow-sm">
                    <div className="card-header bg-transparent border-0 p-4">
                      <div className="d-flex justify-content-between align-items-center">
                        <h5 className="fw-bold mb-0">Pending Provisioning Requests</h5>
                        <button 
                          onClick={handleAutoProvision}
                          className="btn btn-primary btn-sm d-flex align-items-center"
                        >
                          <Zap className="me-2" size={16} />
                          Auto-Provision Eligible
                        </button>
                      </div>
                    </div>
                    <div className="card-body p-0">
                      <div className="table-responsive">
                        <table className="table table-hover mb-0">
                          <thead className="bg-light">
                            <tr>
                              <th className="border-0 px-4 py-3">User</th>
                              <th className="border-0 px-4 py-3">Role</th>
                              <th className="border-0 px-4 py-3">Days Waiting</th>
                              <th className="border-0 px-4 py-3">Priority</th>
                              <th className="border-0 px-4 py-3">Actions</th>
                            </tr>
                          </thead>
                          <tbody>
                            {pendingRequests.map((request) => (
                              <tr key={request.temporaryUserId}>
                                <td className="px-4 py-3">
                                  <div>
                                    <div className="fw-semibold">
                                      {request.firstName} {request.lastName}
                                    </div>
                                    <div className="small text-muted">
                                      {request.email}
                                    </div>
                                  </div>
                                </td>
                                <td className="px-4 py-3">
                                  <span className="badge bg-info bg-opacity-10 text-info">
                                    {request.role}
                                  </span>
                                </td>
                                <td className="px-4 py-3">
                                  <span className={`fw-semibold ${request.daysWaiting > 7 ? 'text-danger' : request.daysWaiting > 3 ? 'text-warning' : 'text-success'}`}>
                                    {request.daysWaiting} days
                                  </span>
                                </td>
                                <td className="px-4 py-3">
                                  <span className={`badge bg-${getPriorityColor(request.priority)} bg-opacity-10 text-${getPriorityColor(request.priority)}`}>
                                    {request.priority}
                                  </span>
                                </td>
                                <td className="px-4 py-3">
                                  <div className="d-flex gap-2">
                                    <button className="btn btn-sm btn-outline-success">
                                      <CheckCircle size={14} />
                                    </button>
                                    <button className="btn btn-sm btn-outline-secondary">
                                      <Settings size={14} />
                                    </button>
                                  </div>
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

            {/* Automation Tab */}
            {activeTab === 'automation' && (
              <div className="row g-4">
                <div className="col-lg-8">
                  <div className="card border-0 rounded-4 shadow-sm h-100">
                    <div className="card-body p-4">
                      <div className="d-flex align-items-center mb-4">
                        <Zap className="text-primary me-3" size={24} />
                        <h5 className="card-title fw-bold mb-0">Automation Rules</h5>
                      </div>
                      
                      <div className="alert alert-info">
                        <AlertCircle className="me-2" size={16} />
                        Auto-provisioning rules help streamline user onboarding based on predefined criteria.
                      </div>

                      <div className="space-y-3">
                        <div className="border rounded-3 p-3">
                          <div className="d-flex justify-content-between align-items-center">
                            <div>
                              <div className="fw-semibold">Standard User Auto-Provision</div>
                              <div className="small text-muted">
                                Automatically provision users with 'User' role after 24 hours
                              </div>
                            </div>
                            <div className="form-check form-switch">
                              <input className="form-check-input" type="checkbox" defaultChecked />
                            </div>
                          </div>
                        </div>

                        <div className="border rounded-3 p-3">
                          <div className="d-flex justify-content-between align-items-center">
                            <div>
                              <div className="fw-semibold">Priority Role Fast-Track</div>
                              <div className="small text-muted">
                                Immediately provision Admin and Manager roles
                              </div>
                            </div>
                            <div className="form-check form-switch">
                              <input className="form-check-input" type="checkbox" defaultChecked />
                            </div>
                          </div>
                        </div>

                        <div className="border rounded-3 p-3">
                          <div className="d-flex justify-content-between align-items-center">
                            <div>
                              <div className="fw-semibold">Department-Based Access</div>
                              <div className="small text-muted">
                                Grant application access based on user department
                              </div>
                            </div>
                            <div className="form-check form-switch">
                              <input className="form-check-input" type="checkbox" />
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
                        <Target className="text-primary me-3" size={24} />
                        <h5 className="card-title fw-bold mb-0">Quick Actions</h5>
                      </div>
                      
                      <div className="d-grid gap-3">
                        <button 
                          onClick={handleAutoProvision}
                          className="btn btn-primary d-flex align-items-center justify-content-center"
                        >
                          <Zap className="me-2" size={16} />
                          Run Auto-Provision
                        </button>
                        
                        <button className="btn btn-outline-secondary d-flex align-items-center justify-content-center">
                          <Settings className="me-2" size={16} />
                          Configure Rules
                        </button>
                        
                        <button className="btn btn-outline-info d-flex align-items-center justify-content-center">
                          <FileText className="me-2" size={16} />
                          View Templates
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Bulk Operations Tab */}
            {activeTab === 'bulk' && (
              <div className="row g-4">
                <div className="col-12">
                  <div className="card border-0 rounded-4 shadow-sm">
                    <div className="card-body p-4">
                      <div className="d-flex align-items-center mb-4">
                        <Upload className="text-primary me-3" size={24} />
                        <h5 className="card-title fw-bold mb-0">Bulk User Operations</h5>
                      </div>
                      
                      <div className="row g-4">
                        <div className="col-lg-6">
                          <div className="border rounded-3 p-4 text-center">
                            <Upload className="text-primary mb-3" size={48} />
                            <h6 className="fw-semibold mb-2">Import Users</h6>
                            <p className="text-muted small mb-3">
                              Upload CSV file to create multiple users at once
                            </p>
                            <button className="btn btn-primary">
                              Choose File
                            </button>
                          </div>
                        </div>
                        
                        <div className="col-lg-6">
                          <div className="border rounded-3 p-4 text-center">
                            <Download className="text-success mb-3" size={48} />
                            <h6 className="fw-semibold mb-2">Export Template</h6>
                            <p className="text-muted small mb-3">
                              Download CSV template for bulk user import
                            </p>
                            <button className="btn btn-outline-success">
                              Download Template
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
      `}</style>
    </div>
  );
}