import React, { useState, useEffect } from 'react';
import { Shield, ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';
import { auditLogApi } from '../../services/api';

interface AuditLog {
    auditLogId: string;
    userId?: string;
    username: string;
    actionType: string;
    metadata?: string;
    createdAt: string;
}

interface PaginationData {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
}

const AuditLogs: React.FC = () => {
    const [auditLogs, setAuditLogs] = useState<AuditLog[]>([]);
    const [loading, setLoading] = useState(false);
    const [pagination, setPagination] = useState<PaginationData>({
        page: 1,
        pageSize: 20,
        totalCount: 0,
        totalPages: 0
    });

    const fetchAuditLogs = async (page: number = 1, pageSize: number = 20) => {
        setLoading(true);
        try {
            const response = await auditLogApi.getAuditLogs(page, pageSize);
            if (response && response.data?.data) {
                setAuditLogs(response.data.data);
                setPagination({
                    page: response.data.page || 1,
                    pageSize: response.data.pageSize || pageSize,
                    totalCount: response.data.totalCount || 0,
                    totalPages: response.data.totalPages || 1
                });
            } else {
                setAuditLogs([]);
            }
        } catch (error) {
            console.error('Failed to fetch audit logs:', error);
            setAuditLogs([]);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchAuditLogs();
    }, []);

    const handlePageChange = (newPage: number) => {
        if (newPage >= 1 && newPage <= pagination.totalPages) {
            fetchAuditLogs(newPage, pagination.pageSize);
        }
    };

    const handlePageSizeChange = (newPageSize: number) => {
        fetchAuditLogs(1, newPageSize);
    };



    return (
        <div className="min-vh-100 bg-light">
            <div className="container-fluid py-4">
                {/* Header */}
                <div className="mb-4">
                    <div className="d-flex align-items-center mb-2">
                        <Shield className="text-primary me-3" size={32} />
                        <h1 className="display-5 fw-bold text-dark mb-0">Audit Logs</h1>
                    </div>
                    <p className="text-muted fs-6">Track and monitor system security events and user actions</p>
                </div>

                {/* Audit Logs Table */}
                <div className="card shadow-sm border-0 rounded-4" style={{ transition: 'none', transform: 'none' }}>
                    <div className="card-body p-4" style={{ transition: 'none', transform: 'none' }}>
                        <h2 className="card-title fw-bold text-dark mb-4 d-flex align-items-center">
                            <Shield className="text-primary me-2" size={24} />
                            Security Audit Trail
                        </h2>
                        
                        {loading ? (
                            <div className="d-flex align-items-center text-muted">
                                <div className="spinner-border spinner-border-sm me-2" role="status">
                                    <span className="visually-hidden">Loading...</span>
                                </div>
                                Loading audit logs...
                            </div>
                        ) : (
                            <div className="table-responsive">
                                <table className="table" style={{ cursor: 'default' }}>
                                    <thead className="table-light">
                                        <tr>
                                            <th className="fw-semibold">User</th>
                                            <th className="fw-semibold">Action</th>
                                            <th className="fw-semibold">Metadata</th>
                                            <th className="fw-semibold">Date</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {auditLogs.map((log, index) => (
                                            <tr key={`${log.auditLogId}-${index}`} style={{ transition: 'none', transform: 'none' }}>
                                                <td className="fw-semibold">{log.username}</td>
                                                <td><span className="badge bg-primary">{log.actionType}</span></td>
                                                <td className="small text-muted">{log.metadata}</td>
                                                <td className="small">{new Date(log.createdAt).toLocaleString()}</td>
                                            </tr>
                                        ))}
                                        {auditLogs.length === 0 && (
                                            <tr>
                                                <td colSpan={4} className="text-center py-5 text-muted">
                                                    <Shield size={48} className="mb-3 opacity-50" />
                                                    <p>No audit logs available</p>
                                                </td>
                                            </tr>
                                        )}
                                    </tbody>
                                </table>
                            </div>
                        )}
                        
                        {/* Pagination Controls */}
                        {!loading && auditLogs.length > 0 && (
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
            </div>
        </div>
    );
};

export default AuditLogs;