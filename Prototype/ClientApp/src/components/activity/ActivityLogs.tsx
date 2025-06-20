import React, { useState, useEffect } from 'react';
import { Activity } from 'lucide-react';

interface UserActivityLog {
    userActivityLogId: string;
    userId: string;
    actionType: string;
    timestamp: string;
    user: {
        username: string;
    };
}

const ActivityLogs: React.FC = () => {
    const [userActivityLogs, setUserActivityLogs] = useState<UserActivityLog[]>([]);
    const [loading, setLoading] = useState(false);

    const fetchUserActivityLogs = async () => {
        setLoading(true);
        try {
            const response = await fetch('/UserActivitySettings');
            const data = await response.json();
            setUserActivityLogs(data);
        } catch (error) {
            console.error('Failed to fetch user activity logs:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchUserActivityLogs();
    }, []);

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
                <div className="card shadow-sm border-0 rounded-4">
                    <div className="card-body p-4">
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
                                <table className="table table-hover">
                                    <thead className="table-light">
                                        <tr>
                                            <th className="fw-semibold">User</th>
                                            <th className="fw-semibold">Action</th>
                                            <th className="fw-semibold">Timestamp</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {userActivityLogs.map((log) => (
                                            <tr key={log.userActivityLogId}>
                                                <td className="fw-semibold">{log.user.username}</td>
                                                <td><span className="badge bg-info">{log.actionType}</span></td>
                                                <td className="small">{new Date(log.timestamp).toLocaleString()}</td>
                                            </tr>
                                        ))}
                                        {userActivityLogs.length === 0 && (
                                            <tr>
                                                <td colSpan={3} className="text-center py-5 text-muted">
                                                    <Activity size={48} className="mb-3 opacity-50" />
                                                    <p>No activity logs available</p>
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

export default ActivityLogs;