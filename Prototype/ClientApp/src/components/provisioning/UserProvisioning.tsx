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
  ChevronsRight
} from 'lucide-react';
import { userProvisioningApi } from '../../services/api';
import { progressService, ProgressUpdate, JobStart, JobComplete, JobError } from '../../services/signalr';

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
  const [uploadedFiles, setUploadedFiles] = useState<File[]>([]);
  const [allMigrationData, setAllMigrationData] = useState<any[]>([]);
  const [fileDataMap, setFileDataMap] = useState<Map<string, any[]>>(new Map());
  const [showPreview, setShowPreview] = useState(false);
  const [migrationProgress, setMigrationProgress] = useState<number>(0);
  const [migrationStatus, setMigrationStatus] = useState<'idle' | 'processing' | 'completed' | 'error'>('idle');
  const [migrationResults, setMigrationResults] = useState<{
    successful: number;
    failed: number;
    errors: string[];
    processedFiles: number;
    totalFiles: number;
  } | null>(null);
  
  // Pagination state for preview
  const [previewCurrentPage, setPreviewCurrentPage] = useState(1);
  const [previewPageSize, setPreviewPageSize] = useState(10);
  
  // Drag and drop state
  const [isDragging, setIsDragging] = useState(false);
  const [dragError, setDragError] = useState<string | null>(null);
  const [dragCounter, setDragCounter] = useState(0);
  
  // SignalR state
  const [currentJobId, setCurrentJobId] = useState<string | null>(null);
  const [progressDetails, setProgressDetails] = useState<ProgressUpdate | null>(null);

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
    const interval = setInterval(fetchData, 5 * 60 * 1000); // Refresh every 5 minutes
    return () => clearInterval(interval);
  }, []);

  // Prevent default drag behavior globally to remove browser drag image
  useEffect(() => {
    const handleDocumentDragOver = (e: DragEvent) => {
      e.preventDefault();
    };
    
    const handleDocumentDrop = (e: DragEvent) => {
      e.preventDefault();
    };

    document.addEventListener('dragover', handleDocumentDragOver);
    document.addEventListener('drop', handleDocumentDrop);

    return () => {
      document.removeEventListener('dragover', handleDocumentDragOver);
      document.removeEventListener('drop', handleDocumentDrop);
    };
  }, []);

  // SignalR event handlers
  useEffect(() => {
    const handleJobStarted = (jobStart: JobStart) => {
      console.log('🎬 SignalR: Job started:', jobStart);
      setCurrentJobId(jobStart.jobId);
      setMigrationStatus('processing');
      setMigrationProgress(10);
    };

    const handleProgressUpdate = (progress: ProgressUpdate) => {
      console.log('📈 SignalR: Progress update:', progress);
      setProgressDetails(progress);
      setMigrationProgress(progress.progressPercentage);
    };

    const handleJobCompleted = (result: JobComplete) => {
      console.log('🎉 SignalR: Job completed:', result);
      
      // Add a delay to ensure users see the progress bar working
      setTimeout(() => {
        setMigrationStatus(result.success ? 'completed' : 'error');
        setMigrationProgress(100);
        
        if (result.success && result.data) {
          setMigrationResults({
            successful: result.data.processedRecords || 0,
            failed: result.data.failedRecords || 0,
            errors: result.data.errors || [],
            processedFiles: result.data.processedFiles || 1,
            totalFiles: result.data.totalFiles || 1
          });
        }
        
        setCurrentJobId(null);
        setProgressDetails(null);
      }, 1500); // Show progress for 1.5 seconds minimum
    };

    const handleJobError = (error: JobError) => {
      console.log('❌ SignalR: Job error:', error);
      setMigrationStatus('error');
      setMigrationResults({
        successful: 0,
        failed: 1,
        errors: [error.error],
        processedFiles: 0,
        totalFiles: 1
      });
      setCurrentJobId(null);
      setProgressDetails(null);
    };

    // Set up event listeners
    progressService.onJobStarted(handleJobStarted);
    progressService.onProgressUpdate(handleProgressUpdate);
    progressService.onJobCompleted(handleJobCompleted);
    progressService.onJobError(handleJobError);

    // Initialize SignalR connection on component mount
    console.log('🔗 Initializing SignalR connection...');
    progressService.ensureConnection().catch(error => {
      console.error('❌ Failed to initialize SignalR connection:', error);
    });

    // Cleanup function
    return () => {
      progressService.offJobStarted(handleJobStarted);
      progressService.offProgressUpdate(handleProgressUpdate);
      progressService.offJobCompleted(handleJobCompleted);
      progressService.offJobError(handleJobError);
      
      // Leave progress group if we have an active job
      if (currentJobId) {
        progressService.leaveProgressGroup(currentJobId);
      }
    };
  }, [currentJobId]);

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
    console.log('handleFileUpload called');
    console.log('Event target:', event.target);
    console.log('Files:', event.target.files);
    const files = Array.from(event.target.files || []);
    console.log('Files array:', files);
    if (files.length > 0) {
      console.log('Processing files...');
      processFiles(files);
    } else {
      console.log('No files selected');
    }
    // Clear the input value so the same file can be selected again
    event.target.value = '';
  };

  const processFiles = async (files: File[]) => {
    console.log('Processing files:', files.map(f => f.name));
    setDragError(null);
    const supportedFormats = ['csv', 'json', 'xml', 'xlsx', 'xls'];
    const validFiles: File[] = [];
    const errors: string[] = [];
    
    // Validate all files first
    for (const file of files) {
      const fileExtension = file.name.split('.').pop()?.toLowerCase();
      console.log(`File: ${file.name}, Extension: ${fileExtension}`);
      if (!supportedFormats.includes(fileExtension || '')) {
        errors.push(`${file.name}: Unsupported format (${fileExtension?.toUpperCase()})`);
      } else {
        validFiles.push(file);
      }
    }
    
    console.log('Valid files:', validFiles.map(f => f.name));
    console.log('Errors during validation:', errors);
    
    if (errors.length > 0) {
      setDragError(`Some files have unsupported formats:\n${errors.join('\n')}`);
    }
    
    if (validFiles.length === 0) {
      console.log('No valid files to process');
      return;
    }
    
    setUploadedFiles(validFiles);
    console.log('Set uploaded files state');
    
    // Process files for preview
    const newFileDataMap = new Map<string, any[]>();
    let allData: any[] = [];
    
    for (const file of validFiles) {
      const fileExtension = file.name.split('.').pop()?.toLowerCase();
      
      // For Excel files, we'll process them on the backend
      if (fileExtension === 'xlsx' || fileExtension === 'xls') {
        console.log(`Skipping client-side parsing for Excel file: ${file.name}`);
        newFileDataMap.set(file.name, []); // Empty data for Excel files
        continue;
      }
      
      // For text-based formats, parse for preview
      try {
        console.log(`Parsing file: ${file.name}`);
        const fileData = await parseFile(file);
        console.log(`Parsed ${fileData.length} records from ${file.name}:`, fileData.slice(0, 2));
        newFileDataMap.set(file.name, fileData);
        allData = [...allData, ...fileData];
      } catch (error) {
        console.error(`Error parsing file ${file.name}:`, error);
        errors.push(`${file.name}: Failed to parse - ${error}`);
      }
    }
    
    console.log('All parsed data:', allData.length, 'records');
    console.log('File data map:', Array.from(newFileDataMap.entries()));
    
    setFileDataMap(newFileDataMap);
    setAllMigrationData(allData);
    setPreviewCurrentPage(1);
    setShowPreview(allData.length > 0);
    
    console.log('Updated state - showPreview:', allData.length > 0, 'allData length:', allData.length);
    console.log('Sample data:', allData.slice(0, 3));
    
    if (errors.length > 0) {
      setDragError(`Some files could not be processed:\n${errors.join('\n')}`);
    }
  };

  const processFile = (file: File) => {
    processFiles([file]);
  };

  // Drag and drop handlers
  const handleDragEnter = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    
    if (e.dataTransfer.types && e.dataTransfer.types.includes('Files')) {
      setDragCounter(prev => prev + 1);
      if (!isDragging) {
        setIsDragging(true);
        setDragError(null);
      }
    }
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    
    setDragCounter(prev => {
      const newCounter = prev - 1;
      if (newCounter <= 0) {
        setIsDragging(false);
        return 0;
      }
      return newCounter;
    });
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.dataTransfer.types && e.dataTransfer.types.includes('Files')) {
      e.dataTransfer.dropEffect = 'copy';
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    
    setIsDragging(false);
    setDragCounter(0);

    const files = Array.from(e.dataTransfer.files);
    if (files.length === 0) return;

    processFiles(files);
  };

  const parseFile = async (file: File): Promise<any[]> => {
    try {
      const text = await file.text();
      let parsedData: any[] = [];
      const fileExtension = file.name.split('.').pop()?.toLowerCase();

      switch (fileExtension) {
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
        default:
          throw new Error(`Unsupported format: ${fileExtension}`);
      }

      return parsedData;
    } catch (error) {
      console.error('Error parsing file:', error);
      throw new Error(`Error parsing ${file.name}: ${error}. Please check the file format and try again.`);
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
    if (uploadedFiles.length === 0 && allMigrationData.length === 0) return;

    console.log('🚀 Starting bulk migration with SignalR tracking...');
    setMigrationStatus('processing');
    setMigrationProgress(0);
    setMigrationResults(null);
    setProgressDetails(null);

    try {
      // Process only the first file for now with SignalR progress tracking
      if (uploadedFiles.length > 0) {
        const file = uploadedFiles[0];
        
        // Show initial progress
        setMigrationProgress(1);
        
        // First connect to SignalR before starting the upload
        console.log('🔗 Connecting to SignalR...');
        try {
          await progressService.ensureConnection();
          console.log('✅ SignalR connected successfully');
          setMigrationProgress(5);
        } catch (signalRError) {
          console.error('❌ Failed to connect to SignalR:', signalRError);
          // Continue without SignalR
        }
        
        // Create FormData to send the file to the progress-enabled endpoint
        const formData = new FormData();
        formData.append('file', file);
        formData.append('ignoreErrors', 'false');

        console.log('📁 Uploading file:', file.name, 'Size:', file.size, 'bytes');
        setMigrationProgress(10);
        
        // Call the SignalR-enabled API endpoint (now returns JobId immediately)
        const response = await userProvisioningApi.bulkProvisionWithProgress(formData);
        console.log('📊 API Response:', response);

        if (response.success && response.data && response.data.JobId) {
          // Get the job ID from the immediate response
          const jobId = response.data.JobId;
          setCurrentJobId(jobId);
          
          console.log('🔗 Joining SignalR group for job:', jobId);
          
          try {
            await progressService.joinProgressGroup(jobId);
            console.log('✅ Successfully joined SignalR progress group for job:', jobId);
            setMigrationProgress(15);
            
            // The progress updates will be handled by SignalR event handlers
            // Backend will start processing in background and send progress updates
            console.log('⏳ Waiting for SignalR progress updates from background processing...');
            
          } catch (signalRError) {
            console.error('❌ Failed to join SignalR progress group:', signalRError);
            // If SignalR fails, we can't get progress updates
            setMigrationStatus('error');
            setMigrationResults({
              successful: 0,
              failed: 1,
              errors: ['Failed to connect to progress tracking'],
              processedFiles: 0,
              totalFiles: 1
            });
          }
        } else {
          throw new Error('Failed to start bulk upload with progress tracking');
        }
      } else {
        throw new Error('No files to process');
      }
    } catch (error) {
      console.error('❌ Error in bulk migration:', error);
      setMigrationStatus('error');
      setMigrationResults({
        successful: 0,
        failed: 1,
        errors: [error instanceof Error ? error.message : 'Unknown error occurred'],
        processedFiles: 0,
        totalFiles: uploadedFiles.length
      });
    }
  };

  // Legacy function for fallback (keeping the original complex logic for reference)
  const processBulkMigrationFallback = async () => {
    if (uploadedFiles.length === 0 && allMigrationData.length === 0) return;

    setMigrationStatus('processing');
    setMigrationProgress(0);

    try {
      const totalFiles = uploadedFiles.length;
      setMigrationProgress(10);

      // Fallback: Process files individually with fake progress
      let successful = 0;
      let failed = 0;
      const errors: string[] = [];
      let processedFiles = 0;

      for (let fileIndex = 0; fileIndex < uploadedFiles.length; fileIndex++) {
        const file = uploadedFiles[fileIndex];
        const fileProgress = (fileIndex / totalFiles) * 80 + 20; // Reserve 20% for initial setup
        
        try {
          setMigrationProgress(fileProgress);
          
          // Create FormData to send the file
          const formData = new FormData();
          formData.append('file', file);
          formData.append('ignoreErrors', 'false');
          formData.append('fileIndex', fileIndex.toString());
          formData.append('totalFiles', totalFiles.toString());

          const bulkResponse = await userProvisioningApi.bulkProvisionUsers(formData);
          console.log('Bulk API Response:', bulkResponse);

          if (bulkResponse.success && bulkResponse.data) {
            // Handle bulk response - map to expected format
            successful += bulkResponse.data.processedRecords || 0;
            failed += bulkResponse.data.failedRecords || 0;
            if (bulkResponse.data.errors && Array.isArray(bulkResponse.data.errors)) {
              errors.push(...bulkResponse.data.errors.map((error: any) => 
                typeof error === 'string' ? `${file.name}: ${error}` : `${file.name}: ${error.errorMessage || 'Unknown error'}`
              ));
            }
            processedFiles++;
          } else {
            throw new Error(`Bulk API failed for ${file.name}, falling back to individual processing`);
          }
        } catch (bulkError) {
          console.warn(`Bulk API not available or failed for ${file.name}, processing individually:`, bulkError);
          
          // Fallback: Process users from this file individually
          const fileData = fileDataMap.get(file.name) || [];
          for (let i = 0; i < fileData.length; i++) {
            const user = fileData[i];
            
            try {
              // Validate required fields - handle both camelCase and PascalCase
              const email = user.email || user.Email;
              const firstName = user.firstName || user.FirstName;
              const lastName = user.lastName || user.LastName;
              
              if (!email || !firstName || !lastName) {
                console.log('User data:', user);
                throw new Error(`Missing required fields: email, firstName, or lastName`);
              }

              // Map user data to registration format
              const registrationData = {
                firstName: firstName,
                lastName: lastName,
                username: user.username || user.Username || email,
                email: email,
                phoneNumber: user.phone || user.phoneNumber || user.Phone || user.PhoneNumber || '',
                password: user.password || user.Password || 'TempPassword123!', // Default temp password
                reEnterPassword: user.password || user.Password || 'TempPassword123!',
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
                errors.push(`${file.name} - Failed to create user ${user.email}: ${response.data?.message || 'Unknown error'}`);
              }
            } catch (error: any) {
              failed++;
              errors.push(`${file.name} - Error processing user ${user.email}: ${error.message}`);
            }
            
            // Update progress for individual processing within file
            const overallProgress = fileProgress + ((i + 1) / fileData.length) * (80 / totalFiles);
            setMigrationProgress(overallProgress);
            
            // Small delay to prevent overwhelming the API
            await new Promise(resolve => setTimeout(resolve, 50));
          }
          
          processedFiles++;
        }
        
        setMigrationProgress(((fileIndex + 1) / totalFiles) * 80 + 20);
      }

      setMigrationResults({ 
        successful, 
        failed, 
        errors, 
        processedFiles, 
        totalFiles 
      });
      setMigrationProgress(100);
      setMigrationStatus('completed');
    } catch (error) {
      console.error('Migration failed:', error);
      setMigrationStatus('error');
      setMigrationResults({
        successful: 0,
        failed: allMigrationData.length,
        errors: [`Migration failed: ${error instanceof Error ? error.message : 'Unknown error'}`],
        processedFiles: 0,
        totalFiles: uploadedFiles.length
      });
    }
  };

  const resetMigration = () => {
    setUploadedFiles([]);
    setAllMigrationData([]);
    setFileDataMap(new Map());
    setShowPreview(false);
    setMigrationProgress(0);
    setMigrationStatus('idle');
    setMigrationResults(null);
    setPreviewCurrentPage(1);
    setIsDragging(false);
    setDragError(null);
    setDragCounter(0);
  };


  const downloadTemplate = (format: 'csv' | 'json' | 'xml') => {
    let content = '';
    let filename = '';
    let mimeType = '';

    const sampleData = {
      UserId: '550e8400-e29b-41d4-a716-446655440000',
      FirstName: 'John',
      LastName: 'Doe',
      Username: 'jdoe',
      Email: 'john.doe@company.com',
      PhoneNumber: '+1-555-0123',
      Password: 'TempPassword123!',
      IsActive: 'true',
      Role: 'User',
      CreatedAt: '2024-06-24T12:00:00Z',
      UpdatedAt: '2024-06-24T12:00:00Z'
    };

    switch (format) {
      case 'csv':
        content = 'UserId,FirstName,LastName,Username,Email,PhoneNumber,Password,IsActive,Role,CreatedAt,UpdatedAt\n';
        content += `${sampleData.UserId},${sampleData.FirstName},${sampleData.LastName},${sampleData.Username},${sampleData.Email},${sampleData.PhoneNumber},${sampleData.Password},${sampleData.IsActive},${sampleData.Role},${sampleData.CreatedAt},${sampleData.UpdatedAt}`;
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
    <UserId>${sampleData.UserId}</UserId>
    <FirstName>${sampleData.FirstName}</FirstName>
    <LastName>${sampleData.LastName}</LastName>
    <Username>${sampleData.Username}</Username>
    <Email>${sampleData.Email}</Email>
    <PhoneNumber>${sampleData.PhoneNumber}</PhoneNumber>
    <Password>${sampleData.Password}</Password>
    <IsActive>${sampleData.IsActive}</IsActive>
    <Role>${sampleData.Role}</Role>
    <CreatedAt>${sampleData.CreatedAt}</CreatedAt>
    <UpdatedAt>${sampleData.UpdatedAt}</UpdatedAt>
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
                {/* Key Metrics Cards */}
                <div className="row g-4 mb-4">
                  <div className="col-lg-4 col-md-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4 text-center">
                        <Clock className="text-warning mb-3" size={40} />
                        <h2 className="fw-bold mb-1">{overview.summary.pendingUsers}</h2>
                        <h6 className="text-muted mb-2">Pending Requests</h6>
                        <div className="small text-warning">
                          Awaiting provisioning
                        </div>
                      </div>
                    </div>
                  </div>
                  
                  <div className="col-lg-4 col-md-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4 text-center">
                        <CheckCircle className="text-success mb-3" size={40} />
                        <h2 className="fw-bold mb-1">{overview.summary.recentlyProvisioned}</h2>
                        <h6 className="text-muted mb-2">Recently Provisioned</h6>
                        <div className="small text-muted">Last 7 days</div>
                      </div>
                    </div>
                  </div>
                  
                  <div className="col-lg-4 col-md-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4 text-center">
                        <Target className="text-info mb-3" size={40} />
                        <h2 className="fw-bold mb-1">{overview.efficiency.autoProvisioningRate}%</h2>
                        <h6 className="text-muted mb-2">Automation Rate</h6>
                        <div className="small text-info">Auto-provisioning efficiency</div>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Detailed Metrics */}
                <div className="row g-4 mb-4">
                  <div className="col-lg-6">
                    <div className="card border-0 rounded-4 shadow-sm h-100">
                      <div className="card-body p-4">
                        <div className="d-flex align-items-center justify-content-between mb-4">
                          <div className="d-flex align-items-center">
                            <Users className="text-primary me-3" size={24} />
                            <h5 className="card-title fw-bold mb-0">User Overview</h5>
                          </div>
                          <div className="text-end">
                            <div className="h4 fw-bold text-primary mb-0">{overview.summary.totalUsers}</div>
                            <div className="small text-muted">Total Users</div>
                          </div>
                        </div>
                        
                        <div className="row g-3">
                          <div className="col-6">
                            <div className="text-center p-3 bg-light rounded-3">
                              <div className="h5 fw-bold text-success mb-1">
                                {overview.userMetrics.verified}
                              </div>
                              <div className="small text-muted">Verified</div>
                            </div>
                          </div>
                          
                          <div className="col-6">
                            <div className="text-center p-3 bg-light rounded-3">
                              <div className="h5 fw-bold text-info mb-1">
                                {overview.userMetrics.accessGranted}
                              </div>
                              <div className="small text-muted">With Access</div>
                            </div>
                          </div>
                          
                          <div className="col-12">
                            <div className="text-center p-3 bg-primary bg-opacity-10 rounded-3">
                              <div className="h5 fw-bold text-primary mb-1">
                                {overview.userMetrics.rolesAssigned}
                              </div>
                              <div className="small text-muted">Roles Assigned</div>
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
                          <h5 className="card-title fw-bold mb-0">Performance Metrics</h5>
                        </div>
                        
                        <div className="space-y-3">
                          <div className="d-flex justify-content-between align-items-center py-3 border-bottom">
                            <span className="fw-semibold">Avg Provisioning Time</span>
                            <span className="fw-bold text-primary">
                              {overview.efficiency.avgProvisioningTime}h
                            </span>
                          </div>
                          
                          <div className="d-flex justify-content-between align-items-center py-3 border-bottom">
                            <span className="fw-semibold">Pending Backlog</span>
                            <span className="fw-bold text-warning">
                              {overview.efficiency.pendingBacklog}
                            </span>
                          </div>
                          
                          <div className="d-flex justify-content-between align-items-center py-3">
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
                        <div className="d-flex gap-2">
                          <button 
                            onClick={fetchData}
                            className="btn btn-outline-secondary btn-sm d-flex align-items-center"
                            disabled={loading}
                          >
                            <RefreshCw className={`me-2 ${loading ? 'rotating' : ''}`} size={16} />
                            Refresh
                          </button>
                          <button 
                            onClick={handleAutoProvision}
                            className="btn btn-primary btn-sm d-flex align-items-center"
                          >
                            <Zap className="me-2" size={16} />
                            Auto-Provision Eligible
                          </button>
                        </div>
                      </div>
                    </div>
                    <div className="card-body p-0">
                      <div className="table-responsive">
                        <table className="table mb-0">
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
                      



                      {/* Unified File Upload/Display Section */}
                      {migrationStatus === 'idle' && (
                        <div className="row g-4 mb-4">
                          <div className="col-12">
                            <h6 className="fw-semibold mb-3">
                              {uploadedFiles.length > 0 ? `Uploaded Files (${uploadedFiles.length})` : 'Upload Data Files'}
                            </h6>
                            
                            {uploadedFiles.length === 0 ? (
                              /* Upload Area */
                              <div 
                                className={`drag-zone border border-dashed rounded-4 p-5 text-center position-relative ${
                                  isDragging ? 'border-primary bg-primary bg-opacity-5 shadow-sm drag-active' : 'border-secondary'
                                } ${dragError ? 'border-danger bg-danger bg-opacity-5' : ''}`}
                                onDragEnter={handleDragEnter}
                                onDragLeave={handleDragLeave}
                                onDragOver={handleDragOver}
                                onDrop={handleDrop}
                                style={{ 
                                  cursor: 'pointer',
                                  minHeight: '220px',
                                  display: 'flex',
                                  flexDirection: 'column',
                                  justifyContent: 'center',
                                  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
                                  borderWidth: '2px'
                                }}
                              >
                                <div className="d-inline-flex align-items-center justify-content-center rounded-circle bg-secondary bg-opacity-10 mb-4" style={{ width: '80px', height: '80px' }}>
                                  <Upload 
                                    className={`${isDragging ? 'text-primary' : 'text-secondary'}`} 
                                    size={32} 
                                  />
                                </div>
                                <h6 className={`fw-semibold mb-2 ${isDragging ? 'text-primary' : 'text-dark'}`}>
                                  {isDragging 
                                    ? 'Drop your files here' 
                                    : 'Upload Data Files'
                                  }
                                </h6>
                                <p className="text-muted mb-4 mx-3">
                                  {isDragging 
                                    ? 'Release to upload the files'
                                    : 'Drag and drop multiple files here or click the button below to browse'
                                  }
                                </p>
                                <input
                                  type="file"
                                  accept=".csv,.json,.xml,.xlsx,.xls"
                                  onChange={handleFileUpload}
                                  className="form-control d-none"
                                  id="fileUploadInitial"
                                  multiple
                                />
                                <button 
                                  type="button"
                                  className="btn btn-primary btn-lg px-4"
                                  onClick={() => {
                                    console.log('Button clicked - triggering file input');
                                    const fileInput = document.getElementById('fileUploadInitial') as HTMLInputElement;
                                    if (fileInput) {
                                      fileInput.click();
                                    }
                                  }}
                                >
                                  <Upload className="me-2" size={18} />
                                  Choose Files
                                </button>
                                
                                {/* Error message */}
                                {dragError && (
                                  <div className="mt-3">
                                    <div className="alert alert-danger py-2 mb-0">
                                      <AlertCircle className="me-2" size={16} />
                                      {dragError}
                                    </div>
                                  </div>
                                )}
                                
                                {/* Drag overlay */}
                                {isDragging && (
                                  <div 
                                    className="position-absolute top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center rounded-4"
                                    style={{ 
                                      pointerEvents: 'none',
                                      background: 'linear-gradient(135deg, rgba(13, 110, 253, 0.08) 0%, rgba(13, 110, 253, 0.12) 100%)',
                                      border: '3px dashed var(--bs-primary)',
                                      animation: 'pulse 2s infinite'
                                    }}
                                  >
                                    <div className="text-center">
                                      <div className="d-inline-flex align-items-center justify-content-center rounded-circle bg-primary bg-opacity-10 mb-3" style={{ width: '80px', height: '80px' }}>
                                        <Upload className="text-primary" size={32} />
                                      </div>
                                      <h5 className="text-primary fw-bold mb-1">Drop files here</h5>
                                      <p className="text-primary mb-0 small">Release to upload multiple files</p>
                                    </div>
                                  </div>
                                )}
                                
                                {/* Supported formats info */}
                                <div className="mt-3">
                                  <small className="text-muted">
                                    Supported formats: CSV, JSON, XML, XLSX, XLS
                                  </small>
                                </div>
                              </div>
                            ) : (
                              /* Multiple Files Display Area */
                              <div className="space-y-3">
                                <div className="d-flex align-items-center justify-content-between mb-3">
                                  <div className="d-flex align-items-center">
                                    <Database className="me-2 text-primary" size={20} />
                                    <span className="fw-semibold">Total Records: {allMigrationData.length}</span>
                                  </div>
                                  <div className="d-flex gap-2">
                                    <button 
                                      onClick={() => document.getElementById('fileUploadAdditional')?.click()}
                                      className="btn btn-outline-primary btn-sm"
                                      title="Add more files"
                                    >
                                      <Upload size={14} className="me-1" />
                                      Add More
                                    </button>
                                    <button 
                                      onClick={resetMigration} 
                                      className="btn btn-outline-secondary btn-sm"
                                      title="Remove all files"
                                    >
                                      <Trash2 size={14} />
                                      Remove All
                                    </button>
                                  </div>
                                </div>
                                
                                {/* Files List */}
                                {uploadedFiles.map((file, index) => {
                                  const fileData = fileDataMap.get(file.name) || [];
                                  const fileExtension = file.name.split('.').pop()?.toLowerCase();
                                  
                                  return (
                                    <div key={index} className="card border-0 shadow-sm">
                                      <div className="card-body p-3">
                                        <div className="d-flex align-items-center justify-content-between">
                                          <div className="d-flex align-items-center">
                                            {fileExtension === 'xlsx' || fileExtension === 'xls' ? (
                                              <FileSpreadsheet className="me-3 text-success" size={20} />
                                            ) : fileExtension === 'json' ? (
                                              <FileCode className="me-3 text-info" size={20} />
                                            ) : fileExtension === 'xml' ? (
                                              <FileText className="me-3 text-warning" size={20} />
                                            ) : (
                                              <FileSpreadsheet className="me-3 text-primary" size={20} />
                                            )}
                                            <div>
                                              <h6 className="fw-semibold mb-0">{file.name}</h6>
                                              <small className="text-muted">
                                                {(file.size / 1024).toFixed(1)} KB • 
                                                {fileExtension?.toUpperCase()} • 
                                                {fileData.length > 0 ? `${fileData.length} records` : 'Server processing required'}
                                              </small>
                                            </div>
                                          </div>
                                          <button 
                                            onClick={() => {
                                              const newFiles = uploadedFiles.filter((_, i) => i !== index);
                                              const newFileDataMap = new Map(fileDataMap);
                                              newFileDataMap.delete(file.name);
                                              setUploadedFiles(newFiles);
                                              setFileDataMap(newFileDataMap);
                                              
                                              // Recalculate all migration data
                                              const newAllData = Array.from(newFileDataMap.values()).flat();
                                              setAllMigrationData(newAllData);
                                              setShowPreview(newAllData.length > 0);
                                            }}
                                            className="btn btn-sm btn-outline-danger"
                                            title="Remove this file"
                                          >
                                            <Trash2 size={14} />
                                          </button>
                                        </div>
                                      </div>
                                    </div>
                                  );
                                })}
                                
                                {/* Hidden file input for adding more files */}
                                <input
                                  type="file"
                                  accept=".csv,.json,.xml,.xlsx,.xls"
                                  onChange={handleFileUpload}
                                  className="form-control d-none"
                                  id="fileUploadAdditional"
                                  multiple
                                />
                              </div>
                            )}
                            
                            {/* Migration Action Button - Show when files are uploaded */}
                            {uploadedFiles.length > 0 && (
                              <div className="text-center mt-4">
                                <button 
                                  onClick={processBulkMigration}
                                  className="btn btn-success btn-lg px-5"
                                  disabled={migrationStatus === 'processing'}
                                >
                                  <Upload className="me-2" size={18} />
                                  Start Migration ({allMigrationData.length} records from {uploadedFiles.length} files)
                                </button>
                                <p className="text-muted small mt-2">
                                  All files will be processed in sequence
                                </p>
                              </div>
                            )}
                                
                                {/* Combined Data Preview */}
                                {console.log('Render check - showPreview:', showPreview, 'allMigrationData.length:', allMigrationData.length)}
                                {showPreview && allMigrationData.length > 0 && (
                                  <div className="mt-4">
                                    <div className="card border-0 shadow-sm">
                                      <div className="card-body p-4">
                                      <div className="d-flex align-items-center justify-content-between mb-3">
                                        <div className="d-flex align-items-center">
                                          <Eye className="me-2 text-primary" size={20} />
                                          <h6 className="fw-semibold mb-0">Data Preview</h6>
                                        </div>
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
                                      </div>
                                      
                                      <div className="border rounded-3 overflow-hidden mb-3">
                                        <div className="table-responsive">
                                          <table className="table table-sm mb-0">
                                            <thead className="bg-light">
                                              <tr>
                                                <th className="px-3 py-2 border-0" style={{ width: '60px' }}>#</th>
                                                {Object.keys(allMigrationData[0] || {}).map((key) => (
                                                  <th key={key} className="px-3 py-2 border-0 fw-semibold">
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
                                                    <td className="px-3 py-2 text-muted small">
                                                      {actualIndex}
                                                    </td>
                                                    {Object.values(row).map((value: any, idx) => (
                                                      <td key={idx} className="px-3 py-2 small">
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
                                      </div>
                                      </div>
                                    </div>
                                  </div>
                                )}
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

      {/* Migration Progress - Outside of tabs */}
      {activeTab === 'bulk' && (
        <div className="container-fluid py-0">
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
                                <div className="d-flex justify-content-between align-items-center mb-2">
                                  <p className="small text-muted mb-0">
                                    {progressDetails?.currentOperation || 'Processing user data...'} {Math.round(migrationProgress)}% complete
                                  </p>
                                  {progressDetails && (
                                    <small className="text-muted">
                                      {progressDetails.processedRecords}/{progressDetails.totalRecords} records
                                    </small>
                                  )}
                                </div>
                                {progressDetails && (
                                  <div className="row g-3 mt-2">
                                    <div className="col-md-3">
                                      <div className="text-center">
                                        <div className="h6 mb-1">{progressDetails.processedRecords}</div>
                                        <small className="text-muted">Processed</small>
                                      </div>
                                    </div>
                                    <div className="col-md-3">
                                      <div className="text-center">
                                        <div className="h6 mb-1">{progressDetails.totalRecords}</div>
                                        <small className="text-muted">Total</small>
                                      </div>
                                    </div>
                                    <div className="col-md-3">
                                      <div className="text-center">
                                        <div className="h6 mb-1">{progressDetails.processedFiles}</div>
                                        <small className="text-muted">Files Done</small>
                                      </div>
                                    </div>
                                    <div className="col-md-3">
                                      <div className="text-center">
                                        <div className="h6 mb-1">{progressDetails.totalFiles}</div>
                                        <small className="text-muted">Total Files</small>
                                      </div>
                                    </div>
                                  </div>
                                )}
                                {currentJobId && (
                                  <div className="mt-3">
                                    <small className="text-muted">Job ID: {currentJobId}</small>
                                  </div>
                                )}
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
                                {/* File Processing Summary */}
                                <div className="d-flex align-items-center justify-content-center mb-4">
                                  <div className="text-center">
                                    <h5 className="fw-bold text-primary mb-1">
                                      {migrationResults.processedFiles || 0} / {migrationResults.totalFiles || 0}
                                    </h5>
                                    <p className="small text-muted mb-0">Files Processed</p>
                                  </div>
                                </div>
                                
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
      )}

      <style>{`
        .rotating {
          animation: spin 1s linear infinite;
        }
        
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
        
        @keyframes pulse {
          0%, 100% { 
            opacity: 1;
            transform: scale(1);
          }
          50% { 
            opacity: 0.8;
            transform: scale(1.02);
          }
        }
        
        @keyframes dragEnter {
          0% { 
            transform: scale(1);
            border-width: 2px;
          }
          100% { 
            transform: scale(1.01);
            border-width: 3px;
          }
        }
        
        .drag-active {
          animation: dragEnter 0.2s ease-out forwards;
        }
        
        .space-y-3 > * + * {
          margin-top: 0.75rem;
        }
        
        /* Disable default browser drag image */
        * {
          -webkit-user-drag: none;
          -khtml-user-drag: none;
          -moz-user-drag: none;
          -o-user-drag: none;
          user-drag: none;
        }
        
        /* Enhanced drag zone styles */
        .drag-zone {
          position: relative;
          overflow: hidden;
          transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
        }
        
        .drag-zone:hover {
          border-color: var(--bs-primary) !important;
          background-color: rgba(13, 110, 253, 0.02) !important;
          transform: translateY(-2px);
          box-shadow: 0 4px 20px rgba(13, 110, 253, 0.15) !important;
        }
        
        .drag-zone::before {
          content: '';
          position: absolute;
          top: -2px;
          left: -2px;
          right: -2px;
          bottom: -2px;
          background: linear-gradient(45deg, transparent 30%, rgba(13, 110, 253, 0.1) 50%, transparent 70%);
          opacity: 0;
          transition: opacity 0.3s ease;
          pointer-events: none;
          border-radius: inherit;
        }
        
        .drag-zone.drag-active::before {
          opacity: 1;
          animation: shimmer 2s infinite;
        }
        
        @keyframes shimmer {
          0% { background-position: -200% 0; }
          100% { background-position: 200% 0; }
        }
      `}</style>
    </div>
  );
}