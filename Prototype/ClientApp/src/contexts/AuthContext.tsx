import React, { createContext, useContext, useEffect, useState, useCallback, ReactNode } from 'react';

// Helper function to get API base URL (same as in api.ts)
const getApiBaseUrl = () => {
  // In development, use relative URLs to leverage the proxy
  return '';
};

interface User {
  userId: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber: string;
  role: string;
}

interface LoginCredentials {
  username: string;
  password: string;
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  loading: boolean;
  login: (credentials: LoginCredentials) => Promise<{ success: boolean; message: string }>;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const clearAuthState = useCallback(() => {
    setUser(null);
    setToken(null);
    localStorage.removeItem('authToken');
  }, []);

  const fetchUserProfile = useCallback(async (authToken: string) => {
    try {
      const response = await fetch(`${getApiBaseUrl()}/settings/user-profile`, {
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const data = await response.json();
        if (data.success && data.user) {
          setUser(data.user);
        } else {
          // Invalid token, clear auth state
          clearAuthState();
        }
      } else {
        // Unauthorized, clear auth state
        clearAuthState();
      }
    } catch (error) {
      console.error('Failed to fetch user profile:', error);
      clearAuthState();
    }
  }, [clearAuthState]);

  // Initialize auth state from localStorage on mount
  useEffect(() => {
    const initializeAuth = async () => {
      const savedToken = localStorage.getItem('authToken');
      if (savedToken) {
        setToken(savedToken);
        await fetchUserProfile(savedToken);
      }
      setLoading(false);
    };

    initializeAuth();
  }, [fetchUserProfile]);

  const login = async (credentials: LoginCredentials): Promise<{ success: boolean; message: string }> => {
    try {
      setLoading(true);
      const response = await fetch(`${getApiBaseUrl()}/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(credentials),
      });

      // Check if response has content before parsing
      let data: any = {};
      const contentType = response.headers.get("content-type");
      if (contentType && contentType.indexOf("application/json") !== -1) {
        const text = await response.text();
        if (text) {
          try {
            data = JSON.parse(text);
          } catch (e) {
            console.error('Failed to parse JSON response:', e);
            data = { success: false, message: 'Invalid response from server' };
          }
        }
      }

      if (response.ok && data.success) {
        const authToken = data.token;
        setToken(authToken);
        localStorage.setItem('authToken', authToken);
        
        // Fetch user profile
        await fetchUserProfile(authToken);
        
        return { success: true, message: data.message || 'Login successful' };
      } else {
        // Handle different error scenarios
        if (response.status === 401) {
          return { success: false, message: data.message || 'Invalid credentials' };
        } else if (response.status === 503) {
          return { success: false, message: 'Authentication service temporarily unavailable' };
        } else {
          return { success: false, message: data.message || 'Login failed' };
        }
      }
    } catch (error) {
      console.error('Login error:', error);
      return { success: false, message: 'Network error occurred' };
    } finally {
      setLoading(false);
    }
  };

  const logout = async () => {
    try {
      // Call logout endpoint if token exists
      if (token) {
        await fetch(`${getApiBaseUrl()}/logout`, {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`,
          },
        });
      }
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      clearAuthState();
    }
  };


  const isAuthenticated = !!token && !!user;

  const value: AuthContextType = {
    user,
    token,
    loading,
    login,
    logout,
    isAuthenticated,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

export default AuthContext;