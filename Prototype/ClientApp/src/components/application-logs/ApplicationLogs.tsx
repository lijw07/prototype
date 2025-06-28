import React, { useCallback } from 'react';
import { FileText, Clock, User, Monitor } from 'lucide-react';
import { applicationLogApi } from '../../services/api';
import { Pagination, LoadingSpinner } from '../shared';
import { formatDateTime } from '../../utils/dateUtils';
import { useApiWithErrorHandling, usePagination } from '../../hooks/shared';

interface ApplicationLog {
    applicationLogId: string;
    applicationId: string;
    applicationName: string;
    actionType: string;
    metadata?: string;
    createdAt: string;
    updatedAt?: string;
}


const ApplicationLogs: React.FC = () => {
    // Use pagination hook
    const pagination = usePagination({
        initialPageSize: 20
    });

    // Use API hook with error handling for fetching application logs
    const { 
        data: logsData, 
        loading, 
        error, 
        execute: fetchLogs,
        canRetry,
        retry,
        isNetworkError
    } = useApiWithErrorHandling(
        (page: number, pageSize: number) => applicationLogApi.getApplicationLogs(page, pageSize),
        {
            immediate: true,
            showErrorNotification: true,
            retryable: true,
            maxRetries: 2,
            onSuccess: (data) => {
                if (data && data.data) {
                    // Update pagination with response data
                    pagination.setTotalCount(data.totalCount || 0);
                }
            },
            dependencies: [pagination.currentPage, pagination.pageSize]
        }
    );

    // Extract application logs from API response
    const applicationLogs = logsData?.data || [];

    // Handle page changes
    const handlePageChange = useCallback((newPage: number) => {
        pagination.setPage(newPage);
        fetchLogs(newPage, pagination.pageSize);
    }, [pagination, fetchLogs]);

    // Handle page size changes
    const handlePageSizeChange = useCallback((newPageSize: number) => {
        pagination.setPageSize(newPageSize);
        fetchLogs(1, newPageSize);
    }, [pagination, fetchLogs]);

    const getActionBadgeColor = (actionType: string) => {
        switch (actionType.toLowerCase()) {
            case 'navigation':
                return 'bg-primary';
            case 'page view':
                return 'bg-info';
            case 'form submission':
                return 'bg-success';
            case 'error':
                return 'bg-danger';
            default:
                return 'bg-secondary';
        }
    };

    return (
        <div className="min-vh-100 bg-light">
            <div className="container-fluid py-4">
                {/* Header */}
                <div className="mb-4">
                    <div className="d-flex align-items-center mb-2">
                        <FileText className="text-primary me-3" size={32} />
                        <h1 className="display-5 fw-bold text-dark mb-0">Application Logs</h1>
                    </div>
                    <p className="text-muted fs-6">Track navigation events and application interactions</p>
                </div>

                {/* Application Logs Table */}
                <div className="card shadow-sm border-0 rounded-4" style={{ transition: 'none', transform: 'none' }}>
                    <div className="card-body p-4" style={{ transition: 'none', transform: 'none' }}>
                        <h2 className="card-title fw-bold text-dark mb-4 d-flex align-items-center">
                            <Monitor className="text-primary me-2" size={24} />
                            Navigation & Interaction Timeline
                        </h2>
                        
                        {loading ? (
                            <LoadingSpinner 
                                text="Loading application logs..." 
                                size="sm"
                            />
                        ) : (
                            <div className="table-responsive">
                                <table className="table" style={{ cursor: 'default' }}>
                                    <thead className="table-light">
                                        <tr>
                                            <th className="fw-semibold">Application</th>
                                            <th className="fw-semibold">Action</th>
                                            <th className="fw-semibold">Details</th>
                                            <th className="fw-semibold">
                                                Created At
                                            </th>
                                            <th className="fw-semibold">Updated At</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {(Array.isArray(applicationLogs) ? applicationLogs : []).map((log, index) => (
                                            <tr key={`${log.applicationLogId}-${index}`} style={{ transition: 'none', transform: 'none' }}>
                                                <td>
                                                    <span className="badge bg-light text-dark border">
                                                        {log.applicationName}
                                                    </span>
                                                </td>
                                                <td>
                                                    <span className={`badge ${getActionBadgeColor(log.actionType)}`}>
                                                        {log.actionType}
                                                    </span>
                                                </td>
                                                <td className="small text-muted">{log.metadata}</td>
                                                <td className="small">{formatDateTime(log.createdAt)}</td>
                                                <td className="small">{formatDateTime(log.updatedAt)}</td>
                                            </tr>
                                        ))}
                                        {(Array.isArray(applicationLogs) ? applicationLogs : []).length === 0 && (
                                            <tr>
                                                <td colSpan={5} className="text-center py-5 text-muted">
                                                    <FileText size={48} className="mb-3 opacity-50" />
                                                    <p>No application logs available</p>
                                                </td>
                                            </tr>
                                        )}
                                    </tbody>
                                </table>
                            </div>
                        )}
                        
                        {/* Pagination Controls */}
                        {!loading && applicationLogs.length > 0 && (
                            <Pagination
                                currentPage={pagination.currentPage}
                                totalPages={pagination.totalPages}
                                pageSize={pagination.pageSize}
                                totalCount={pagination.totalCount}
                                onPageChange={handlePageChange}
                                onPageSizeChange={handlePageSizeChange}
                                className="mt-4 pt-3 border-top"
                            />
                        )}
                        
                        {/* Enhanced Error State */}
                        {error && (
                            <div className="alert alert-danger mt-3 d-flex justify-content-between align-items-center">
                                <div>
                                    <strong>Error loading application logs</strong>
                                    <div className="small mt-1">
                                        {isNetworkError ? 
                                            'Network connection issue. Please check your internet connection.' :
                                            typeof error === 'string' ? error : 'An unexpected error occurred.'
                                        }
                                    </div>
                                </div>
                                {canRetry && (
                                    <button 
                                        className="btn btn-sm btn-outline-danger"
                                        onClick={retry}
                                        disabled={loading}
                                    >
                                        {loading ? 'Retrying...' : 'Retry'}
                                    </button>
                                )}
                            </div>
                        )}
                    </div>
                </div>

                {/* Statistics Cards */}
                <div className="row g-4 mt-4">
                    <div className="col-md-3">
                        <div className="card border-0 rounded-4 shadow-sm" style={{ transition: 'none', transform: 'none' }}>
                            <div className="card-body p-4 text-center" style={{ transition: 'none', transform: 'none' }}>
                                <Monitor className="text-primary mb-2" size={32} />
                                <h3 className="display-6 fw-bold text-dark">{pagination.totalCount}</h3>
                                <p className="text-muted mb-0">Total Events</p>
                            </div>
                        </div>
                    </div>
                    <div className="col-md-3">
                        <div className="card border-0 rounded-4 shadow-sm" style={{ transition: 'none', transform: 'none' }}>
                            <div className="card-body p-4 text-center" style={{ transition: 'none', transform: 'none' }}>
                                <User className="text-success mb-2" size={32} />
                                <h3 className="display-6 fw-bold text-dark">
                                    {new Set(applicationLogs.map(log => log.applicationId)).size}
                                </h3>
                                <p className="text-muted mb-0">Applications</p>
                            </div>
                        </div>
                    </div>
                    <div className="col-md-3">
                        <div className="card border-0 rounded-4 shadow-sm" style={{ transition: 'none', transform: 'none' }}>
                            <div className="card-body p-4 text-center" style={{ transition: 'none', transform: 'none' }}>
                                <FileText className="text-info mb-2" size={32} />
                                <h3 className="display-6 fw-bold text-dark">
                                    {applicationLogs.filter(log => log.actionType.includes('Create')).length}
                                </h3>
                                <p className="text-muted mb-0">Create Actions</p>
                            </div>
                        </div>
                    </div>
                    <div className="col-md-3">
                        <div className="card border-0 rounded-4 shadow-sm" style={{ transition: 'none', transform: 'none' }}>
                            <div className="card-body p-4 text-center" style={{ transition: 'none', transform: 'none' }}>
                                <Clock className="text-warning mb-2" size={32} />
                                <h3 className="display-6 fw-bold text-dark">
                                    {applicationLogs.length > 0 ? 'Recent' : 'None'}
                                </h3>
                                <p className="text-muted mb-0">Latest Activity</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ApplicationLogs;