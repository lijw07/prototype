// API Service Layer for centralized HTTP requests and error handling

// Import types from centralized type definitions
import {
  ApiResponse,
  ApiError,
  ConnectionTestResponse,
  User,
  TemporaryUser,
  Role,
  RoleDeletionConstraints,
  Application,
  PaginatedResponse,
  LoginResponse,
  RegisterRequest,
  DashboardStatistics,
  UserCounts,
  AuditLog,
  UserActivityLog,
  ApplicationLog,
  SecurityOverview,
  SystemHealthMetrics
} from '../types/api.types';

class ApiService {
  private baseUrl: string;

  constructor(baseUrl: string = '') {
    this.baseUrl = baseUrl;
  }

  private getAuthHeaders(): HeadersInit {
    const token = localStorage.getItem('authToken');
    return {
      'Content-Type': 'application/json',
      ...(token && { 'Authorization': `Bearer ${token}` }),
    };
  }

  private async handleResponse<T>(response: Response): Promise<T> {
    let data: any;
    
    try {
      data = await response.json();
    } catch {
      data = { message: 'Invalid response format' };
    }

    if (!response.ok) {
      const error: ApiError = {
        message: data.message || `HTTP ${response.status}: ${response.statusText}`,
        status: response.status,
        errors: data.errors,
      };
      throw error;
    }

    return data;
  }

  async get<T>(endpoint: string): Promise<T> {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      method: 'GET',
      headers: this.getAuthHeaders(),
    });
    return this.handleResponse<T>(response);
  }

  async post<T>(endpoint: string, data?: any): Promise<T> {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: data ? JSON.stringify(data) : undefined,
    });
    return this.handleResponse<T>(response);
  }

  async put<T>(endpoint: string, data?: any): Promise<T> {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      method: 'PUT',
      headers: this.getAuthHeaders(),
      body: data ? JSON.stringify(data) : undefined,
    });
    return this.handleResponse<T>(response);
  }

  async delete<T>(endpoint: string): Promise<T> {
    const fullUrl = `${this.baseUrl}${endpoint}`;
    const headers = this.getAuthHeaders();
    const response = await fetch(fullUrl, {
      method: 'DELETE',
      headers: headers,
    });
    return this.handleResponse<T>(response);
  }
}

// Create singleton instance with proper base URL
const getApiBaseUrl = () => {
  // In development, use localhost for browser access
  if (process.env.NODE_ENV === 'development') {
    // Always use localhost for browser access, regardless of REACT_APP_API_URL
    return 'http://localhost:8080';
  }
  // In production, use relative URLs (handled by proxy)
  return '';
};

export const api = new ApiService(getApiBaseUrl());

// Authentication API
export const authApi = {
  login: (credentials: { username: string; password: string }) =>
    api.post<ApiResponse<LoginResponse>>('/login', credentials),
  
  logout: () =>
    api.post<ApiResponse<{ message: string }>>('/logout'),
  
  register: (userData: RegisterRequest) =>
    api.post<ApiResponse>('/login/register', userData),
  
  forgotPassword: (email: string, userRecoveryType: string) =>
    api.post<ApiResponse>('/login/forgot-user', { email, userRecoveryType }),
  
  resetPassword: (token: string, newPassword: string, reTypePassword: string) =>
    api.post<ApiResponse>('/login/password-reset', { token, newPassword, reTypePassword }),
  
  verifyUser: (token: string) =>
    api.post<ApiResponse>(`/login/verify-user?token=${token}`),
};

// User Settings API
export const userApi = {
  getProfile: () =>
    api.get<ApiResponse<{ user: User }>>('/settings/user-profile'),
  
  updateProfile: (userData: {
    firstName: string;
    lastName: string;
    email: string;
  }) =>
    api.put<ApiResponse<{ user: User }>>('/settings/user-profile', userData),
  
  changePassword: (passwordData: {
    currentPassword: string;
    newPassword: string;
    reTypeNewPassword: string;
  }) =>
    api.post<ApiResponse<{ message: string }>>('/settings/user-profile', passwordData),
  
  getAllUsers: (page: number = 1, pageSize: number = 10) =>
    api.get<ApiResponse<PaginatedResponse<User>>>(`/navigation/user-administration/all?page=${page}&pageSize=${pageSize}`),

  getUserCounts: () =>
    api.get<ApiResponse<UserCounts>>('/navigation/user-administration/counts'),
  
  updateUser: (userData: {
    userId: string;
    firstName: string;
    lastName: string;
    username: string;
    email: string;
    phoneNumber?: string;
    role: string;
    isActive: boolean;
  }) =>
    api.put<ApiResponse<{ message: string; user?: any }>>('/navigation/user-administration/update', userData),
  
  deleteUser: (userId: string) => {
    const url = `/navigation/user-administration/delete/${userId}`;
    return api.delete<ApiResponse<{ message: string }>>(url);
  },
  
  updateTemporaryUser: (userData: {
    temporaryUserId: string;
    firstName: string;
    lastName: string;
    username: string;
    email: string;
    phoneNumber?: string;
  }) =>
    api.put<ApiResponse<{ user: TemporaryUser }>>('/navigation/temporary-user-management/update', userData),
  
  deleteTemporaryUser: (temporaryUserId: string) => {
    const url = `/navigation/temporary-user-management/delete/${temporaryUserId}`;
    return api.delete<ApiResponse<{ success: boolean; message?: string }>>(url);
  },
};

