import React, { useState, useEffect } from 'react';
import { FileText, Clock, User, Monitor, ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';
import { applicationLogApi } from '../../services/api';

interface ApplicationLog {
    applicationLogId: string;
    applicationId: string;
    applicationName: string;
    actionType: string;
    metadata: string;
    createdAt: string;
    updatedAt: string;
}

interface PaginationData {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
}

const ApplicationLogs: React.FC = () => {
    const [applicationLogs, setApplicationLogs] = useState<ApplicationLog[]>([]);
    const [loading, setLoading] = useState(false);
    const [pagination, setPagination] = useState<PaginationData>({
        page: 1,
        pageSize: 20,
        totalCount: 0,
        totalPages: 0
    });

    const fetchApplicationLogs = async (page: number = 1, pageSize: number = 20) => {
        setLoading(true);
        try {
            const response = await applicationLogApi.getApplicationLogs(page, pageSize);
            if (response && response.success && response.data && response.data.data) {
                setApplicationLogs(response.data.data);
                setPagination({
                    page: response.data.page,
                    pageSize: response.data.pageSize,
                    totalCount: response.data.totalCount,
                    totalPages: response.data.totalPages
                });
            } else {
                setApplicationLogs([]);
                setPagination({
                    page: 1,
                    pageSize: 20,
                    totalCount: 0,
                    totalPages: 0
                });
            }
        } catch (error) {
            console.error('Failed to fetch application logs:', error);
            setApplicationLogs([]);
            setPagination({
                page: 1,
                pageSize: 20,
                totalCount: 0,
                totalPages: 0
            });
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchApplicationLogs();
    }, []);

    const handlePageChange = (newPage: number) => {
        if (newPage >= 1 && newPage <= pagination.totalPages) {
            fetchApplicationLogs(newPage, pagination.pageSize);
        }
    };

    const handlePageSizeChange = (newPageSize: number) => {
        fetchApplicationLogs(1, newPageSize);
    };

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
                            <div className="d-flex align-items-center text-muted">
                                <div className="spinner-border spinner-border-sm me-2" role="status">
                                    <span className="visually-hidden">Loading...</span>
                                </div>
                                Loading application logs...
                            </div>
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
                                                <td className="small">{new Date(log.createdAt).toLocaleString()}</td>
                                                <td className="small">{new Date(log.updatedAt).toLocaleString()}</td>
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
                        {!loading && (Array.isArray(applicationLogs) ? applicationLogs : []).length > 0 && (
                            <div className="d-flex justify-content-between align-items-center mt-4 pt-3 border-top">
                                <div className="d-flex align-items-center gap-3">
                                    <span className="text-muted small">
                                        Showing {((pagination.page - 1) * pagination.pageSize) + 1} to {Math.min(pagination.page * pagination.pageSize, pagination.totalCount)} of {pagination.totalCount} entries
                                    </span>
                                    <div className="d-flex align-items-center gap-2">
                                        <label className="text-muted small mb-0">Per page:</label>
                                        <select 
                                            className="form-select form-select-sm" 
                                            style={{width: 'auto'}}
                                            value={pagination.pageSize}
                                            onChange={(e) => handlePageSizeChange(Number(e.target.value))}
                                        >
                                            <option value={10}>10</option>
                                            <option value={20}>20</option>
                                            <option value={50}>50</option>
                                            <option value={100}>100</option>
                                        </select>
                                    </div>
                                </div>
                                
                                <div className="d-flex gap-1">
                                    <button
                                        className="btn btn-outline-secondary btn-sm"
                                        onClick={() => handlePageChange(1)}
                                        disabled={pagination.page === 1}
                                    >
                                        <ChevronsLeft size={16} />
                                    </button>
                                    <button
                                        className="btn btn-outline-secondary btn-sm"
                                        onClick={() => handlePageChange(pagination.page - 1)}
                                        disabled={pagination.page === 1}
                                    >
                                        <ChevronLeft size={16} />
                                    </button>
                                    
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
                                            <button
                                                key={pageNum}
                                                className={`btn btn-sm ${pageNum === pagination.page ? 'btn-primary' : 'btn-outline-secondary'}`}
                                                onClick={() => handlePageChange(pageNum)}
                                            >
                                                {pageNum}
                                            </button>
                                        );
                                    })}
                                    
                                    <button
                                        className="btn btn-outline-secondary btn-sm"
                                        onClick={() => handlePageChange(pagination.page + 1)}
                                        disabled={pagination.page === pagination.totalPages}
                                    >
                                        <ChevronRight size={16} />
                                    </button>
                                    <button
                                        className="btn btn-outline-secondary btn-sm"
                                        onClick={() => handlePageChange(pagination.totalPages)}
                                        disabled={pagination.page === pagination.totalPages}
                                    >
                                        <ChevronsRight size={16} />
                                    </button>
                                </div>
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
                                    {new Set((Array.isArray(applicationLogs) ? applicationLogs : []).map(log => log.applicationId)).size}
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
                                    {(Array.isArray(applicationLogs) ? applicationLogs : []).filter(log => log.actionType.includes('Create')).length}
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
                                    {(Array.isArray(applicationLogs) ? applicationLogs : []).length > 0 ? 'Recent' : 'None'}
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