import React, { useState, useEffect } from 'react';
import { Activity, ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';
import { activityLogApi } from '../../services/api';

interface UserActivityLog {
    userActivityLogId: string;
    userId: string;
    username: string;
    ipAddress?: string;
    deviceInformation: string;
    actionType: string;
    description: string;
    timestamp: string;
}

interface PaginationData {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
}

const ActivityLogs: React.FC = () => {
    const [userActivityLogs, setUserActivityLogs] = useState<UserActivityLog[]>([]);
    const [loading, setLoading] = useState(false);
    const [pagination, setPagination] = useState<PaginationData>({
        page: 1,
        pageSize: 20,
        totalCount: 0,
        totalPages: 0
    });

    const fetchUserActivityLogs = async (page: number = 1, pageSize: number = 20) => {
        setLoading(true);
        try {
            const response = await activityLogApi.getActivityLogs(page, pageSize);
            if (response && response.data) {
                setUserActivityLogs(response.data);
                setPagination({
                    page: response.page,
                    pageSize: response.pageSize,
                    totalCount: response.totalCount,
                    totalPages: response.totalPages
                });
            } else {
                setUserActivityLogs([]);
            }
        } catch (error) {
            console.error('Failed to fetch user activity logs:', error);
            setUserActivityLogs([]);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchUserActivityLogs();
    }, []);

    const handlePageChange = (newPage: number) => {
        if (newPage >= 1 && newPage <= pagination.totalPages) {
            fetchUserActivityLogs(newPage, pagination.pageSize);
        }
    };

    const handlePageSizeChange = (newPageSize: number) => {
        fetchUserActivityLogs(1, newPageSize);
    };

    return (
        <div className="min-vh-100 bg-light">
            <div className="container-fluid py-4">
                {/* Header */}
                <div className="mb-4">
                    <div className="d-flex align-items-center mb-2">
                        <Activity className="text-primary me-3" size={32} />
                        <h1 className="display-5 fw-bold text-dark mb-0">Activity Logs</h1>
                    </div>
                    <p className="text-muted fs-6">Monitor user interactions and system activities</p>
                </div>

                {/* Activity Logs Table */}
                <div className="card shadow-sm border-0 rounded-4" style={{ transition: 'none', transform: 'none' }}>
                    <div className="card-body p-4" style={{ transition: 'none', transform: 'none' }}>
                        <h2 className="card-title fw-bold text-dark mb-4 d-flex align-items-center">
                            <Activity className="text-primary me-2" size={24} />
                            User Activity Timeline
                        </h2>
                        
                        {loading ? (
                            <div className="d-flex align-items-center text-muted">
                                <div className="spinner-border spinner-border-sm me-2" role="status">
                                    <span className="visually-hidden">Loading...</span>
                                </div>
                                Loading activity logs...
                            </div>
                        ) : (
                            <div className="table-responsive">
                                <table className="table" style={{ cursor: 'default' }}>
                                    <thead className="table-light">
                                        <tr>
                                            <th className="fw-semibold">User</th>
                                            <th className="fw-semibold">Action</th>
                                            <th className="fw-semibold">Description</th>
                                            <th className="fw-semibold">IP Address</th>
                                            <th className="fw-semibold">Timestamp</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {userActivityLogs.map((log, index) => (
                                            <tr key={`${log.userActivityLogId}-${index}`} style={{ transition: 'none', transform: 'none' }}>
                                                <td className="fw-semibold">{log.username}</td>
                                                <td><span className="badge bg-info">{log.actionType}</span></td>
                                                <td className="small text-muted">{log.description}</td>
                                                <td className="small">{log.ipAddress || 'N/A'}</td>
                                                <td className="small">{new Date(log.timestamp).toLocaleString()}</td>
                                            </tr>
                                        ))}
                                        {userActivityLogs.length === 0 && (
                                            <tr>
                                                <td colSpan={5} className="text-center py-5 text-muted">
                                                    <Activity size={48} className="mb-3 opacity-50" />
                                                    <p>No activity logs available</p>
                                                </td>
                                            </tr>
                                        )}
                                    </tbody>
                                </table>
                            </div>
                        )}
                        
                        {/* Pagination Controls */}
                        {!loading && pagination.totalCount > 0 && pagination.totalPages > 1 && (
                            <div className="d-flex justify-content-between align-items-center mt-4">
                                <div className="d-flex align-items-center gap-3">
                                    <span className="text-muted">
                                        Showing {((pagination.page - 1) * pagination.pageSize) + 1} to {Math.min(pagination.page * pagination.pageSize, pagination.totalCount)} of {pagination.totalCount} entries
                                    </span>
                                    <div className="d-flex align-items-center gap-2">
                                        <span className="text-muted small">Entries per page:</span>
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
                                
                                <nav>
                                    <ul className="pagination pagination-sm mb-0">
                                        <li className={`page-item ${pagination.page === 1 ? 'disabled' : ''}`}>
                                            <button 
                                                className="page-link" 
                                                onClick={() => handlePageChange(1)}
                                                disabled={pagination.page === 1}
                                            >
                                                <ChevronsLeft size={16} />
                                            </button>
                                        </li>
                                        <li className={`page-item ${pagination.page === 1 ? 'disabled' : ''}`}>
                                            <button 
                                                className="page-link" 
                                                onClick={() => handlePageChange(pagination.page - 1)}
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
                                                        onClick={() => handlePageChange(pageNum)}
                                                    >
                                                        {pageNum}
                                                    </button>
                                                </li>
                                            );
                                        })}
                                        
                                        <li className={`page-item ${pagination.page === pagination.totalPages ? 'disabled' : ''}`}>
                                            <button 
                                                className="page-link" 
                                                onClick={() => handlePageChange(pagination.page + 1)}
                                                disabled={pagination.page === pagination.totalPages}
                                            >
                                                <ChevronRight size={16} />
                                            </button>
                                        </li>
                                        <li className={`page-item ${pagination.page === pagination.totalPages ? 'disabled' : ''}`}>
                                            <button 
                                                className="page-link" 
                                                onClick={() => handlePageChange(pagination.totalPages)}
                                                disabled={pagination.page === pagination.totalPages}
                                            >
                                                <ChevronsRight size={16} />
                                            </button>
                                        </li>
                                    </ul>
                                </nav>
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ActivityLogs;