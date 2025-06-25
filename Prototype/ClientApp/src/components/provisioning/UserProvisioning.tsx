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
  X
} from 'lucide-react';
import { userProvisioningApi } from '../../services/api';
import { progressService, ProgressUpdate, JobStart, JobComplete, JobError } from '../../services/signalr';
import { useMigration } from '../../context/MigrationContext';

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

type MigrationStatus = 'idle' | 'processing' | 'completed' | 'error';

// Error Display Component with truncation and expand functionality
const ErrorDisplay: React.FC<{ errors: string[] }> = ({ errors }) => {
  const [expandedErrors, setExpandedErrors] = useState<Set<number>>(new Set());
  const [showAllErrors, setShowAllErrors] = useState(false);
  
  const MAX_ERROR_LENGTH = 200; // Characters before truncation
  const MAX_INITIAL_ERRORS = 3; // Only show first 3 errors initially
  
  const toggleErrorExpansion = (index: number) => {
    const newExpanded = new Set(expandedErrors);
    if (newExpanded.has(index)) {
      newExpanded.delete(index);
    } else {
      newExpanded.add(index);
    }
    setExpandedErrors(newExpanded);
  };
  
  const truncateError = (error: string, maxLength: number) => {
    if (error.length <= maxLength) return error;
    return error.substring(0, maxLength);
  };
  
  const errorsToShow = showAllErrors ? errors : errors.slice(0, MAX_INITIAL_ERRORS);
  
  return (
    <div className="mb-3">
      <h6 className="fw-semibold text-danger mb-2">Errors:</h6>
      <div className="alert alert-danger">
        <ul className="mb-0 ps-3">
          {errorsToShow.map((error, index) => {
            const isExpanded = expandedErrors.has(index);
            const needsTruncation = error.length > MAX_ERROR_LENGTH;
            const displayError = isExpanded || !needsTruncation 
              ? error 
              : truncateError(error, MAX_ERROR_LENGTH);
            
            return (
              <li key={index} className="small mb-2">
                <div>
                  {displayError}
                  {!isExpanded && needsTruncation && (
                    <>
                      <span className="text-muted">...</span>
                      <button
                        type="button"
                        className="btn btn-link btn-sm p-0 ms-1 text-danger"
                        style={{ fontSize: '0.75rem', textDecoration: 'underline' }}
                        onClick={() => toggleErrorExpansion(index)}
                      >
                        Show More
                      </button>
                    </>
                  )}
                  {isExpanded && needsTruncation && (
                    <button
                      type="button"
                      className="btn btn-link btn-sm p-0 ms-1 text-danger"
                      style={{ fontSize: '0.75rem', textDecoration: 'underline' }}
                      onClick={() => toggleErrorExpansion(index)}
                    >
                      Show Less
                    </button>
                  )}
                </div>
              </li>
            );
          })}
          
          {/* Show remaining errors count and toggle */}
          {errors.length > MAX_INITIAL_ERRORS && (
            <li className="small text-muted mt-2">
              {!showAllErrors ? (
                <>
                  ... and {errors.length - MAX_INITIAL_ERRORS} more errors
                  <button
                    type="button"
                    className="btn btn-link btn-sm p-0 ms-2 text-danger"
                    style={{ fontSize: '0.75rem', textDecoration: 'underline' }}
                    onClick={() => setShowAllErrors(true)}
                  >
                    Show All Errors
                  </button>
                </>
              ) : (
                <button
                  type="button"
                  className="btn btn-link btn-sm p-0 text-danger"
                  style={{ fontSize: '0.75rem', textDecoration: 'underline' }}
                  onClick={() => setShowAllErrors(false)}
                >
                  Show Less Errors
                </button>
              )}
            </li>
          )}
        </ul>
      </div>
    </div>
  );
};

