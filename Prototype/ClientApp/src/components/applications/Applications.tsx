import React, { useState, useEffect } from 'react';
import { Database, Plus, Trash2, TestTube, Eye, EyeOff, ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight, Loader, CheckCircle2, AlertTriangle, Search } from 'lucide-react';
import { applicationApi } from '../../services/api';

interface Application {
    applicationId: string;
    applicationName: string;
    applicationDescription: string;
    applicationDataSourceType: number | string;
    connection: {
        host: string;
        port: string;
        databaseName: string;
        authenticationType: string;
        username?: string;
        // Additional fields based on authentication type
        authenticationDatabase?: string;
        awsAccessKeyId?: string;
        awsRoleArn?: string;
        principal?: string;
        serviceName?: string;
        serviceRealm?: string;
        canonicalizeHostName?: boolean;
    };
    createdAt?: string;
    updatedAt?: string;
}

// Enum mapping to convert numeric values to string names
const DataSourceTypeEnum: { [key: number]: string } = {
    // Database connections
    0: 'MicrosoftSqlServer',
    1: 'MySql', 
    2: 'PostgreSql',
    3: 'MongoDb',
    4: 'Redis',
    5: 'Oracle',
    6: 'MariaDb',
    7: 'Sqlite',
    8: 'Cassandra',
    9: 'ElasticSearch',
    
    // API connections
    10: 'RestApi',
    11: 'GraphQL',
    12: 'SoapApi', 
    13: 'ODataApi',
    14: 'WebSocket',
    
    // File-based connections
    15: 'CsvFile',
    16: 'JsonFile',
    17: 'XmlFile',
    18: 'ExcelFile',
    19: 'ParquetFile',
    20: 'YamlFile',
    21: 'TextFile',
    
    // Cloud storage
    22: 'AzureBlobStorage',
    23: 'AmazonS3',
    24: 'GoogleCloudStorage',
    
    // Message queues
    25: 'RabbitMQ',
    26: 'ApacheKafka',
    27: 'AzureServiceBus'
};

// Helper function to get enum string name from numeric value
const getDataSourceTypeName = (value: number | string): string => {
    if (typeof value === 'number') {
        return DataSourceTypeEnum[value] || 'MicrosoftSqlServer';
    }
    return value || 'MicrosoftSqlServer';
};