// Application Settings API
export const applicationApi = {
  getApplications: (page: number = 1, pageSize: number = 10) =>
    api.get<ApiResponse<PaginatedResponse<Application>>>(`/navigation/applications/get-applications?page=${page}&pageSize=${pageSize}`),
  
  createApplication: (applicationData: Partial<Application>) =>
    api.post<ApiResponse<{ application: Application }>>('/navigation/applications/new-application-connection', applicationData),
  
  updateApplication: (applicationId: string, applicationData: Partial<Application>) =>
    api.put<ApiResponse<{ application: Application }>>(`/navigation/applications/update-application/${applicationId}`, applicationData),
  
  deleteApplication: (applicationId: string) =>
    api.delete<ApiResponse>(`/navigation/applications/delete-application/${applicationId}`),
  
  testConnection: (connectionData: any) =>
    api.post<ConnectionTestResponse>('/navigation/applications/test-application-connection', connectionData),
};

// Role Settings API
export const roleApi = {
  getAllRoles: (page: number = 1, pageSize: number = 10) =>
    api.get<ApiResponse<PaginatedResponse<Role>>>(`/navigation/roles?page=${page}&pageSize=${pageSize}`),
  
  getRoleById: (roleId: string) =>
    api.get<ApiResponse<{ role: Role }>>(`/navigation/roles/${roleId}`),
  
  createRole: (roleData: { roleName: string }) =>
    api.post<ApiResponse<{ message: string; role: Role }>>('/navigation/roles', roleData),
  
  updateRole: (roleId: string, roleData: { roleName: string }) =>
    api.put<ApiResponse<{ message: string; role: Role }>>(`/navigation/roles/${roleId}`, roleData),
  
  getRoleDeletionConstraints: (roleId: string) =>
    api.get<ApiResponse<RoleDeletionConstraints>>(`/navigation/roles/${roleId}/deletion-constraints`),
  
  deleteRole: (roleId: string) =>
    api.delete<ApiResponse<{ message: string }>>(`/navigation/roles/${roleId}`),
};

// Dashboard API
export const dashboardApi = {
  getStatistics: () =>
    api.get<ApiResponse<DashboardStatistics>>('/navigation/dashboard/statistics'),
};

// Audit Logs API
export const auditLogApi = {
  getAuditLogs: (page: number = 1, pageSize: number = 100) =>
    api.get<ApiResponse<PaginatedResponse<AuditLog>>>(`/navigation/audit-log?page=${page}&pageSize=${pageSize}`),
};

// User Activity Logs API
export const activityLogApi = {
  getActivityLogs: (page: number = 1, pageSize: number = 100) =>
    api.get<ApiResponse<PaginatedResponse<UserActivityLog>>>(`/navigation/user-activity?page=${page}&pageSize=${pageSize}`),
};

// Application Logs API
export const applicationLogApi = {
  getApplicationLogs: (page: number = 1, pageSize: number = 100) =>
    api.get<ApiResponse<PaginatedResponse<ApplicationLog>>>(`/navigation/application-log?page=${page}&pageSize=${pageSize}`),
};

// Security Dashboard API
export const securityDashboardApi = {
  getSecurityOverview: () =>
    api.get<ApiResponse<SecurityOverview>>('/navigation/security-dashboard/overview'),
  
  getFailedLogins: (days: number = 7) =>
    api.get<ApiResponse<UserActivityLog[]>>(`/navigation/security-dashboard/failed-logins?days=${days}`),
};

// System Health API
export const systemHealthApi = {
  getHealthOverview: () =>
    api.get<ApiResponse<SystemHealthMetrics>>('/navigation/system-health/overview'),
    
  getDatabaseConnections: () =>
    api.get<ApiResponse<any[]>>('/navigation/system-health/database-connections'),
    
  getPerformanceMetrics: () =>
    api.get<ApiResponse<any>>('/navigation/system-health/performance-metrics'),
};

