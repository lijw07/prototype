import React, { useState, useEffect } from 'react';
import { Shield } from 'lucide-react';

interface AuditLog {
    auditLogId: string;
    userId: string;
    username: string;
    actionType: string;
    metadata: string;
    createdAt: string;
}

const AuditLogs: React.FC = () => {
    const [auditLogs, setAuditLogs] = useState<AuditLog[]>([]);
    const [loading, setLoading] = useState(false);

    const fetchAuditLogs = async () => {
        setLoading(true);
        try {
            const response = await fetch('/AuditLogSettings');
            const data = await response.json();
            setAuditLogs(data);
        } catch (error) {
            console.error('Failed to fetch audit logs:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchAuditLogs();
    }, []);

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
                <div className="card shadow-sm border-0 rounded-4">
                    <div className="card-body p-4">
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
                                <table className="table table-hover">
                                    <thead className="table-light">
                                        <tr>
                                            <th className="fw-semibold">User</th>
                                            <th className="fw-semibold">Action</th>
                                            <th className="fw-semibold">Metadata</th>
                                            <th className="fw-semibold">Date</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {auditLogs.map((log) => (
                                            <tr key={log.auditLogId}>
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
                    </div>
                </div>
            </div>
        </div>
    );
};

export default AuditLogs;