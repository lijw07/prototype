import React, { useState, useEffect } from 'react';
import { FileText, Clock, User, Monitor } from 'lucide-react';

interface ApplicationLog {
    applicationLogId: string;
    userId: string;
    applicationId: string;
    actionType: string;
    metadata: string;
    timestamp: string;
    user: {
        username: string;
    };
    application: {
        applicationName: string;
    };
}

const ApplicationLogs: React.FC = () => {
    const [applicationLogs, setApplicationLogs] = useState<ApplicationLog[]>([]);
    const [loading, setLoading] = useState(false);

    const fetchApplicationLogs = async () => {
        setLoading(true);
        try {
            const response = await fetch('/ApplicationLogSettings');
            const data = await response.json();
            if (data.success && data.data) {
                setApplicationLogs(data.data);
            } else {
                // Mock data for demonstration
                setApplicationLogs([
                    {
                        applicationLogId: '1',
                        userId: 'user1',
                        applicationId: 'app1',
                        actionType: 'Navigation',
                        metadata: 'Navigated to Dashboard',
                        timestamp: new Date().toISOString(),
                        user: { username: 'john.doe' },
                        application: { applicationName: 'CAMS Admin' }
                    },
                    {
                        applicationLogId: '2',
                        userId: 'user1',
                        applicationId: 'app1',
                        actionType: 'Page View',
                        metadata: 'Viewed Applications page',
                        timestamp: new Date(Date.now() - 300000).toISOString(),
                        user: { username: 'john.doe' },
                        application: { applicationName: 'CAMS Admin' }
                    },
                    {
                        applicationLogId: '3',
                        userId: 'user2',
                        applicationId: 'app2',
                        actionType: 'Navigation',
                        metadata: 'Navigated to Settings',
                        timestamp: new Date(Date.now() - 600000).toISOString(),
                        user: { username: 'jane.smith' },
                        application: { applicationName: 'Production DB' }
                    }
                ]);
            }
        } catch (error) {
            console.error('Failed to fetch application logs:', error);
            // Fallback to mock data
            setApplicationLogs([]);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchApplicationLogs();
    }, []);

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
                <div className="card shadow-sm border-0 rounded-4">
                    <div className="card-body p-4">
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
                                <table className="table table-hover">
                                    <thead className="table-light">
                                        <tr>
                                            <th className="fw-semibold">
                                                <User size={16} className="me-1" />
                                                User
                                            </th>
                                            <th className="fw-semibold">Application</th>
                                            <th className="fw-semibold">Action</th>
                                            <th className="fw-semibold">Details</th>
                                            <th className="fw-semibold">
                                                <Clock size={16} className="me-1" />
                                                Timestamp
                                            </th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {applicationLogs.map((log) => (
                                            <tr key={log.applicationLogId}>
                                                <td className="fw-semibold">{log.user.username}</td>
                                                <td>
                                                    <span className="badge bg-light text-dark border">
                                                        {log.application.applicationName}
                                                    </span>
                                                </td>
                                                <td>
                                                    <span className={`badge ${getActionBadgeColor(log.actionType)}`}>
                                                        {log.actionType}
                                                    </span>
                                                </td>
                                                <td className="small text-muted">{log.metadata}</td>
                                                <td className="small">{new Date(log.timestamp).toLocaleString()}</td>
                                            </tr>
                                        ))}
                                        {applicationLogs.length === 0 && (
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
                    </div>
                </div>

                {/* Statistics Cards */}
                <div className="row g-4 mt-4">
                    <div className="col-md-3">
                        <div className="card border-0 rounded-4 shadow-sm">
                            <div className="card-body p-4 text-center">
                                <Monitor className="text-primary mb-2" size={32} />
                                <h3 className="display-6 fw-bold text-dark">{applicationLogs.length}</h3>
                                <p className="text-muted mb-0">Total Events</p>
                            </div>
                        </div>
                    </div>
                    <div className="col-md-3">
                        <div className="card border-0 rounded-4 shadow-sm">
                            <div className="card-body p-4 text-center">
                                <User className="text-success mb-2" size={32} />
                                <h3 className="display-6 fw-bold text-dark">
                                    {new Set(applicationLogs.map(log => log.userId)).size}
                                </h3>
                                <p className="text-muted mb-0">Active Users</p>
                            </div>
                        </div>
                    </div>
                    <div className="col-md-3">
                        <div className="card border-0 rounded-4 shadow-sm">
                            <div className="card-body p-4 text-center">
                                <FileText className="text-info mb-2" size={32} />
                                <h3 className="display-6 fw-bold text-dark">
                                    {applicationLogs.filter(log => log.actionType === 'Navigation').length}
                                </h3>
                                <p className="text-muted mb-0">Navigations</p>
                            </div>
                        </div>
                    </div>
                    <div className="col-md-3">
                        <div className="card border-0 rounded-4 shadow-sm">
                            <div className="card-body p-4 text-center">
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