const Applications: React.FC = () => {
    // Shared function to calculate pagination data to ensure consistency
    const calculatePaginationData = () => {
        const hasFilters = searchTerm !== '' || filterConnectionType !== 'all' || 
                          filterAuthType !== 'all' || sortOrder === 'oldest';
        
        // Use allApplications for filtering/sorting, applications for backend pagination
        const sourceApplications = hasFilters ? allApplications : applications;
        
        // Safety check - if source applications is empty, return default values
        if (!sourceApplications || sourceApplications.length === 0) {
            return {
                hasFilters,
                currentApplications: [],
                displayTotalPages: 0,
                displayTotalCount: 0,
                filteredApplications: []
            };
        }
        
        const filteredApplications = sourceApplications.filter(app => {
            const matchesSearch = searchTerm === '' || 
                                 app.applicationName.toLowerCase().includes(searchTerm.toLowerCase()) ||
                                 app.applicationDescription.toLowerCase().includes(searchTerm.toLowerCase());
            
            const matchesConnectionType = filterConnectionType === 'all' || 
                                        getDataSourceTypeName(app.applicationDataSourceType) === filterConnectionType;
            
            const matchesAuthType = filterAuthType === 'all' || 
                                  app.connection.authenticationType === filterAuthType;
            
            return matchesSearch && matchesConnectionType && matchesAuthType;
        });

        // Sort applications based on creation date
        const sortedApplications = [...filteredApplications].sort((a, b) => {
            if (!a.createdAt || !b.createdAt) return 0;
            const dateA = new Date(a.createdAt).getTime();
            const dateB = new Date(b.createdAt).getTime();
            return sortOrder === 'newest' ? dateB - dateA : dateA - dateB;
        });

        let displayTotalPages: number, displayTotalCount: number, currentApplications: Application[];
        if (hasFilters) {
            // Client-side pagination for filtered/sorted results
            displayTotalPages = Math.ceil(sortedApplications.length / pageSize);
            displayTotalCount = sortedApplications.length;
            const startIndex = (currentPage - 1) * pageSize;
            const endIndex = startIndex + pageSize;
            currentApplications = sortedApplications.slice(startIndex, endIndex);
        } else {
            // Backend pagination for unfiltered results
            displayTotalPages = totalPages;
            displayTotalCount = totalCount;
            currentApplications = applications;
        }

        return {
            hasFilters,
            currentApplications,
            displayTotalPages,
            displayTotalCount,
            filteredApplications
        };
    };
    const [applications, setApplications] = useState<Application[]>([]);
    const [allApplications, setAllApplications] = useState<Application[]>([]); // Store all applications for client-side operations
    const [loading, setLoading] = useState(false);
    const [testingConnection, setTestingConnection] = useState(false);
    const [connectionTestResult, setConnectionTestResult] = useState<{message: string, success: boolean} | null>(null);
    const [showApplicationForm, setShowApplicationForm] = useState(false);
    const [editingApp, setEditingApp] = useState<Application | null>(null);
    const [submitSuccess, setSubmitSuccess] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
    const [deletingApp, setDeletingApp] = useState<Application | null>(null);
    const [deleteSuccess, setDeleteSuccess] = useState(false);
    
    // Pagination state
    const [currentPage, setCurrentPage] = useState(1);
    const [pageSize, setPageSize] = useState(4);
    const [totalCount, setTotalCount] = useState(0);
    const [totalPages, setTotalPages] = useState(0);
    
    // Search and filter state
    const [searchTerm, setSearchTerm] = useState('');
    const [filterConnectionType, setFilterConnectionType] = useState<string>('all');
    const [filterAuthType, setFilterAuthType] = useState<string>('all');
    const [sortOrder, setSortOrder] = useState<'newest' | 'oldest'>('newest');

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
            awsRoleArn: '',
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
            caCertificateFilePath: '',
            // API-specific fields
            apiEndpoint: '',
            httpMethod: 'GET',
            headers: '',
            requestBody: '',
            apiKey: '',
            bearerToken: '',
            clientId: '',
            clientSecret: '',
            refreshToken: '',
            authorizationUrl: '',
            tokenUrl: '',
            scope: '',
            // File-specific fields
            filePath: '',
            fileFormat: '',
            delimiter: ',',
            encoding: 'UTF-8',
            hasHeader: true,
            customProperties: '',
            // Database instance field
            instance: '',
            // Azure Storage fields
            azureStorageAccountName: '',
            azureStorageAccountKey: '',
            azureSasToken: '',
            azureTenantId: '',
            azureClientId: '',
            azureClientSecret: '',
            // Amazon S3 fields
            s3BucketName: '',
            awsRegion: '',
            // Google Cloud Storage fields
            gcpServiceAccountJson: '',
            gcpAccessKeyId: '',
            gcpSecretAccessKey: ''
        }
    });

    const [showPasswords, setShowPasswords] = useState({
        connection: false,
        awsSecret: false,
        awsSession: false,
        password: false,
        apiKey: false,
        azureSecret: false,
        azureKey: false,
        gcpSecret: false
    });

    const fetchAllApplications = async () => {
        try {
            // Fetch all applications by requesting a large page size
            const response = await applicationApi.getApplications(1, 1000); // Large enough to get all
            if (response.success && response.data?.data) {
                setAllApplications(response.data.data);
                return response.data.data;
            }
            return [];
        } catch (error) {
            console.error('Failed to fetch all applications:', error);
            return [];
        }
    };

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

    // Smart refetch that handles empty pages after deletion
    const refetchApplications = async () => {
        setLoading(true);
        try {
            const response = await applicationApi.getApplications(currentPage, pageSize);
            if (response.success && response.data?.data) {
                const newTotalPages = response.data.totalPages || 1;
                const newTotalCount = response.data.totalCount || 0;
                
                // If current page is beyond available pages and we have items, go to last page
                if (currentPage > newTotalPages && newTotalCount > 0) {
                    await fetchApplications(newTotalPages, pageSize);
                } else if (newTotalCount === 0) {
                    // If no items at all, go to page 1
                    setCurrentPage(1);
                    setApplications([]);
                    setAllApplications([]);
                    setTotalCount(0);
                    setTotalPages(1);
                    setLoading(false);
                } else {
                    // Normal case - update with current data
                    setApplications(response.data.data);
                    setTotalCount(newTotalCount);
                    setTotalPages(newTotalPages);
                    setLoading(false);
                }
            }
            // Also refresh all applications
            await fetchAllApplications();
        } catch (error) {
            console.error('Failed to fetch applications:', error);
            setLoading(false);
        }
    };

    // Refetch and navigate to first page (where new items should appear)
    const refetchAndGoToFirstPage = async () => {
        setLoading(true);
        try {
            // Navigate to the first page where new items should appear
            await fetchApplications(1, pageSize);
            // Also refresh all applications
            await fetchAllApplications();
        } catch (error) {
            console.error('Failed to fetch applications:', error);
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchApplications(1, pageSize);
        // Also fetch all applications for client-side operations
        fetchAllApplications();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    // Reset form to initial state
    const resetForm = () => {
        setApplicationForm({
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
                awsRoleArn: '',
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
                caCertificateFilePath: '',
                // API-specific fields
                apiEndpoint: '',
                httpMethod: 'GET',
                headers: '',
                requestBody: '',
                apiKey: '',
                bearerToken: '',
                clientId: '',
                clientSecret: '',
                refreshToken: '',
                authorizationUrl: '',
                tokenUrl: '',
                scope: '',
                // File-specific fields
                filePath: '',
                fileFormat: '',
                delimiter: ',',
                encoding: 'UTF-8',
                hasHeader: true,
                customProperties: '',
                // Database instance field
                instance: '',
                // Azure Storage fields
                azureStorageAccountName: '',
                azureStorageAccountKey: '',
                azureSasToken: '',
                azureTenantId: '',
                azureClientId: '',
                azureClientSecret: '',
                // Amazon S3 fields
                s3BucketName: '',
                awsRegion: '',
                // Google Cloud Storage fields
                gcpServiceAccountJson: '',
                gcpAccessKeyId: '',
                gcpSecretAccessKey: ''
            }
        });
        setEditingApp(null);
        setSubmitSuccess(false);
    };

    // Add escape key listener to close modals
    useEffect(() => {
        const handleEscapeKey = (event: KeyboardEvent) => {
            if (event.key === 'Escape') {
                if (showApplicationForm) {
                    setShowApplicationForm(false);
                    resetForm();
                }
            }
        };

        document.addEventListener('keydown', handleEscapeKey);
        return () => {
            document.removeEventListener('keydown', handleEscapeKey);
        };
    }, [showApplicationForm]);

    // Effect to populate form when editing an application
    useEffect(() => {
        if (editingApp) {
            setApplicationForm({
                applicationName: editingApp.applicationName,
                applicationDescription: editingApp.applicationDescription,
                dataSourceType: getDataSourceTypeName(editingApp.applicationDataSourceType),
                connectionSource: {
                    host: editingApp.connection.host,
                    port: editingApp.connection.port,
                    databaseName: editingApp.connection.databaseName,
                    authenticationType: editingApp.connection.authenticationType,
                    username: editingApp.connection?.username || '',
                    password: '',  // We don't get password back from API for security - user must re-enter
                    // AWS IAM fields
                    awsAccessKeyId: editingApp.connection?.awsAccessKeyId || '',
                    awsSecretAccessKey: '', // Not returned for security
                    awsSessionToken: '', // Not returned for security
                    awsRoleArn: editingApp.connection?.awsRoleArn || '',
                    // Kerberos/GSSAPI fields
                    principal: editingApp.connection?.principal || '',
                    serviceName: editingApp.connection?.serviceName || '',
                    serviceRealm: editingApp.connection?.serviceRealm || '',
                    canonicalizeHostName: editingApp.connection?.canonicalizeHostName || false,
                    // MongoDB specific fields
                    authenticationDatabase: editingApp.connection?.authenticationDatabase || '',
                    // X.509 certificate fields
                    certificateFilePath: '',
                    privateKeyFilePath: '',
                    caCertificateFilePath: '',
                    // API-specific fields
                    apiEndpoint: '',
                    httpMethod: 'GET',
                    headers: '',
                    requestBody: '',
                    apiKey: '',
                    bearerToken: '',
                    clientId: '',
                    clientSecret: '',
                    refreshToken: '',
                    authorizationUrl: '',
                    tokenUrl: '',
                    scope: '',
                    // File-specific fields
                    filePath: '',
                    fileFormat: '',
                    delimiter: ',',
                    encoding: 'UTF-8',
                    hasHeader: true,
                    customProperties: '',
                    // Database instance field
                    instance: '',
                    // Azure Storage fields
                    azureStorageAccountName: '',
                    azureStorageAccountKey: '',
                    azureSasToken: '',
                    azureTenantId: '',
                    azureClientId: '',
                    azureClientSecret: '',
                    // Amazon S3 fields
                    s3BucketName: '',
                    awsRegion: '',
                    // Google Cloud Storage fields
                    gcpServiceAccountJson: '',
                    gcpAccessKeyId: '',
                    gcpSecretAccessKey: ''
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
                    awsRoleArn: '',
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
                    caCertificateFilePath: '',
                    // API-specific fields
                    apiEndpoint: '',
                    httpMethod: 'GET',
                    headers: '',
                    requestBody: '',
                    apiKey: '',
                    bearerToken: '',
                    clientId: '',
                    clientSecret: '',
                    refreshToken: '',
                    authorizationUrl: '',
                    tokenUrl: '',
                    scope: '',
                    // File-specific fields
                    filePath: '',
                    fileFormat: '',
                    delimiter: ',',
                    encoding: 'UTF-8',
                    hasHeader: true,
                    customProperties: '',
                    // Database instance field
                    instance: '',
                    // Azure Storage fields
                    azureStorageAccountName: '',
                    azureStorageAccountKey: '',
                    azureSasToken: '',
                    azureTenantId: '',
                    azureClientId: '',
                    azureClientSecret: '',
                    // Amazon S3 fields
                    s3BucketName: '',
                    awsRegion: '',
                    // Google Cloud Storage fields
                    gcpServiceAccountJson: '',
                    gcpAccessKeyId: '',
                    gcpSecretAccessKey: ''
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
        if (isSubmitting) return;
        
        setIsSubmitting(true);
        
        try {
            // Generate connection URL for submission
            const formDataWithUrl = {
                ...applicationForm,
                connectionSource: {
                    ...applicationForm.connectionSource,
                    url: generateConnectionUrl(applicationForm.connectionSource)
                }
            };

            let response;
            if (editingApp) {
                response = await applicationApi.updateApplication(editingApp.applicationId, formDataWithUrl);
            } else {
                response = await applicationApi.createApplication(formDataWithUrl);
            }
            
            if (response.success) {
                setSubmitSuccess(true);
                if (editingApp) {
                    // When editing, stay on current page
                    fetchApplications(currentPage, pageSize);
                } else {
                    // When creating new, go to first page where new item should appear
                    refetchAndGoToFirstPage();
                }
                
                // Form will be closed manually by user clicking X
            } else {
                console.error('Server response:', response);
                console.error('Response errors:', response.errors);
                alert(`${response.message || 'Failed to save application'}${response.errors ? '\nErrors: ' + Object.keys(response.errors).join(', ') : ''}`);
            }
        } catch (error: any) {
            console.error('Failed to save application:', error);
            console.error('Full error details:', JSON.stringify(error, null, 2));
            alert(error.message || 'Failed to save application');
        } finally {
            setIsSubmitting(false);
        }
    };

    const testConnection = async (application?: any) => {
        if (testingConnection) return; // Prevent multiple simultaneous tests
        
        setTestingConnection(true);
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
            
            setConnectionTestResult({
                message: response.message,
                success: response.success
            });
            
            // Auto-hide the result after 5 seconds
            setTimeout(() => setConnectionTestResult(null), 5000);
        } catch (error: any) {
            console.error('Connection test failed:', error);
            setConnectionTestResult({
                message: error.message || 'Connection test failed',
                success: false
            });
            
            // Auto-hide the result after 5 seconds
            setTimeout(() => setConnectionTestResult(null), 5000);
        } finally {
            setTestingConnection(false);
        }
    };

    const deleteApplication = async () => {
        if (!deletingApp) return;

        try {
            const response = await applicationApi.deleteApplication(deletingApp.applicationId);
            
            if (response.success) {
                setDeleteSuccess(true);
                refetchApplications();
                
                // Delete success modal will be closed manually by user clicking X
            } else {
                alert(response.message || 'Failed to delete application');
            }
        } catch (error: any) {
            console.error('Failed to delete application:', error);
            alert(error.message || 'Failed to delete application');
        }
    };

    const confirmDeleteApplication = (app: Application) => {
        setDeletingApp(app);
        setShowDeleteConfirm(true);
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
                                onClick={() => {
                                    resetForm();
                                    setShowApplicationForm(true);
                                }}
                                className="btn btn-primary rounded-3 fw-semibold d-flex align-items-center"
                            >
                                <Plus className="me-2" size={18} />
                                <span>Add Application</span>
                            </button>
                        </div>

                        {/* Search and Filter Controls */}
                        <div className="row g-3 mb-4">
                            <div className="col-md-4">
                                <div className="position-relative">
                                    <Search className="position-absolute top-50 start-0 translate-middle-y ms-3 text-muted" size={16} />
                                    <input
                                        type="text"
                                        className="form-control rounded-3 ps-5"
                                        placeholder="Search applications..."
                                        value={searchTerm}
                                        onChange={(e) => {
                                            setSearchTerm(e.target.value);
                                            setCurrentPage(1); // Reset to first page when searching
                                        }}
                                    />
                                </div>
                            </div>
                            <div className="col-md-3">
                                <select
                                    className="form-select rounded-3"
                                    value={filterConnectionType}
                                    onChange={(e) => {
                                        setFilterConnectionType(e.target.value);
                                        setCurrentPage(1); // Reset to first page when filtering
                                    }}
                                >
                                    <option value="all">All Connection Types</option>
                                    <optgroup label="Database Connections">
                                        <option value="MicrosoftSqlServer">Microsoft SQL Server</option>
                                        <option value="MySql">MySQL</option>
                                        <option value="PostgreSql">PostgreSQL</option>
                                        <option value="MongoDb">MongoDB</option>
                                        <option value="Redis">Redis</option>
                                        <option value="Oracle">Oracle Database</option>
                                        <option value="MariaDb">MariaDB</option>
                                        <option value="Sqlite">SQLite</option>
                                        <option value="Cassandra">Apache Cassandra</option>
                                        <option value="ElasticSearch">Elasticsearch</option>
                                    </optgroup>
                                    <optgroup label="API Connections">
                                        <option value="RestApi">REST API</option>
                                        <option value="GraphQL">GraphQL</option>
                                        <option value="SoapApi">SOAP API</option>
                                        <option value="ODataApi">OData API</option>
                                        <option value="WebSocket">WebSocket</option>
                                    </optgroup>
                                    <optgroup label="File Connections">
                                        <option value="CsvFile">CSV File</option>
                                        <option value="JsonFile">JSON File</option>
                                        <option value="XmlFile">XML File</option>
                                        <option value="ExcelFile">Excel File</option>
                                        <option value="ParquetFile">Parquet File</option>
                                        <option value="YamlFile">YAML File</option>
                                        <option value="TextFile">Text File</option>
                                    </optgroup>
                                    <optgroup label="Cloud Storage">
                                        <option value="AzureBlobStorage">Azure Blob Storage</option>
                                        <option value="AmazonS3">Amazon S3</option>
                                        <option value="GoogleCloudStorage">Google Cloud Storage</option>
                                    </optgroup>
                                    <optgroup label="Message Queues">
                                        <option value="RabbitMQ">RabbitMQ</option>
                                        <option value="ApacheKafka">Apache Kafka</option>
                                        <option value="AzureServiceBus">Azure Service Bus</option>
                                    </optgroup>
                                </select>
                            </div>
                            <div className="col-md-3">
                                <select
                                    className="form-select rounded-3"
                                    value={filterAuthType}
                                    onChange={(e) => {
                                        setFilterAuthType(e.target.value);
                                        setCurrentPage(1); // Reset to first page when filtering
                                    }}
                                >
                                    <option value="all">All Auth Types</option>
                                    <option value="UserPassword">Username & Password</option>
                                    <option value="WindowsIntegrated">Windows Integrated</option>
                                    <option value="AzureAdPassword">Azure AD with Password</option>
                                    <option value="AzureAdIntegrated">Azure AD Integrated</option>
                                    <option value="NoAuth">No Authentication</option>
                                    <option value="Kerberos">Kerberos</option>
                                    <option value="AwsIam">AWS IAM</option>
                                    <option value="ApiKey">API Key</option>
                                    <option value="BearerToken">Bearer Token</option>
                                </select>
                            </div>
                            <div className="col-md-2">
                                <select
                                    className="form-select rounded-3"
                                    value={sortOrder}
                                    onChange={async (e) => {
                                        const newSortOrder = e.target.value as 'newest' | 'oldest';
                                        setSortOrder(newSortOrder);
                                        setCurrentPage(1); // Reset to first page when sorting changes
                                        
                                        // If switching to oldest first and we don't have all applications, fetch them
                                        if (newSortOrder === 'oldest' && allApplications.length === 0) {
                                            await fetchAllApplications();
                                        }
                                    }}
                                >
                                    <option value="newest">Newest First</option>
                                    <option value="oldest">Oldest First</option>
                                </select>
                            </div>
                        </div>

                        <div className="row g-3">
                            {(() => {
                                const { currentApplications } = calculatePaginationData();
                                return currentApplications.map((app) => (
                                <div key={app.applicationId} className="col-12">
                                    <div 
                                        className="card border border-light rounded-3 h-100" 
                                        style={{cursor: 'pointer'}}
                                        onClick={() => {
                                            setEditingApp(app);
                                            setShowApplicationForm(true);
                                        }}
                                    >
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
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            testConnection(app);
                                                        }}
                                                        className="btn btn-outline-primary btn-sm rounded-3"
                                                        title="Test Connection"
                                                        disabled={testingConnection}
                                                    >
                                                        {testingConnection ? (
                                                            <Loader size={16} className="animate-spin" />
                                                        ) : (
                                                            <TestTube size={16} />
                                                        )}
                                                    </button>
                                                    <button
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            confirmDeleteApplication(app);
                                                        }}
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
                                ));
                            })()}
                            {(() => {
                                const { hasFilters, filteredApplications } = calculatePaginationData();
                                const isEmpty = hasFilters ? filteredApplications.length === 0 : applications.length === 0;
                                
                                return isEmpty && !loading && (
                                    <div className="col-12">
                                        <div className="text-center py-5 text-muted">
                                            <Database size={48} className="mb-3 opacity-50" />
                                            <p>{hasFilters ? 'No applications match your search criteria' : 'No applications configured yet'}</p>
                                        </div>
                                    </div>
                                );
                            })()}
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
                        {(() => {
                            const { hasFilters, currentApplications, displayTotalPages, displayTotalCount } = calculatePaginationData();
                            
                            return (displayTotalCount > 4 || sortOrder === 'oldest') && (
                                <div className="d-flex justify-content-between align-items-center mt-4">
                                    <div className="d-flex align-items-center gap-3">
                                        <span className="text-muted">
                                            {hasFilters ? (
                                                `Showing ${currentApplications.length} of ${displayTotalCount} applications (filtered)`
                                            ) : (
                                                `Showing ${((currentPage - 1) * pageSize) + 1} to ${Math.min(currentPage * pageSize, displayTotalCount)} of ${displayTotalCount} applications`
                                            )}
                                        </span>
                                        <div className="d-flex align-items-center gap-2">
                                            <span className="text-muted small">Applications per page:</span>
                                            <select
                                                className="form-select form-select-sm"
                                                style={{ width: 'auto' }}
                                                value={pageSize}
                                                onChange={(e) => {
                                                    const newPageSize = parseInt(e.target.value);
                                                    setPageSize(newPageSize);
                                                    setCurrentPage(1);
                                                    if (!hasFilters) {
                                                        fetchApplications(1, newPageSize);
                                                    }
                                                }}
                                            >
                                                <option value={4}>4</option>
                                                <option value={10}>10</option>
                                                <option value={20}>20</option>
                                                <option value={50}>50</option>
                                            </select>
                                        </div>
                                    </div>
                                    
                                    <nav>
                                        <ul className="pagination pagination-sm mb-0">
                                            <li className={`page-item ${currentPage === 1 ? 'disabled' : ''}`}>
                                                <button 
                                                    className="page-link" 
                                                    onClick={() => {
                                                        setCurrentPage(1);
                                                        if (!hasFilters) {
                                                            fetchApplications(1);
                                                        }
                                                    }}
                                                    disabled={currentPage === 1}
                                                >
                                                    <ChevronsLeft size={16} />
                                                </button>
                                            </li>
                                            <li className={`page-item ${currentPage === 1 ? 'disabled' : ''}`}>
                                                <button 
                                                    className="page-link" 
                                                    onClick={() => {
                                                        const newPage = currentPage - 1;
                                                        setCurrentPage(newPage);
                                                        if (!hasFilters) {
                                                            fetchApplications(newPage);
                                                        }
                                                    }}
                                                    disabled={currentPage === 1}
                                                >
                                                    <ChevronLeft size={16} />
                                                </button>
                                            </li>
                                            
                                            {/* Page numbers */}
                                            {Array.from({ length: Math.min(5, displayTotalPages) }, (_, i) => {
                                                let pageNum: number;
                                                if (displayTotalPages <= 5) {
                                                    pageNum = i + 1;
                                                } else if (currentPage <= 3) {
                                                    pageNum = i + 1;
                                                } else if (currentPage >= displayTotalPages - 2) {
                                                    pageNum = displayTotalPages - 4 + i;
                                                } else {
                                                    pageNum = currentPage - 2 + i;
                                                }
                                                
                                                return (
                                                    <li key={pageNum} className={`page-item ${currentPage === pageNum ? 'active' : ''}`}>
                                                        <button 
                                                            className="page-link" 
                                                            onClick={() => {
                                                                setCurrentPage(pageNum);
                                                                if (!hasFilters) {
                                                                    fetchApplications(pageNum);
                                                                }
                                                            }}
                                                        >
                                                            {pageNum}
                                                        </button>
                                                    </li>
                                                );
                                            })}
                                            
                                            <li className={`page-item ${currentPage === displayTotalPages ? 'disabled' : ''}`}>
                                                <button 
                                                    className="page-link" 
                                                    onClick={() => {
                                                        const newPage = currentPage + 1;
                                                        setCurrentPage(newPage);
                                                        if (!hasFilters) {
                                                            fetchApplications(newPage);
                                                        }
                                                    }}
                                                    disabled={currentPage === displayTotalPages}
                                                >
                                                    <ChevronRight size={16} />
                                                </button>
                                            </li>
                                            <li className={`page-item ${currentPage === displayTotalPages ? 'disabled' : ''}`}>
                                                <button 
                                                    className="page-link" 
                                                    onClick={() => {
                                                        setCurrentPage(displayTotalPages);
                                                        if (!hasFilters) {
                                                            fetchApplications(displayTotalPages);
                                                        }
                                                    }}
                                                    disabled={currentPage === displayTotalPages}
                                                >
                                                    <ChevronsRight size={16} />
                                                </button>
                                            </li>
                                        </ul>
                                    </nav>
                                </div>
                            );
                        })()}
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
                                            resetForm();
                                        }}
                                    ></button>
                                </div>
                                <div className="modal-body">
                                    {submitSuccess ? (
                                        <div className="text-center py-4">
                                            <CheckCircle2 size={64} className="text-success mb-3" />
                                            <h4 className="text-success fw-bold">
                                                {editingApp ? 'Application Updated Successfully!' : 'Application Created Successfully!'}
                                            </h4>
                                            <p className="text-muted">
                                                {editingApp ? 'The application has been updated.' : 'The new application has been created.'}
                                            </p>
                                        </div>
                                    ) : (
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
                                                    const getDefaultAuthType = (type: string) => {
                                                        switch (type) {
                                                            case 'MicrosoftSqlServer':
                                                            case 'MySql':
                                                            case 'PostgreSql':
                                                            case 'MongoDb':
                                                            case 'Redis':
                                                            case 'Oracle':
                                                            case 'MariaDb':
                                                            case 'Sqlite':
                                                            case 'Cassandra':
                                                            case 'ElasticSearch':
                                                            case 'RabbitMQ':
                                                            case 'ApacheKafka':
                                                            case 'AzureServiceBus':
                                                                return 'UserPassword';
                                                            case 'RestApi':
                                                            case 'GraphQL':
                                                            case 'SoapApi':
                                                            case 'ODataApi':
                                                            case 'WebSocket':
                                                            case 'CsvFile':
                                                            case 'JsonFile':
                                                            case 'XmlFile':
                                                            case 'ExcelFile':
                                                            case 'ParquetFile':
                                                            case 'YamlFile':
                                                            case 'TextFile':
                                                                return 'NoAuth';
                                                            case 'AzureBlobStorage':
                                                                return 'AzureAccessKey';
                                                            case 'AmazonS3':
                                                                return 'AwsIam';
                                                            case 'GoogleCloudStorage':
                                                                return 'ServicePrincipal';
                                                            default:
                                                                return 'NoAuth';
                                                        }
                                                    };
                                                    const getDefaultPort = (type: string) => {
                                                        switch (type) {
                                                            case 'MicrosoftSqlServer': return '1433';
                                                            case 'MySql': return '3306';
                                                            case 'PostgreSql': return '5432';
                                                            case 'MongoDb': return '27017';
                                                            case 'Redis': return '6379';
                                                            case 'Oracle': return '1521';
                                                            case 'MariaDb': return '3306';
                                                            case 'Sqlite': return '';
                                                            case 'Cassandra': return '9042';
                                                            case 'ElasticSearch': return '9200';
                                                            case 'RestApi': return '443';
                                                            case 'GraphQL': return '443';
                                                            case 'SoapApi': return '443';
                                                            case 'ODataApi': return '443';
                                                            case 'WebSocket': return '443';
                                                            case 'RabbitMQ': return '5672';
                                                            case 'ApacheKafka': return '9092';
                                                            case 'AzureServiceBus': return '443';
                                                            default: return '';
                                                        }
                                                    };
                                                    const newAuthType = getDefaultAuthType(e.target.value);
                                                    const defaultPort = getDefaultPort(e.target.value);
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
                                                <optgroup label="Database Connections">
                                                    <option value="MicrosoftSqlServer">Microsoft SQL Server</option>
                                                    <option value="MySql">MySQL</option>
                                                    <option value="PostgreSql">PostgreSQL</option>
                                                    <option value="MongoDb">MongoDB</option>
                                                    <option value="Redis">Redis</option>
                                                    <option value="Oracle">Oracle Database</option>
                                                    <option value="MariaDb">MariaDB</option>
                                                    <option value="Sqlite">SQLite</option>
                                                    <option value="Cassandra">Apache Cassandra</option>
                                                    <option value="ElasticSearch">Elasticsearch</option>
                                                </optgroup>
                                                <optgroup label="API Connections">
                                                    <option value="RestApi">REST API</option>
                                                    <option value="GraphQL">GraphQL</option>
                                                    <option value="SoapApi">SOAP API</option>
                                                    <option value="ODataApi">OData API</option>
                                                    <option value="WebSocket">WebSocket</option>
                                                </optgroup>
                                                <optgroup label="File Connections">
                                                    <option value="CsvFile">CSV File</option>
                                                    <option value="JsonFile">JSON File</option>
                                                    <option value="XmlFile">XML File</option>
                                                    <option value="ExcelFile">Excel File</option>
                                                    <option value="ParquetFile">Parquet File</option>
                                                    <option value="YamlFile">YAML File</option>
                                                    <option value="TextFile">Text File</option>
                                                </optgroup>
                                                <optgroup label="Cloud Storage">
                                                    <option value="AzureBlobStorage">Azure Blob Storage</option>
                                                    <option value="AmazonS3">Amazon S3</option>
                                                    <option value="GoogleCloudStorage">Google Cloud Storage</option>
                                                </optgroup>
                                                <optgroup label="Message Queues">
                                                    <option value="RabbitMQ">RabbitMQ</option>
                                                    <option value="ApacheKafka">Apache Kafka</option>
                                                    <option value="AzureServiceBus">Azure Service Bus</option>
                                                </optgroup>
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
                                                        <option value="WindowsIntegrated">Windows Integrated</option>
                                                        <option value="Kerberos">Kerberos</option>
                                                        <option value="AzureAdPassword">Azure AD with Password</option>
                                                        <option value="AzureAdInteractive">Azure AD Interactive</option>
                                                        <option value="AzureAdIntegrated">Azure AD Integrated</option>
                                                        <option value="AzureAdDefault">Azure AD Default</option>
                                                        <option value="AzureAdMsi">Azure AD MSI</option>
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

                                                {/* PostgreSQL options */}
                                                {applicationForm.dataSourceType === 'PostgreSql' && (
                                                    <>
                                                        <option value="UserPassword">Username & Password</option>
                                                        <option value="WindowsIntegrated">Windows Integrated</option>
                                                    </>
                                                )}

                                                {/* Redis options */}
                                                {applicationForm.dataSourceType === 'Redis' && (
                                                    <>
                                                        <option value="UserPassword">Username & Password (ACL)</option>
                                                        <option value="NoAuth">No Authentication</option>
                                                    </>
                                                )}

                                                {/* MariaDB options (same as MySQL) */}
                                                {applicationForm.dataSourceType === 'MariaDb' && (
                                                    <>
                                                        <option value="UserPassword">Username & Password</option>
                                                        <option value="NoAuth">No Authentication</option>
                                                    </>
                                                )}

                                                {/* Oracle options */}
                                                {applicationForm.dataSourceType === 'Oracle' && (
                                                    <>
                                                        <option value="UserPassword">Username & Password</option>
                                                        <option value="WindowsIntegrated">Windows Integrated</option>
                                                    </>
                                                )}

                                                {/* SQLite options */}
                                                {applicationForm.dataSourceType === 'Sqlite' && (
                                                    <>
                                                        <option value="NoAuth">File-based (No Auth)</option>
                                                        <option value="UserPassword">Password Protected</option>
                                                    </>
                                                )}

                                                {/* Cassandra options */}
                                                {applicationForm.dataSourceType === 'Cassandra' && (
                                                    <>
                                                        <option value="UserPassword">Username & Password</option>
                                                        <option value="NoAuth">No Authentication</option>
                                                    </>
                                                )}

                                                {/* Elasticsearch options */}
                                                {applicationForm.dataSourceType === 'ElasticSearch' && (
                                                    <>
                                                        <option value="UserPassword">Username & Password</option>
                                                        <option value="NoAuth">No Authentication</option>
                                                        <option value="ApiKey">API Key</option>
                                                    </>
                                                )}

                                                {/* REST API options */}
                                                {applicationForm.dataSourceType === 'RestApi' && (
                                                    <>
                                                        <option value="NoAuth">No Authentication</option>
                                                        <option value="ApiKey">API Key</option>
                                                        <option value="BearerToken">Bearer Token</option>
                                                        <option value="BasicAuth">Basic Authentication</option>
                                                        <option value="OAuth2">OAuth 2.0</option>
                                                        <option value="JwtToken">JWT Token</option>
                                                        <option value="Custom">Custom Headers</option>
                                                    </>
                                                )}

                                                {/* GraphQL options */}
                                                {applicationForm.dataSourceType === 'GraphQL' && (
                                                    <>
                                                        <option value="NoAuth">No Authentication</option>
                                                        <option value="ApiKey">API Key</option>
                                                        <option value="BearerToken">Bearer Token</option>
                                                        <option value="BasicAuth">Basic Authentication</option>
                                                        <option value="OAuth2">OAuth 2.0</option>
                                                        <option value="JwtToken">JWT Token</option>
                                                    </>
                                                )}

                                                {/* SOAP API options */}
                                                {applicationForm.dataSourceType === 'SoapApi' && (
                                                    <>
                                                        <option value="NoAuth">No Authentication</option>
                                                        <option value="BasicAuth">Basic Authentication</option>
                                                        <option value="Custom">Custom Headers</option>
                                                    </>
                                                )}

                                                {/* OData API options */}
                                                {applicationForm.dataSourceType === 'ODataApi' && (
                                                    <>
                                                        <option value="NoAuth">No Authentication</option>
                                                        <option value="ApiKey">API Key</option>
                                                        <option value="BearerToken">Bearer Token</option>
                                                        <option value="BasicAuth">Basic Authentication</option>
                                                        <option value="OAuth2">OAuth 2.0</option>
                                                    </>
                                                )}

                                                {/* WebSocket options */}
                                                {applicationForm.dataSourceType === 'WebSocket' && (
                                                    <>
                                                        <option value="NoAuth">No Authentication</option>
                                                        <option value="ApiKey">API Key</option>
                                                        <option value="BearerToken">Bearer Token</option>
                                                        <option value="Custom">Custom Headers</option>
                                                    </>
                                                )}

                                                {/* File connection options */}
                                                {['CsvFile', 'JsonFile', 'XmlFile', 'ExcelFile', 'ParquetFile', 'YamlFile', 'TextFile'].includes(applicationForm.dataSourceType) && (
                                                    <>
                                                        <option value="NoAuth">Local File System</option>
                                                        <option value="FileSystem">Network Share</option>
                                                    </>
                                                )}

                                                {/* Cloud storage options */}
                                                {applicationForm.dataSourceType === 'AzureBlobStorage' && (
                                                    <>
                                                        <option value="AzureAccessKey">Access Key</option>
                                                        <option value="SharedAccessSignature">SAS Token</option>
                                                        <option value="ServicePrincipal">Service Principal</option>
                                                        <option value="AzureAdIntegrated">Azure AD Integrated</option>
                                                    </>
                                                )}

                                                {applicationForm.dataSourceType === 'AmazonS3' && (
                                                    <>
                                                        <option value="AwsIam">AWS IAM</option>
                                                        <option value="AwsAccessKey">AWS Access Key</option>
                                                    </>
                                                )}

                                                {applicationForm.dataSourceType === 'GoogleCloudStorage' && (
                                                    <>
                                                        <option value="ServicePrincipal">Service Account</option>
                                                        <option value="GcpAccessKey">HMAC Access Key</option>
                                                    </>
                                                )}

                                                {/* Message queue options */}
                                                {['RabbitMQ', 'ApacheKafka', 'AzureServiceBus'].includes(applicationForm.dataSourceType) && (
                                                    <>
                                                        <option value="UserPassword">Username & Password</option>
                                                        <option value="NoAuth">No Authentication</option>
                                                        <option value="ServicePrincipal">Service Principal</option>
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
                                            // MariaDB types that need username
                                            (applicationForm.dataSourceType === 'MariaDb' && 
                                             applicationForm.connectionSource.authenticationType === 'UserPassword') ||
                                            // PostgreSQL types that need username
                                            (applicationForm.dataSourceType === 'PostgreSql' && 
                                             applicationForm.connectionSource.authenticationType === 'UserPassword') ||
                                            // MongoDB types that need username
                                            (applicationForm.dataSourceType === 'MongoDb' && 
                                             ['UserPassword', 'ScramSha1', 'ScramSha256', 'PlainLdap'].includes(applicationForm.connectionSource.authenticationType)) ||
                                            // Redis types that need username
                                            (applicationForm.dataSourceType === 'Redis' && 
                                             applicationForm.connectionSource.authenticationType === 'UserPassword') ||
                                            // Oracle types that need username
                                            (applicationForm.dataSourceType === 'Oracle' && 
                                             applicationForm.connectionSource.authenticationType === 'UserPassword') ||
                                            // SQLite types that need username
                                            (applicationForm.dataSourceType === 'Sqlite' && 
                                             applicationForm.connectionSource.authenticationType === 'UserPassword') ||
                                            // Cassandra types that need username
                                            (applicationForm.dataSourceType === 'Cassandra' && 
                                             applicationForm.connectionSource.authenticationType === 'UserPassword') ||
                                            // Elasticsearch types that need username
                                            (applicationForm.dataSourceType === 'ElasticSearch' && 
                                             ['UserPassword', 'ApiKey'].includes(applicationForm.connectionSource.authenticationType))
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
                                            // MariaDB types that need password
                                            (applicationForm.dataSourceType === 'MariaDb' && 
                                             applicationForm.connectionSource.authenticationType === 'UserPassword') ||
                                            // PostgreSQL types that need password
                                            (applicationForm.dataSourceType === 'PostgreSql' && 
                                             applicationForm.connectionSource.authenticationType === 'UserPassword') ||
                                            // MongoDB types that need password
                                            (applicationForm.dataSourceType === 'MongoDb' && 
                                             ['UserPassword', 'ScramSha1', 'ScramSha256', 'PlainLdap'].includes(applicationForm.connectionSource.authenticationType)) ||
                                            // Redis types that need password
                                            (applicationForm.dataSourceType === 'Redis' && 
                                             applicationForm.connectionSource.authenticationType === 'UserPassword') ||
                                            // Oracle types that need password
                                            (applicationForm.dataSourceType === 'Oracle' && 
                                             applicationForm.connectionSource.authenticationType === 'UserPassword') ||
                                            // SQLite types that need password
                                            (applicationForm.dataSourceType === 'Sqlite' && 
                                             applicationForm.connectionSource.authenticationType === 'UserPassword') ||
                                            // Cassandra types that need password
                                            (applicationForm.dataSourceType === 'Cassandra' && 
                                             applicationForm.connectionSource.authenticationType === 'UserPassword') ||
                                            // Elasticsearch types that need password
                                            (applicationForm.dataSourceType === 'ElasticSearch' && 
                                             ['UserPassword', 'ApiKey'].includes(applicationForm.connectionSource.authenticationType))
                                        ) && (
                                            <div className="col-12">
                                                <label className="form-label fw-semibold">
                                                    Password
                                                    {editingApp && (
                                                        <span className="text-danger ms-2" style={{ fontSize: '0.875rem' }}>
                                                            * Required - please re-enter
                                                        </span>
                                                    )}
                                                </label>
                                                <div className="position-relative">
                                                    <input
                                                        type={showPasswords.connection ? "text" : "password"}
                                                        value={applicationForm.connectionSource.password}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, password: e.target.value}
                                                        })}
                                                        className="form-control rounded-3 pe-5"
                                                        placeholder={editingApp ? "Re-enter database password" : "Database password"}
                                                        required={!!editingApp}
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

                                        {/* AWS Access Key fields */}
                                        {(['AwsAccessKey', 'AwsSessionToken'].includes(applicationForm.connectionSource.authenticationType)) && (
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
                                                    <label className="form-label fw-semibold">
                                                        AWS Secret Access Key
                                                        {editingApp && (
                                                            <span className="text-danger ms-2" style={{ fontSize: '0.875rem' }}>
                                                                * Required - please re-enter
                                                            </span>
                                                        )}
                                                    </label>
                                                    <div className="position-relative">
                                                        <input
                                                            type={showPasswords.awsSecret ? "text" : "password"}
                                                            value={applicationForm.connectionSource.awsSecretAccessKey || ''}
                                                            onChange={(e) => setApplicationForm({
                                                                ...applicationForm,
                                                                connectionSource: {...applicationForm.connectionSource, awsSecretAccessKey: e.target.value}
                                                            })}
                                                            className="form-control rounded-3 pe-5"
                                                            placeholder={editingApp ? "Re-enter AWS Secret Access Key" : "AWS Secret Access Key"}
                                                            required={!!editingApp}
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
                                                    <label className="form-label fw-semibold">
                                                        AWS Session Token (Optional)
                                                        {editingApp && (
                                                            <span className="text-info ms-2" style={{ fontSize: '0.875rem' }}>
                                                                - Re-enter if needed
                                                            </span>
                                                        )}
                                                    </label>
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

                                        {/* AWS IAM fields */}
                                        {applicationForm.connectionSource.authenticationType === 'AwsIam' && (
                                            <>
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">IAM Role ARN (Optional)</label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.awsRoleArn || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, awsRoleArn: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="arn:aws:iam::123456789012:role/MyRole"
                                                    />
                                                    <div className="form-text">
                                                        Leave empty to use default IAM role or instance profile
                                                    </div>
                                                </div>
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">AWS Region</label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.awsRegion || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, awsRegion: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="us-east-1"
                                                    />
                                                </div>
                                            </>
                                        )}

                                        {/* Amazon S3 specific fields */}
                                        {applicationForm.dataSourceType === 'AmazonS3' && (
                                            <>
                                                <div className="col-md-6">
                                                    <label className="form-label fw-semibold">Bucket Name</label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.s3BucketName || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, s3BucketName: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="my-s3-bucket"
                                                    />
                                                </div>
                                                <div className="col-md-6">
                                                    <label className="form-label fw-semibold">Region</label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.awsRegion || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, awsRegion: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="us-east-1"
                                                    />
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

                                        {/* Azure Storage fields */}
                                        {applicationForm.dataSourceType === 'AzureBlobStorage' && ['AzureAccessKey', 'SharedAccessSignature'].includes(applicationForm.connectionSource.authenticationType) && (
                                            <>
                                                {applicationForm.connectionSource.authenticationType === 'AzureAccessKey' && (
                                                    <>
                                                        <div className="col-12">
                                                            <label className="form-label fw-semibold">Storage Account Name</label>
                                                            <input
                                                                type="text"
                                                                value={applicationForm.connectionSource.azureStorageAccountName || ''}
                                                                onChange={(e) => setApplicationForm({
                                                                    ...applicationForm,
                                                                    connectionSource: {...applicationForm.connectionSource, azureStorageAccountName: e.target.value}
                                                                })}
                                                                className="form-control rounded-3"
                                                                placeholder="mystorageaccount"
                                                            />
                                                        </div>
                                                        <div className="col-12">
                                                            <label className="form-label fw-semibold">Storage Account Key</label>
                                                            <div className="position-relative">
                                                                <input
                                                                    type={showPasswords.azureKey ? "text" : "password"}
                                                                    value={applicationForm.connectionSource.azureStorageAccountKey || ''}
                                                                    onChange={(e) => setApplicationForm({
                                                                        ...applicationForm,
                                                                        connectionSource: {...applicationForm.connectionSource, azureStorageAccountKey: e.target.value}
                                                                    })}
                                                                    className="form-control rounded-3 pe-5"
                                                                    placeholder="Storage account access key"
                                                                />
                                                                <button
                                                                    type="button"
                                                                    onClick={() => togglePasswordVisibility('azureKey')}
                                                                    className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                                                                >
                                                                    {showPasswords.azureKey ? <EyeOff size={16} /> : <Eye size={16} />}
                                                                </button>
                                                            </div>
                                                        </div>
                                                    </>
                                                )}
                                                {applicationForm.connectionSource.authenticationType === 'SharedAccessSignature' && (
                                                    <div className="col-12">
                                                        <label className="form-label fw-semibold">SAS Token</label>
                                                        <div className="position-relative">
                                                            <input
                                                                type={showPasswords.password ? "text" : "password"}
                                                                value={applicationForm.connectionSource.azureSasToken || ''}
                                                                onChange={(e) => setApplicationForm({
                                                                    ...applicationForm,
                                                                    connectionSource: {...applicationForm.connectionSource, azureSasToken: e.target.value}
                                                                })}
                                                                className="form-control rounded-3 pe-5"
                                                                placeholder="?sv=2021-06-08&ss=bfqt&srt=sco..."
                                                            />
                                                            <button
                                                                type="button"
                                                                onClick={() => togglePasswordVisibility('password')}
                                                                className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                                                            >
                                                                {showPasswords.password ? <EyeOff size={16} /> : <Eye size={16} />}
                                                            </button>
                                                        </div>
                                                    </div>
                                                )}
                                            </>
                                        )}

                                        {/* Azure Blob Storage Service Principal fields */}
                                        {applicationForm.dataSourceType === 'AzureBlobStorage' && applicationForm.connectionSource.authenticationType === 'ServicePrincipal' && (
                                            <>
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">Tenant ID</label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.azureTenantId || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, azureTenantId: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="00000000-0000-0000-0000-000000000000"
                                                    />
                                                </div>
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">Client ID</label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.azureClientId || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, azureClientId: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="00000000-0000-0000-0000-000000000000"
                                                    />
                                                </div>
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">Client Secret</label>
                                                    <div className="position-relative">
                                                        <input
                                                            type={showPasswords.azureSecret ? "text" : "password"}
                                                            value={applicationForm.connectionSource.azureClientSecret || ''}
                                                            onChange={(e) => setApplicationForm({
                                                                ...applicationForm,
                                                                connectionSource: {...applicationForm.connectionSource, azureClientSecret: e.target.value}
                                                            })}
                                                            className="form-control rounded-3 pe-5"
                                                            placeholder="Client secret value"
                                                        />
                                                        <button
                                                            type="button"
                                                            onClick={() => togglePasswordVisibility('azureSecret')}
                                                            className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                                                        >
                                                            {showPasswords.azureSecret ? <EyeOff size={16} /> : <Eye size={16} />}
                                                        </button>
                                                    </div>
                                                </div>
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">Storage Account Name</label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.azureStorageAccountName || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, azureStorageAccountName: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="mystorageaccount"
                                                    />
                                                </div>
                                            </>
                                        )}

                                        {/* Google Cloud Storage fields */}
                                        {applicationForm.dataSourceType === 'GoogleCloudStorage' && (
                                            <>
                                                {applicationForm.connectionSource.authenticationType === 'ServicePrincipal' && (
                                                    <div className="col-12">
                                                        <label className="form-label fw-semibold">Service Account JSON</label>
                                                        <textarea
                                                            value={applicationForm.connectionSource.gcpServiceAccountJson || ''}
                                                            onChange={(e) => setApplicationForm({
                                                                ...applicationForm,
                                                                connectionSource: {...applicationForm.connectionSource, gcpServiceAccountJson: e.target.value}
                                                            })}
                                                            className="form-control rounded-3"
                                                            rows={4}
                                                            placeholder='{"type": "service_account", "project_id": "my-project", ...}'
                                                        />
                                                        <div className="form-text">
                                                            Paste the complete service account JSON key file content
                                                        </div>
                                                    </div>
                                                )}
                                                {applicationForm.connectionSource.authenticationType === 'GcpAccessKey' && (
                                                    <>
                                                        <div className="col-12">
                                                            <label className="form-label fw-semibold">GCP Access Key ID</label>
                                                            <input
                                                                type="text"
                                                                value={applicationForm.connectionSource.gcpAccessKeyId || ''}
                                                                onChange={(e) => setApplicationForm({
                                                                    ...applicationForm,
                                                                    connectionSource: {...applicationForm.connectionSource, gcpAccessKeyId: e.target.value}
                                                                })}
                                                                className="form-control rounded-3"
                                                                placeholder="GCP HMAC Access Key ID"
                                                            />
                                                        </div>
                                                        <div className="col-12">
                                                            <label className="form-label fw-semibold">GCP Secret Access Key</label>
                                                            <div className="position-relative">
                                                                <input
                                                                    type={showPasswords.gcpSecret ? "text" : "password"}
                                                                    value={applicationForm.connectionSource.gcpSecretAccessKey || ''}
                                                                    onChange={(e) => setApplicationForm({
                                                                        ...applicationForm,
                                                                        connectionSource: {...applicationForm.connectionSource, gcpSecretAccessKey: e.target.value}
                                                                    })}
                                                                    className="form-control rounded-3 pe-5"
                                                                    placeholder="GCP HMAC Secret Access Key"
                                                                />
                                                                <button
                                                                    type="button"
                                                                    onClick={() => togglePasswordVisibility('gcpSecret')}
                                                                    className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                                                                >
                                                                    {showPasswords.gcpSecret ? <EyeOff size={16} /> : <Eye size={16} />}
                                                                </button>
                                                            </div>
                                                        </div>
                                                    </>
                                                )}
                                            </>
                                        )}

                                        {/* API Connection fields */}
                                        {['RestApi', 'GraphQL', 'SoapApi', 'ODataApi', 'WebSocket'].includes(applicationForm.dataSourceType) && (
                                            <>
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">API Endpoint URL</label>
                                                    <input
                                                        type="url"
                                                        value={applicationForm.connectionSource.apiEndpoint || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, apiEndpoint: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder="https://api.example.com/v1/endpoint"
                                                    />
                                                </div>
                                                {applicationForm.dataSourceType !== 'GraphQL' && (
                                                    <div className="col-12">
                                                        <label className="form-label fw-semibold">HTTP Method</label>
                                                        <select
                                                            value={applicationForm.connectionSource.httpMethod || 'GET'}
                                                            onChange={(e) => setApplicationForm({
                                                                ...applicationForm,
                                                                connectionSource: {...applicationForm.connectionSource, httpMethod: e.target.value}
                                                            })}
                                                            className="form-select rounded-3"
                                                        >
                                                            <option value="GET">GET</option>
                                                            <option value="POST">POST</option>
                                                            <option value="PUT">PUT</option>
                                                            <option value="DELETE">DELETE</option>
                                                            <option value="PATCH">PATCH</option>
                                                            <option value="HEAD">HEAD</option>
                                                            <option value="OPTIONS">OPTIONS</option>
                                                        </select>
                                                    </div>
                                                )}
                                                {['ApiKey', 'BearerToken', 'JwtToken'].includes(applicationForm.connectionSource.authenticationType) && (
                                                    <div className="col-12">
                                                        <label className="form-label fw-semibold">
                                                            {applicationForm.connectionSource.authenticationType === 'ApiKey' ? 'API Key' : 'Bearer Token'}
                                                        </label>
                                                        <div className="position-relative">
                                                            <input
                                                                type={showPasswords.apiKey ? "text" : "password"}
                                                                value={applicationForm.connectionSource.apiKey || applicationForm.connectionSource.bearerToken || ''}
                                                                onChange={(e) => {
                                                                    const field = applicationForm.connectionSource.authenticationType === 'ApiKey' ? 'apiKey' : 'bearerToken';
                                                                    setApplicationForm({
                                                                        ...applicationForm,
                                                                        connectionSource: {...applicationForm.connectionSource, [field]: e.target.value}
                                                                    });
                                                                }}
                                                                className="form-control rounded-3 pe-5"
                                                                placeholder={applicationForm.connectionSource.authenticationType === 'ApiKey' ? 'Your API key' : 'Bearer token or JWT'}
                                                            />
                                                            <button
                                                                type="button"
                                                                onClick={() => setShowPasswords(prev => ({ ...prev, apiKey: !prev.apiKey }))}
                                                                className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                                                            >
                                                                {showPasswords.apiKey ? <EyeOff size={16} /> : <Eye size={16} />}
                                                            </button>
                                                        </div>
                                                    </div>
                                                )}
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">Custom Headers (JSON format, optional)</label>
                                                    <textarea
                                                        value={applicationForm.connectionSource.headers || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, headers: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        rows={3}
                                                        placeholder='{"Content-Type": "application/json", "X-Custom-Header": "value"}'
                                                    />
                                                </div>
                                                {['POST', 'PUT', 'PATCH'].includes(applicationForm.connectionSource.httpMethod || '') && (
                                                    <div className="col-12">
                                                        <label className="form-label fw-semibold">Request Body (optional)</label>
                                                        <textarea
                                                            value={applicationForm.connectionSource.requestBody || ''}
                                                            onChange={(e) => setApplicationForm({
                                                                ...applicationForm,
                                                                connectionSource: {...applicationForm.connectionSource, requestBody: e.target.value}
                                                            })}
                                                            className="form-control rounded-3"
                                                            rows={4}
                                                            placeholder={
                                                                applicationForm.dataSourceType === 'GraphQL' 
                                                                    ? '{"query": "{ users { id name } }"}' 
                                                                    : '{"key": "value"}'
                                                            }
                                                        />
                                                    </div>
                                                )}
                                            </>
                                        )}

                                        {/* File Connection fields */}
                                        {['CsvFile', 'JsonFile', 'XmlFile', 'ExcelFile', 'ParquetFile', 'YamlFile', 'TextFile', 'AmazonS3', 'AzureBlobStorage', 'GoogleCloudStorage'].includes(applicationForm.dataSourceType) && (
                                            <>
                                                <div className="col-12">
                                                    <label className="form-label fw-semibold">
                                                        {['AmazonS3', 'AzureBlobStorage', 'GoogleCloudStorage'].includes(applicationForm.dataSourceType) 
                                                            ? 'File Path (bucket/container/object)' 
                                                            : 'File Path'
                                                        }
                                                    </label>
                                                    <input
                                                        type="text"
                                                        value={applicationForm.connectionSource.filePath || ''}
                                                        onChange={(e) => setApplicationForm({
                                                            ...applicationForm,
                                                            connectionSource: {...applicationForm.connectionSource, filePath: e.target.value}
                                                        })}
                                                        className="form-control rounded-3"
                                                        placeholder={
                                                            applicationForm.dataSourceType === 'AmazonS3' ? 's3://my-bucket/path/to/file.csv' :
                                                            applicationForm.dataSourceType === 'AzureBlobStorage' ? 'my-container/path/to/file.csv' :
                                                            applicationForm.dataSourceType === 'GoogleCloudStorage' ? 'gs://my-bucket/path/to/file.csv' :
                                                            '/path/to/local/file.csv'
                                                        }
                                                    />
                                                </div>
                                                {['CsvFile', 'TextFile'].includes(applicationForm.dataSourceType) && (
                                                    <>
                                                        <div className="col-md-6">
                                                            <label className="form-label fw-semibold">Delimiter</label>
                                                            <input
                                                                type="text"
                                                                value={applicationForm.connectionSource.delimiter || ','}
                                                                onChange={(e) => setApplicationForm({
                                                                    ...applicationForm,
                                                                    connectionSource: {...applicationForm.connectionSource, delimiter: e.target.value}
                                                                })}
                                                                className="form-control rounded-3"
                                                                placeholder=","
                                                                maxLength={1}
                                                            />
                                                        </div>
                                                        <div className="col-md-6">
                                                            <label className="form-label fw-semibold">Encoding</label>
                                                            <select
                                                                value={applicationForm.connectionSource.encoding || 'utf-8'}
                                                                onChange={(e) => setApplicationForm({
                                                                    ...applicationForm,
                                                                    connectionSource: {...applicationForm.connectionSource, encoding: e.target.value}
                                                                })}
                                                                className="form-select rounded-3"
                                                            >
                                                                <option value="utf-8">UTF-8</option>
                                                                <option value="utf-16">UTF-16</option>
                                                                <option value="ascii">ASCII</option>
                                                                <option value="iso-8859-1">ISO-8859-1</option>
                                                            </select>
                                                        </div>
                                                        <div className="col-12">
                                                            <div className="form-check">
                                                                <input
                                                                    type="checkbox"
                                                                    checked={applicationForm.connectionSource.hasHeader || false}
                                                                    onChange={(e) => setApplicationForm({
                                                                        ...applicationForm,
                                                                        connectionSource: {...applicationForm.connectionSource, hasHeader: e.target.checked}
                                                                    })}
                                                                    className="form-check-input"
                                                                    id="hasHeader"
                                                                />
                                                                <label className="form-check-label fw-semibold" htmlFor="hasHeader">
                                                                    File has header row
                                                                </label>
                                                            </div>
                                                        </div>
                                                    </>
                                                )}
                                            </>
                                        )}

                                        {/* Instance field for databases that support it */}
                                        {['MicrosoftSqlServer', 'Oracle'].includes(applicationForm.dataSourceType) && (
                                            <div className="col-12">
                                                <label className="form-label fw-semibold">Instance (optional)</label>
                                                <input
                                                    type="text"
                                                    value={applicationForm.connectionSource.instance || ''}
                                                    onChange={(e) => setApplicationForm({
                                                        ...applicationForm,
                                                        connectionSource: {...applicationForm.connectionSource, instance: e.target.value}
                                                    })}
                                                    className="form-control rounded-3"
                                                    placeholder={applicationForm.dataSourceType === 'MicrosoftSqlServer' ? 'SQLEXPRESS' : 'ORCL'}
                                                />
                                            </div>
                                        )}
                                        </div>
                                    )}
                                </div>
                                {!submitSuccess && (
                                    <div className="modal-footer border-0 pt-0">
                                    <div className="d-flex gap-2">
                                        <button
                                            onClick={() => testConnection()}
                                            className="btn btn-warning rounded-3 fw-semibold d-flex align-items-center"
                                            disabled={testingConnection || isSubmitting}
                                        >
                                            {testingConnection ? (
                                                <>
                                                    <Loader className="me-2 animate-spin" size={16} />
                                                    Testing Connection...
                                                </>
                                            ) : (
                                                <>
                                                    <TestTube className="me-2" size={16} />
                                                    Test Connection
                                                </>
                                            )}
                                        </button>
                                        <button
                                            onClick={handleApplicationSubmit}
                                            className="btn btn-success rounded-3 fw-semibold d-flex align-items-center"
                                            disabled={isSubmitting || testingConnection}
                                        >
                                            {isSubmitting ? (
                                                <>
                                                    <Loader className="me-2 animate-spin" size={16} />
                                                    {editingApp ? 'Updating...' : 'Creating...'}
                                                </>
                                            ) : (
                                                editingApp ? 'Update Application' : 'Create Application'
                                            )}
                                        </button>
                                    </div>
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>
                )}

                {/* Connection Test Result Toast */}
                {connectionTestResult && (
                    <div className="position-fixed top-0 end-0 p-3" style={{ zIndex: 1055 }}>
                        <div className={`alert ${connectionTestResult.success ? 'alert-success' : 'alert-danger'} d-flex align-items-center shadow-lg`} role="alert">
                            <div className="me-2">
                                {connectionTestResult.success ? '✅' : '❌'}
                            </div>
                            <div className="flex-grow-1">
                                <strong>Connection Test {connectionTestResult.success ? 'Successful' : 'Failed'}</strong>
                                <br />
                                {connectionTestResult.message}
                            </div>
                            <button 
                                type="button" 
                                className="btn-close" 
                                onClick={() => setConnectionTestResult(null)}
                                aria-label="Close"
                            ></button>
                        </div>
                    </div>
                )}

                {/* Delete Confirmation Modal */}
                {showDeleteConfirm && deletingApp && (
                    <div className="modal d-block" style={{backgroundColor: 'rgba(0,0,0,0.5)', position: 'fixed', top: 0, left: 0, width: '100%', height: '100%', zIndex: 1050}}>
                        <div className="modal-dialog modal-dialog-centered">
                            <div className="modal-content border-0 rounded-4">
                                <div className="modal-header border-0 pb-0">
                                    <h3 className="modal-title fw-bold text-danger">
                                        {deleteSuccess ? 'Application Deleted' : 'Confirm Deletion'}
                                    </h3>
                                    <button
                                        type="button"
                                        className="btn-close"
                                        onClick={() => {
                                            setShowDeleteConfirm(false);
                                            setDeletingApp(null);
                                            setDeleteSuccess(false);
                                        }}
                                    ></button>
                                </div>
                                <div className="modal-body">
                                    {deleteSuccess ? (
                                        <div className="text-center py-4">
                                            <CheckCircle2 size={64} className="text-success mb-3" />
                                            <h4 className="text-success fw-bold">Application Deleted Successfully!</h4>
                                            <p className="text-muted">
                                                <strong>{deletingApp.applicationName}</strong> has been permanently removed.
                                            </p>
                                        </div>
                                    ) : (
                                        <div className="text-center py-4">
                                            <AlertTriangle size={64} className="text-warning mb-3" />
                                            <h4 className="fw-bold">Are you sure you want to delete this application?</h4>
                                            <div className="bg-light rounded-3 p-3 my-3">
                                                <h5 className="fw-bold text-dark mb-1">{deletingApp.applicationName}</h5>
                                                <p className="text-muted small mb-0">{deletingApp.applicationDescription}</p>
                                            </div>
                                            <p className="text-danger small fw-semibold">
                                                ⚠️ This action cannot be undone. All associated data will be permanently removed.
                                            </p>
                                        </div>
                                    )}
                                </div>
                                {!deleteSuccess && (
                                    <div className="modal-footer border-0 pt-0">
                                        <button
                                            onClick={() => {
                                                setShowDeleteConfirm(false);
                                                setDeletingApp(null);
                                            }}
                                            className="btn btn-secondary rounded-3 fw-semibold"
                                        >
                                            Cancel
                                        </button>
                                        <button
                                            onClick={deleteApplication}
                                            className="btn btn-danger rounded-3 fw-semibold d-flex align-items-center"
                                        >
                                            <Trash2 className="me-2" size={16} />
                                            Delete Application
                                        </button>
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default Applications;