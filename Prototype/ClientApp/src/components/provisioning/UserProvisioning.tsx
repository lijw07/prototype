import React, { useState, useEffect } from 'react';
import { 
  Users, 
  UserPlus, 
  Clock, 
  CheckCircle,
  AlertCircle,
  Settings,
  Zap,
  FileText,
  Upload,
  Download,
  RefreshCw,
  ArrowUpRight,
  ArrowDownRight,
  Target,
  TrendingUp,
  BarChart3,
  Database,
  FileCode,
  FileSpreadsheet,
  Eye,
  Trash2,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  Archive,
  Save,
  FolderOpen
} from 'lucide-react';
import { userProvisioningApi } from '../../services/api';

interface ProvisioningOverview {
  summary: {
    totalUsers: number;
    pendingUsers: number;
    recentlyProvisioned: number;
    provisioningEfficiency: number;
  };
  userMetrics: {
    total: number;
    verified: number;
    pending: number;
    accessGranted: number;
    rolesAssigned: number;
  };
  applicationAccess: {
    totalApplications: number;
    usersWithAccess: number;
    averageAppsPerUser: number;
    accessCoverage: number;
  };
  efficiency: {
    avgProvisioningTime: number;
    autoProvisioningRate: number;
    pendingBacklog: number;
    throughput: number;
  };
}

interface PendingRequest {
  temporaryUserId: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  role: string;
  createdAt: string;
  daysWaiting: number;
  priority: string;
}

