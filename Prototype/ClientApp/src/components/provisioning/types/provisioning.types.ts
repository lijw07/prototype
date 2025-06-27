// Type definitions for User Provisioning components
// Centralized types following clean code principles

import type { User, Role, PaginatedResponse, BulkUploadJobComplete } from '../../../types/api.types';

// Provisioning Overview Types
export interface ProvisioningMetrics {
  totalPendingRequests: number;
  totalProvisionedUsers: number;
  totalFailedRequests: number;
  autoProvisioningEnabled: boolean;
  lastProvisioningRun?: string;
  provisioiningTemplatesCount: number;
  averageProcessingTime?: string;
}

export interface ProvisioningTrend {
  date: string;
  provisioned: number;
  failed: number;
  pending: number;
}

// Pending Requests Types
export interface PendingProvisioningRequest {
  requestId: string;
  temporaryUserId?: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber?: string;
  requestedRole: string;
  requestedRoleId: string;
  requestedApplications?: string[];
  requestDate: string;
  requestedBy?: string;
  requestedByEmail?: string;
  status: 'pending' | 'reviewing' | 'approved' | 'rejected' | 'processing';
  priority: 'low' | 'medium' | 'high' | 'urgent';
  approvalReason?: string;
  rejectionReason?: string;
  notes?: string;
  estimatedCompletionDate?: string;
}

// Provisioning Templates Types
export interface ProvisioningTemplate {
  templateId: string;
  templateName: string;
  description?: string;
  roleId: string;
  roleName: string;
  defaultApplications: ApplicationAccess[];
  autoApprovalEnabled: boolean;
  requiresManagerApproval: boolean;
  approvalWorkflow?: ApprovalStep[];
  isActive: boolean;
  createdAt: string;
  createdBy: string;
  updatedAt?: string;
  updatedBy?: string;
}

export interface ApplicationAccess {
  applicationId: string;
  applicationName: string;
  accessLevel: 'read' | 'write' | 'admin';
  autoGrant: boolean;
}

export interface ApprovalStep {
  stepId: string;
  stepName: string;
  approverRole: string;
  isRequired: boolean;
  order: number;
}

// Auto Provisioning Types
export interface AutoProvisioningConfig {
  enabled: boolean;
  scheduleType: 'manual' | 'hourly' | 'daily' | 'weekly';
  scheduleTime?: string;
  batchSize: number;
  retryFailedRequests: boolean;
  notifyOnCompletion: boolean;
  notificationEmails: string[];
  eligibilityCriteria: EligibilityCriteria;
}

export interface EligibilityCriteria {
  minimumRequestAge: number; // hours
  requiredDocuments: string[];
  managerApprovalRequired: boolean;
  backgroundCheckRequired: boolean;
  trainingCompletionRequired: boolean;
}

// Bulk Upload Types
export interface BulkUploadFile {
  fileId: string;
  file: File;
  fileName: string;
  fileSize: string;
  fileType: 'csv' | 'xlsx' | 'xls' | 'json' | 'xml';
  uploadedAt: string;
  status: 'pending' | 'parsing' | 'validating' | 'processing' | 'completed' | 'failed';
  progress: number;
  recordsTotal: number;
  recordsProcessed: number;
  recordsSuccessful: number;
  recordsFailed: number;
  errors: FileProcessingError[];
  preview?: PreviewRecord[];
  detectedTableType?: 'users' | 'user_roles' | 'applications' | 'mixed' | 'unknown';
}

export interface FileProcessingError {
  errorId: string;
  rowNumber?: number;
  column?: string;
  errorType: 'validation' | 'parsing' | 'business_rule' | 'duplicate' | 'system';
  errorMessage: string;
  suggestedFix?: string;
  severity: 'error' | 'warning' | 'info';
}

export interface PreviewRecord {
  rowNumber: number;
  data: { [column: string]: any };
  isValid: boolean;
  errors: FileProcessingError[];
  detectedType?: 'user' | 'user_role' | 'application';
}

// Bulk Upload Options
export interface BulkUploadOptions {
  strategy: 'core' | 'multiple' | 'progress' | 'queue';
  parseOptions: {
    detectTableTypes: boolean;
    skipEmptyRows: boolean;
    trimWhitespace: boolean;
    validateFormat: boolean;
    headerRow: number;
  };
  processingOptions: {
    validateOnly: boolean;
    dryRun: boolean;
    autoProvision: boolean;
    batchSize: number;
    continueOnError: boolean;
  };
  notificationOptions: {
    notifyOnStart: boolean;
    notifyOnProgress: boolean;
    notifyOnCompletion: boolean;
    emailRecipients: string[];
  };
}

// Migration Progress Types
export interface MigrationSession {
  sessionId: string;
  jobId: string;
  sessionName: string;
  startedAt: string;
  completedAt?: string;
  status: 'preparing' | 'in_progress' | 'paused' | 'completed' | 'failed' | 'cancelled';
  totalFiles: number;
  processedFiles: number;
  totalRecords: number;
  processedRecords: number;
  successfulRecords: number;
  failedRecords: number;
  overallProgress: number;
  currentOperation?: string;
  estimatedTimeRemaining?: string;
  errors: MigrationError[];
  warnings: MigrationWarning[];
}

