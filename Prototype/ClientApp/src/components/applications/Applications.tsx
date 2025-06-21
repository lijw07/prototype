import React, { useState, useEffect } from 'react';
import { Database, Plus, Edit, Trash2, TestTube, Eye, EyeOff, ChevronLeft, ChevronRight } from 'lucide-react';
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
    
    // Pagination state
    const [currentPage, setCurrentPage] = useState(1);
    const [pageSize, setPageSize] = useState(10);
    const [totalCount, setTotalCount] = useState(0);
    const [totalPages, setTotalPages] = useState(0);

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
            password: '',
            // AWS IAM fields
            awsAccessKeyId: '',
            awsSecretAccessKey: '',
            awsSessionToken: '',
            // Kerberos/GSSAPI fields
            principal: '',
            serviceName: '',
            serviceRealm: '',
            canonicalizeHostName: false,
            // MongoDB specific fields
            authenticationDatabase: '',
            // X.509 certificate fields
            certificateFilePath: '',
            privateKeyFilePath: '',
            caCertificateFilePath: ''
        }
    });

    const [showPasswords, setShowPasswords] = useState({
        connection: false,
        awsSecret: false,
        awsSession: false
    });

    const fetchApplications = async (page: number = currentPage, size: number = pageSize) => {
        setLoading(true);
        try {
            const response = await applicationApi.getApplications(page, size);
            if (response.success && response.data?.data) {
                setApplications(response.data.data);
                setCurrentPage(response.data.page || page);
                setPageSize(response.data.pageSize || size);
                setTotalCount(response.data.totalCount || 0);
                setTotalPages(response.data.totalPages || 1);
            }
        } catch (error) {
            console.error('Failed to fetch applications:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchApplications(1, pageSize);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    // Effect to populate form when editing an application
    useEffect(() => {
        if (editingApp) {
            setApplicationForm({
                applicationName: editingApp.applicationName,
                applicationDescription: editingApp.applicationDescription,
                dataSourceType: 'MicrosoftSqlServer',
                connectionSource: {
                    host: editingApp.connection.host,
                    port: editingApp.connection.port,
                    databaseName: editingApp.connection.databaseName,
                    authenticationType: editingApp.connection.authenticationType,
                    username: '', // We don't get username back from API for security
                    password: '',  // We don't get password back from API for security
                    // AWS IAM fields
                    awsAccessKeyId: '',
                    awsSecretAccessKey: '',
                    awsSessionToken: '',
                    // Kerberos/GSSAPI fields
                    principal: '',
                    serviceName: '',
                    serviceRealm: '',
                    canonicalizeHostName: false,
                    // MongoDB specific fields
                    authenticationDatabase: '',
                    // X.509 certificate fields
                    certificateFilePath: '',
                    privateKeyFilePath: '',
                    caCertificateFilePath: ''
                }
            });
        } else {
            // Reset form when not editing
            setApplicationForm({
                applicationName: '',
                applicationDescription: '',
                dataSourceType: 'MicrosoftSqlServer',
                connectionSource: {
                    host: '',
                    port: '1433', // Default for SQL Server
                    databaseName: '',
                    authenticationType: 'UserPassword',
                    username: '',
                    password: '',
                    // AWS IAM fields
                    awsAccessKeyId: '',
                    awsSecretAccessKey: '',
                    awsSessionToken: '',
                    // Kerberos/GSSAPI fields
                    principal: '',
                    serviceName: '',
                    serviceRealm: '',
                    canonicalizeHostName: false,
                    // MongoDB specific fields
                    authenticationDatabase: '',
                    // X.509 certificate fields
                    certificateFilePath: '',
                    privateKeyFilePath: '',
                    caCertificateFilePath: ''
                }
            });
        }
    }, [editingApp]);

    const generateConnectionUrl = (connectionSource: any) => {
        const { host, port, databaseName, authenticationType, username, password } = connectionSource;
        
        // Handle different data source types
        switch (applicationForm.dataSourceType) {
            case 'MicrosoftSqlServer':
                const sqlServerNeedsCredentials = ['UserPassword', 'Kerberos', 'AzureAdPassword'].includes(authenticationType);
                return `Server=${host},${port};Database=${databaseName};${sqlServerNeedsCredentials ? `User Id=${username};Password=${password};` : 'Integrated Security=true;'}TrustServerCertificate=true;`;
                
            case 'MySql':
                const mysqlNeedsCredentials = authenticationType === 'UserPassword';
                return `Server=${host};Port=${port};Database=${databaseName};${mysqlNeedsCredentials ? `Uid=${username};Pwd=${password};` : ''}`;
                
            case 'MongoDb':
                const mongoNeedsCredentials = ['UserPassword', 'ScramSha1', 'ScramSha256', 'PlainLdap'].includes(authenticationType);
                if (mongoNeedsCredentials) {
                    return `mongodb://${username}:${password}@${host}:${port}/${databaseName}`;
                } else {
                    return `mongodb://${host}:${port}/${databaseName}`;
                }
                
            default:
                return `Server=${host},${port};Database=${databaseName};Integrated Security=true;TrustServerCertificate=true;`;
        }
    };

    const handleApplicationSubmit = async () => {
        try {
            // Generate connection URL for submission
            const formDataWithUrl = {
                ...applicationForm,
                connectionSource: {
                    ...applicationForm.connectionSource,
                    url: generateConnectionUrl(applicationForm.connectionSource)
                }
            };

            console.log('Submitting application data:', JSON.stringify(formDataWithUrl, null, 2));

            let response;
            if (editingApp) {
                response = await applicationApi.updateApplication(editingApp.applicationId, formDataWithUrl);
            } else {
                response = await applicationApi.createApplication(formDataWithUrl);
            }
            
            if (response.success) {
                alert(editingApp ? 'Application updated successfully!' : 'Application created successfully!');
                setShowApplicationForm(false);
                setEditingApp(null);
                fetchApplications(currentPage, pageSize);
            } else {
                console.error('Server response:', response);
                console.error('Response errors:', response.errors);
                alert(`${response.message || 'Failed to save application'}${response.errors ? '\nErrors: ' + Object.keys(response.errors).join(', ') : ''}`);
            }
        } catch (error: any) {
            console.error('Failed to save application:', error);
            console.error('Full error details:', JSON.stringify(error, null, 2));
            alert(error.message || 'Failed to save application');
        }
    };

    const testConnection = async (application?: any) => {
        try {
            let connectionData;
            if (application && application.applicationId) {
                // For existing applications, just send the applicationId
                connectionData = { applicationId: application.applicationId };
            } else {
                // For new applications, send full connection data
                connectionData = {
                    ...applicationForm,
                    connectionSource: {
                        ...applicationForm.connectionSource,
                        url: generateConnectionUrl(applicationForm.connectionSource)
                    }
                };
            }
            
            const response = await applicationApi.testConnection(connectionData);
            
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
                fetchApplications(currentPage, pageSize);
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
                                                        onClick={() => testConnection(app)}
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
                            {applications.length === 0 && !loading && (
                                <div className="col-12">
                                    <div className="text-center py-5 text-muted">
                                        <Database size={48} className="mb-3 opacity-50" />
                                        <p>No applications configured yet</p>
                                    </div>
                                </div>
                            )}
                            {loading && (
                                <div className="col-12">
                                    <div className="text-center py-5 text-muted">
                                        <div className="spinner-border" role="status">
                                            <span className="visually-hidden">Loading...</span>
                                        </div>
                                        <p className="mt-2">Loading applications...</p>
                                    </div>
                                </div>
                            )}
                        </div>

                        {/* Pagination Controls */}
                        {(totalPages > 1 || totalCount > 0) && (
                            <div className="d-flex justify-content-between align-items-center mt-4">
                                <div className="d-flex align-items-center gap-3">
                                    <div className="text-muted small">
                                        Showing {applications.length} of {totalCount} applications
                                    </div>
                                    <div className="d-flex align-items-center gap-2">
                                        <label className="text-muted small mb-0">Show:</label>
                                        <select
                                            value={pageSize}
                                            onChange={(e) => {
                                                const newPageSize = parseInt(e.target.value);
                                                setPageSize(newPageSize);
                                                fetchApplications(1, newPageSize);
                                            }}
                                            className="form-select form-select-sm"
                                            style={{ width: 'auto' }}
                                        >
                                            <option value={5}>5</option>
                                            <option value={10}>10</option>
                                            <option value={20}>20</option>
                                            <option value={50}>50</option>
                                        </select>
                                    </div>
                                </div>
                                <div className="d-flex align-items-center gap-2">
                                    <button
                                        onClick={() => fetchApplications(currentPage - 1)}
                                        disabled={currentPage <= 1}
                                        className="btn btn-outline-secondary btn-sm d-flex align-items-center"
                                    >
                                        <ChevronLeft size={16} className="me-1" />
                                        Previous
                                    </button>
                                    
                                    <div className="d-flex align-items-center gap-1">
                                        {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                                            let pageNumber: number;
                                            if (totalPages <= 5) {
                                                pageNumber = i + 1;
                                            } else if (currentPage <= 3) {
                                                pageNumber = i + 1;
                                            } else if (currentPage >= totalPages - 2) {
                                                pageNumber = totalPages - 4 + i;
                                            } else {
                                                pageNumber = currentPage - 2 + i;
                                            }
                                            
                                            return (
                                                <button
                                                    key={pageNumber}
                                                    onClick={() => fetchApplications(pageNumber)}
                                                    className={`btn btn-sm ${currentPage === pageNumber ? 'btn-primary' : 'btn-outline-secondary'}`}
                                                >
                                                    {pageNumber}
                                                </button>
                                            );
                                        })}
                                    </div>
                                    
                                    <button
                                        onClick={() => fetchApplications(currentPage + 1)}
                                        disabled={currentPage >= totalPages}
                                        className="btn btn-outline-secondary btn-sm d-flex align-items-center"
                                    >
                                        Next
                                        <ChevronRight size={16} className="ms-1" />
                                    </button>
                                </div>
                            </div>
                        )}
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
                                        <div className="col-md-6">
                                            <label className="form-label fw-semibold">Data Source Type</label>
                                            <select
                                                value={applicationForm.dataSourceType}
                                                onChange={(e) => {
                                                    // Reset authentication type and port when data source changes
                                                    const newAuthType = 'UserPassword';
                                                    const defaultPort = e.target.value === 'MicrosoftSqlServer' ? '1433' 
                                                                       : e.target.value === 'MySql' ? '3306'
                                                                       : e.target.value === 'MongoDb' ? '27017'
                                                                       : '1433';
                                                    setApplicationForm({
                                                        ...applicationForm, 
                                                        dataSourceType: e.target.value,
                                                        connectionSource: {
                                                            ...applicationForm.connectionSource,
                                                            authenticationType: newAuthType,
                                                            port: defaultPort
                                                        }
                                                    });
                                                }}
                                                className="form-select rounded-3"
                                            >
                                                <option value="MicrosoftSqlServer">Microsoft SQL Server</option>
                                                <option value="MySql">MySQL</option>
                                                <option value="MongoDb">MongoDB</option>
                                            </select>
                                        </div>
                                        <div className="col-md-6">
                                            <label className="form-label fw-semibold">Authentication Type</label>
                                            <select
                                                value={applicationForm.connectionSource.authenticationType}
                                                onChange={(e) => setApplicationForm({
                                                    ...applicationForm,
                                                    connectionSource: {...applicationForm.connectionSource, authenticationType: e.target.value}
                                                })}
                                                className="form-select rounded-3"
                                            >
                                                {/* Microsoft SQL Server options */}
                                                {applicationForm.dataSourceType === 'MicrosoftSqlServer' && (
                                                    <>
                                                        <option value="UserPassword">Username & Password</option>
                                                        <option value="Kerberos">Kerberos</option>
                                                        <option value="AzureAdPassword">Azure AD with Password</option>
                                                        <option value="AzureAdInteractive">Azure AD Interactive</option>
                                                        <option value="AzureAdIntegrated">Azure AD Integrated</option>
                                                        <option value="AzureAdDefault">Azure AD Default</option>
                                                        <option value="AzureAdMsi">Azure AD MSI</option>
                                                        <option value="NoAuth">No Authentication</option>
                                                    </>
                                                )}
                                                
                                                {/* MySQL options */}
                                                {applicationForm.dataSourceType === 'MySql' && (
                                                    <>
                                                        <option value="UserPassword">Username & Password</option>
                                                        <option value="NoAuth">No Authentication</option>
                                                    </>
                                                )}
                                                
                                                {/* MongoDB options */}
                                                {applicationForm.dataSourceType === 'MongoDb' && (
                                                    <>
                                                        <option value="UserPassword">Username & Password</option>
                                                        <option value="ScramSha1">SCRAM-SHA-1</option>
                                                        <option value="ScramSha256">SCRAM-SHA-256</option>
                                                        <option value="AwsIam">AWS IAM</option>
                                                        <option value="X509">X.509</option>
                                                        <option value="GssapiKerberos">GSSAPI (Kerberos)</option>
                                                        <option value="PlainLdap">Plain LDAP</option>
                                                        <option value="NoAuth">No Authentication</option>
                                                    </>
                                                )}
                                            </select>
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
                                        {/* Username field - show for most authentication types */}
                                        {(
                                            // SQL Server types that need username
                                            (applicationForm.dataSourceType === 'MicrosoftSqlServer' && 
                                             ['UserPassword', 'Kerberos', 'AzureAdPassword', 'AzureAdInteractive'].includes(applicationForm.connectionSource.authenticationType)) ||
                                            // MySQL types that need username  
                                            (applicationForm.dataSourceType === 'MySql' && 
                                             applicationForm.connectionSource.authenticationType === 'UserPassword') ||
                                            // MongoDB types that need username
                                            (applicationForm.dataSourceType === 'MongoDb' && 
                                             ['UserPassword', 'ScramSha1', 'ScramSha256', 'PlainLdap'].includes(applicationForm.connectionSource.authenticationType))
                                        ) && (
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
                                        )}

                                        {/* Password field - show for authentication types that need password */}
                                        {(
                                            // SQL Server types that need password
                                            (applicationForm.dataSourceType === 'MicrosoftSqlServer' && 
                                             ['UserPassword', 'Kerberos', 'AzureAdPassword'].includes(applicationForm.connectionSource.authenticationType)) ||
                                            // MySQL types that need password
                                            (applicationForm.dataSourceType === 'MySql' && 
                                             applicationForm.connectionSource.authenticationType === 'UserPassword') ||
                                            // MongoDB types that need password
                                            (applicationForm.dataSourceType === 'MongoDb' && 
                                             ['UserPassword', 'ScramSha1', 'ScramSha256', 'PlainLdap'].includes(applicationForm.connectionSource.authenticationType))
                                        ) && (
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
                                        )}

                                        {/* AWS IAM fields */}
                                        {applicationForm.connectionSource.authenticationType === 'AwsIam' && (
                                            <>
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">AWS Access Key ID</label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.awsAccessKeyId || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, awsAccessKeyId: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="AWS Access Key ID"
                                                    />
                                                </div>
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">AWS Secret Access Key</label>
                                                    <div className="position-relative">
                                                        <input
                                                            type={showPasswords.awsSecret ? "text" : "password"}
                                                            value={applicationForm.connectionSource.awsSecretAccessKey || ''}
                                                            onChange={(e) => setApplicationForm({
                                                                ...applicationForm,
                                                                connectionSource: {...applicationForm.connectionSource, awsSecretAccessKey: e.target.value}
                                                            })}
                                                            className="form-control rounded-3 pe-5"
                                                            placeholder="AWS Secret Access Key"
                                                        />
                                                        <button
                                                            type="button"
                                                            onClick={() => setShowPasswords(prev => ({ ...prev, awsSecret: !prev.awsSecret }))}
                                                            className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                                                        >
                                                            {showPasswords.awsSecret ? <EyeOff size={16} /> : <Eye size={16} />}
                                                        </button>
                                                    </div>
                                                </div>
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">AWS Session Token (Optional)</label>
                                                    <div className="position-relative">
                                                        <input
                                                            type={showPasswords.awsSession ? "text" : "password"}
                                                            value={applicationForm.connectionSource.awsSessionToken || ''}
                                                            onChange={(e) => setApplicationForm({
                                                                ...applicationForm,
                                                                connectionSource: {...applicationForm.connectionSource, awsSessionToken: e.target.value}
                                                            })}
                                                            className="form-control rounded-3 pe-5"
                                                            placeholder="AWS Session Token (for temporary credentials)"
                                                        />
                                                        <button
                                                            type="button"
                                                            onClick={() => setShowPasswords(prev => ({ ...prev, awsSession: !prev.awsSession }))}
                                                            className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                                                        >
                                                            {showPasswords.awsSession ? <EyeOff size={16} /> : <Eye size={16} />}
                                                        </button>
                                                    </div>
                                                </div>
                                            </>
                                        )}

                                        {/* Kerberos/GSSAPI fields */}
                                        {(applicationForm.connectionSource.authenticationType === 'Kerberos' || 
                                          applicationForm.connectionSource.authenticationType === 'GssapiKerberos') && (
                                            <>
                                                <div className="col-md-6">
                                                    <label className="form-label fw-semibold">Principal</label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.principal || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, principal: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="user@REALM.COM"
                                                    />
                                                </div>
                                                <div className="col-md-6">
                                                    <label className="form-label fw-semibold">Service Name</label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.serviceName || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, serviceName: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="Service name (e.g., mongodb)"
                                                    />
                                                </div>
                                                <div className="col-md-6">
                                                    <label className="form-label fw-semibold">Service Realm</label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.serviceRealm || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, serviceRealm: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="REALM.COM"
                                                    />
                                                </div>
                                                <div className="col-md-6">
                                                    <div className="form-check mt-4">
                                                        <input
                                                            className="form-check-input"
                                                            type="checkbox"
                                                            checked={applicationForm.connectionSource.canonicalizeHostName || false}
                                                            onChange={(e) => setApplicationForm({
                                                                ...applicationForm,
                                                                connectionSource: {...applicationForm.connectionSource, canonicalizeHostName: e.target.checked}
                                                            })}
                                                            id="canonicalizeHostName"
                                                        />
                                                        <label className="form-check-label" htmlFor="canonicalizeHostName">
                                                            Canonicalize Host Name
                                                        </label>
                                                    </div>
                                                </div>
                                            </>
                                        )}

                                        {/* MongoDB Authentication Database field (for SCRAM authentication) */}
                                        {(applicationForm.connectionSource.authenticationType === 'ScramSha1' ||
                                          applicationForm.connectionSource.authenticationType === 'ScramSha256') && (
                                            <div className="col-12">
                                                <label className="form-label fw-semibold">Authentication Database</label>
                                                <input
                                                    type="text"
                                                    value={applicationForm.connectionSource.authenticationDatabase || ''}
                                                    onChange={(e) => setApplicationForm({
                                                        ...applicationForm,
                                                        connectionSource: {...applicationForm.connectionSource, authenticationDatabase: e.target.value}
                                                    })}
                                                    className="form-control rounded-3"
                                                    placeholder="admin"
                                                />
                                                <div className="form-text">
                                                    The database where the user is defined (usually 'admin' for MongoDB)
                                                </div>
                                            </div>
                                        )}

                                        {/* X.509 Certificate fields */}
                                        {applicationForm.connectionSource.authenticationType === 'X509' && (
                                            <>
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">Certificate File Path</label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.certificateFilePath || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, certificateFilePath: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="/path/to/certificate.pem"
                                                    />
                                                </div>
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">Private Key File Path</label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.privateKeyFilePath || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, privateKeyFilePath: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="/path/to/private-key.pem"
                                                    />
                                                </div>
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">CA Certificate File Path (Optional)</label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.caCertificateFilePath || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, caCertificateFilePath: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="/path/to/ca-certificate.pem"
                                                    />
                                                </div>
                                            </>
                                        )}
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