export default function UserProvisioning() {
  const [overview, setOverview] = useState<ProvisioningOverview | null>(null);
  const [pendingRequests, setPendingRequests] = useState<PendingRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('overview');
  const [lastUpdated, setLastUpdated] = useState<Date>(new Date());
  
  // Bulk migration state
  const [uploadFormat, setUploadFormat] = useState<'csv' | 'json' | 'xml'>('csv');
  const [uploadedFile, setUploadedFile] = useState<File | null>(null);
  const [allMigrationData, setAllMigrationData] = useState<any[]>([]);
  const [showPreview, setShowPreview] = useState(false);
  const [migrationProgress, setMigrationProgress] = useState<number>(0);
  const [migrationStatus, setMigrationStatus] = useState<'idle' | 'processing' | 'completed' | 'error'>('idle');
  const [migrationResults, setMigrationResults] = useState<{
    successful: number;
    failed: number;
    errors: string[];
  } | null>(null);
  
  // Pagination state for preview
  const [previewCurrentPage, setPreviewCurrentPage] = useState(1);
  const [previewPageSize, setPreviewPageSize] = useState(10);
  
  // File save state
  const [saveFile, setSaveFile] = useState(true);
  const [savedFileName, setSavedFileName] = useState('');
  const [saveDescription, setSaveDescription] = useState('');
  const [savedFiles, setSavedFiles] = useState<any[]>([]);
  const [showSavedFiles, setShowSavedFiles] = useState(false);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [overviewResponse, requestsResponse] = await Promise.all([
        userProvisioningApi.getProvisioningOverview(),
        userProvisioningApi.getPendingRequests(1, 20)
      ]);

      if (overviewResponse.success) {
        setOverview(overviewResponse.data);
      }
      
      if (requestsResponse.success) {
        setPendingRequests(requestsResponse.data.requests);
      }
      
      setLastUpdated(new Date());
    } catch (error) {
      console.error('Failed to fetch provisioning data:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
    loadSavedFiles();
    loadCurrentSession();
    const interval = setInterval(fetchData, 5 * 60 * 1000); // Refresh every 5 minutes
    return () => clearInterval(interval);
  }, []);

  // Auto-save session when critical state changes
  useEffect(() => {
    if (uploadedFile && allMigrationData.length > 0) {
      const timeoutId = setTimeout(saveCurrentSession, 500); // Save after 500ms delay
      return () => clearTimeout(timeoutId);
    }
  }, [uploadedFile, allMigrationData, migrationStatus, migrationProgress, migrationResults]);

  const handleAutoProvision = async () => {
    try {
      const response = await userProvisioningApi.autoProvisionUsers({
        criteria: { maxDaysWaiting: 7 },
        maxUsers: 10,
        applyDefaultAccess: true
      });
      
      if (response.success) {
        await fetchData();
        alert(`Auto-provisioned ${response.data.processedCount} users`);
      }
    } catch (error) {
      console.error('Auto-provisioning failed:', error);
      alert('Auto-provisioning failed');
    }
  };

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'High': return 'danger';
      case 'Medium': return 'warning';
      default: return 'success';
    }
  };

  const getTrendIcon = (value: number, size: number = 16) => {
    if (value > 0) return <ArrowUpRight className="text-success" size={size} />;
    if (value < 0) return <ArrowDownRight className="text-danger" size={size} />;
    return <div className="text-muted" style={{ width: size, height: size }}>–</div>;
  };

  // Pagination helper functions
  const getPaginatedPreviewData = () => {
    const startIndex = (previewCurrentPage - 1) * previewPageSize;
    const endIndex = startIndex + previewPageSize;
    return allMigrationData.slice(startIndex, endIndex);
  };

  const getTotalPreviewPages = () => {
    return Math.ceil(allMigrationData.length / previewPageSize);
  };

  const getPreviewPaginationInfo = () => {
    const totalRecords = allMigrationData.length;
    const startRecord = (previewCurrentPage - 1) * previewPageSize + 1;
    const endRecord = Math.min(previewCurrentPage * previewPageSize, totalRecords);
    return { startRecord, endRecord, totalRecords };
  };

  // Bulk migration helper functions
  const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      setUploadedFile(file);
      setSavedFileName(file.name.replace(/\.[^/.]+$/, "")); // Remove extension for default name
      parseFileForPreview(file);
    }
  };

  const parseFileForPreview = async (file: File) => {
    try {
      const text = await file.text();
      let parsedData: any[] = [];

      switch (uploadFormat) {
        case 'csv':
          parsedData = parseCSV(text);
          break;
        case 'json':
          parsedData = JSON.parse(text);
          if (!Array.isArray(parsedData)) {
            parsedData = [parsedData];
          }
          break;
        case 'xml':
          parsedData = parseXML(text);
          break;
      }

      setAllMigrationData(parsedData); // Store all data for migration
      setPreviewCurrentPage(1); // Reset to first page
      setShowPreview(true);
      
      // Save current session after successful parsing
      setTimeout(saveCurrentSession, 100); // Small delay to ensure state is updated
    } catch (error) {
      console.error('Error parsing file:', error);
      alert(`Error parsing ${uploadFormat.toUpperCase()} file: ${error}. Please check the format and try again.`);
    }
  };

  const parseCSV = (text: string): any[] => {
    const lines = text.trim().split('\n');
    if (lines.length < 2) return [];
    
    const headers = lines[0].split(',').map(h => h.trim().replace(/"/g, ''));
    const data = [];
    
    for (let i = 1; i < lines.length; i++) {
      const values = lines[i].split(',').map(v => v.trim().replace(/"/g, ''));
      const row: any = {};
      headers.forEach((header, index) => {
        row[header] = values[index] || '';
      });
      data.push(row);
    }
    
    return data;
  };

  const parseXML = (text: string): any[] => {
    // Simple XML parser for user data
    const users: any[] = [];
    const userMatches = text.match(/<user[^>]*>[\s\S]*?<\/user>/g);
    
    if (userMatches) {
      userMatches.forEach(userXml => {
        const user: any = {};
        const fieldMatches = userXml.match(/<(\w+)>([^<]*)<\/\1>/g);
        
        if (fieldMatches) {
          fieldMatches.forEach(field => {
            const match = field.match(/<(\w+)>([^<]*)<\/\1>/);
            if (match) {
              user[match[1]] = match[2];
            }
          });
        }
        
        users.push(user);
      });
    }
    
    return users;
  };

  const processBulkMigration = async () => {
    if (!uploadedFile || allMigrationData.length === 0) return;

    setMigrationStatus('processing');
    setMigrationProgress(0);

    try {
      const totalUsers = allMigrationData.length;
      let successful = 0;
      let failed = 0;
      const errors: string[] = [];

      // First, try to use the bulk provisioning API
      try {
        setMigrationProgress(10);
        
        // Create FormData to send the file
        const formData = new FormData();
        formData.append('file', uploadedFile);
        formData.append('ignoreErrors', 'false');

        const bulkResponse = await userProvisioningApi.bulkProvisionUsers(formData);
        setMigrationProgress(90);

        if (bulkResponse.success && bulkResponse.data) {
          // Handle bulk response - map to expected format
          successful = bulkResponse.data.processedRecords || 0;
          failed = bulkResponse.data.failedRecords || 0;
          if (bulkResponse.data.errors && Array.isArray(bulkResponse.data.errors)) {
            errors.push(...bulkResponse.data.errors);
          }
          setMigrationProgress(100);
        } else {
          throw new Error('Bulk API failed, falling back to individual processing');
        }
      } catch (bulkError) {
        console.warn('Bulk API not available or failed, processing individually:', bulkError);
        
        // Fallback: Process users individually using registration API
        for (let i = 0; i < totalUsers; i++) {
          const user = allMigrationData[i];
          
          try {
            // Validate required fields
            if (!user.email || !user.firstName || !user.lastName) {
              throw new Error(`Missing required fields: email, firstName, or lastName`);
            }

            // Map user data to registration format
            const registrationData = {
              firstName: user.firstName,
              lastName: user.lastName,
              username: user.username || user.email,
              email: user.email,
              phoneNumber: user.phone || user.phoneNumber || '',
              password: user.password || 'TempPassword123!', // Default temp password
              reEnterPassword: user.password || 'TempPassword123!',
              role: user.role || 'User',
              department: user.department || '',
              isTemporary: true // Mark as temporary for bulk imports
            };

            // Call the registration API
            const response = await userProvisioningApi.autoProvisionUsers({
              user: registrationData,
              autoApprove: true
            });

            if (response.success) {
              successful++;
            } else {
              failed++;
              errors.push(`Failed to create user ${user.email}: ${response.data?.message || 'Unknown error'}`);
            }
          } catch (error: any) {
            failed++;
            errors.push(`Error processing user ${user.email}: ${error.message}`);
          }
          
          // Update progress
          setMigrationProgress(((i + 1) / totalUsers) * 100);
          
          // Small delay to prevent overwhelming the API
          await new Promise(resolve => setTimeout(resolve, 50));
        }
      }

      setMigrationResults({ successful, failed, errors });
      setMigrationStatus('completed');
      
      // Save session after migration completion
      setTimeout(saveCurrentSession, 100);
    } catch (error) {
      console.error('Migration failed:', error);
      setMigrationStatus('error');
      setMigrationResults({
        successful: 0,
        failed: allMigrationData.length,
        errors: [`Migration failed: ${error instanceof Error ? error.message : 'Unknown error'}`]
      });
      setTimeout(saveCurrentSession, 100);
    }
  };

  const resetMigration = () => {
    setUploadedFile(null);
    setAllMigrationData([]);
    setShowPreview(false);
    setMigrationProgress(0);
    setMigrationStatus('idle');
    setMigrationResults(null);
    setPreviewCurrentPage(1);
    setSavedFileName('');
    setSaveDescription('');
    clearCurrentSession(); // Clear the saved session when resetting
  };

  const saveUploadedFile = async () => {
    if (!uploadedFile || !savedFileName.trim()) return;

    try {
      // In a real implementation, this would save to a backend API
      const fileData = {
        id: Date.now().toString(),
        name: savedFileName.trim(),
        description: saveDescription.trim(),
        originalFileName: uploadedFile.name,
        format: uploadFormat,
        size: uploadedFile.size,
        recordCount: allMigrationData.length,
        uploadDate: new Date().toISOString(),
        data: allMigrationData
      };

      // For demo purposes, save to localStorage
      const existingSavedFiles = JSON.parse(localStorage.getItem('savedMigrationFiles') || '[]');
      existingSavedFiles.push(fileData);
      localStorage.setItem('savedMigrationFiles', JSON.stringify(existingSavedFiles));
      
      setSavedFiles(existingSavedFiles);
      alert(`File "${savedFileName}" saved successfully!`);
    } catch (error) {
      console.error('Error saving file:', error);
      alert('Error saving file. Please try again.');
    }
  };

  const loadSavedFiles = () => {
    try {
      const savedFilesData = JSON.parse(localStorage.getItem('savedMigrationFiles') || '[]');
      setSavedFiles(savedFilesData);
    } catch (error) {
      console.error('Error loading saved files:', error);
      setSavedFiles([]);
    }
  };

  const loadSavedFile = (savedFile: any) => {
    setAllMigrationData(savedFile.data);
    setUploadFormat(savedFile.format);
    setPreviewCurrentPage(1);
    setShowPreview(true);
    setSavedFileName(savedFile.name);
    setSaveDescription(savedFile.description);
    
    // Create a mock file object for display
    const mockFile = new File([''], savedFile.originalFileName, { type: 'text/plain' });
    setUploadedFile(mockFile);
    
    alert(`Loaded "${savedFile.name}" with ${savedFile.recordCount} records`);
  };

  const deleteSavedFile = (fileId: string) => {
    try {
      const existingSavedFiles = JSON.parse(localStorage.getItem('savedMigrationFiles') || '[]');
      const updatedFiles = existingSavedFiles.filter((file: any) => file.id !== fileId);
      localStorage.setItem('savedMigrationFiles', JSON.stringify(updatedFiles));
      setSavedFiles(updatedFiles);
      alert('File deleted successfully!');
    } catch (error) {
      console.error('Error deleting file:', error);
      alert('Error deleting file. Please try again.');
    }
  };

  const saveCurrentSession = () => {
    try {
      if (uploadedFile && allMigrationData.length > 0) {
        const sessionData = {
          uploadFormat,
          fileName: uploadedFile.name,
          fileSize: uploadedFile.size,
          allMigrationData,
          showPreview,
          savedFileName,
          saveDescription,
          previewCurrentPage,
          previewPageSize,
          migrationStatus,
          migrationProgress,
          migrationResults,
          timestamp: new Date().toISOString()
        };
        localStorage.setItem('currentMigrationSession', JSON.stringify(sessionData));
        console.log('Session saved:', {
          fileName: uploadedFile.name,
          recordCount: allMigrationData.length,
          status: migrationStatus
        });
      } else {
        console.log('Session not saved - missing requirements:', {
          hasFile: !!uploadedFile,
          hasData: allMigrationData.length > 0
        });
      }
    } catch (error) {
      console.error('Error saving current session:', error);
    }
  };

  const loadCurrentSession = () => {
    try {
      const sessionData = localStorage.getItem('currentMigrationSession');
      console.log('Attempting to load session...', !!sessionData);
      
      if (sessionData) {
        const session = JSON.parse(sessionData);
        console.log('Session found:', {
          fileName: session.fileName,
          recordCount: session.allMigrationData?.length,
          timestamp: session.timestamp
        });
        
        // Check if session is less than 24 hours old
        const sessionAge = new Date().getTime() - new Date(session.timestamp).getTime();
        const twentyFourHours = 24 * 60 * 60 * 1000;
        
        console.log('Session age check:', {
          ageHours: Math.round(sessionAge / (60 * 60 * 1000)),
          isValid: sessionAge < twentyFourHours,
          hasData: session.allMigrationData?.length > 0
        });
        
        if (sessionAge < twentyFourHours && session.allMigrationData?.length > 0) {
          // Restore session data
          setUploadFormat(session.uploadFormat);
          setAllMigrationData(session.allMigrationData);
          setShowPreview(session.showPreview);
          setSavedFileName(session.savedFileName || '');
          setSaveDescription(session.saveDescription || '');
          setPreviewCurrentPage(session.previewCurrentPage || 1);
          setPreviewPageSize(session.previewPageSize || 10);
          setMigrationStatus(session.migrationStatus || 'idle');
          setMigrationProgress(session.migrationProgress || 0);
          setMigrationResults(session.migrationResults || null);
          
          // Create a mock file object for display
          if (session.fileName) {
            const mockFile = new File([''], session.fileName, { type: 'text/plain' });
            setUploadedFile(mockFile);
          }
          
          // Show user that session was restored
          console.log(`Session restored successfully: ${session.fileName} with ${session.allMigrationData.length} records`);
        } else {
          // Clear old session
          clearCurrentSession();
        }
      }
    } catch (error) {
      console.error('Error loading current session:', error);
      clearCurrentSession();
    }
  };

  const clearCurrentSession = () => {
    try {
      localStorage.removeItem('currentMigrationSession');
    } catch (error) {
      console.error('Error clearing current session:', error);
    }
  };

  const downloadTemplate = (format: 'csv' | 'json' | 'xml') => {
    let content = '';
    let filename = '';
    let mimeType = '';

    const sampleData = {
      firstName: 'John',
      lastName: 'Doe',
      email: 'john.doe@company.com',
      username: 'jdoe',
      role: 'User',
      department: 'Engineering',
      phone: '+1-555-0123'
    };

    switch (format) {
      case 'csv':
        content = 'firstName,lastName,email,username,role,department,phone\n';
        content += `${sampleData.firstName},${sampleData.lastName},${sampleData.email},${sampleData.username},${sampleData.role},${sampleData.department},${sampleData.phone}`;
        filename = 'user-import-template.csv';
        mimeType = 'text/csv';
        break;
      case 'json':
        content = JSON.stringify([sampleData], null, 2);
        filename = 'user-import-template.json';
        mimeType = 'application/json';
        break;
      case 'xml':
        content = `<?xml version="1.0" encoding="UTF-8"?>
<users>
  <user>
    <firstName>${sampleData.firstName}</firstName>
    <lastName>${sampleData.lastName}</lastName>
    <email>${sampleData.email}</email>
    <username>${sampleData.username}</username>
    <role>${sampleData.role}</role>
    <department>${sampleData.department}</department>
    <phone>${sampleData.phone}</phone>
  </user>
</users>`;
        filename = 'user-import-template.xml';
        mimeType = 'application/xml';
        break;
    }

    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  if (loading && !overview) {
    return (
      <div className="min-vh-100 bg-light">
        <div className="container-fluid py-4">
          <div className="d-flex justify-content-center align-items-center" style={{ height: '50vh' }}>
            <div className="text-center">
              <div className="spinner-border text-primary mb-3" role="status">
                <span className="visually-hidden">Loading...</span>
              </div>
              <h5>Loading User Provisioning...</h5>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-vh-100 bg-light">
      <div className="container-fluid py-4">
        {/* Header */}
        <div className="row mb-4">
          <div className="col-12">
            <div className="d-flex justify-content-between align-items-center">
              <div>
                <h1 className="display-6 fw-bold mb-2 d-flex align-items-center">
                  <UserPlus className="text-primary me-3" size={32} />
                  User Provisioning
                </h1>
                <p className="text-muted mb-0">
                  Automated user lifecycle management and access provisioning
                </p>
              </div>
              <div className="text-end">
                <button 
                  onClick={fetchData}
                  className="btn btn-outline-primary btn-sm d-flex align-items-center me-2"
                  disabled={loading}
                >
                  <RefreshCw className={`me-2 ${loading ? 'rotating' : ''}`} size={16} />
                  Refresh
                </button>
                <small className="text-muted d-block mt-1">
                  Last updated: {lastUpdated.toLocaleTimeString()}
                </small>
              </div>
            </div>
          </div>
        </div>

        {/* Navigation Tabs */}
        <div className="row mb-4">
          <div className="col-12">
            <ul className="nav nav-pills nav-fill bg-white rounded-4 shadow-sm p-2">
              <li className="nav-item">
                <button
                  className={`nav-link ${activeTab === 'overview' ? 'active' : ''}`}
                  onClick={() => setActiveTab('overview')}
                >
                  <BarChart3 size={16} className="me-2" />
                  Overview
                </button>
              </li>
              <li className="nav-item">
                <button
                  className={`nav-link ${activeTab === 'requests' ? 'active' : ''}`}
                  onClick={() => setActiveTab('requests')}
                >
                  <Clock size={16} className="me-2" />
                  Pending Requests
                </button>
              </li>
              <li className="nav-item">
                <button
                  className={`nav-link ${activeTab === 'automation' ? 'active' : ''}`}
                  onClick={() => setActiveTab('automation')}
                >
                  <Zap size={16} className="me-2" />
                  Automation
                </button>
              </li>
              <li className="nav-item">
                <button
                  className={`nav-link ${activeTab === 'bulk' ? 'active' : ''}`}
                  onClick={() => setActiveTab('bulk')}
                >
                  <Upload size={16} className="me-2" />
                  Bulk Operations
                </button>
              </li>
            </ul>
          </div>
        </div>

        {overview && (
          <>
            {/* Overview Tab */}
            {activeTab === 'overview' && (
              <>
                {/* Summary Cards */}
                <div className="row g-4 mb-4">
                  <div className="col-lg-3 col-md-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4 text-center">
                        <Users className="text-primary mb-3" size={40} />
                        <h2 className="fw-bold mb-1">{overview.summary.totalUsers}</h2>
                        <h6 className="text-muted mb-2">Total Users</h6>
                        <div className="d-flex align-items-center justify-content-center">
                          {getTrendIcon(5)}
                          <span className="small ms-1 text-success">5% growth</span>
                        </div>
                      </div>
                    </div>
                  </div>
                  
                  <div className="col-lg-3 col-md-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4 text-center">
                        <Clock className="text-warning mb-3" size={40} />
                        <h2 className="fw-bold mb-1">{overview.summary.pendingUsers}</h2>
                        <h6 className="text-muted mb-2">Pending Users</h6>
                        <div className="small text-warning">
                          Awaiting provisioning
                        </div>
                      </div>
                    </div>
                  </div>
                  
                  <div className="col-lg-3 col-md-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4 text-center">
                        <CheckCircle className="text-success mb-3" size={40} />
                        <h2 className="fw-bold mb-1">{overview.summary.recentlyProvisioned}</h2>
                        <h6 className="text-muted mb-2">Recent Provisions</h6>
                        <div className="small text-muted">Last 7 days</div>
                      </div>
                    </div>
                  </div>
                  
                  <div className="col-lg-3 col-md-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4 text-center">
                        <Target className="text-info mb-3" size={40} />
                        <h2 className="fw-bold mb-1">{overview.summary.provisioningEfficiency}%</h2>
                        <h6 className="text-muted mb-2">Automation Rate</h6>
                        <div className="small text-info">Efficiency score</div>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Detailed Metrics */}
                <div className="row g-4 mb-4">
                  <div className="col-lg-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4">
                        <div className="d-flex align-items-center mb-4">
                          <Users className="text-primary me-3" size={24} />
                          <h5 className="card-title fw-bold mb-0">User Metrics</h5>
                        </div>
                        
                        <div className="row g-3">
                          <div className="col-6">
                            <div className="text-center p-3 bg-light rounded-3">
                              <div className="h4 fw-bold text-primary mb-1">
                                {overview.userMetrics.verified}
                              </div>
                              <div className="small text-muted">Verified Users</div>
                            </div>
                          </div>
                          
                          <div className="col-6">
                            <div className="text-center p-3 bg-light rounded-3">
                              <div className="h4 fw-bold text-success mb-1">
                                {overview.userMetrics.accessGranted}
                              </div>
                              <div className="small text-muted">With Access</div>
                            </div>
                          </div>
                          
                          <div className="col-6">
                            <div className="text-center p-3 bg-light rounded-3">
                              <div className="h4 fw-bold text-info mb-1">
                                {overview.userMetrics.rolesAssigned}
                              </div>
                              <div className="small text-muted">Roles Assigned</div>
                            </div>
                          </div>
                          
                          <div className="col-6">
                            <div className="text-center p-3 bg-light rounded-3">
                              <div className="h4 fw-bold text-warning mb-1">
                                {overview.userMetrics.pending}
                              </div>
                              <div className="small text-muted">Pending</div>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="col-lg-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4">
                        <div className="d-flex align-items-center mb-4">
                          <TrendingUp className="text-primary me-3" size={24} />
                          <h5 className="card-title fw-bold mb-0">Efficiency Metrics</h5>
                        </div>
                        
                        <div className="space-y-3">
                          <div className="d-flex justify-content-between align-items-center py-2">
                            <span className="fw-semibold">Avg Provisioning Time</span>
                            <span className="fw-bold text-primary">
                              {overview.efficiency.avgProvisioningTime}h
                            </span>
                          </div>
                          
                          <div className="d-flex justify-content-between align-items-center py-2">
                            <span className="fw-semibold">Auto-Provisioning Rate</span>
                            <span className="fw-bold text-success">
                              {overview.efficiency.autoProvisioningRate}%
                            </span>
                          </div>
                          
                          <div className="d-flex justify-content-between align-items-center py-2">
                            <span className="fw-semibold">Pending Backlog</span>
                            <span className="fw-bold text-warning">
                              {overview.efficiency.pendingBacklog}
                            </span>
                          </div>
                          
                          <div className="d-flex justify-content-between align-items-center py-2">
                            <span className="fw-semibold">Weekly Throughput</span>
                            <span className="fw-bold text-info">
                              {overview.efficiency.throughput}
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </>
            )}

            {/* Pending Requests Tab */}
            {activeTab === 'requests' && (
              <div className="row">
                <div className="col-12">
                  <div className="card border-0 rounded-4 shadow-sm">
                    <div className="card-header bg-transparent border-0 p-4">
                      <div className="d-flex justify-content-between align-items-center">
                        <h5 className="fw-bold mb-0">Pending Provisioning Requests</h5>
                        <button 
                          onClick={handleAutoProvision}
                          className="btn btn-primary btn-sm d-flex align-items-center"
                        >
                          <Zap className="me-2" size={16} />
                          Auto-Provision Eligible
                        </button>
                      </div>
                    </div>
                    <div className="card-body p-0">
                      <div className="table-responsive">
                        <table className="table table-hover mb-0">
                          <thead className="bg-light">
                            <tr>
                              <th className="border-0 px-4 py-3">User</th>
                              <th className="border-0 px-4 py-3">Role</th>
                              <th className="border-0 px-4 py-3">Days Waiting</th>
                              <th className="border-0 px-4 py-3">Priority</th>
                              <th className="border-0 px-4 py-3">Actions</th>
                            </tr>
                          </thead>
                          <tbody>
                            {pendingRequests.map((request) => (
                              <tr key={request.temporaryUserId}>
                                <td className="px-4 py-3">
                                  <div>
                                    <div className="fw-semibold">
                                      {request.firstName} {request.lastName}
                                    </div>
                                    <div className="small text-muted">
                                      {request.email}
                                    </div>
                                  </div>
                                </td>
                                <td className="px-4 py-3">
                                  <span className="badge bg-info bg-opacity-10 text-info">
                                    {request.role}
                                  </span>
                                </td>
                                <td className="px-4 py-3">
                                  <span className={`fw-semibold ${request.daysWaiting > 7 ? 'text-danger' : request.daysWaiting > 3 ? 'text-warning' : 'text-success'}`}>
                                    {request.daysWaiting} days
                                  </span>
                                </td>
                                <td className="px-4 py-3">
                                  <span className={`badge bg-${getPriorityColor(request.priority)} bg-opacity-10 text-${getPriorityColor(request.priority)}`}>
                                    {request.priority}
                                  </span>
                                </td>
                                <td className="px-4 py-3">
                                  <div className="d-flex gap-2">
                                    <button className="btn btn-sm btn-outline-success">
                                      <CheckCircle size={14} />
                                    </button>
                                    <button className="btn btn-sm btn-outline-secondary">
                                      <Settings size={14} />
                                    </button>
                                  </div>
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Automation Tab */}
            {activeTab === 'automation' && (
              <div className="row g-4">
                <div className="col-lg-8">
                  <div className="card border-0 rounded-4 shadow-sm h-100">
                    <div className="card-body p-4">
                      <div className="d-flex align-items-center mb-4">
                        <Zap className="text-primary me-3" size={24} />
                        <h5 className="card-title fw-bold mb-0">Automation Rules</h5>
                      </div>
                      
                      <div className="alert alert-info">
                        <AlertCircle className="me-2" size={16} />
                        Auto-provisioning rules help streamline user onboarding based on predefined criteria.
                      </div>

                      <div className="space-y-3">
                        <div className="border rounded-3 p-3">
                          <div className="d-flex justify-content-between align-items-center">
                            <div>
                              <div className="fw-semibold">Standard User Auto-Provision</div>
                              <div className="small text-muted">
                                Automatically provision users with 'User' role after 24 hours
                              </div>
                            </div>
                            <div className="form-check form-switch">
                              <input className="form-check-input" type="checkbox" defaultChecked />
                            </div>
                          </div>
                        </div>

                        <div className="border rounded-3 p-3">
                          <div className="d-flex justify-content-between align-items-center">
                            <div>
                              <div className="fw-semibold">Priority Role Fast-Track</div>
                              <div className="small text-muted">
                                Immediately provision Admin and Manager roles
                              </div>
                            </div>
                            <div className="form-check form-switch">
                              <input className="form-check-input" type="checkbox" defaultChecked />
                            </div>
                          </div>
                        </div>

                        <div className="border rounded-3 p-3">
                          <div className="d-flex justify-content-between align-items-center">
                            <div>
                              <div className="fw-semibold">Department-Based Access</div>
                              <div className="small text-muted">
                                Grant application access based on user department
                              </div>
                            </div>
                            <div className="form-check form-switch">
                              <input className="form-check-input" type="checkbox" />
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="col-lg-4">
                  <div className="card border-0 rounded-4 shadow-sm h-100">
                    <div className="card-body p-4">
                      <div className="d-flex align-items-center mb-4">
                        <Target className="text-primary me-3" size={24} />
                        <h5 className="card-title fw-bold mb-0">Quick Actions</h5>
                      </div>
                      
                      <div className="d-grid gap-3">
                        <button 
                          onClick={handleAutoProvision}
                          className="btn btn-primary d-flex align-items-center justify-content-center"
                        >
                          <Zap className="me-2" size={16} />
                          Run Auto-Provision
                        </button>
                        
                        <button className="btn btn-outline-secondary d-flex align-items-center justify-content-center">
                          <Settings className="me-2" size={16} />
                          Configure Rules
                        </button>
                        
                        <button className="btn btn-outline-info d-flex align-items-center justify-content-center">
                          <FileText className="me-2" size={16} />
                          View Templates
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Bulk Operations Tab */}
            {activeTab === 'bulk' && (
              <div className="row g-4">
                {/* Format Selection and Upload */}
                <div className="col-12">
                  <div className="card border-0 rounded-4 shadow-sm">
                    <div className="card-body p-4">
                      <div className="d-flex align-items-center justify-content-between mb-4">
                        <div className="d-flex align-items-center">
                          <Database className="text-primary me-3" size={24} />
                          <h5 className="card-title fw-bold mb-0">Bulk User Migration & Data Import</h5>
                        </div>
                        {migrationStatus !== 'idle' && (
                          <button onClick={resetMigration} className="btn btn-outline-secondary btn-sm">
                            <RefreshCw size={16} className="me-2" />
                            Reset
                          </button>
                        )}
                      </div>
                      
                      {/* Format Selection */}
                      <div className="row g-4 mb-4">
                        <div className="col-12">
                          <h6 className="fw-semibold mb-3">Select Data Format</h6>
                          <div className="row g-3">
                            <div className="col-md-4">
                              <div 
                                className={`border rounded-3 p-3 text-center cursor-pointer ${uploadFormat === 'csv' ? 'border-primary bg-primary bg-opacity-10' : ''}`}
                                onClick={() => setUploadFormat('csv')}
                                style={{ cursor: 'pointer' }}
                              >
                                <FileSpreadsheet className={`mb-2 ${uploadFormat === 'csv' ? 'text-primary' : 'text-muted'}`} size={32} />
                                <h6 className="fw-semibold mb-1">CSV Format</h6>
                                <p className="small text-muted mb-0">Comma-separated values</p>
                              </div>
                            </div>
                            <div className="col-md-4">
                              <div 
                                className={`border rounded-3 p-3 text-center cursor-pointer ${uploadFormat === 'json' ? 'border-primary bg-primary bg-opacity-10' : ''}`}
                                onClick={() => setUploadFormat('json')}
                                style={{ cursor: 'pointer' }}
                              >
                                <FileCode className={`mb-2 ${uploadFormat === 'json' ? 'text-primary' : 'text-muted'}`} size={32} />
                                <h6 className="fw-semibold mb-1">JSON Format</h6>
                                <p className="small text-muted mb-0">JavaScript Object Notation</p>
                              </div>
                            </div>
                            <div className="col-md-4">
                              <div 
                                className={`border rounded-3 p-3 text-center cursor-pointer ${uploadFormat === 'xml' ? 'border-primary bg-primary bg-opacity-10' : ''}`}
                                onClick={() => setUploadFormat('xml')}
                                style={{ cursor: 'pointer' }}
                              >
                                <FileText className={`mb-2 ${uploadFormat === 'xml' ? 'text-primary' : 'text-muted'}`} size={32} />
                                <h6 className="fw-semibold mb-1">XML Format</h6>
                                <p className="small text-muted mb-0">Extensible Markup Language</p>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>

                      {/* Template Download */}
                      <div className="row g-4 mb-4">
                        <div className="col-12">
                          <div className="alert alert-info">
                            <Download className="me-2" size={16} />
                            Download a sample template to understand the required format for your data migration.
                          </div>
                          <div className="d-flex gap-2 flex-wrap">
                            <button 
                              onClick={() => downloadTemplate('csv')}
                              className="btn btn-outline-primary btn-sm"
                            >
                              <FileSpreadsheet size={16} className="me-2" />
                              Download CSV Template
                            </button>
                            <button 
                              onClick={() => downloadTemplate('json')}
                              className="btn btn-outline-primary btn-sm"
                            >
                              <FileCode size={16} className="me-2" />
                              Download JSON Template
                            </button>
                            <button 
                              onClick={() => downloadTemplate('xml')}
                              className="btn btn-outline-primary btn-sm"
                            >
                              <FileText size={16} className="me-2" />
                              Download XML Template
                            </button>
                            <button 
                              onClick={() => setShowSavedFiles(!showSavedFiles)}
                              className="btn btn-outline-success btn-sm"
                            >
                              <Archive size={16} className="me-2" />
                              Saved Files ({savedFiles.length})
                            </button>
                          </div>
                        </div>
                      </div>

                      {/* Saved Files Section */}
                      {showSavedFiles && (
                        <div className="row g-4 mb-4">
                          <div className="col-12">
                            <div className="card border border-success">
                              <div className="card-header bg-success bg-opacity-10">
                                <div className="d-flex align-items-center justify-content-between">
                                  <h6 className="fw-semibold mb-0">
                                    <Archive className="me-2" size={16} />
                                    Saved Migration Files
                                  </h6>
                                  <button 
                                    onClick={() => setShowSavedFiles(false)}
                                    className="btn btn-sm btn-outline-secondary"
                                  >
                                    <Eye size={14} />
                                  </button>
                                </div>
                              </div>
                              <div className="card-body p-0">
                                {savedFiles.length === 0 ? (
                                  <div className="text-center p-4 text-muted">
                                    <Archive size={48} className="mb-3 opacity-50" />
                                    <p className="mb-0">No saved files yet. Upload and save files for easy reuse.</p>
                                  </div>
                                ) : (
                                  <div className="table-responsive">
                                    <table className="table table-hover mb-0">
                                      <thead className="bg-light">
                                        <tr>
                                          <th className="border-0 px-4 py-3">Name</th>
                                          <th className="border-0 px-4 py-3">Format</th>
                                          <th className="border-0 px-4 py-3">Records</th>
                                          <th className="border-0 px-4 py-3">Saved Date</th>
                                          <th className="border-0 px-4 py-3">Actions</th>
                                        </tr>
                                      </thead>
                                      <tbody>
                                        {savedFiles.map((file) => (
                                          <tr key={file.id}>
                                            <td className="px-4 py-3">
                                              <div>
                                                <div className="fw-semibold">{file.name}</div>
                                                {file.description && (
                                                  <div className="small text-muted">{file.description}</div>
                                                )}
                                              </div>
                                            </td>
                                            <td className="px-4 py-3">
                                              <span className={`badge bg-primary bg-opacity-10 text-primary`}>
                                                {file.format.toUpperCase()}
                                              </span>
                                            </td>
                                            <td className="px-4 py-3">
                                              <span className="fw-semibold">{file.recordCount}</span>
                                            </td>
                                            <td className="px-4 py-3">
                                              <div className="small text-muted">
                                                {new Date(file.uploadDate).toLocaleDateString()}
                                              </div>
                                            </td>
                                            <td className="px-4 py-3">
                                              <div className="d-flex gap-1">
                                                <button 
                                                  onClick={() => loadSavedFile(file)}
                                                  className="btn btn-sm btn-outline-primary"
                                                  title="Load this file"
                                                >
                                                  <FolderOpen size={14} />
                                                </button>
                                                <button 
                                                  onClick={() => {
                                                    if (window.confirm(`Delete "${file.name}"?`)) {
                                                      deleteSavedFile(file.id);
                                                    }
                                                  }}
                                                  className="btn btn-sm btn-outline-danger"
                                                  title="Delete this file"
                                                >
                                                  <Trash2 size={14} />
                                                </button>
                                              </div>
                                            </td>
                                          </tr>
                                        ))}
                                      </tbody>
                                    </table>
                                  </div>
                                )}
                              </div>
                            </div>
                          </div>
                        </div>
                      )}

                      {/* File Upload */}
                      {migrationStatus === 'idle' && (
                        <div className="row g-4 mb-4">
                          <div className="col-12">
                            <h6 className="fw-semibold mb-3">Upload Data File</h6>
                            <div className="border border-dashed rounded-3 p-4 text-center">
                              <Upload className="text-muted mb-3" size={48} />
                              <h6 className="fw-semibold mb-2">
                                Upload {uploadFormat.toUpperCase()} File
                              </h6>
                              <p className="text-muted mb-3">
                                Select your {uploadFormat.toUpperCase()} file containing user data for bulk migration
                              </p>
                              <input
                                type="file"
                                accept={`.${uploadFormat}`}
                                onChange={handleFileUpload}
                                className="form-control d-none"
                                id="fileUpload"
                              />
                              <label htmlFor="fileUpload" className="btn btn-primary">
                                Choose {uploadFormat.toUpperCase()} File
                              </label>
                              {uploadedFile && (
                                <div className="mt-3">
                                  <p className="small text-success mb-0">
                                    ✓ File uploaded: {uploadedFile.name}
                                  </p>
                                </div>
                              )}
                            </div>
                          </div>
                        </div>
                      )}

                      {/* File Save Options */}
                      {uploadedFile && allMigrationData.length > 0 && migrationStatus === 'idle' && (
                        <div className="row g-4 mb-4">
                          <div className="col-12">
                            <div className="card border border-info">
                              <div className="card-header bg-info bg-opacity-10">
                                <div className="d-flex align-items-center">
                                  <Save className="me-2" size={16} />
                                  <h6 className="fw-semibold mb-0">Save File Options</h6>
                                </div>
                              </div>
                              <div className="card-body p-4">
                                <div className="form-check mb-3">
                                  <input 
                                    className="form-check-input" 
                                    type="checkbox" 
                                    id="saveFileCheck"
                                    checked={saveFile}
                                    onChange={(e) => setSaveFile(e.target.checked)}
                                  />
                                  <label className="form-check-label fw-semibold" htmlFor="saveFileCheck">
                                    Save this file for future use
                                  </label>
                                  <div className="small text-muted mt-1">
                                    Saved files can be easily reloaded later without re-uploading
                                  </div>
                                </div>
                                
                                {saveFile && (
                                  <div className="row g-3">
                                    <div className="col-md-6">
                                      <label className="form-label fw-semibold">File Name <span className="text-danger">*</span></label>
                                      <input
                                        type="text"
                                        className="form-control"
                                        value={savedFileName}
                                        onChange={(e) => setSavedFileName(e.target.value)}
                                        placeholder="Enter a name for this file"
                                        maxLength={50}
                                      />
                                      <div className="small text-muted mt-1">
                                        {savedFileName.length}/50 characters
                                      </div>
                                    </div>
                                    <div className="col-md-6">
                                      <label className="form-label fw-semibold">Description (Optional)</label>
                                      <textarea
                                        className="form-control"
                                        value={saveDescription}
                                        onChange={(e) => setSaveDescription(e.target.value)}
                                        placeholder="Optional description for this file"
                                        rows={2}
                                        maxLength={200}
                                      />
                                      <div className="small text-muted mt-1">
                                        {saveDescription.length}/200 characters
                                      </div>
                                    </div>
                                    <div className="col-12">
                                      <button 
                                        onClick={saveUploadedFile}
                                        className="btn btn-info btn-sm"
                                        disabled={!savedFileName.trim()}
                                      >
                                        <Save className="me-2" size={16} />
                                        Save File ({allMigrationData.length} records)
                                      </button>
                                    </div>
                                  </div>
                                )}
                              </div>
                            </div>
                          </div>
                        </div>
                      )}

                      {/* Preview Section */}
                      {showPreview && allMigrationData.length > 0 && (
                        <div className="row g-4 mb-4">
                          <div className="col-12">
                            <div className="card border border-primary">
                              <div className="card-header bg-primary bg-opacity-10">
                                <div className="d-flex align-items-center justify-content-between">
                                  <div className="d-flex align-items-center">
                                    <Eye className="me-2" size={16} />
                                    <h6 className="fw-semibold mb-0">Data Preview</h6>
                                  </div>
                                  <div className="d-flex align-items-center gap-3">
                                    <div className="d-flex align-items-center gap-2">
                                      <label className="small fw-semibold mb-0">Rows per page:</label>
                                      <select 
                                        value={previewPageSize} 
                                        onChange={(e) => {
                                          setPreviewPageSize(Number(e.target.value));
                                          setPreviewCurrentPage(1);
                                        }}
                                        className="form-select form-select-sm"
                                        style={{ width: 'auto' }}
                                      >
                                        <option value={5}>5</option>
                                        <option value={10}>10</option>
                                        <option value={25}>25</option>
                                        <option value={50}>50</option>
                                        <option value={100}>100</option>
                                      </select>
                                    </div>
                                    <button onClick={() => setShowPreview(false)} className="btn btn-sm btn-outline-secondary">
                                      <Trash2 size={14} />
                                    </button>
                                  </div>
                                </div>
                              </div>
                              <div className="card-body p-0">
                                <div className="table-responsive">
                                  <table className="table table-sm mb-0">
                                    <thead className="bg-light">
                                      <tr>
                                        <th className="px-3 py-2 border-0" style={{ width: '60px' }}>#</th>
                                        {Object.keys(allMigrationData[0] || {}).map((key) => (
                                          <th key={key} className="px-3 py-2 border-0">
                                            {key}
                                          </th>
                                        ))}
                                      </tr>
                                    </thead>
                                    <tbody>
                                      {getPaginatedPreviewData().map((row, index) => {
                                        const actualIndex = (previewCurrentPage - 1) * previewPageSize + index + 1;
                                        return (
                                          <tr key={index}>
                                            <td className="px-3 py-2 text-muted">
                                              {actualIndex}
                                            </td>
                                            {Object.values(row).map((value: any, idx) => (
                                              <td key={idx} className="px-3 py-2">
                                                {String(value)}
                                              </td>
                                            ))}
                                          </tr>
                                        );
                                      })}
                                    </tbody>
                                  </table>
                                </div>
                              </div>
                              <div className="card-footer bg-light">
                                <div className="d-flex justify-content-between align-items-center">
                                  <div className="d-flex align-items-center gap-4">
                                    <p className="small text-muted mb-0">
                                      Showing {getPreviewPaginationInfo().startRecord} to {getPreviewPaginationInfo().endRecord} of {getPreviewPaginationInfo().totalRecords} records
                                    </p>
                                    
                                    {/* Pagination Controls */}
                                    <div className="d-flex align-items-center gap-1">
                                      <button
                                        onClick={() => setPreviewCurrentPage(1)}
                                        disabled={previewCurrentPage === 1}
                                        className="btn btn-sm btn-outline-secondary"
                                        title="First page"
                                      >
                                        <ChevronsLeft size={14} />
                                      </button>
                                      <button
                                        onClick={() => setPreviewCurrentPage(previewCurrentPage - 1)}
                                        disabled={previewCurrentPage === 1}
                                        className="btn btn-sm btn-outline-secondary"
                                        title="Previous page"
                                      >
                                        <ChevronLeft size={14} />
                                      </button>
                                      
                                      <span className="small text-muted mx-2">
                                        Page {previewCurrentPage} of {getTotalPreviewPages()}
                                      </span>
                                      
                                      <button
                                        onClick={() => setPreviewCurrentPage(previewCurrentPage + 1)}
                                        disabled={previewCurrentPage === getTotalPreviewPages()}
                                        className="btn btn-sm btn-outline-secondary"
                                        title="Next page"
                                      >
                                        <ChevronRight size={14} />
                                      </button>
                                      <button
                                        onClick={() => setPreviewCurrentPage(getTotalPreviewPages())}
                                        disabled={previewCurrentPage === getTotalPreviewPages()}
                                        className="btn btn-sm btn-outline-secondary"
                                        title="Last page"
                                      >
                                        <ChevronsRight size={14} />
                                      </button>
                                    </div>
                                  </div>
                                  
                                  <button 
                                    onClick={processBulkMigration}
                                    className="btn btn-success"
                                    disabled={migrationStatus === 'processing'}
                                  >
                                    <Upload className="me-2" size={16} />
                                    Start Migration ({allMigrationData.length} records)
                                  </button>
                                </div>
                              </div>
                            </div>
                          </div>
                        </div>
                      )}

                      {/* Migration Progress */}
                      {migrationStatus === 'processing' && (
                        <div className="row g-4 mb-4">
                          <div className="col-12">
                            <div className="card border border-warning">
                              <div className="card-body p-4">
                                <h6 className="fw-semibold mb-3">
                                  <RefreshCw className="me-2 rotating" size={16} />
                                  Migration in Progress...
                                </h6>
                                <div className="progress mb-3" style={{ height: '8px' }}>
                                  <div 
                                    className="progress-bar progress-bar-striped progress-bar-animated"
                                    style={{ width: `${migrationProgress}%` }}
                                  ></div>
                                </div>
                                <p className="small text-muted mb-0">
                                  Processing user data... {Math.round(migrationProgress)}% complete
                                </p>
                              </div>
                            </div>
                          </div>
                        </div>
                      )}

                      {/* Migration Results */}
                      {migrationStatus === 'completed' && migrationResults && (
                        <div className="row g-4">
                          <div className="col-12">
                            <div className="card border border-success">
                              <div className="card-header bg-success bg-opacity-10">
                                <h6 className="fw-semibold mb-0">
                                  <CheckCircle className="me-2" size={16} />
                                  Migration Completed
                                </h6>
                              </div>
                              <div className="card-body p-4">
                                <div className="row g-3 mb-3">
                                  <div className="col-md-6">
                                    <div className="text-center p-3 bg-success bg-opacity-10 rounded-3">
                                      <h4 className="fw-bold text-success mb-1">
                                        {migrationResults.successful}
                                      </h4>
                                      <p className="small text-muted mb-0">Successfully Migrated</p>
                                    </div>
                                  </div>
                                  <div className="col-md-6">
                                    <div className="text-center p-3 bg-danger bg-opacity-10 rounded-3">
                                      <h4 className="fw-bold text-danger mb-1">
                                        {migrationResults.failed}
                                      </h4>
                                      <p className="small text-muted mb-0">Failed</p>
                                    </div>
                                  </div>
                                </div>
                                
                                {migrationResults.errors.length > 0 && (
                                  <div className="alert alert-warning">
                                    <h6 className="fw-semibold mb-2">Migration Errors:</h6>
                                    <ul className="mb-0">
                                      {migrationResults.errors.slice(0, 5).map((error, index) => (
                                        <li key={index} className="small">{error}</li>
                                      ))}
                                      {migrationResults.errors.length > 5 && (
                                        <li className="small text-muted">
                                          ... and {migrationResults.errors.length - 5} more errors
                                        </li>
                                      )}
                                    </ul>
                                  </div>
                                )}
                                
                                <div className="d-flex gap-2">
                                  <button onClick={resetMigration} className="btn btn-primary">
                                    Start New Migration
                                  </button>
                                  <button onClick={fetchData} className="btn btn-outline-primary">
                                    Refresh Data
                                  </button>
                                </div>
                              </div>
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      <style>{`
        .rotating {
          animation: spin 1s linear infinite;
        }
        
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
        
        .space-y-3 > * + * {
          margin-top: 0.75rem;
        }
      `}</style>
    </div>
  );
}