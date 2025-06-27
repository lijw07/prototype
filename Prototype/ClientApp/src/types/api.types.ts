// Core API Response Types based on Backend Analysis
// These interfaces match the exact backend ApiResponse<T> structure

// Base API Response wrapper used by all endpoints
export interface ApiResponse<T = any> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
  fieldErrors?: { [field: string]: string[] };
  errorCode?: string;
  timestamp?: string;
}

// Pagination wrapper for list endpoints
export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// Error interface for API error handling
export interface ApiError {
  message: string;
  status: number;
  errors?: string[];
  fieldErrors?: { [field: string]: string[] };
  errorCode?: string;
}

// Connection Test Response (specific format from backend)
export interface ConnectionTestResponse {
  success: boolean;
  message: string;
  connectionValid?: boolean;
  errors?: string[];
}

// User-related types
export interface User {
  userId: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber: string; // Made required to match UserSettings interface
  isActive?: boolean;
  role?: string;
  lastLogin?: string;
  createdAt?: string;
  updatedAt?: string;
  isTemporary?: boolean;
}

export interface TemporaryUser {
  temporaryUserId: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber?: string;
  createdAt: string;
  expiresAt?: string;
}

// Role-related types
export interface Role {
  userRoleId: string;
  role: string;
  createdAt: string;
  createdBy: string;
}

export interface RoleDeletionConstraints {
  canDelete: boolean;
  usersCount: number;
  temporaryUsersCount: number;
  constraintMessage: string;
  roleName: string;
}

// Application-related types
export interface Application {
  applicationId: string;
  applicationName: string;
  applicationDescription: string;
  applicationDataSourceType: DataSourceType | number; // Components use number enum
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
  connectionId?: string;
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
}

export interface ApplicationConnection {
  applicationConnectionId: string;
  applicationId: string;
  host: string;
  port: string;
  databaseName?: string;
  username?: string;
  // Note: Password is never returned by the API for security
  connectionString?: string;
  isActive: boolean;
  testConnectionResult?: boolean;
  lastConnectionTest?: string;
  createdAt: string;
  updatedAt?: string;
}

// Data Source Types (matches backend enum)
export enum DataSourceType {
  MicrosoftSqlServer = 'MicrosoftSqlServer',
  MySql = 'MySql',
  PostgreSql = 'PostgreSql',
  MongoDb = 'MongoDb',
  Redis = 'Redis',
  Oracle = 'Oracle',
  MariaDb = 'MariaDb',
  Sqlite = 'Sqlite',
  Cassandra = 'Cassandra',
  ElasticSearch = 'ElasticSearch',
  RestApi = 'RestApi',
  GraphQL = 'GraphQL',
  SoapApi = 'SoapApi',
  ODataApi = 'ODataApi',
  WebSocket = 'WebSocket',
  CsvFile = 'CsvFile',
  JsonFile = 'JsonFile',
  XmlFile = 'XmlFile',
  ExcelFile = 'ExcelFile',
  ParquetFile = 'ParquetFile',
  YamlFile = 'YamlFile',
  AmazonS3 = 'AmazonS3',
  AzureBlobStorage = 'AzureBlobStorage',
  GoogleCloudStorage = 'GoogleCloudStorage'
}

// Logging types
export interface AuditLog {
  auditLogId: string;
  userId?: string;
  userName?: string;
  username: string; // Additional field used by components
  actionType: string;
  description: string;
  ipAddress?: string;
  userAgent?: string;
  timestamp: string;
  success: boolean;
  errorMessage?: string;
  metadata?: string; // Additional field used by components
  createdAt: string; // Additional field used by components
}

export interface UserActivityLog {
  userActivityLogId: string;
  userId: string;
  userName: string;
  username: string; // Additional field used by components
  actionType: string;
  description: string;
  ipAddress?: string;
  deviceInfo?: string;
  deviceInformation: string; // Additional field used by components
  timestamp: string;
  applicationId?: string;
  applicationName?: string;
}

export interface ApplicationLog {
  applicationLogId: string;
  applicationId: string;
  applicationName: string;
  logLevel: LogLevel;
  message: string;
  metadata?: string;
  timestamp: string;
  correlationId?: string;
  actionType: string; // Additional field used by components
  createdAt: string; // Additional field used by components
  updatedAt?: string; // Additional field used by components
}

export enum LogLevel {
  Debug = 'Debug',
  Information = 'Information',
  Warning = 'Warning',
  Error = 'Error',
  Critical = 'Critical'
}

// Authentication types
export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  message: string;
  token?: string;
  refreshToken?: string;
  user?: User;
  expiresAt?: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber: string;
  password: string;
  reEnterPassword: string;
}

// Dashboard statistics
export interface DashboardStatistics {
  totalApplications: number;
  totalRoles: number;
  totalUsers: number;
  totalVerifiedUsers: number;
  totalTemporaryUsers: number;
  recentActivity: number;
  systemHealth: 'healthy' | 'warning' | 'error';
  uptime: string;
  recentActivities: any[]; // Will be typed more specifically later
}

export interface UserCounts {
  totalUsers: number;
  totalVerifiedUsers: number;
  totalTemporaryUsers: number;
}

// Bulk Upload types (SignalR integration)
export interface BulkUploadProgress {
  jobId: string;
  progressPercentage: number;
  status: string;
  currentOperation?: string;
  processedRecords: number;
  totalRecords: number;
  currentFileName?: string;
  processedFiles: number;
  totalFiles: number;
  timestamp: string;
  errors?: string[];
}

export interface BulkUploadJobStart {
  jobId: string;
  jobType: string;
  totalFiles: number;
  estimatedTotalRecords: number;
  startTime: string;
}

export interface BulkUploadJobComplete {
  jobId: string;
  success: boolean;
  message: string;
  data?: any;
  completedAt: string;
  totalDuration: string;
}

export interface BulkUploadJobError {
  jobId: string;
  error: string;
  timestamp: string;
}

// Form validation types
export interface ValidationError {
  field: string;
  message: string;
}

export interface FormErrors {
  [field: string]: string | undefined;
}

// Common UI state types
export interface LoadingState {
  isLoading: boolean;
  error?: string | null;
}

export interface PaginationState {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// Security and system health types (flexible to match component expectations)
export interface SecurityOverview {
  failedLoginAttempts: number;
  activeUserSessions: number;
  recentSecurityEvents: number;
  lastSecurityScan?: string;
  systemHealthScore: number;
  securityFrameworks?: any[];
  riskLevel?: string;
  vulnerabilities?: any[];
}

export interface SystemHealthMetrics {
  databaseStatus?: 'healthy' | 'warning' | 'error';
  apiResponseTime?: number;
  memoryUsage?: number;
  cpuUsage?: number;
  activeConnections?: number;
  lastHealthCheck?: string;
  overall?: {
    status: string;
    healthScore: number;
    lastChecked: string;
    responseTime: number;
  };
  database?: {
    mainDatabase: string;
    applicationConnections: {
      healthy: number;
      total: number;
      percentage: number;
    };
  };
  performance?: {
    cpu: { usage: number; status: string };
    memory: { usage: number; status: string; available: string };
    disk: { usage: number; status: string; available: string };
    network: { status: string; latency: number };
  };
  services?: {
    authentication: string;
    authorization: string;
    api: string;
    database: string;
  };
}