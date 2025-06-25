import React, { useState, useEffect } from 'react';
import { 
  Plus, 
  Send, 
  Clock, 
  CheckCircle, 
  XCircle, 
  AlertCircle, 
  Filter, 
  RefreshCw,
  Github,
  Database,
  Cloud,
  Shield,
  Settings,
  Monitor,
  Code,
  Users,
  FileText,
  Search,
  Eye,
  Star,
  TrendingUp,
  Zap,
  Calendar,
  Bell,
  ChevronRight,
  MoreVertical,
  ExternalLink
} from 'lucide-react';
import { userRequestsApi } from '../../services/api';

interface UserRequest {
  id: string;
  toolName: string;
  toolCategory: string;
  reason: string;
  status: 'pending' | 'approved' | 'denied';
  requestedAt: string;
  reviewedAt?: string;
  reviewedBy?: string;
  comments?: string;
  priority: 'low' | 'medium' | 'high';
}

interface ToolCategory {
  id: string;
  name: string;
  icon: React.ComponentType<any>;
  tools: Array<{
    id: string;
    name: string;
    description: string;
    requiresApproval: boolean;
  }>;
}

const toolCategories: ToolCategory[] = [
  {
    id: 'development',
    name: 'Development Tools',
    icon: Code,
    tools: [
      { id: 'github', name: 'GitHub Repository Access', description: 'Access to company GitHub repositories', requiresApproval: true },
      { id: 'gitlab', name: 'GitLab Access', description: 'Access to GitLab projects and repositories', requiresApproval: true },
      { id: 'jira', name: 'Jira Project Access', description: 'Access to specific Jira projects', requiresApproval: true },
      { id: 'confluence', name: 'Confluence Spaces', description: 'Access to documentation spaces', requiresApproval: false },
    ]
  },
  {
    id: 'database',
    name: 'Database Access',
    icon: Database,
    tools: [
      { id: 'prod-db', name: 'Production Database', description: 'Read-only access to production database', requiresApproval: true },
      { id: 'staging-db', name: 'Staging Database', description: 'Full access to staging database', requiresApproval: true },
      { id: 'analytics-db', name: 'Analytics Database', description: 'Access to analytics and reporting database', requiresApproval: true },
    ]
  },
  {
    id: 'cloud',
    name: 'Cloud Services',
    icon: Cloud,
    tools: [
      { id: 'aws-console', name: 'AWS Console Access', description: 'Access to AWS management console', requiresApproval: true },
      { id: 'azure-portal', name: 'Azure Portal', description: 'Access to Azure cloud services', requiresApproval: true },
      { id: 'gcp-console', name: 'Google Cloud Console', description: 'Access to Google Cloud Platform', requiresApproval: true },
    ]
  },
  {
    id: 'security',
    name: 'Security Tools',
    icon: Shield,
    tools: [
      { id: 'vault', name: 'HashiCorp Vault', description: 'Access to secrets management', requiresApproval: true },
      { id: 'okta-admin', name: 'Okta Admin', description: 'User management in Okta', requiresApproval: true },
      { id: 'splunk', name: 'Splunk Dashboards', description: 'Access to security monitoring dashboards', requiresApproval: true },
    ]
  },
  {
    id: 'monitoring',
    name: 'Monitoring & Analytics',
    icon: Monitor,
    tools: [
      { id: 'grafana', name: 'Grafana Dashboards', description: 'Access to monitoring dashboards', requiresApproval: false },
      { id: 'datadog', name: 'Datadog Access', description: 'Application performance monitoring', requiresApproval: true },
      { id: 'newrelic', name: 'New Relic', description: 'Application monitoring and analytics', requiresApproval: true },
    ]
  },
  {
    id: 'collaboration',
    name: 'Collaboration Tools',
    icon: Users,
    tools: [
      { id: 'slack-channels', name: 'Slack Private Channels', description: 'Access to specific private channels', requiresApproval: false },
      { id: 'teams-channels', name: 'Microsoft Teams', description: 'Access to team channels', requiresApproval: false },
      { id: 'notion', name: 'Notion Workspaces', description: 'Access to team documentation', requiresApproval: false },
    ]
  }
];

