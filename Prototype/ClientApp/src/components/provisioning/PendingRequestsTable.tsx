// Pending Requests Table Component
// Follows SRP: Only responsible for displaying and managing pending provisioning requests

import React, { useEffect, useState, useCallback } from 'react';
import { 
  Clock,
  CheckCircle,
  XCircle,
  Eye,
  Search,
  Filter,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  AlertCircle,
  Mail,
  Calendar,
  User
} from 'lucide-react';
import { useProvisioning } from './hooks/useProvisioning';
import type { PendingRequestsProps } from './types/provisioning.types';

// Use the hook's interface for compatibility
interface PendingRequest {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  requestedRole: string;
  requestDate: string;
  status: 'pending' | 'approved' | 'rejected';
  requestedBy?: string;
}

const PendingRequestsTable: React.FC<PendingRequestsProps> = ({
  className = '',
  filters,
  onRequestAction,
  pageSize: initialPageSize = 10,
  onError,
  onSuccess
}) => {
  const {
    pendingRequests,
    pagination,
    loading,
    error,
    fetchPendingRequests,
    updateRequestStatus,
    changePage,
    changePageSize
  } = useProvisioning();

  const [searchTerm, setSearchTerm] = useState(filters?.searchTerm || '');
  const [statusFilter, setStatusFilter] = useState<string>(filters?.status?.[0] || 'all');
  const [priorityFilter, setPriorityFilter] = useState<string>(filters?.priority?.[0] || 'all');
  const [selectedRequest, setSelectedRequest] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  // Initial data fetch
  useEffect(() => {
    fetchPendingRequests(1, initialPageSize);
  }, [fetchPendingRequests, initialPageSize]);

  // Handle errors
  useEffect(() => {
    if (error && onError) {
      onError(error);
    }
  }, [error, onError]);

  // Filter requests based on search and filters
  const filteredRequests = pendingRequests.filter(request => {
    const matchesSearch = searchTerm === '' || 
      request.firstName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      request.lastName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      request.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      request.requestedRole.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesStatus = statusFilter === 'all' || request.status === statusFilter;
    // Priority filter disabled for now since the hook interface doesn't include priority
    const matchesPriority = priorityFilter === 'all' || true; // Always match for now

    return matchesSearch && matchesStatus && matchesPriority;
  });

  // Handle request approval
  const handleApproveRequest = useCallback(async (requestId: string) => {
    setActionLoading(requestId);
    try {
      const result = await updateRequestStatus(requestId, 'approved');
      if (result.success) {
        if (onSuccess) {
          onSuccess('Request approved successfully');
        }
        if (onRequestAction) {
          onRequestAction(requestId, 'approved');
        }
      } else {
        if (onError) {
          onError(result.message);
        }
      }
    } finally {
      setActionLoading(null);
    }
  }, [updateRequestStatus, onSuccess, onError, onRequestAction]);

  // Handle request rejection
  const handleRejectRequest = useCallback(async (requestId: string, reason?: string) => {
    setActionLoading(requestId);
    try {
      const result = await updateRequestStatus(requestId, 'rejected', reason);
      if (result.success) {
        if (onSuccess) {
          onSuccess('Request rejected successfully');
        }
        if (onRequestAction) {
          onRequestAction(requestId, 'rejected');
        }
      } else {
        if (onError) {
          onError(result.message);
        }
      }
    } finally {
      setActionLoading(null);
    }
  }, [updateRequestStatus, onSuccess, onError, onRequestAction]);

  // Format date for display
  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  // Get priority badge color
  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'urgent': return 'danger';
      case 'high': return 'warning';
      case 'medium': return 'info';
      case 'low': return 'secondary';
      default: return 'secondary';
    }
  };

  // Get status badge color
  const getStatusColor = (status: string) => {
    switch (status) {
      case 'pending': return 'warning';
      case 'reviewing': return 'info';
      case 'approved': return 'success';
      case 'rejected': return 'danger';
      case 'processing': return 'primary';
      default: return 'secondary';
    }
  };

  if (loading && pendingRequests.length === 0) {
    return (
      <div className={`card border-0 rounded-4 shadow-sm ${className}`}>
        <div className="card-body p-4">
          <div className="d-flex align-items-center justify-content-center py-5">
            <div className="spinner-border text-primary me-3" role="status">
              <span className="visually-hidden">Loading...</span>
            </div>
            <span className="text-muted">Loading pending requests...</span>
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
            <div className="rounded-circle bg-warning bg-opacity-10 p-3 me-3">
              <Clock className="text-warning" size={24} />
            </div>
            <div>
              <h5 className="card-title mb-0 fw-bold text-dark">Pending Requests</h5>
              <p className="text-muted small mb-0">
                {filteredRequests.length} of {pagination.totalCount} requests
              </p>
            </div>
          </div>
        </div>

        {/* Filters and Search */}
        <div className="row g-3">
          <div className="col-md-4">
            <div className="position-relative">
              <Search className="position-absolute top-50 start-0 translate-middle-y ms-3 text-muted" size={18} />
              <input
                type="text"
                className="form-control rounded-3 ps-5"
                placeholder="Search by name, email, or role..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />
            </div>
          </div>
          <div className="col-md-3">
            <select
              className="form-select rounded-3"
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
            >
              <option value="all">All Status</option>
              <option value="pending">Pending</option>
              <option value="reviewing">Reviewing</option>
              <option value="processing">Processing</option>
            </select>
          </div>
          <div className="col-md-3">
            <select
              className="form-select rounded-3"
              value={priorityFilter}
              onChange={(e) => setPriorityFilter(e.target.value)}
            >
              <option value="all">All Priority</option>
              <option value="urgent">Urgent</option>
              <option value="high">High</option>
              <option value="medium">Medium</option>
              <option value="low">Low</option>
            </select>
          </div>
          <div className="col-md-2">
            <div className="d-flex align-items-center justify-content-end">
              <Filter className="text-muted me-2" size={18} />
              <span className="small text-muted">Filters</span>
            </div>
          </div>
        </div>
      </div>

      <div className="card-body p-4">
        {filteredRequests.length === 0 ? (
          <div className="text-center py-5">
            <Clock size={48} className="text-muted opacity-50 mb-3" />
            <h5 className="text-muted mb-2">No Pending Requests</h5>
            <p className="text-muted">
              {searchTerm || statusFilter !== 'all' || priorityFilter !== 'all' 
                ? 'No requests match your current filters.' 
                : 'All provisioning requests have been processed.'}
            </p>
          </div>
        ) : (
          <>
            {/* Requests Table */}
            <div className="table-responsive">
              <table className="table table-hover align-middle">
                <thead className="table-light">
                  <tr>
                    <th className="border-0 fw-semibold">Requestor</th>
                    <th className="border-0 fw-semibold">Role</th>
                    <th className="border-0 fw-semibold">Priority</th>
                    <th className="border-0 fw-semibold">Status</th>
                    <th className="border-0 fw-semibold">Request Date</th>
                    <th className="border-0 fw-semibold text-center">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredRequests.map((request) => (
                    <React.Fragment key={request.id}>
                      <tr>
                      <td>
                        <div className="d-flex align-items-center">
                          <div className="rounded-circle bg-primary bg-opacity-10 p-2 me-3">
                            <User size={16} className="text-primary" />
                          </div>
                          <div>
                            <div className="fw-semibold text-dark">
                              {request.firstName} {request.lastName}
                            </div>
                            <div className="small text-muted d-flex align-items-center">
                              <Mail size={12} className="me-1" />
                              {request.email}
                            </div>
                          </div>
                        </div>
                      </td>
                      <td>
                        <span className="badge bg-info bg-opacity-10 text-info">
                          {request.requestedRole}
                        </span>
                      </td>
                      <td>
                        <span className="badge bg-secondary bg-opacity-10 text-secondary">
                          Standard
                        </span>
                      </td>
                      <td>
                        <span className={`badge bg-${getStatusColor(request.status)} bg-opacity-10 text-${getStatusColor(request.status)}`}>
                          {request.status.charAt(0).toUpperCase() + request.status.slice(1)}
                        </span>
                      </td>
                      <td>
                        <div className="d-flex align-items-center text-muted small">
                          <Calendar size={12} className="me-1" />
                          {formatDate(request.requestDate)}
                        </div>
                      </td>
                      <td className="text-center">
                        <div className="btn-group" role="group">
                          {/* View Details */}
                          <button
                            className="btn btn-outline-secondary btn-sm"
                            title="View Details"
                            onClick={() => setSelectedRequest(
                              selectedRequest === request.id ? null : request.id
                            )}
                          >
                            <Eye size={14} />
                          </button>

                          {/* Approve */}
                          {request.status === 'pending' && (
                            <button
                              className="btn btn-outline-success btn-sm"
                              title="Approve Request"
                              disabled={actionLoading === request.id}
                              onClick={() => handleApproveRequest(request.id)}
                            >
                              {actionLoading === request.id ? (
                                <div className="spinner-border spinner-border-sm" role="status">
                                  <span className="visually-hidden">Loading...</span>
                                </div>
                              ) : (
                                <CheckCircle size={14} />
                              )}
                            </button>
                          )}

                          {/* Reject */}
                          {request.status === 'pending' && (
                            <button
                              className="btn btn-outline-danger btn-sm"
                              title="Reject Request"
                              disabled={actionLoading === request.id}
                              onClick={() => handleRejectRequest(request.id)}
                            >
                              <XCircle size={14} />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                    
                    {/* Expandable Details Row */}
                    {selectedRequest === request.id && (
                      <tr>
                        <td colSpan={6}>
                          <div className="bg-light rounded-3 p-4 my-2">
                            <h6 className="fw-semibold mb-3">Request Details</h6>
                            <div className="row g-3">
                              <div className="col-md-4">
                                <strong>Email:</strong>
                                <div className="text-muted">{request.email}</div>
                              </div>
                              <div className="col-md-4">
                                <strong>Role:</strong>
                                <div className="text-muted">{request.requestedRole}</div>
                              </div>
                              <div className="col-md-4">
                                <strong>Requested By:</strong>
                                <div className="text-muted">{request.requestedBy || 'Self-service'}</div>
                              </div>
                              <div className="col-12">
                                <strong>Request Date:</strong>
                                <div className="text-muted mt-1">{formatDate(request.requestDate)}</div>
                              </div>
                            </div>
                          </div>
                        </td>
                      </tr>
                    )}
                    </React.Fragment>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {pagination.totalPages > 1 && (
              <div className="d-flex justify-content-between align-items-center mt-4">
                <div className="d-flex align-items-center gap-3">
                  <span className="text-muted small">
                    Showing {((pagination.page - 1) * pagination.pageSize) + 1} to {
                      Math.min(pagination.page * pagination.pageSize, pagination.totalCount)
                    } of {pagination.totalCount} requests
                  </span>
                  <div className="d-flex align-items-center gap-2">
                    <span className="text-muted small">Per page:</span>
                    <select 
                      className="form-select form-select-sm" 
                      style={{width: 'auto'}}
                      value={pagination.pageSize}
                      onChange={(e) => changePageSize(Number(e.target.value))}
                    >
                      <option value={10}>10</option>
                      <option value={25}>25</option>
                      <option value={50}>50</option>
                      <option value={100}>100</option>
                    </select>
                  </div>
                </div>
                
                <nav>
                  <ul className="pagination pagination-sm mb-0">
                    <li className={`page-item ${pagination.page === 1 ? 'disabled' : ''}`}>
                      <button 
                        className="page-link" 
                        onClick={() => changePage(1)}
                        disabled={pagination.page === 1}
                      >
                        <ChevronsLeft size={16} />
                      </button>
                    </li>
                    <li className={`page-item ${pagination.page === 1 ? 'disabled' : ''}`}>
                      <button 
                        className="page-link" 
                        onClick={() => changePage(pagination.page - 1)}
                        disabled={pagination.page === 1}
                      >
                        <ChevronLeft size={16} />
                      </button>
                    </li>
                    
                    {/* Page numbers */}
                    {Array.from({ length: Math.min(5, pagination.totalPages) }, (_, i) => {
                      let pageNum: number;
                      if (pagination.totalPages <= 5) {
                        pageNum = i + 1;
                      } else if (pagination.page <= 3) {
                        pageNum = i + 1;
                      } else if (pagination.page >= pagination.totalPages - 2) {
                        pageNum = pagination.totalPages - 4 + i;
                      } else {
                        pageNum = pagination.page - 2 + i;
                      }
                      
                      return (
                        <li key={pageNum} className={`page-item ${pagination.page === pageNum ? 'active' : ''}`}>
                          <button 
                            className="page-link" 
                            onClick={() => changePage(pageNum)}
                          >
                            {pageNum}
                          </button>
                        </li>
                      );
                    })}
                    
                    <li className={`page-item ${pagination.page === pagination.totalPages ? 'disabled' : ''}`}>
                      <button 
                        className="page-link" 
                        onClick={() => changePage(pagination.page + 1)}
                        disabled={pagination.page === pagination.totalPages}
                      >
                        <ChevronRight size={16} />
                      </button>
                    </li>
                    <li className={`page-item ${pagination.page === pagination.totalPages ? 'disabled' : ''}`}>
                      <button 
                        className="page-link" 
                        onClick={() => changePage(pagination.totalPages)}
                        disabled={pagination.page === pagination.totalPages}
                      >
                        <ChevronsRight size={16} />
                      </button>
                    </li>
                  </ul>
                </nav>
              </div>
            )}
          </>
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

export default PendingRequestsTable;