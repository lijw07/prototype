// API Service Layer for centralized HTTP requests and error handling

interface ApiResponse<T = any> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

interface ConnectionTestResponse {
  success: boolean;
  message: string;
  connectionValid?: boolean;
  errors?: string[];
}

interface ApiError {
  message: string;
  status: number;
  errors?: string[];
}

class ApiService {
  private baseUrl: string;

  constructor(baseUrl: string = '') {
    this.baseUrl = baseUrl;
  }

  private getAuthHeaders(): HeadersInit {
    const token = localStorage.getItem('authToken');
    console.log('Auth token exists:', !!token);
    if (token) {
      console.log('Token length:', token.length);
    }
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
    console.log('DELETE request to:', fullUrl);
    const headers = this.getAuthHeaders();
    console.log('DELETE headers:', headers);
    const response = await fetch(fullUrl, {
      method: 'DELETE',
      headers: headers,
    });
    console.log('DELETE response status:', response.status);
    console.log('DELETE response headers:', Object.fromEntries(response.headers.entries()));
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
    api.post<ApiResponse>('/login', credentials),
  
  logout: () =>
    api.post<ApiResponse>('/logout'),
  
  register: (userData: {
    firstName: string;
    lastName: string;
    username: string;
    email: string;
    phoneNumber: string;
    password: string;
    reEnterPassword: string;
  }) =>
    api.post<ApiResponse>('/Register', userData),
  
  forgotPassword: (email: string, userRecoveryType: string) =>
    api.post<ApiResponse>('/ForgotUser', { email, userRecoveryType }),
  
  resetPassword: (token: string, newPassword: string, reTypePassword: string) =>
    api.post<ApiResponse>('/PasswordReset', { token, newPassword, reTypePassword }),
  
  verifyUser: (token: string) =>
    api.post<ApiResponse>(`/VerifyUser?token=${token}`),
};

// User Settings API
export const userApi = {
  getProfile: () =>
    api.get<{ success: boolean; user: any; message?: string }>('/settings/user/profile'),
  
  updateProfile: (userData: {
    firstName: string;
    lastName: string;
    email: string;
  }) =>
    api.put<{ success: boolean; message?: string; user?: any }>('/settings/user/update-profile', userData),
  
  changePassword: (passwordData: {
    currentPassword: string;
    newPassword: string;
    reTypeNewPassword: string;
  }) =>
    api.post<{ success: boolean; message?: string }>('/settings/user/change-password', passwordData),
  
  getAllUsers: (page: number = 1, pageSize: number = 10) =>
    api.get<{ success: boolean; data?: { data: any[]; page: number; pageSize: number; totalCount: number; totalPages: number }; users?: any[]; message?: string }>(`/settings/user/all?page=${page}&pageSize=${pageSize}`),

  getUserCounts: () =>
    api.get<{ success: boolean; data: { totalUsers: number; totalVerifiedUsers: number; totalTemporaryUsers: number }; message?: string }>('/settings/user/counts'),
  
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
    api.put<{ success: boolean; user?: any; message?: string }>('/settings/user/update', userData),
  
  deleteUser: (userId: string) => {
    const url = `/settings/user/delete/${userId}`;
    console.log('DELETE User URL:', url);
    return api.delete<{ success: boolean; message?: string }>(url);
  },
  
  updateTemporaryUser: (userData: {
    temporaryUserId: string;
    firstName: string;
    lastName: string;
    username: string;
    email: string;
    phoneNumber?: string;
  }) =>
    api.put<{ success: boolean; user?: any; message?: string }>('/settings/user/update-temporary', userData),
  
  deleteTemporaryUser: (temporaryUserId: string) => {
    const url = `/settings/user/delete-temporary/${temporaryUserId}`;
    console.log('DELETE Temporary User URL:', url);
    return api.delete<{ success: boolean; message?: string }>(url);
  },
};

// Application Settings API
export const applicationApi = {
  getApplications: (page: number = 1, pageSize: number = 10) =>
    api.get<ApiResponse<any>>(`/settings/applications/get-applications?page=${page}&pageSize=${pageSize}`),
  
  createApplication: (applicationData: any) =>
    api.post<ApiResponse>('/settings/applications/new-application-connection', applicationData),
  
  updateApplication: (applicationId: string, applicationData: any) =>
    api.put<ApiResponse>(`/settings/applications/update-application/${applicationId}`, applicationData),
  
  deleteApplication: (applicationId: string) =>
    api.delete<ApiResponse>(`/settings/applications/delete-application/${applicationId}`),
  
  testConnection: (connectionData: any) =>
    api.post<ConnectionTestResponse>('/settings/applications/test-application-connection', connectionData),
};

// Role Settings API
export const roleApi = {
  getAllRoles: (page: number = 1, pageSize: number = 10) =>
    api.get<{ success: boolean; data?: { data: any[]; page: number; pageSize: number; totalCount: number; totalPages: number }; roles?: any[]; message?: string }>(`/settings/roles?page=${page}&pageSize=${pageSize}`),
  
  getRoleById: (roleId: string) =>
    api.get<{ success: boolean; role: any; message?: string }>(`/settings/roles/${roleId}`),
  
  createRole: (roleData: { roleName: string }) =>
    api.post<{ success: boolean; role?: any; message?: string }>('/settings/roles', roleData),
  
  updateRole: (roleId: string, roleData: { roleName: string }) =>
    api.put<{ success: boolean; role?: any; message?: string }>(`/settings/roles/${roleId}`, roleData),
  
  getRoleDeletionConstraints: (roleId: string) =>
    api.get<{ success: boolean; canDelete: boolean; usersCount: number; temporaryUsersCount: number; constraintMessage: string; roleName: string }>(`/settings/roles/${roleId}/deletion-constraints`),
  
  deleteRole: (roleId: string) =>
    api.delete<{ success: boolean; message?: string }>(`/settings/roles/${roleId}`),
};

// Dashboard API
export const dashboardApi = {
  getStatistics: () =>
    api.get<{ success: boolean; data: any; message?: string }>('/Dashboard/statistics'),
};

// Audit Logs API
export const auditLogApi = {
  getAuditLogs: (page: number = 1, pageSize: number = 100) =>
    api.get<{ data: any[]; page: number; pageSize: number; totalCount: number; totalPages: number }>(`/AuditLogSettings?page=${page}&pageSize=${pageSize}`),
};

// User Activity Logs API
export const activityLogApi = {
  getActivityLogs: (page: number = 1, pageSize: number = 100) =>
    api.get<{ data: any[]; page: number; pageSize: number; totalCount: number; totalPages: number }>(`/UserActivitySettings?page=${page}&pageSize=${pageSize}`),
};

// Application Logs API
export const applicationLogApi = {
  getApplicationLogs: (page: number = 1, pageSize: number = 100) =>
    api.get<{ success: boolean; data: { data: any[]; page: number; pageSize: number; totalCount: number; totalPages: number } }>(`/ApplicationLogSettings?page=${page}&pageSize=${pageSize}`),
};

// Security Dashboard API
export const securityDashboardApi = {
  getSecurityOverview: () =>
    api.get<{ success: boolean; data: any }>('/api/security-dashboard/overview'),
  
  getFailedLogins: (days: number = 7) =>
    api.get<{ success: boolean; data: any[] }>(`/api/security-dashboard/failed-logins?days=${days}`),
};

// System Health API
export const systemHealthApi = {
  getHealthOverview: () =>
    api.get<{ success: boolean; data: any }>('/api/system-health/overview'),
    
  getDatabaseConnections: () =>
    api.get<{ success: boolean; data: any[] }>('/api/system-health/database-connections'),
    
  getPerformanceMetrics: () =>
    api.get<{ success: boolean; data: any }>('/api/system-health/performance-metrics'),
};

// Executive Dashboard API
// Executive Dashboard API
export const executiveDashboardApi = {
  getExecutiveOverview: () =>
    api.get<{ success: boolean; data: any }>('/api/executive-dashboard/overview'),
    
  getBusinessMetrics: () =>
    api.get<{ success: boolean; data: any }>('/api/executive-dashboard/business-metrics'),
    
  getGrowthTrends: (months: number = 6) =>
    api.get<{ success: boolean; data: any }>(`/api/executive-dashboard/growth-trends?months=${months}`),
};

// User Provisioning API
export const userProvisioningApi = {
  getProvisioningOverview: () =>
    api.get<{ success: boolean; data: any }>('/api/user-provisioning/overview'),
    
  getPendingRequests: (page: number = 1, pageSize: number = 10) =>
    api.get<{ success: boolean; data: any }>(`/api/user-provisioning/pending-requests?page=${page}&pageSize=${pageSize}`),
    
  autoProvisionUsers: (request: any) =>
    api.post<{ success: boolean; data: any }>('/api/user-provisioning/auto-provision', request),
    
  bulkProvisionUsers: (request: any) =>
    api.post<{ success: boolean; data: any }>('/api/user-provisioning/bulk-provision', request),
    
  getProvisioningTemplates: () =>
    api.get<{ success: boolean; data: any }>('/api/user-provisioning/provisioning-templates'),
};

// Compliance API
export const complianceApi = {
  getComplianceOverview: () =>
    api.get<{ success: boolean; data: any }>('/api/compliance/overview'),
    
  generateAuditReport: (period: string = '30', format: string = 'summary') =>
    api.get<{ success: boolean; data: any }>(`/api/compliance/audit-report?period=${period}&format=${format}`),
    
  getPolicyViolations: (page: number = 1, pageSize: number = 10) =>
    api.get<{ success: boolean; data: any }>(`/api/compliance/policy-violations?page=${page}&pageSize=${pageSize}`),
    
  getComplianceFrameworks: () =>
    api.get<{ success: boolean; data: any }>('/api/compliance/frameworks'),
    
  generateCustomReport: (request: any) =>
    api.post<{ success: boolean; data: any }>('/api/compliance/generate-report', request),
};

export default api;
export type { ApiResponse, ConnectionTestResponse, ApiError };