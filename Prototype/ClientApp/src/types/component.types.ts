// Component-specific interfaces that extend or differ from API types
// These interfaces are used by components and may not match the backend exactly

import { User } from './api.types';

// Settings component interface (more restrictive than User)
export interface UserSettings {
  userId: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber: string; // Required in settings
}

// Health Dashboard specific interface
export interface HealthOverview {
  overall: {
    status: string;
    healthScore: number;
    lastChecked: string;
    responseTime: number;
  };
  database: {
    mainDatabase: string;
    applicationConnections: {
      healthy: number;
      total: number;
      percentage: number;
    };
  };
  performance: {
    cpu: { usage: number; status: string };
    memory: { usage: number; status: string; available: string };
    disk: { usage: number; status: string; available: string };
    network: { status: string; latency: number };
  };
  services: {
    authentication: string;
    authorization: string;
    api: string;
    database: string;
  };
}

// Security Dashboard specific interface
export interface SecurityData {
  failedLoginAttempts: number;
  activeUserSessions: number;
  recentSecurityEvents: number;
  lastSecurityScan?: string;
  systemHealthScore: number;
  securityFrameworks?: any[];
  riskLevel?: string;
  vulnerabilities?: any[];
}

// Application component interface (includes embedded connection)
export interface ApplicationWithConnection {
  applicationId: string;
  applicationName: string;
  applicationDescription: string;
  applicationDataSourceType: number;
  connection: {
    host: string;
    port: string;
    databaseName: string;
    authenticationType: string;
    username?: string;
    authenticationDatabase?: string;
    awsAccessKeyId?: string;
    awsRoleArn?: string;
    principal?: string;
    serviceName?: string;
    serviceRealm?: string;
    canonicalizeHostName?: boolean;
  };
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
}

// Extended User interface with all optional fields for different component needs
export interface ExtendedUser extends User {
  isActive: boolean;
  role: string;
  createdAt: string;
  phoneNumber?: string; // Optional for backward compatibility
}

// Form-specific interfaces
export interface UserFormData {
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber: string;
  role: string;
  isActive: boolean;
}

export interface EditUserForm {
  userId: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber: string;
  role: string;
  isActive: boolean;
}

export interface NewUserForm {
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber: string;
  password: string;
  reEnterPassword: string;
  role: string;
}

// Component state interfaces
export interface LoadingState {
  isLoading: boolean;
  error?: string | null;
}

export interface PaginationData {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// Dashboard specific interfaces
export interface DashboardLogEntry {
  id: string;
  type: 'audit' | 'activity' | 'application';
  message: string;
  timestamp: string;
  user?: string;
  severity: 'info' | 'warning' | 'error';
}

export interface DashboardStats {
  totalApplications: number;
  totalRoles: number;
  totalUsers: number;
  totalVerifiedUsers: number;
  totalTemporaryUsers: number;
  recentActivity: number;
  systemHealth: 'healthy' | 'warning' | 'error';
  uptime: string;
  recentActivities: DashboardLogEntry[];
}

// Filter and search interfaces
export interface FilterState {
  searchTerm: string;
  filterRole: string;
  filterStatus: string;
  filterVerification: string;
  sortBy: string;
}

export interface ApplicationFilter {
  searchTerm: string;
  dataSourceType: string;
  status: string;
  sortBy: string;
}

// Modal and UI state interfaces
export interface ModalState {
  isOpen: boolean;
  data?: any;
  type?: string;
}

export interface ConfirmationModalProps {
  isOpen: boolean;
  title: string;
  message: string;
  onConfirm: () => void;
  onCancel: () => void;
  confirmText?: string;
  cancelText?: string;
  variant?: 'danger' | 'warning' | 'info';
}

// Error handling interfaces
export interface FormError {
  field: string;
  message: string;
}

export interface ValidationErrors {
  [field: string]: string | undefined;
}

// Migration and bulk upload interfaces (component-specific)
export interface MigrationProgress {
  isActive: boolean;
  isMinimized: boolean;
  progress: number;
  status: string;
  currentFile?: string;
  totalFiles?: number;
  processedFiles?: number;
  errors: string[];
  jobId?: string;
}

export interface BulkUploadState {
  isUploading: boolean;
  progress: number;
  status: string;
  errors: string[];
  results: any[];
  jobId?: string;
}

// Navigation and routing interfaces
export interface NavigationItem {
  id: string;
  name: string;
  href: string;
  icon: any; // Lucide icon component
  requiresRole?: string[];
  isActive?: boolean;
}

// Theme and styling interfaces
export interface ThemeConfig {
  primaryColor: string;
  secondaryColor: string;
  backgroundColor: string;
  textColor: string;
  borderRadius: string;
}

// Component prop interfaces
export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string;
  align?: 'left' | 'center' | 'right';
  render?: (value: any, row: any) => React.ReactNode;
}

export interface PaginationProps {
  currentPage: number;
  totalPages: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
}