export interface MigrationError {
  errorId: string;
  timestamp: string;
  errorType: 'file_parsing' | 'validation' | 'database' | 'business_rule' | 'system';
  errorMessage: string;
  fileName?: string;
  rowNumber?: number;
  column?: string;
  stackTrace?: string;
  isCritical: boolean;
}

export interface MigrationWarning {
  warningId: string;
  timestamp: string;
  warningType: 'data_quality' | 'performance' | 'duplicate' | 'format';
  warningMessage: string;
  fileName?: string;
  rowNumber?: number;
  suggestedAction?: string;
}

// Component State Types
export interface ProvisioningOverviewState {
  metrics: ProvisioningMetrics | null;
  trends: ProvisioningTrend[];
  recentActivity: ProvisioningActivity[];
  isLoading: boolean;
  error: string | null;
  lastUpdated?: string;
}

export interface ProvisioningActivity {
  activityId: string;
  timestamp: string;
  activityType: 'request_submitted' | 'auto_provisioned' | 'manually_approved' | 'bulk_upload' | 'template_applied';
  description: string;
  userId?: string;
  userName?: string;
  recordsAffected: number;
  success: boolean;
}

// Form Types
export interface ProvisioningRequestForm {
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber: string;
  requestedRole: string;
  requestedApplications: string[];
  businessJustification: string;
  requestPriority: 'low' | 'medium' | 'high' | 'urgent';
  estimatedStartDate: string;
  managerEmail?: string;
  notes?: string;
}

export interface TemplateForm {
  templateName: string;
  description: string;
  roleId: string;
  defaultApplications: ApplicationAccess[];
  autoApprovalEnabled: boolean;
  requiresManagerApproval: boolean;
  isActive: boolean;
}

// Filter and Search Types
export interface ProvisioningFilters {
  status: string[];
  priority: string[];
  role: string[];
  dateRange: {
    start?: string;
    end?: string;
  };
  searchTerm: string;
  sortBy: string;
  sortDirection: 'asc' | 'desc';
}

// API Response Types specific to Provisioning
export interface ProvisioningOverviewResponse {
  metrics: ProvisioningMetrics;
  recentTrends: ProvisioningTrend[];
  recentActivity: ProvisioningActivity[];
}

export interface PendingRequestsResponse extends PaginatedResponse<PendingProvisioningRequest> {
  summary: {
    totalPending: number;
    highPriority: number;
    awaitingApproval: number;
    processingTime: {
      average: number;
      median: number;
    };
  };
}

export interface TemplatesResponse extends PaginatedResponse<ProvisioningTemplate> {
  activeTemplates: number;
  mostUsedTemplate: string;
}

// Event Types for component communication
export interface ProvisioningEvents {
  onRequestApproved: (requestId: string) => void;
  onRequestRejected: (requestId: string, reason: string) => void;
  onBulkUploadStarted: (jobId: string) => void;
  onBulkUploadCompleted: (jobId: string, results: BulkUploadJobComplete) => void;
  onAutoProvisioningTriggered: (templateId?: string) => void;
  onTemplateCreated: (template: ProvisioningTemplate) => void;
  onTemplateUpdated: (templateId: string, template: Partial<ProvisioningTemplate>) => void;
}

// Utility Types
export type ProvisioningStatus = 'idle' | 'loading' | 'processing' | 'completed' | 'error';
export type FileUploadStatus = 'pending' | 'uploading' | 'processing' | 'completed' | 'failed';
export type RequestPriority = 'low' | 'medium' | 'high' | 'urgent';
export type ApprovalStatus = 'pending' | 'approved' | 'rejected' | 'escalated';

// Component Props Types
export interface BaseProvisioningProps {
  className?: string;
  onError?: (error: string) => void;
  onSuccess?: (message: string) => void;
}

export interface ProvisioningOverviewProps extends BaseProvisioningProps {
  refreshInterval?: number;
  showTrends?: boolean;
  showRecentActivity?: boolean;
}

export interface PendingRequestsProps extends BaseProvisioningProps {
  filters?: Partial<ProvisioningFilters>;
  onRequestAction?: (requestId: string, action: string) => void;
  pageSize?: number;
}

export interface BulkUploadProps extends BaseProvisioningProps {
  allowedFileTypes?: string[];
  maxFileSize?: number;
  maxFiles?: number;
  onUploadComplete?: (results: BulkUploadJobComplete) => void;
  onProgressUpdate?: (progress: number) => void;
}

export interface AutoProvisioningProps extends BaseProvisioningProps {
  config?: Partial<AutoProvisioningConfig>;
  onConfigUpdate?: (config: AutoProvisioningConfig) => void;
}