export default function UserRequests() {
  const [requests, setRequests] = useState<UserRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [showNewRequestModal, setShowNewRequestModal] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState<string>('');
  const [selectedTool, setSelectedTool] = useState<string>('');
  const [requestReason, setRequestReason] = useState('');
  const [priority, setPriority] = useState<'low' | 'medium' | 'high'>('medium');
  const [filterStatus, setFilterStatus] = useState<string>('all');

  useEffect(() => {
    fetchRequests();
  }, []);

  // Handle escape key to close modal
  useEffect(() => {
    const handleEscapeKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && showNewRequestModal) {
        setShowNewRequestModal(false);
        resetForm();
      }
    };

    if (showNewRequestModal) {
      document.addEventListener('keydown', handleEscapeKey);
    }

    return () => {
      document.removeEventListener('keydown', handleEscapeKey);
    };
  }, [showNewRequestModal]);

  const fetchRequests = async () => {
    try {
      setLoading(true);
      const response = await userRequestsApi.getUserRequests();
      if (response.success) {
        setRequests(response.data);
      }
    } catch (error) {
      console.error('Failed to fetch user requests:', error);
      // Mock data for development
      setRequests([
        {
          id: '1',
          toolName: 'GitHub Repository Access',
          toolCategory: 'Development Tools',
          reason: 'Need access to work on the new authentication feature',
          status: 'pending',
          requestedAt: new Date().toISOString(),
          priority: 'high'
        },
        {
          id: '2',
          toolName: 'Production Database',
          toolCategory: 'Database Access',
          reason: 'Investigation of performance issues reported by customers',
          status: 'approved',
          requestedAt: new Date(Date.now() - 86400000).toISOString(),
          reviewedAt: new Date().toISOString(),
          reviewedBy: 'admin@company.com',
          priority: 'high'
        }
      ]);
    } finally {
      setLoading(false);
    }
  };

  const submitRequest = async () => {
    if (!selectedTool || !requestReason.trim()) return;

    const selectedToolData = toolCategories
      .find(cat => cat.id === selectedCategory)
      ?.tools.find(tool => tool.id === selectedTool);

    if (!selectedToolData) return;

    try {
      const newRequest = {
        toolId: selectedTool,
        toolName: selectedToolData.name,
        toolCategory: toolCategories.find(cat => cat.id === selectedCategory)?.name || '',
        reason: requestReason,
        priority: priority
      };

      const response = await userRequestsApi.createRequest(newRequest);
      if (response.success) {
        await fetchRequests();
        setShowNewRequestModal(false);
        resetForm();
      }
    } catch (error) {
      console.error('Failed to submit request:', error);
      // Mock success for development
      const mockRequest: UserRequest = {
        id: Date.now().toString(),
        toolName: selectedToolData.name,
        toolCategory: toolCategories.find(cat => cat.id === selectedCategory)?.name || '',
        reason: requestReason,
        status: 'pending',
        requestedAt: new Date().toISOString(),
        priority: priority
      };
      setRequests(prev => [mockRequest, ...prev]);
      setShowNewRequestModal(false);
      resetForm();
    }
  };

  const resetForm = () => {
    setSelectedCategory('');
    setSelectedTool('');
    setRequestReason('');
    setPriority('medium');
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'pending': return <Clock className="text-warning" size={16} />;
      case 'approved': return <CheckCircle className="text-success" size={16} />;
      case 'denied': return <XCircle className="text-danger" size={16} />;
      default: return <AlertCircle className="text-muted" size={16} />;
    }
  };

  const getStatusBadge = (status: string) => {
    const baseClasses = "badge";
    switch (status) {
      case 'pending': return `${baseClasses} bg-warning`;
      case 'approved': return `${baseClasses} bg-success`;
      case 'denied': return `${baseClasses} bg-danger`;
      default: return `${baseClasses} bg-secondary`;
    }
  };

  const getPriorityBadge = (priority: string) => {
    const baseClasses = "badge";
    switch (priority) {
      case 'high': return `${baseClasses} bg-danger bg-opacity-10 text-danger`;
      case 'medium': return `${baseClasses} bg-warning bg-opacity-10 text-warning`;
      case 'low': return `${baseClasses} bg-info bg-opacity-10 text-info`;
      default: return `${baseClasses} bg-secondary`;
    }
  };

  const filteredRequests = requests.filter(request => 
    filterStatus === 'all' || request.status === filterStatus
  );

  const selectedCategoryData = toolCategories.find(cat => cat.id === selectedCategory);

  return (
    <div className="min-vh-100 bg-light">
      <div className="container-fluid py-4">
        {/* Header */}
        <div className="mb-4">
          <div className="d-flex align-items-center mb-2">
            <Settings className="text-primary me-3" size={32} />
            <h1 className="display-5 fw-bold text-dark mb-0">User Requests</h1>
          </div>
          <p className="text-muted fs-6">Request access to external tools and track your requests</p>
        </div>

        {/* Stats Cards */}
        <div className="row g-4 mb-4">
          <div className="col-lg-3 col-md-6">
            <div className="card border-0 rounded-4 shadow-sm h-100">
              <div className="card-body p-4">
                <div className="d-flex align-items-center justify-content-between mb-3">
                  <div className="rounded-3 p-3 bg-warning bg-opacity-10">
                    <Clock className="text-warning" size={24} />
                  </div>
                </div>
                <h3 className="display-6 fw-bold text-dark mb-1">
                  {filteredRequests.filter(r => r.status === 'pending').length}
                </h3>
                <h6 className="fw-semibold text-muted mb-1">Pending Requests</h6>
                <p className="small text-muted mb-0">Awaiting approval</p>
              </div>
            </div>
          </div>
          <div className="col-lg-3 col-md-6">
            <div className="card border-0 rounded-4 shadow-sm h-100">
              <div className="card-body p-4">
                <div className="d-flex align-items-center justify-content-between mb-3">
                  <div className="rounded-3 p-3 bg-success bg-opacity-10">
                    <CheckCircle className="text-success" size={24} />
                  </div>
                </div>
                <h3 className="display-6 fw-bold text-dark mb-1">
                  {filteredRequests.filter(r => r.status === 'approved').length}
                </h3>
                <h6 className="fw-semibold text-muted mb-1">Approved Requests</h6>
                <p className="small text-muted mb-0">Access granted</p>
              </div>
            </div>
          </div>
          <div className="col-lg-3 col-md-6">
            <div className="card border-0 rounded-4 shadow-sm h-100">
              <div className="card-body p-4">
                <div className="d-flex align-items-center justify-content-between mb-3">
                  <div className="rounded-3 p-3 bg-danger bg-opacity-10">
                    <XCircle className="text-danger" size={24} />
                  </div>
                </div>
                <h3 className="display-6 fw-bold text-dark mb-1">
                  {filteredRequests.filter(r => r.status === 'denied').length}
                </h3>
                <h6 className="fw-semibold text-muted mb-1">Denied Requests</h6>
                <p className="small text-muted mb-0">Access declined</p>
              </div>
            </div>
          </div>
          <div className="col-lg-3 col-md-6">
            <div className="card border-0 rounded-4 shadow-sm h-100">
              <div className="card-body p-4">
                <div className="d-flex align-items-center justify-content-between mb-3">
                  <div className="rounded-3 p-3 bg-primary bg-opacity-10">
                    <Settings className="text-primary" size={24} />
                  </div>
                </div>
                <h3 className="display-6 fw-bold text-dark mb-1">
                  {filteredRequests.length}
                </h3>
                <h6 className="fw-semibold text-muted mb-1">Total Requests</h6>
                <p className="small text-muted mb-0">All time</p>
              </div>
            </div>
          </div>
        </div>

        {/* Quick Actions */}
        <div className="row mb-4">
          <div className="col-12">
            <div className="card border-0 rounded-4 shadow-sm bg-white">
              <div className="card-body p-4">
                <div className="d-flex justify-content-between align-items-center">
                  <div>
                    <h5 className="fw-bold text-dark mb-1">Quick Actions</h5>
                    <p className="text-muted mb-0">Manage your access requests</p>
                  </div>
                  <div className="d-flex gap-2">
                    <button 
                      onClick={fetchRequests}
                      className="btn btn-outline-secondary rounded-3"
                      disabled={loading}
                    >
                      <RefreshCw className={`me-2 ${loading ? 'rotating' : ''}`} size={16} />
                      Refresh
                    </button>
                    <button 
                      onClick={() => setShowNewRequestModal(true)}
                      className="btn btn-primary rounded-3 fw-semibold"
                    >
                      <Plus className="me-2" size={16} />
                      New Request
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Filters */}
        <div className="row mb-4">
          <div className="col-12">
            <div className="card border-0 rounded-4 shadow-sm bg-white">
              <div className="card-body p-4">
                <div className="row align-items-center">
                  <div className="col-md-6">
                    <div className="d-flex align-items-center mb-2">
                      <Filter className="text-primary me-2" size={20} />
                      <h6 className="fw-bold mb-0">Filter Requests</h6>
                    </div>
                    <select 
                      className="form-select rounded-3"
                      value={filterStatus}
                      onChange={(e) => setFilterStatus(e.target.value)}
                    >
                      <option value="all">All Requests ({requests.length})</option>
                      <option value="pending">Pending ({requests.filter(r => r.status === 'pending').length})</option>
                      <option value="approved">Approved ({requests.filter(r => r.status === 'approved').length})</option>
                      <option value="denied">Denied ({requests.filter(r => r.status === 'denied').length})</option>
                    </select>
                  </div>
                  <div className="col-md-6">
                    <div className="d-flex align-items-center mb-2">
                      <Search className="text-primary me-2" size={20} />
                      <h6 className="fw-bold mb-0">Search Tools</h6>
                    </div>
                    <div className="position-relative">
                      <input 
                        type="text" 
                        className="form-control rounded-3" 
                        placeholder="Search by tool name..."
                        style={{ paddingLeft: '40px' }}
                      />
                      <Search className="position-absolute text-muted" size={16} style={{ left: '12px', top: '50%', transform: 'translateY(-50%)' }} />
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Requests List */}
        <div className="row">
          <div className="col-12">
            <div className="card border-0 rounded-4 shadow-sm bg-white">
              <div className="card-body p-4">
                <div className="d-flex align-items-center mb-4">
                  <FileText className="text-primary me-3" size={24} />
                  <h5 className="card-title fw-bold mb-0">Your Requests</h5>
                </div>
                {loading ? (
                  <div className="text-center py-5">
                    <div className="spinner-border text-primary mb-3" role="status">
                      <span className="visually-hidden">Loading...</span>
                    </div>
                    <p className="text-muted">Loading your requests...</p>
                  </div>
                ) : filteredRequests.length === 0 ? (
                  <div className="text-center py-5">
                    <div className="mb-4">
                      <div className="rounded-3 p-4 bg-primary bg-opacity-10 d-inline-flex">
                        <Settings className="text-primary" size={64} />
                      </div>
                    </div>
                    <h4 className="fw-bold text-dark mb-3">No requests found</h4>
                    <p className="text-muted mb-4">Start by requesting access to the tools you need for your work.</p>
                    <div className="d-flex justify-content-center gap-3">
                      <button 
                        onClick={() => setShowNewRequestModal(true)}
                        className="btn btn-primary btn-lg rounded-3 fw-semibold"
                      >
                        <Plus className="me-2" size={20} />
                        Create Your First Request
                      </button>
                      <button 
                        className="btn btn-outline-secondary btn-lg rounded-3"
                      >
                        <Eye className="me-2" size={20} />
                        Browse Available Tools
                      </button>
                    </div>
                  </div>
                ) : (
                  <div className="row g-3">
                    {filteredRequests.map((request) => (
                        <div key={request.id} className="col-12">
                          <div className="card border-0 rounded-4 shadow-sm h-100" style={{ 
                            background: request.status === 'approved' ? 'rgba(40, 167, 69, 0.05)' : 
                                       request.status === 'denied' ? 'rgba(220, 53, 69, 0.05)' : 
                                       'rgba(255, 193, 7, 0.05)'
                          }}>
                            <div className="card-body p-4">
                              <div className="row align-items-center">
                                <div className="col-md-8">
                                  <div className="d-flex align-items-center mb-2">
                                    <div className="rounded-3 p-2 bg-primary bg-opacity-10 me-3">
                                      <Settings size={20} className="text-primary" />
                                    </div>
                                    <div>
                                      <h6 className="fw-bold mb-0">{request.toolName}</h6>
                                      <small className="text-muted">{request.toolCategory}</small>
                                    </div>
                                  </div>
                                  <p className="text-muted small mb-2">
                                    {request.reason}
                                  </p>
                                </div>
                                <div className="col-md-4">
                                  <div className="text-end">
                                    <div className="mb-2">
                                      <span className={getPriorityBadge(request.priority)}>
                                        {request.priority}
                                      </span>
                                      <span className={`${getStatusBadge(request.status)} ms-2`}>
                                        {request.status}
                                      </span>
                                    </div>
                                    <div className="d-flex justify-content-end gap-2">
                                      <button className="btn btn-sm btn-outline-primary rounded-3">
                                        <Eye size={14} />
                                      </button>
                                      <button className="btn btn-sm btn-outline-secondary rounded-3">
                                        <MoreVertical size={14} />
                                      </button>
                                    </div>
                                  </div>
                                </div>
                              </div>
                              <div className="row mt-3">
                                <div className="col-12">
                                  <div className="d-flex align-items-center justify-content-between">
                                    <div className="d-flex align-items-center text-muted small">
                                      <Calendar size={14} className="me-1" />
                                      Requested {new Date(request.requestedAt).toLocaleDateString()}
                                      {request.reviewedAt && (
                                        <>
                                          <span className="mx-2">•</span>
                                          <CheckCircle size={14} className="me-1" />
                                          Reviewed {new Date(request.reviewedAt).toLocaleDateString()}
                                        </>
                                      )}
                                    </div>
                                    {request.reviewedBy && (
                                      <span className="badge bg-light text-dark rounded-3">
                                        By {request.reviewedBy}
                                      </span>
                                    )}
                                  </div>
                                  {request.comments && (
                                    <div className="mt-2 p-2 bg-light rounded-3">
                                      <small className="text-muted"><strong>Comments:</strong> {request.comments}</small>
                                    </div>
                                  )}
                                </div>
                              </div>
                            </div>
                          </div>
                        </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* New Request Modal */}
        {showNewRequestModal && (
          <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
            <div className="modal-dialog modal-xl">
              <div className="modal-content border-0 rounded-4 shadow-lg">
                <div className="modal-header border-0">
                  <div className="d-flex align-items-center">
                    <div className="rounded-3 p-3 bg-primary bg-opacity-10 me-3">
                      <Plus className="text-primary" size={24} />
                    </div>
                    <div>
                      <h4 className="modal-title fw-bold mb-1">Request Tool Access</h4>
                      <p className="text-muted mb-0">Choose the tools you need to enhance your productivity</p>
                      <small className="text-muted">Press <kbd>Esc</kbd> to close</small>
                    </div>
                  </div>
                  <button 
                    type="button" 
                    className="btn-close" 
                    onClick={() => setShowNewRequestModal(false)}
                  ></button>
                </div>
                <div className="modal-body p-4">
                  <div className="mb-4">
                    <label className="form-label fw-semibold mb-3">Choose Tool Category</label>
                    <div className="row g-3">
                      {toolCategories.map((category) => {
                        const Icon = category.icon;
                        return (
                          <div key={category.id} className="col-md-4">
                            <div 
                              className={`card border-2 h-100 ${
                                selectedCategory === category.id ? 'border-primary bg-primary bg-opacity-10' : 'border-light'
                              }`}
                              style={{ 
                                borderRadius: '12px', 
                                cursor: 'pointer'
                              }}
                              onClick={() => {
                                setSelectedCategory(category.id);
                                setSelectedTool('');
                              }}
                            >
                              <div className="card-body text-center p-4">
                                <div className={`rounded-3 p-3 d-inline-flex mb-3 ${
                                  selectedCategory === category.id ? 'bg-primary' : 'bg-light'
                                }`}>
                                  <Icon className={selectedCategory === category.id ? 'text-white' : 'text-primary'} size={32} />
                                </div>
                                <h6 className="fw-semibold mb-2">{category.name}</h6>
                                <small className="text-muted">{category.tools.length} tools available</small>
                              </div>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>

                  {selectedCategoryData && (
                    <div className="mb-4">
                      <label className="form-label fw-semibold mb-3">Select Specific Tool</label>
                      <div className="row g-3">
                        {selectedCategoryData.tools.map((tool) => (
                          <div key={tool.id} className="col-md-6">
                            <div 
                              className={`card border-2 h-100 ${
                                selectedTool === tool.id ? 'border-success bg-success bg-opacity-10' : 'border-light'
                              }`}
                              style={{ 
                                borderRadius: '12px', 
                                cursor: 'pointer'
                              }}
                              onClick={() => setSelectedTool(tool.id)}
                            >
                              <div className="card-body p-3">
                                <div className="d-flex align-items-start">
                                  <div className={`rounded-3 p-2 me-3 ${
                                    selectedTool === tool.id ? 'bg-success' : 'bg-light'
                                  }`}>
                                    <ExternalLink className={selectedTool === tool.id ? 'text-white' : 'text-success'} size={16} />
                                  </div>
                                  <div className="flex-grow-1">
                                    <h6 className="fw-semibold mb-1">{tool.name}</h6>
                                    <p className="text-muted small mb-2">{tool.description}</p>
                                    <div className="d-flex gap-2">
                                      {tool.requiresApproval ? (
                                        <span className="badge bg-warning bg-opacity-10 text-warning">
                                          <AlertCircle size={12} className="me-1" />
                                          Requires Approval
                                        </span>
                                      ) : (
                                        <span className="badge bg-success bg-opacity-10 text-success">
                                          <CheckCircle size={12} className="me-1" />
                                          Auto-Approved
                                        </span>
                                      )}
                                    </div>
                                  </div>
                                </div>
                              </div>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  <div className="mb-4">
                    <label className="form-label fw-semibold mb-3">Business Justification</label>
                    <textarea 
                      className="form-control rounded-3"
                      rows={5}
                      placeholder="Please provide a detailed explanation of why you need access to this tool and how it will help you accomplish your work objectives..."
                      value={requestReason}
                      onChange={(e) => setRequestReason(e.target.value)}
                    />
                    <div className="d-flex justify-content-between mt-2">
                      <small className="text-muted">Be specific about your use case and timeline</small>
                      <small className="text-muted">
                        {requestReason.length}/500 characters
                      </small>
                    </div>
                  </div>

                  <div className="mb-4">
                    <label className="form-label fw-semibold mb-3">Request Priority</label>
                    <div className="row g-3">
                      {[
                        { value: 'low', label: 'Low Priority', desc: 'Can wait a few days', color: 'info', icon: Calendar },
                        { value: 'medium', label: 'Medium Priority', desc: 'Needed within a week', color: 'warning', icon: Clock },
                        { value: 'high', label: 'High Priority', desc: 'Urgent, needed ASAP', color: 'danger', icon: Zap }
                      ].map((p) => {
                        const Icon = p.icon;
                        return (
                          <div key={p.value} className="col-md-4">
                            <div 
                              className={`card border-2 h-100 ${
                                priority === p.value ? `border-${p.color} bg-${p.color} bg-opacity-10` : 'border-light'
                              }`}
                              style={{ 
                                borderRadius: '12px', 
                                cursor: 'pointer'
                              }}
                              onClick={() => setPriority(p.value as 'low' | 'medium' | 'high')}
                            >
                              <div className="card-body text-center p-3">
                                <div className={`rounded-3 p-2 d-inline-flex mb-2 ${
                                  priority === p.value ? `bg-${p.color}` : 'bg-light'
                                }`}>
                                  <Icon className={priority === p.value ? 'text-white' : `text-${p.color}`} size={20} />
                                </div>
                                <h6 className="fw-semibold mb-1">{p.label}</h6>
                                <small className="text-muted">{p.desc}</small>
                              </div>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                </div>
                <div className="modal-footer border-0">
                  <div className="d-flex justify-content-between align-items-center w-100">
                    <div>
                      {selectedTool && selectedCategoryData && (
                        <div className="d-flex align-items-center text-muted">
                          <Bell size={16} className="me-2" />
                          <small>
                            {selectedCategoryData.tools.find(t => t.id === selectedTool)?.requiresApproval 
                              ? 'This request will be reviewed by an administrator'
                              : 'This request will be auto-approved'
                            }
                          </small>
                        </div>
                      )}
                    </div>
                    <div className="d-flex gap-2">
                      <button 
                        type="button" 
                        className="btn btn-secondary rounded-3" 
                        onClick={() => setShowNewRequestModal(false)}
                      >
                        Cancel
                      </button>
                      <button 
                        type="button" 
                        className="btn btn-primary rounded-3 fw-semibold"
                        onClick={submitRequest}
                        disabled={!selectedTool || !requestReason.trim()}
                      >
                        <Send className="me-2" size={18} />
                        Submit Request
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
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