// Executive Dashboard API
export const executiveDashboardApi = {
  getExecutiveOverview: () =>
    api.get<{ success: boolean; data: any }>('/api/executive-dashboard/overview'),
    
  getBusinessMetrics: () =>
    api.get<{ success: boolean; data: any }>('/api/executive-dashboard/business-metrics'),
    
  getGrowthTrends: (months: number = 6) =>
    api.get<{ success: boolean; data: any }>(`/api/executive-dashboard/growth-trends?months=${months}`),
};

// Analytics Overview API
export const analyticsOverviewApi = {
  getOverview: () =>
    api.get<{ success: boolean; data: any }>('/navigation/analytics-overview/overview'),
    
  getBusinessMetrics: () =>
    api.get<{ success: boolean; data: any }>('/navigation/analytics-overview/business-metrics'),
    
  getGrowthTrends: (months: number = 6) =>
    api.get<{ success: boolean; data: any }>(`/navigation/analytics-overview/growth-trends?months=${months}`),
};

// User Provisioning API
export const userProvisioningApi = {
  getProvisioningOverview: () =>
    api.get<{ success: boolean; data: any }>('/navigation/user-provisioning/overview'),
    
  getPendingRequests: (page: number = 1, pageSize: number = 10) =>
    api.get<{ success: boolean; data: any }>(`/navigation/user-provisioning/pending-requests?page=${page}&pageSize=${pageSize}`),
    
  autoProvisionUsers: (request: any) =>
    api.post<{ success: boolean; data: any }>('/navigation/user-provisioning/auto-provision', request),
    
  bulkProvisionUsers: (formData: FormData) =>
    fetch(`${getApiBaseUrl()}/bulk-upload/core/upload`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('authToken') || ''}`,
      },
      body: formData
    }).then(response => response.json()),
    
  bulkProvisionMultipleFiles: (formData: FormData) =>
    fetch(`${getApiBaseUrl()}/bulk-upload/multiple/upload-multiple`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('authToken') || ''}`,
      },
      body: formData
    }).then(response => response.json()),
    
  bulkProvisionWithProgress: (formData: FormData) =>
    fetch(`${getApiBaseUrl()}/bulk-upload/progress/upload-with-progress`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('authToken') || ''}`,
      },
      body: formData
    }).then(response => response.json()),
    
  bulkProvisionWithQueue: (formData: FormData) =>
    fetch(`${getApiBaseUrl()}/bulk-upload/queue/upload`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('authToken') || ''}`,
      },
      body: formData
    }).then(response => response.json()),
    
  getQueueStatus: (jobId: string) =>
    api.get<ApiResponse<any>>(`/bulk-upload/queue/status/${jobId}`),
    
  cancelQueue: (jobId: string) =>
    api.post<ApiResponse<any>>(`/bulk-upload/queue/cancel/${jobId}`),
    
  getProvisioningTemplates: () =>
    api.get<{ success: boolean; data: any }>('/navigation/user-provisioning/provisioning-templates'),
};

// Compliance API
export const complianceApi = {
  getComplianceOverview: () =>
    api.get<{ success: boolean; data: any }>('/navigation/compliance/overview'),
    
  generateAuditReport: (period: string = '30', format: string = 'summary') =>
    api.get<{ success: boolean; data: any }>(`/navigation/compliance/audit-report?period=${period}&format=${format}`),
    
  getPolicyViolations: (page: number = 1, pageSize: number = 10) =>
    api.get<{ success: boolean; data: any }>(`/navigation/compliance/policy-violations?page=${page}&pageSize=${pageSize}`),
    
  getComplianceFrameworks: () =>
    api.get<{ success: boolean; data: any }>('/navigation/compliance/frameworks'),
    
  generateCustomReport: (request: any) =>
    api.post<{ success: boolean; data: any }>('/navigation/compliance/generate-report', request),
};

// User Requests API
export const userRequestsApi = {
  getUserRequests: () =>
    api.get<{ success: boolean; data: any[] }>('/navigation/user-requests'),
    
  createRequest: (request: any) =>
    api.post<{ success: boolean; data: any }>('/navigation/user-requests', request),
    
  getRequestById: (id: string) =>
    api.get<{ success: boolean; data: any }>(`/navigation/user-requests/${id}`),
    
  updateRequestStatus: (id: string, status: string, comments?: string) =>
    api.put<{ success: boolean; data: any }>(`/navigation/user-requests/${id}/status`, { status, comments }),
    
  getAvailableTools: () =>
    api.get<{ success: boolean; data: any[] }>('/navigation/user-requests/available-tools'),
    
  cancelRequest: (id: string) =>
    api.delete<{ success: boolean; data: any }>(`/navigation/user-requests/${id}`),
};

export default api;
export type { ApiResponse, ConnectionTestResponse, ApiError };