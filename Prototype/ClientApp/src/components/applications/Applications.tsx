import React, { useState, useEffect } from 'react';
import { Database, Plus, Edit, Trash2, TestTube, Eye, EyeOff } from 'lucide-react';
import { applicationApi } from '../../services/api';

interface Application {
    applicationId: string;
    applicationName: string;
    applicationDescription: string;
    connection: {
        host: string;
        port: string;
        databaseName: string;
        authenticationType: string;
    };
}

const Applications: React.FC = () => {
    const [applications, setApplications] = useState<Application[]>([]);
    const [loading, setLoading] = useState(false);
    const [showApplicationForm, setShowApplicationForm] = useState(false);
    const [editingApp, setEditingApp] = useState<Application | null>(null);

    // Application form state
    const [applicationForm, setApplicationForm] = useState({
        applicationName: '',
        applicationDescription: '',
        dataSourceType: 'MicrosoftSqlServer',
        connectionSource: {
            host: '',
            port: '1433',
            databaseName: '',
            authenticationType: 'UserPassword',
            username: '',
            password: ''
        }
    });

    const [showPasswords, setShowPasswords] = useState({
        connection: false
    });

    const fetchApplications = async () => {
        setLoading(true);
        try {
            const response = await applicationApi.getApplications();
            if (response.success && response.data?.data) {
                setApplications(response.data.data);
            }
        } catch (error) {
            console.error('Failed to fetch applications:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchApplications();
    }, []);

    const handleApplicationSubmit = async () => {
        try {
            let response;
            if (editingApp) {
                response = await applicationApi.updateApplication(editingApp.applicationId, applicationForm);
            } else {
                response = await applicationApi.createApplication(applicationForm);
            }
            
            if (response.success) {
                alert(editingApp ? 'Application updated successfully!' : 'Application created successfully!');
                setShowApplicationForm(false);
                setEditingApp(null);
                fetchApplications();
            } else {
                alert(response.message || 'Failed to save application');
            }
        } catch (error: any) {
            console.error('Failed to save application:', error);
            alert(error.message || 'Failed to save application');
        }
    };

    const testConnection = async (applicationId?: string) => {
        try {
            const response = await applicationApi.testConnection(
                applicationId ? { applicationId } : applicationForm
            );
            
            if (response.success) {
                alert('Connection test successful!');
            } else {
                alert(response.message || 'Connection test failed');
            }
        } catch (error: any) {
            console.error('Connection test failed:', error);
            alert(error.message || 'Connection test failed');
        }
    };

    const deleteApplication = async (applicationId: string) => {
        if (!window.confirm('Are you sure you want to delete this application?')) return;

        try {
            const response = await applicationApi.deleteApplication(applicationId);
            
            if (response.success) {
                alert('Application deleted successfully!');
                fetchApplications();
            } else {
                alert(response.message || 'Failed to delete application');
            }
        } catch (error: any) {
            console.error('Failed to delete application:', error);
            alert(error.message || 'Failed to delete application');
        }
    };

    const togglePasswordVisibility = (field: keyof typeof showPasswords) => {
        setShowPasswords(prev => ({ ...prev, [field]: !prev[field] }));
    };

    return (
        <div className="min-vh-100 bg-light">
            <div className="container-fluid py-4">
                {/* Header */}
                <div className="mb-4">
                    <div className="d-flex align-items-center mb-2">
                        <Database className="text-primary me-3" size={32} />
                        <h1 className="display-5 fw-bold text-dark mb-0">Applications</h1>
                    </div>
                    <p className="text-muted fs-6">Manage your database applications and connections</p>
                </div>

                {/* Applications */}
                <div className="card shadow-sm border-0 rounded-4">
                    <div className="card-body p-4">
                        <div className="d-flex justify-content-between align-items-center mb-4">
                            <h2 className="card-title fw-bold text-dark mb-0 d-flex align-items-center">
                                <Database className="text-primary me-2" size={24} />
                                Database Applications
                            </h2>
                            <button
                                onClick={() => setShowApplicationForm(true)}
                                className="btn btn-primary rounded-3 fw-semibold d-flex align-items-center"
                            >
                                <Plus className="me-2" size={18} />
                                <span>Add Application</span>
                            </button>
                        </div>

                        <div className="row g-3">
                            {applications.map((app) => (
                                <div key={app.applicationId} className="col-12">
                                    <div className="card border border-light rounded-3 h-100">
                                        <div className="card-body p-3">
                                            <div className="d-flex justify-content-between align-items-start">
                                                <div className="flex-grow-1">
                                                    <h5 className="card-title fw-bold text-dark mb-1">{app.applicationName}</h5>
                                                    <p className="card-text text-muted small mb-2">{app.applicationDescription}</p>
                                                    <div className="badge bg-light text-dark border">
                                                        {app.connection.host}:{app.connection.port}/{app.connection.databaseName}
                                                    </div>
                                                </div>
                                                <div className="d-flex gap-1 ms-3">
                                                    <button
                                                        onClick={() => testConnection(app.applicationId)}
                                                        className="btn btn-outline-primary btn-sm rounded-3"
                                                        title="Test Connection"
                                                    >
                                                        <TestTube size={16} />
                                                    </button>
                                                    <button
                                                        onClick={() => {
                                                            setEditingApp(app);
                                                            setShowApplicationForm(true);
                                                        }}
                                                        className="btn btn-outline-secondary btn-sm rounded-3"
                                                        title="Edit"
                                                    >
                                                        <Edit size={16} />
                                                    </button>
                                                    <button
                                                        onClick={() => deleteApplication(app.applicationId)}
                                                        className="btn btn-outline-danger btn-sm rounded-3"
                                                        title="Delete"
                                                    >
                                                        <Trash2 size={16} />
                                                    </button>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            ))}
                            {applications.length === 0 && (
                                <div className="col-12">
                                    <div className="text-center py-5 text-muted">
                                        <Database size={48} className="mb-3 opacity-50" />
                                        <p>No applications configured yet</p>
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>
                </div>

                {showApplicationForm && (
                    <div className="modal d-block" style={{backgroundColor: 'rgba(0,0,0,0.5)'}}>
                        <div className="modal-dialog modal-dialog-centered modal-lg">
                            <div className="modal-content border-0 rounded-4">
                                <div className="modal-header border-0 pb-0">
                                    <h3 className="modal-title fw-bold">
                                        {editingApp ? 'Edit Application' : 'Add New Application'}
                                    </h3>
                                    <button
                                        type="button"
                                        className="btn-close"
                                        onClick={() => {
                                            setShowApplicationForm(false);
                                            setEditingApp(null);
                                        }}
                                    ></button>
                                </div>
                                <div className="modal-body">
                                    <div className="row g-3">
                                        <div className="col-12">
                                            <label className="form-label fw-semibold">Application Name</label>
                                            <input
                                                type="text"
                                                value={applicationForm.applicationName}
                                                onChange={(e) => setApplicationForm({...applicationForm, applicationName: e.target.value})}
                                                className="form-control rounded-3"
                                                placeholder="Enter application name"
                                            />
                                        </div>
                                        <div className="col-12">
                                            <label className="form-label fw-semibold">Description</label>
                                            <textarea
                                                value={applicationForm.applicationDescription}
                                                onChange={(e) => setApplicationForm({...applicationForm, applicationDescription: e.target.value})}
                                                className="form-control rounded-3"
                                                rows={3}
                                                placeholder="Enter application description"
                                            />
                                        </div>
                                        <div className="col-md-8">
                                            <label className="form-label fw-semibold">Host</label>
                                            <input
                                                type="text"
                                                value={applicationForm.connectionSource.host}
                                                onChange={(e) => setApplicationForm({
                                                    ...applicationForm,
                                                    connectionSource: {...applicationForm.connectionSource, host: e.target.value}
                                                })}
                                                className="form-control rounded-3"
                                                placeholder="localhost or server address"
                                            />
                                        </div>
                                        <div className="col-md-4">
                                            <label className="form-label fw-semibold">Port</label>
                                            <input
                                                type="text"
                                                value={applicationForm.connectionSource.port}
                                                onChange={(e) => setApplicationForm({
                                                    ...applicationForm,
                                                    connectionSource: {...applicationForm.connectionSource, port: e.target.value}
                                                })}
                                                className="form-control rounded-3"
                                                placeholder="1433"
                                            />
                                        </div>
                                        <div className="col-12">
                                            <label className="form-label fw-semibold">Database Name</label>
                                            <input
                                                type="text"
                                                value={applicationForm.connectionSource.databaseName}
                                                onChange={(e) => setApplicationForm({
                                                    ...applicationForm,
                                                    connectionSource: {...applicationForm.connectionSource, databaseName: e.target.value}
                                                })}
                                                className="form-control rounded-3"
                                                placeholder="Enter database name"
                                            />
                                        </div>
                                        <div className="col-12">
                                            <label className="form-label fw-semibold">Username</label>
                                            <input
                                                type="text"
                                                value={applicationForm.connectionSource.username}
                                                onChange={(e) => setApplicationForm({
                                                    ...applicationForm,
                                                    connectionSource: {...applicationForm.connectionSource, username: e.target.value}
                                                })}
                                                className="form-control rounded-3"
                                                placeholder="Database username"
                                            />
                                        </div>
                                        <div className="col-12">
                                            <label className="form-label fw-semibold">Password</label>
                                            <div className="position-relative">
                                                <input
                                                    type={showPasswords.connection ? "text" : "password"}
                                                    value={applicationForm.connectionSource.password}
                                                    onChange={(e) => setApplicationForm({
                                                        ...applicationForm,
                                                        connectionSource: {...applicationForm.connectionSource, password: e.target.value}
                                                    })}
                                                    className="form-control rounded-3 pe-5"
                                                    placeholder="Database password"
                                                />
                                                <button
                                                    type="button"
                                                    onClick={() => togglePasswordVisibility('connection')}
                                                    className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                                                >
                                                    {showPasswords.connection ? <EyeOff size={16} /> : <Eye size={16} />}
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                                <div className="modal-footer border-0 pt-0">
                                    <div className="d-flex gap-2">
                                        <button
                                            onClick={() => testConnection()}
                                            className="btn btn-warning rounded-3 fw-semibold d-flex align-items-center"
                                        >
                                            <TestTube className="me-2" size={16} />
                                            Test Connection
                                        </button>
                                        <button
                                            onClick={handleApplicationSubmit}
                                            className="btn btn-success rounded-3 fw-semibold"
                                        >
                                            {editingApp ? 'Update Application' : 'Create Application'}
                                        </button>
                                        <button
                                            onClick={() => {
                                                setShowApplicationForm(false);
                                                setEditingApp(null);
                                            }}
                                            className="btn btn-secondary rounded-3 fw-semibold"
                                        >
                                            Cancel
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default Applications;