export default function UserProvisioning() {
  const { migrationState, updateMigrationState, clearMigrationState, shouldNavigateToBulkTab, setShouldNavigateToBulkTab, setIsOnBulkTab } = useMigration();
  
  const [overview, setOverview] = useState<ProvisioningOverview | null>(null);
  const [pendingRequests, setPendingRequests] = useState<PendingRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState(() => {
    // If there's an active migration, start with bulk tab
    const savedState = localStorage.getItem('cams_migration_state');
    if (savedState) {
      try {
        const parsed = JSON.parse(savedState);
        if (parsed.status === 'processing') {
          return 'bulk';
        }
      } catch (error) {
        // Ignore parsing errors
      }
    }
    return 'overview';
  });
  const [lastUpdated, setLastUpdated] = useState<Date>(new Date());
  
  // Bulk migration state - now managed by context but keeping local state for UI
  const [uploadedFiles, setUploadedFiles] = useState<File[]>([]);
  const [allMigrationData, setAllMigrationData] = useState<any[]>([]);
  const [fileDataMap, setFileDataMap] = useState<Map<string, any[]>>(new Map());
  const [showPreview, setShowPreview] = useState(false);
  
  // Use context state for migration status and results
  const migrationProgress = migrationState?.progress || 0;
  const migrationStatus = migrationState?.status || 'idle';
  const migrationResults = migrationState?.results;
  
  // Track if we're in the middle of a page refresh/session restoration
  const [isSessionRestoration, setIsSessionRestoration] = useState(false);
  
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
  const [isRestoringSession, setIsRestoringSession] = useState(false);
  const [statusCheckInterval, setStatusCheckInterval] = useState<NodeJS.Timeout | null>(null);

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

  // Periodic status checking for when SignalR reconnection fails
  const startPeriodicStatusCheck = (jobId: string) => {
    console.log('🔄 Starting periodic status check for job:', jobId);
    
    // Clear any existing interval
    if (statusCheckInterval) {
      clearInterval(statusCheckInterval);
    }
    
    let attemptCount = 0;
    const maxAttempts = 40; // 10 minutes of attempts (15s * 40 = 600s)
    
    const interval = setInterval(async () => {
      attemptCount++;
      console.log(`🔄 Reconnection attempt ${attemptCount}/${maxAttempts} for job: ${jobId}`);
      
      try {
        // Try to reconnect to SignalR periodically
        await progressService.ensureConnection();
        await progressService.joinProgressGroup(jobId);
        console.log('✅ Periodic reconnection successful for job:', jobId);
        
        // Reset the session restoration flag since we're now connected
        setIsRestoringSession(false);
        
        // Update notification to show we're monitoring again
        
        // If successful, clear the interval
        clearInterval(interval);
        setStatusCheckInterval(null);
      } catch (error) {
        console.log(`🔄 Reconnection attempt ${attemptCount} failed:`, error);
        
        // If we've exhausted all attempts, gracefully handle the situation
        if (attemptCount >= maxAttempts) {
          console.log('⏰ Maximum reconnection attempts reached, assuming job completed');
          clearInterval(interval);
          setStatusCheckInterval(null);
          
          // Calculate time elapsed
          const timeElapsed = migrationState?.startTime ? 
            (Date.now() - new Date(migrationState.startTime).getTime()) / 1000 : 0;
            
          // If it's been a reasonable amount of time, assume completion
          if (timeElapsed > 300) { // At least 5 minutes has passed
            updateMigrationState({
              status: 'completed',
              progress: 100,
              results: migrationState?.results || {
                successful: 0,
                failed: 0,
                errors: ['Migration completed. Connection lost during processing, but job likely finished successfully.'],
                processedFiles: 1,
                totalFiles: 1
              },
              endTime: new Date().toISOString()
            });
            
          } else {
            // If it's too soon, mark as error
            updateMigrationState({
              status: 'error',
              results: {
                successful: 0,
                failed: 1,
                errors: ['Lost connection to migration progress. Please check the database manually.'],
                processedFiles: 0,
                totalFiles: 1
              }
            });
            
          }
        }
      }
    }, 15000); // Check every 15 seconds
    
    setStatusCheckInterval(interval);
  };

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, 5 * 60 * 1000); // Refresh every 5 minutes
    return () => clearInterval(interval);
  }, []);

  // Initialize bulk tab state on component mount
  useEffect(() => {
    const isOnBulk = activeTab === 'bulk';
    setIsOnBulkTab(isOnBulk);
    console.log('🔄 Initial bulk tab state:', isOnBulk, 'activeTab:', activeTab);
  }, []); // Run once on mount

  // Restore migration state and reconnect to SignalR if migration is in progress
  useEffect(() => {
    if (migrationState?.status === 'processing' && migrationState.jobId) {
      console.log('🔄 Restoring migration session for job:', migrationState.jobId);
      console.log('🔍 Migration state on restoration:', migrationState);
      setCurrentJobId(migrationState.jobId);
      setIsRestoringSession(true);
      setIsSessionRestoration(true);
      
      // Clear session restoration flag after a reasonable time
      setTimeout(() => {
        console.log('⏰ Clearing session restoration flag after timeout');
        setIsSessionRestoration(false);
      }, 60000); // 1 minute
      
      // Auto-switch to bulk tab when restoring migration session
      if (activeTab !== 'bulk') {
        console.log('🔄 Switching to bulk tab for migration session restoration');
        setActiveTab('bulk');
        setIsOnBulkTab(true); // Immediately update the bulk tab state
      } else {
        // If already on bulk tab, make sure the state is correct
        setIsOnBulkTab(true);
      }
      
      // Show notification for ongoing migration
      
      // Set a longer timeout for session restoration (backend might still be processing)
      const restorationTimeout = setTimeout(() => {
        console.log('⏰ Session restoration timeout - backend job may still be running');
        setIsRestoringSession(false);
        
        // Update UI to show we're monitoring an active job
        updateMigrationState({
          status: 'processing', // Keep as processing
          progress: Math.min((migrationState.progress || 0) + 5, 95) // Increment slightly but don't reach 100%
        });
        
        // Update notification to show monitoring state
        
        // Set up periodic checking
        startPeriodicStatusCheck(migrationState.jobId!);
      }, 45000); // 45 second timeout (longer for backend processing)
      
      // Reconnect to SignalR progress group
      progressService.ensureConnection()
        .then(() => progressService.joinProgressGroup(migrationState.jobId!))
        .then(() => {
          console.log('✅ Reconnected to SignalR progress group for job:', migrationState.jobId);
          clearTimeout(restorationTimeout);
          setIsRestoringSession(false);
          setIsSessionRestoration(false);
        })
        .catch(error => {
          console.error('❌ Failed to reconnect to SignalR:', error);
          console.log('🔄 Job may still be running on backend. Starting monitoring mode...');
          clearTimeout(restorationTimeout);
          
          // Don't immediately assume failure - start monitoring mode
          setIsRestoringSession(false);
          setIsSessionRestoration(false);
          
          // Update UI to show monitoring state
          updateMigrationState({
            status: 'processing', // Keep as processing
            progress: Math.min((migrationState.progress || 0) + 5, 95)
          });
          
          // Update notification to show monitoring state
          
          // Start periodic checking immediately
          startPeriodicStatusCheck(migrationState.jobId!);
        });
    } else if (migrationState?.status === 'completed') {
      // Show completion notification
    } else if (migrationState?.status === 'error') {
      // Show error notification
    }
  }, [migrationState]);

  // Handle navigation to bulk tab from global indicator
  useEffect(() => {
    if (shouldNavigateToBulkTab) {
      console.log('🔄 Navigating to bulk tab from global indicator');
      setActiveTab('bulk');
      setShouldNavigateToBulkTab(false); // Reset the flag
    }
  }, [shouldNavigateToBulkTab, setShouldNavigateToBulkTab]);

  // Track when user is on bulk tab
  useEffect(() => {
    const isOnBulk = activeTab === 'bulk';
    setIsOnBulkTab(isOnBulk);
    console.log('📊 User is on bulk tab:', isOnBulk);
  }, [activeTab, setIsOnBulkTab]);

  // Debug: Log migration state changes
  useEffect(() => {
    if (migrationState) {
      console.log('📊 Migration state:', migrationStatus, `${migrationProgress}%`);
    }
  }, [migrationStatus, migrationProgress]);

  // Cleanup on component unmount
  useEffect(() => {
    return () => {
      // Clear periodic status checking on unmount
      if (statusCheckInterval) {
        clearInterval(statusCheckInterval);
        setStatusCheckInterval(null);
      }
      
      // Reset bulk tab tracking when leaving UserProvisioning page
      setIsOnBulkTab(false);
    };
  }, [statusCheckInterval, setIsOnBulkTab]);

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
      updateMigrationState({
        status: 'processing',
        progress: 10,
        jobId: jobStart.jobId
      });
      
      // Show notification
    };

    const handleProgressUpdate = (progress: ProgressUpdate) => {
      console.log('📈 SignalR: Progress update:', progress);
      setProgressDetails(progress);
      updateMigrationState({
        progress: progress.progressPercentage
      });
      
      // Update notification
    };

    const handleJobCompleted = (result: JobComplete) => {
      console.log('🎉 SignalR: Job completed:', result);
      
      // Add a delay to ensure users see the progress bar working
      setTimeout(() => {
        const results = result.success && result.data ? {
          successful: result.data.processedRecords || 0,
          failed: result.data.failedRecords || 0,
          errors: result.data.errors || [],
          processedFiles: result.data.processedFiles || 1,
          totalFiles: result.data.totalFiles || 1
        } : null;

        updateMigrationState({
          status: result.success ? 'completed' : 'error',
          progress: 100,
          results,
          endTime: new Date().toISOString()
        });
        
        // Update notification
        
        setCurrentJobId(null);
        setProgressDetails(null);
      }, 1500); // Show progress for 1.5 seconds minimum
    };

    const handleJobError = (error: JobError) => {
      console.log('❌ SignalR: Job error:', error);
      console.log('🔍 Debug - isRestoringSession:', isRestoringSession, 'isSessionRestoration:', isSessionRestoration, 'error.error:', error.error);
      
      // Check if this is a "Load failed" error which commonly happens during page refresh/reconnection
      if (error.error === "Load failed") {
        console.log('🔄 "Load failed" error detected - treating as connection issue, not genuine failure');
        
        // If we're restoring session or just refreshed the page, this is expected
        if (isRestoringSession || isSessionRestoration) {
          console.log('🔄 During session restoration - ignoring Load failed error');
          setIsRestoringSession(false);
          setIsSessionRestoration(false);
          
          // Start monitoring mode instead of marking as error
          updateMigrationState({
            status: 'processing',
            progress: Math.min((migrationState?.progress || 0) + 5, 95)
          });
          
          
          // Start periodic checking to reconnect
          if (currentJobId) {
            startPeriodicStatusCheck(currentJobId);
          }
          
          return;
        } else {
          // Even if not in restoration mode, "Load failed" during active migration should not immediately fail
          console.log('🔄 "Load failed" outside restoration - still treating as connection issue');
          
          // Keep processing state and start monitoring
          updateMigrationState({
            status: 'processing',
            progress: Math.min((migrationState?.progress || 0) + 5, 95)
          });
          
          
          // Start periodic checking to reconnect
          if (currentJobId) {
            startPeriodicStatusCheck(currentJobId);
          }
          
          return;
        }
      } else {
        // Handle genuine errors (not "Load failed")
        console.log('💥 Genuine error occurred:', error.error);
        const errorResults = {
          successful: 0,
          failed: 1,
          errors: [error.error],
          processedFiles: 0,
          totalFiles: 1
        };
        
        updateMigrationState({
          status: 'error',
          results: errorResults
        });
        
        // Update notification
        
        setCurrentJobId(null);
        setProgressDetails(null);
      }
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
      
      // Clear any periodic status checking
      if (statusCheckInterval) {
        clearInterval(statusCheckInterval);
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
    
    // Check for duplicate files
    const existingFileNames = new Set(uploadedFiles.map(f => f.name));
    
    // Validate all files first
    for (const file of files) {
      const fileExtension = file.name.split('.').pop()?.toLowerCase();
      console.log(`File: ${file.name}, Extension: ${fileExtension}`);
      
      // Check for duplicates
      if (existingFileNames.has(file.name)) {
        errors.push(`${file.name}: File already uploaded`);
        continue;
      }
      
      if (!supportedFormats.includes(fileExtension || '')) {
        errors.push(`${file.name}: Unsupported format (${fileExtension?.toUpperCase()})`);
      } else {
        validFiles.push(file);
      }
    }
    
    console.log('Valid files:', validFiles.map(f => f.name));
    console.log('Errors during validation:', errors);
    
    if (errors.length > 0) {
      setDragError(`Some files have issues:\n${errors.join('\n')}`);
    }
    
    if (validFiles.length === 0) {
      console.log('No valid files to process');
      return;
    }
    
    // Add to existing files instead of replacing
    const updatedFiles = [...uploadedFiles, ...validFiles];
    setUploadedFiles(updatedFiles);
    console.log('Added files to existing uploaded files. Total files:', updatedFiles.length);
    
    // Process files for preview - preserve existing data and add new
    const newFileDataMap = new Map(fileDataMap); // Start with existing data
    let allData: any[] = [...allMigrationData]; // Start with existing migration data
    
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
    updateMigrationState({
      status: 'processing',
      progress: 0,
      results: null,
      startTime: new Date().toISOString()
    });
    setProgressDetails(null);
    
    // Show initial notification

    try {
      // Use queue-based processing for multiple files, single file processing for one file
      if (uploadedFiles.length > 1) {
        console.log(`📁 Processing ${uploadedFiles.length} files using queue system...`);
        await processBulkMigrationWithQueue();
      } else if (uploadedFiles.length === 1) {
        console.log('📁 Processing single file with progress tracking...');
        await processSingleFileWithProgress();
      } else {
        throw new Error('No files to process');
      }
    } catch (error) {
      console.error('❌ Error in bulk migration:', error);
      
      // Check if this is a "Load failed" error which indicates a connection issue, not a real failure
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      
      if (errorMessage === 'Load failed') {
        console.log('🔄 "Load failed" in bulk migration - treating as connection issue, keeping processing state');
        
        // Don't change status to error - keep it as processing since the job might be running on backend
        updateMigrationState({
          status: 'processing',
          progress: Math.max(migrationState?.progress || 0, 20) // Ensure some progress is shown
        });
        
        // Update notification to show connection issue
        
        // Start periodic checking to monitor the job
        if (currentJobId) {
          console.log('🔄 Starting monitoring for job:', currentJobId);
          startPeriodicStatusCheck(currentJobId);
        }
      } else {
        // Handle genuine errors
        console.log('💥 Genuine error in bulk migration:', errorMessage);
        updateMigrationState({
          status: 'error',
          results: {
            successful: 0,
            failed: 1,
            errors: [errorMessage],
            processedFiles: 0,
            totalFiles: uploadedFiles.length
          }
        });
      }
    }
  };

  // Legacy function for fallback (keeping the original complex logic for reference)
  const processBulkMigrationFallback = async () => {
    if (uploadedFiles.length === 0 && allMigrationData.length === 0) return;

    updateMigrationState({
      status: 'processing',
      progress: 0,
      results: null
    });

    try {
      const totalFiles = uploadedFiles.length;
      updateMigrationState({ progress: 10 });

      // Fallback: Process files individually with fake progress
      let successful = 0;
      let failed = 0;
      const errors: string[] = [];
      let processedFiles = 0;

      for (let fileIndex = 0; fileIndex < uploadedFiles.length; fileIndex++) {
        const file = uploadedFiles[fileIndex];
        const fileProgress = (fileIndex / totalFiles) * 80 + 20; // Reserve 20% for initial setup
        
        try {
          updateMigrationState({ progress: fileProgress });
          
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
            updateMigrationState({ progress: overallProgress });
            
            // Small delay to prevent overwhelming the API
            await new Promise(resolve => setTimeout(resolve, 50));
          }
          
          processedFiles++;
        }
        
        updateMigrationState({ progress: ((fileIndex + 1) / totalFiles) * 80 + 20 });
      }

      updateMigrationState({
        status: 'completed',
        progress: 100,
        results: { 
          successful, 
          failed, 
          errors, 
          processedFiles, 
          totalFiles 
        },
        endTime: new Date().toISOString()
      });
    } catch (error) {
      console.error('Migration failed:', error);
      updateMigrationState({
        status: 'error',
        results: {
          successful: 0,
          failed: allMigrationData.length,
          errors: [`Migration failed: ${error instanceof Error ? error.message : 'Unknown error'}`],
          processedFiles: 0,
          totalFiles: uploadedFiles.length
        }
      });
    }
  };

  const resetMigration = () => {
    setUploadedFiles([]);
    setAllMigrationData([]);
    setFileDataMap(new Map());
    setShowPreview(false);
    clearMigrationState(); // Clear from context and localStorage
    setPreviewCurrentPage(1);
    setIsDragging(false);
    setDragError(null);
    setDragCounter(0);
    setCurrentJobId(null);
    setProgressDetails(null);
    setIsRestoringSession(false);
    
    // Clear periodic status checking
    if (statusCheckInterval) {
      clearInterval(statusCheckInterval);
      setStatusCheckInterval(null);
    }
    
    // Hide notification
  };

  const cancelMigration = async () => {
    try {
      if (currentJobId) {
        console.log('🚫 Cancelling job:', currentJobId);
        
        // Determine if this is a queue job or single file job
        let response;
        if (uploadedFiles.length > 1) {
          response = await userProvisioningApi.cancelQueue(currentJobId);
        } else {
          response = await fetch(`/api/bulkupload/cancel/${currentJobId}`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
          });
          response = await response.json();
        }

        if (response.success || response.ok) {
          console.log('✅ Job cancelled successfully');
          
          // Update the migration state to cancelled
          updateMigrationState({
            status: 'error',
            endTime: new Date().toISOString(),
            results: {
              successful: 0,
              failed: allMigrationData.length,
              errors: ['Migration cancelled by user'],
              processedFiles: 0,
              totalFiles: uploadedFiles.length
            }
          });

          // Clear periodic status checking
          if (statusCheckInterval) {
            clearInterval(statusCheckInterval);
            setStatusCheckInterval(null);
          }

          setCurrentJobId(null);
          setProgressDetails(null);
        } else {
          console.error('Failed to cancel migration');
        }
      }
    } catch (error) {
      console.error('Error cancelling migration:', error);
    }
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

  // Queue-based processing for multiple files
  const processBulkMigrationWithQueue = async () => {
    console.log(`📁 Starting queue-based processing for ${uploadedFiles.length} files...`);
    
    // First connect to SignalR before starting the upload
    console.log('🔗 Connecting to SignalR...');
    try {
      await progressService.ensureConnection();
      console.log('✅ SignalR connected successfully');
      updateMigrationState({ progress: 5 });
    } catch (signalRError) {
      console.error('❌ Failed to connect to SignalR:', signalRError);
      // Continue without SignalR
    }
    
    // Create FormData for multiple files
    const formData = new FormData();
    uploadedFiles.forEach((file, index) => {
      formData.append('files', file);
    });
    formData.append('ignoreErrors', 'false');
    formData.append('continueOnError', 'true');
    formData.append('processFilesSequentially', 'true');
    
    console.log(`📁 Uploading ${uploadedFiles.length} files to queue...`);
    updateMigrationState({ progress: 10 });
    
    // Call the queue-based API endpoint
    const response = await userProvisioningApi.bulkProvisionWithQueue(formData);
    console.log('📊 Queue API Response:', response);
    
    if (response.success && response.data) {
      const jobId = response.data.jobId;
      setCurrentJobId(jobId);
      
      console.log('✅ Files queued successfully, job ID:', jobId);
      updateMigrationState({ 
        progress: 15,
        jobId: jobId
      });
      
      // Join SignalR group for this job
      try {
        await progressService.joinProgressGroup(jobId);
        console.log('✅ Successfully joined SignalR progress group for job:', jobId);
      } catch (signalRError) {
        console.error('❌ Failed to join SignalR progress group:', signalRError);
        // Fall back to polling
        startQueueStatusPolling(jobId);
      }
    } else {
      throw new Error(response.message || 'Failed to queue files for processing');
    }
  };

  // Single file processing with progress tracking
  const processSingleFileWithProgress = async () => {
    const file = uploadedFiles[0];
    
    // Show initial progress
    updateMigrationState({ progress: 1 });
    
    // First connect to SignalR before starting the upload
    console.log('🔗 Connecting to SignalR...');
    try {
      await progressService.ensureConnection();
      console.log('✅ SignalR connected successfully');
      updateMigrationState({ progress: 5 });
    } catch (signalRError) {
      console.error('❌ Failed to connect to SignalR:', signalRError);
      // Continue without SignalR
    }
    
    // Pre-generate job ID and join SignalR group before API call
    const jobId = progressService.generateJobId();
    setCurrentJobId(jobId);
    console.log('🔗 Pre-joining SignalR group for job:', jobId);
    
    try {
      await progressService.joinProgressGroup(jobId);
      console.log('✅ Successfully pre-joined SignalR progress group for job:', jobId);
      updateMigrationState({ progress: 10, jobId });
    } catch (signalRError) {
      console.error('❌ Failed to pre-join SignalR progress group:', signalRError);
    }
    
    // Create FormData to send the file to the progress-enabled endpoint
    const formData = new FormData();
    formData.append('file', file);
    formData.append('ignoreErrors', 'false');
    formData.append('jobId', jobId); // Send the pre-generated job ID

    console.log('📁 Uploading file:', file.name, 'Size:', file.size, 'bytes');
    updateMigrationState({ progress: 15 });
    
    // Call the SignalR-enabled API endpoint with pre-generated job ID
    const response = await userProvisioningApi.bulkProvisionWithProgress(formData);
    console.log('📊 API Response:', response);

    if (response.success && response.data) {
      // Success case - progress updates will come via SignalR
      console.log('✅ Upload started successfully, waiting for SignalR updates...');
    } else {
      // Error case - show error immediately
      console.error('❌ Upload failed:', response.message);
      updateMigrationState({
        status: 'error',
        results: {
          successful: 0,
          failed: 1,
          errors: [response.message || 'Upload failed'],
          processedFiles: 0,
          totalFiles: 1
        }
      });
    }
  };

  // Polling fallback for queue status when SignalR is not available
  const startQueueStatusPolling = (jobId: string) => {
    console.log('🔄 Starting queue status polling for job:', jobId);
    
    const pollInterval = setInterval(async () => {
      try {
        const response = await userProvisioningApi.getQueueStatus(jobId);
        
        if (response.success && response.data) {
          const queueData = response.data;
          
          // Calculate overall progress based on completed files
          const progress = (queueData.completedFiles / queueData.totalFiles) * 100;
          
          updateMigrationState({ 
            progress: Math.min(progress, 95) // Don't show 100% until actually complete
          });
          
          // Update progress details with queue information
          setProgressDetails({
            jobId: jobId,
            progressPercentage: progress,
            status: queueData.status,
            currentOperation: queueData.processingFile || `Processing file ${queueData.completedFiles + 1} of ${queueData.totalFiles}`,
            processedRecords: 0,
            totalRecords: 0,
            currentFileName: queueData.processingFile,
            processedFiles: queueData.completedFiles,
            totalFiles: queueData.totalFiles,
            timestamp: new Date().toISOString(),
            errors: []
          });
          
          // Check if queue is completed
          if (queueData.status === 'Completed' || queueData.status === 'CompletedWithErrors') {
            clearInterval(pollInterval);
            
            const totalProcessed = queueData.files?.reduce((sum: number, file: any) => sum + (file.processedRecords || 0), 0) || 0;
            const totalFailed = queueData.files?.reduce((sum: number, file: any) => sum + (file.failedRecords || 0), 0) || 0;
            const allErrors = queueData.files?.flatMap((file: any) => 
              (file.errors || []).map((error: string) => `${file.fileName}: ${error}`)
            ) || [];
            
            updateMigrationState({
              status: queueData.status === 'Completed' ? 'completed' : 'error',
              progress: 100,
              endTime: new Date().toISOString(),
              results: {
                successful: totalProcessed,
                failed: totalFailed,
                errors: allErrors,
                processedFiles: queueData.completedFiles,
                totalFiles: queueData.totalFiles
              }
            });
            
            setProgressDetails(null);
            setCurrentJobId(null);
          } else if (queueData.status === 'Failed' || queueData.status === 'Cancelled') {
            clearInterval(pollInterval);
            
            updateMigrationState({
              status: 'error',
              endTime: new Date().toISOString(),
              results: {
                successful: 0,
                failed: queueData.totalFiles,
                errors: [`Queue ${queueData.status.toLowerCase()}`],
                processedFiles: queueData.completedFiles,
                totalFiles: queueData.totalFiles
              }
            });
            
            setProgressDetails(null);
            setCurrentJobId(null);
          }
        }
      } catch (error) {
        console.error('❌ Error polling queue status:', error);
      }
    }, 2000); // Poll every 2 seconds
    
    // Store the interval for cleanup
    setStatusCheckInterval(pollInterval);
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
                        {migrationStatus === 'processing' && (
                          <button onClick={cancelMigration} className="btn btn-outline-danger btn-sm">
                            <X size={16} className="me-2" />
                            Cancel
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
                                  disabled={(migrationStatus as MigrationStatus) === 'processing'}
                                >
                                  <Upload className="me-2" size={18} />
                                  Start Migration ({allMigrationData.length} records from {uploadedFiles.length} files)
                                </button>
                                <p className="text-muted small mt-2">
                                  All files will be processed in sequence
                                </p>
                              </div>
                            )}
                                
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

                      {/* Migration Progress Display - Show when migration is active */}
                      {migrationStatus !== 'idle' && (
                        <div className="row g-4 mb-4">
                          <div className="col-12">
                            <div className="card border-0 rounded-4 shadow-sm bg-light">
                              <div className="card-body p-4">
                                <div className="d-flex align-items-center justify-content-between mb-4">
                                  <div className="d-flex align-items-center">
                                    <div className={`me-3 ${
                                      migrationStatus === 'processing' ? 'text-warning' : 
                                      migrationStatus === 'completed' ? 'text-success' : 'text-danger'
                                    }`}>
                                      {migrationStatus === 'processing' && <RefreshCw className="rotating" size={24} />}
                                      {migrationStatus === 'completed' && <CheckCircle size={24} />}
                                      {migrationStatus === 'error' && <AlertCircle size={24} />}
                                    </div>
                                    <div>
                                      <h5 className="mb-1 fw-bold">
                                        {migrationStatus === 'processing' && 'Migration in Progress'}
                                        {migrationStatus === 'completed' && 'Migration Completed'}
                                        {migrationStatus === 'error' && 'Migration Failed'}
                                      </h5>
                                      <p className="text-muted mb-0 small">
                                        {migrationStatus === 'processing' && 'Processing your bulk data import...'}
                                        {migrationStatus === 'completed' && 'Your bulk migration has been completed successfully.'}
                                        {migrationStatus === 'error' && 'An error occurred during the migration process.'}
                                      </p>
                                    </div>
                                  </div>
                                  {migrationStatus === 'processing' && (
                                    <div className="text-end">
                                      <div className="h4 fw-bold text-warning mb-0">{Math.round(migrationProgress)}%</div>
                                      <div className="small text-muted">Progress</div>
                                    </div>
                                  )}
                                  {migrationStatus === 'completed' && (
                                    <div className="text-end">
                                      <div className="h4 fw-bold text-success mb-0">✓ Complete</div>
                                      <div className="small text-muted">Migration Finished</div>
                                    </div>
                                  )}
                                  {migrationStatus === 'error' && (
                                    <div className="text-end">
                                      <div className="h4 fw-bold text-danger mb-0">✗ Failed</div>
                                      <div className="small text-muted">Migration Error</div>
                                    </div>
                                  )}
                                </div>

                                {/* Progress Bar */}
                                {migrationStatus === 'processing' && (
                                  <div className="mb-4">
                                    <div className="progress" style={{ height: '12px' }}>
                                      <div
                                        className="progress-bar progress-bar-striped progress-bar-animated bg-warning"
                                        style={{ width: `${migrationProgress}%` }}
                                      />
                                    </div>
                                    <div className="d-flex justify-content-between mt-2">
                                      <small className="text-muted">0%</small>
                                      <small className="text-muted">100%</small>
                                    </div>
                                  </div>
                                )}

                                {/* Completion Summary */}
                                {migrationStatus === 'completed' && migrationState?.endTime && (
                                  <div className="mb-4">
                                    <div className="alert alert-success d-flex align-items-center mb-0" role="alert">
                                      <CheckCircle className="me-2" size={20} />
                                      <div className="flex-grow-1">
                                        <div className="fw-semibold">Migration Completed Successfully</div>
                                        <small className="text-muted">
                                          Finished at {new Date(migrationState.endTime).toLocaleString()}
                                        </small>
                                      </div>
                                    </div>
                                  </div>
                                )}

                                {/* Error Summary */}
                                {migrationStatus === 'error' && (
                                  <div className="mb-4">
                                    <div className="alert alert-danger d-flex align-items-center mb-0" role="alert">
                                      <AlertCircle className="me-2" size={20} />
                                      <div className="flex-grow-1">
                                        <div className="fw-semibold">Migration Failed</div>
                                        <small className="text-muted">
                                          Please check the error details below and try again
                                        </small>
                                      </div>
                                    </div>
                                  </div>
                                )}

                                {/* Progress Details */}
                                {progressDetails && migrationStatus === 'processing' && (
                                  <div className="row g-3 mb-4">
                                    <div className="col-md-3">
                                      <div className="text-center p-3 bg-white rounded-3 border">
                                        <div className="h6 fw-bold text-primary mb-1">
                                          {progressDetails.processedRecords}
                                        </div>
                                        <div className="small text-muted">Processed</div>
                                      </div>
                                    </div>
                                    <div className="col-md-3">
                                      <div className="text-center p-3 bg-white rounded-3 border">
                                        <div className="h6 fw-bold text-secondary mb-1">
                                          {progressDetails.totalRecords}
                                        </div>
                                        <div className="small text-muted">Total Records</div>
                                      </div>
                                    </div>
                                    <div className="col-md-3">
                                      <div className="text-center p-3 bg-white rounded-3 border">
                                        <div className="h6 fw-bold text-info mb-1">
                                          {progressDetails.processedFiles} / {progressDetails.totalFiles}
                                        </div>
                                        <div className="small text-muted">Files</div>
                                      </div>
                                    </div>
                                    <div className="col-md-3">
                                      <div className="text-center p-3 bg-white rounded-3 border">
                                        <div className="h6 fw-bold text-warning mb-1">
                                          {progressDetails.currentOperation || 'Processing...'}
                                        </div>
                                        <div className="small text-muted">Current Step</div>
                                      </div>
                                    </div>
                                  </div>
                                )}

                                {/* Results Display */}
                                {migrationResults && (migrationStatus === 'completed' || migrationStatus === 'error') && (
                                  <div className="row g-3 mb-3">
                                    <div className="col-md-4">
                                      <div className="text-center p-3 bg-success bg-opacity-10 rounded-3 border border-success">
                                        <div className="h5 fw-bold text-success mb-1">
                                          {migrationResults.successful}
                                        </div>
                                        <div className="small text-success">Successful</div>
                                      </div>
                                    </div>
                                    <div className="col-md-4">
                                      <div className="text-center p-3 bg-danger bg-opacity-10 rounded-3 border border-danger">
                                        <div className="h5 fw-bold text-danger mb-1">
                                          {migrationResults.failed}
                                        </div>
                                        <div className="small text-danger">Failed</div>
                                      </div>
                                    </div>
                                    <div className="col-md-4">
                                      <div className="text-center p-3 bg-info bg-opacity-10 rounded-3 border border-info">
                                        <div className="h5 fw-bold text-info mb-1">
                                          {migrationResults.processedFiles} / {migrationResults.totalFiles}
                                        </div>
                                        <div className="small text-info">Files Processed</div>
                                      </div>
                                    </div>
                                  </div>
                                )}

                                {/* Error Messages */}
                                {migrationResults?.errors && migrationResults.errors.length > 0 && (
                                  <ErrorDisplay errors={migrationResults.errors} />
                                )}

                                {/* Action Buttons */}
                                <div className="d-flex gap-2 justify-content-end">
                                  {migrationStatus === 'processing' && (
                                    <div className="text-muted small">
                                      <Clock size={14} className="me-1" />
                                      Migration started: {migrationState?.startTime ? 
                                        new Date(migrationState.startTime).toLocaleTimeString() : 'Unknown'}
                                    </div>
                                  )}
                                  {(migrationStatus === 'completed' || migrationStatus === 'error') && (
                                    <button onClick={resetMigration} className="btn btn-primary">
                                      <Upload size={16} className="me-2" />
                                      Start New Migration
                                    </button>
                                  )}
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