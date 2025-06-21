import React, { useState, useEffect } from 'react';
import { 
  Users, 
  Search, 
  Edit3, 
  Trash2, 
  Mail, 
  Phone, 
  Calendar,
  Shield,
  CheckCircle2,
  XCircle,
  MoreVertical,
  UserPlus,
  AlertCircle,
  Eye,
  EyeOff,
  Lock,
  Unlock,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight
} from 'lucide-react';
import { userApi, roleApi } from '../../services/api';
import { authApi } from '../../services/api';

interface Role {
  userRoleId: string;
  role: string;
  createdAt: string;
  createdBy: string;
}

interface User {
  userId: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber?: string;
  isActive: boolean;
  role: string;
  lastLogin?: string;
  createdAt: string;
}

interface NewUserForm {
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber: string;
  password: string;
  reEnterPassword: string;
}

interface EditUserForm {
  userId: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber: string;
  role: string;
  isActive: boolean;
}

export default function Accounts() {
  const [users, setUsers] = useState<User[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterRole, setFilterRole] = useState<string>('all');
  const [showAddUser, setShowAddUser] = useState(false);
  const [userDetailModal, setUserDetailModal] = useState<User | null>(null);
  const [showEditUser, setShowEditUser] = useState(false);
  const [editUserForm, setEditUserForm] = useState<EditUserForm>({
    userId: '',
    firstName: '',
    lastName: '',
    username: '',
    email: '',
    phoneNumber: '',
    role: '',
    isActive: true
  });
  const [editFormErrors, setEditFormErrors] = useState<Partial<EditUserForm>>({});
  const [isEditSubmitting, setIsEditSubmitting] = useState(false);
  const [editSubmitSuccess, setEditSubmitSuccess] = useState(false);
  const [loading, setLoading] = useState(true);
  
  // Pagination state
  const [currentPage, setCurrentPage] = useState(1);
  const [usersPerPage, setUsersPerPage] = useState(20);
  
  // New User Form State
  const [newUserForm, setNewUserForm] = useState<NewUserForm>({
    firstName: '',
    lastName: '',
    username: '',
    email: '',
    phoneNumber: '',
    password: '',
    reEnterPassword: ''
  });
  const [formErrors, setFormErrors] = useState<Partial<NewUserForm>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showReEnterPassword, setShowReEnterPassword] = useState(false);
  const [submitSuccess, setSubmitSuccess] = useState(false);

  // ESC key handler for modal
  useEffect(() => {
    const handleEscKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && showAddUser) {
        resetAddUserModal();
      }
    };

    if (showAddUser) {
      document.addEventListener('keydown', handleEscKey);
    }

    return () => {
      document.removeEventListener('keydown', handleEscKey);
    };
  }, [showAddUser]);

  // Load users and roles from database
  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      
      // Load users - this should always work regardless of roles
      try {
        const usersResponse = await userApi.getAllUsers();
        
        if (usersResponse.success && usersResponse.users) {
          const transformedUsers: User[] = usersResponse.users.map((user: any) => ({
            userId: user.userId,
            firstName: user.firstName,
            lastName: user.lastName,
            username: user.username,
            email: user.email,
            phoneNumber: user.phoneNumber,
            isActive: user.isActive,
            role: user.role,
            lastLogin: user.lastLogin,
            createdAt: user.createdAt
          }));
          
          setUsers(transformedUsers);
        } else {
          console.error('Failed to load users:', usersResponse.message);
          setUsers([]);
        }
      } catch (error) {
        console.error('Error loading users:', error);
        setUsers([]);
      }
      
      // Load roles - independent of users
      try {
        const rolesResponse = await roleApi.getAllRoles();
        
        if (rolesResponse.success && rolesResponse.roles) {
          setRoles(rolesResponse.roles);
        } else {
          console.error('Failed to load roles:', rolesResponse.message);
          setRoles([]);
        }
      } catch (error) {
        console.error('Error loading roles:', error);
        setRoles([]);
      }
      
      setLoading(false);
    };

    loadData();
  }, []);

  const filteredUsers = users.filter(user => {
    const matchesSearch = user.firstName.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         user.lastName.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         user.username.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         user.email.toLowerCase().includes(searchTerm.toLowerCase());
    
    const matchesRole = filterRole === 'all' || user.role.toLowerCase() === filterRole.toLowerCase();
    
    return matchesSearch && matchesRole;
  });

  // Pagination calculations
  const totalPages = Math.ceil(filteredUsers.length / usersPerPage);
  const startIndex = (currentPage - 1) * usersPerPage;
  const endIndex = startIndex + usersPerPage;
  const currentUsers = filteredUsers.slice(startIndex, endIndex);

  // Reset to first page when filters change
  useEffect(() => {
    setCurrentPage(1);
  }, [searchTerm, filterRole]);

  const getRoleBadgeColor = (role: string) => {
    // Create a consistent color mapping based on role name
    const colors = ['primary', 'danger', 'warning', 'info', 'success', 'secondary'];
    const hash = role.toLowerCase().split('').reduce((acc, char) => acc + char.charCodeAt(0), 0);
    return colors[hash % colors.length];
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  const formatLastLogin = (dateString?: string) => {
    if (!dateString) return 'Never';
    const now = new Date();
    const lastLogin = new Date(dateString);
    const diffInHours = Math.floor((now.getTime() - lastLogin.getTime()) / (1000 * 60 * 60));
    
    if (diffInHours < 1) return 'Just now';
    if (diffInHours < 24) return `${diffInHours}h ago`;
    const diffInDays = Math.floor(diffInHours / 24);
    if (diffInDays < 7) return `${diffInDays}d ago`;
    return formatDate(dateString);
  };

  // Form handling functions
  const handleInputChange = (field: keyof NewUserForm, value: string) => {
    setNewUserForm(prev => ({ ...prev, [field]: value }));
    // Clear error when user starts typing
    if (formErrors[field]) {
      setFormErrors(prev => ({ ...prev, [field]: undefined }));
    }
  };

  const validateForm = (): boolean => {
    const errors: Partial<NewUserForm> = {};

    // First name validation
    if (!newUserForm.firstName.trim()) {
      errors.firstName = 'First name is required';
    } else if (newUserForm.firstName.length > 50) {
      errors.firstName = 'First name must be between 1 and 50 characters';
    }

    // Last name validation
    if (!newUserForm.lastName.trim()) {
      errors.lastName = 'Last name is required';
    } else if (newUserForm.lastName.length > 50) {
      errors.lastName = 'Last name must be between 1 and 50 characters';
    }

    // Username validation
    if (!newUserForm.username.trim()) {
      errors.username = 'Username is required';
    } else if (newUserForm.username.length < 3 || newUserForm.username.length > 100) {
      errors.username = 'Username must be between 3 and 100 characters';
    } else if (!/^[a-zA-Z0-9_.-]+$/.test(newUserForm.username)) {
      errors.username = 'Username can only contain letters, numbers, underscores, dots, and hyphens';
    }

    // Email validation
    if (!newUserForm.email.trim()) {
      errors.email = 'Email is required';
    } else if (!/\S+@\S+\.\S+/.test(newUserForm.email)) {
      errors.email = 'Invalid email format';
    } else if (newUserForm.email.length > 255) {
      errors.email = 'Email cannot exceed 255 characters';
    }

    // Phone number validation (basic format)
    if (!newUserForm.phoneNumber.trim()) {
      errors.phoneNumber = 'Phone number is required';
    } else if (newUserForm.phoneNumber.length > 20) {
      errors.phoneNumber = 'Phone number cannot exceed 20 characters';
    }

    // Password validation to match backend requirements
    if (!newUserForm.password.trim()) {
      errors.password = 'Password is required';
    } else if (newUserForm.password.length < 8 || newUserForm.password.length > 128) {
      errors.password = 'Password must be between 8 and 128 characters';
    } else if (!/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/.test(newUserForm.password)) {
      errors.password = 'Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character (@$!%*?&)';
    }

    // Confirm password validation
    if (!newUserForm.reEnterPassword.trim()) {
      errors.reEnterPassword = 'Please confirm password';
    } else if (newUserForm.password !== newUserForm.reEnterPassword) {
      errors.reEnterPassword = 'Passwords do not match';
    }

    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmitNewUser = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) return;

    setIsSubmitting(true);
    setFormErrors({}); // Clear any previous errors
    
    try {
      console.log('Submitting user registration:', newUserForm);
      const response = await authApi.register(newUserForm);
      console.log('Registration response:', response);
      
      if (response && response.success) {
        setSubmitSuccess(true);
        // Reset form
        setNewUserForm({
          firstName: '',
          lastName: '',
          username: '',
          email: '',
          phoneNumber: '',
          password: '',
          reEnterPassword: ''
        });
        setFormErrors({});
        
        // Close modal after 2 seconds
        setTimeout(() => {
          setShowAddUser(false);
          setSubmitSuccess(false);
        }, 2000);
      } else {
        // Handle server errors
        console.error('Registration failed:', response);
        if (response?.errors) {
          // Handle field-specific validation errors from server
          const serverErrors: Partial<NewUserForm> = {};
          const errors = response.errors as unknown as { [key: string]: string | string[] };
          Object.keys(errors).forEach(key => {
            const fieldName = key.toLowerCase();
            const errorValue = errors[key];
            const errorMessage = Array.isArray(errorValue) ? errorValue[0] : errorValue;
            if (fieldName.includes('firstname')) serverErrors.firstName = errorMessage;
            else if (fieldName.includes('lastname')) serverErrors.lastName = errorMessage;
            else if (fieldName.includes('username')) serverErrors.username = errorMessage;
            else if (fieldName.includes('email')) serverErrors.email = errorMessage;
            else if (fieldName.includes('phone')) serverErrors.phoneNumber = errorMessage;
            else if (fieldName.includes('password')) serverErrors.password = errorMessage;
            else serverErrors.email = errorMessage;
          });
          setFormErrors(serverErrors);
        } else {
          setFormErrors({ email: response?.message || 'Registration failed' });
        }
      }
    } catch (error: any) {
      console.error('Registration error:', error);
      // Handle network or parsing errors
      if (error.status === 400 && error.errors) {
        const serverErrors: Partial<NewUserForm> = {};
        const errors = error.errors as unknown as { [key: string]: string | string[] };
        Object.keys(errors).forEach(key => {
          const fieldName = key.toLowerCase();
          const errorValue = errors[key];
          const errorMessage = Array.isArray(errorValue) ? errorValue[0] : errorValue;
          if (fieldName.includes('firstname')) serverErrors.firstName = errorMessage;
          else if (fieldName.includes('lastname')) serverErrors.lastName = errorMessage;
          else if (fieldName.includes('username')) serverErrors.username = errorMessage;
          else if (fieldName.includes('email')) serverErrors.email = errorMessage;
          else if (fieldName.includes('phone')) serverErrors.phoneNumber = errorMessage;
          else if (fieldName.includes('password')) serverErrors.password = errorMessage;
          else serverErrors.email = errorMessage;
        });
        setFormErrors(serverErrors);
      } else {
        setFormErrors({ email: error.message || 'Network error occurred' });
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const resetAddUserModal = () => {
    setNewUserForm({
      firstName: '',
      lastName: '',
      username: '',
      email: '',
      phoneNumber: '',
      password: '',
      reEnterPassword: ''
    });
    setFormErrors({});
    setIsSubmitting(false);
    setSubmitSuccess(false);
    setShowPassword(false);
    setShowReEnterPassword(false);
    setShowAddUser(false);
  };

  // Edit User Form handling
  const handleEditInputChange = (field: keyof EditUserForm, value: string | boolean) => {
    setEditUserForm(prev => ({ ...prev, [field]: value }));
    if (editFormErrors[field]) {
      setEditFormErrors(prev => ({ ...prev, [field]: undefined }));
    }
  };

  const validateEditForm = (): boolean => {
    const errors: Partial<EditUserForm> = {};

    if (!editUserForm.firstName.trim()) {
      errors.firstName = 'First name is required';
    } else if (editUserForm.firstName.length > 50) {
      errors.firstName = 'First name must be between 1 and 50 characters';
    }

    if (!editUserForm.lastName.trim()) {
      errors.lastName = 'Last name is required';
    } else if (editUserForm.lastName.length > 50) {
      errors.lastName = 'Last name must be between 1 and 50 characters';
    }

    if (!editUserForm.username.trim()) {
      errors.username = 'Username is required';
    } else if (editUserForm.username.length < 3 || editUserForm.username.length > 100) {
      errors.username = 'Username must be between 3 and 100 characters';
    } else if (!/^[a-zA-Z0-9_.-]+$/.test(editUserForm.username)) {
      errors.username = 'Username can only contain letters, numbers, underscores, dots, and hyphens';
    }

    if (!editUserForm.email.trim()) {
      errors.email = 'Email is required';
    } else if (!/\S+@\S+\.\S+/.test(editUserForm.email)) {
      errors.email = 'Invalid email format';
    } else if (editUserForm.email.length > 255) {
      errors.email = 'Email cannot exceed 255 characters';
    }

    if (editUserForm.phoneNumber && editUserForm.phoneNumber.length > 20) {
      errors.phoneNumber = 'Phone number cannot exceed 20 characters';
    }

    if (!editUserForm.role.trim()) {
      errors.role = 'Role is required';
    }

    setEditFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleEditUserSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateEditForm()) return;

    setIsEditSubmitting(true);
    setEditFormErrors({});
    
    try {
      const response = await userApi.updateUser(editUserForm);
      
      if (response && response.success) {
        setEditSubmitSuccess(true);
        // Update the user in the local state
        setUsers(prevUsers => 
          prevUsers.map(user => 
            user.userId === editUserForm.userId 
              ? { ...user, ...editUserForm }
              : user
          )
        );
        
        setTimeout(() => {
          setShowEditUser(false);
          setEditSubmitSuccess(false);
          resetEditUserModal();
        }, 2000);
      } else {
        setEditFormErrors({ email: response?.message || 'Update failed' });
      }
    } catch (error: any) {
      console.error('Update user error:', error);
      if (error.status === 400 && error.errors) {
        const serverErrors: Partial<EditUserForm> = {};
        const errors = error.errors as unknown as { [key: string]: string | string[] };
        Object.keys(errors).forEach(key => {
          const fieldName = key.toLowerCase();
          const errorValue = errors[key];
          const errorMessage = Array.isArray(errorValue) ? errorValue[0] : errorValue;
          if (fieldName.includes('firstname')) serverErrors.firstName = errorMessage;
          else if (fieldName.includes('lastname')) serverErrors.lastName = errorMessage;
          else if (fieldName.includes('username')) serverErrors.username = errorMessage;
          else if (fieldName.includes('email')) serverErrors.email = errorMessage;
          else if (fieldName.includes('phone')) serverErrors.phoneNumber = errorMessage;
          else serverErrors.email = errorMessage;
        });
        setEditFormErrors(serverErrors);
      } else {
        setEditFormErrors({ email: error.message || 'Network error occurred' });
      }
    } finally {
      setIsEditSubmitting(false);
    }
  };

  const resetEditUserModal = () => {
    setEditUserForm({
      userId: '',
      firstName: '',
      lastName: '',
      username: '',
      email: '',
      phoneNumber: '',
      role: '',
      isActive: true
    });
    setEditFormErrors({});
    setIsEditSubmitting(false);
    setEditSubmitSuccess(false);
    setShowEditUser(false);
  };

  // Dropdown action handlers
  const handleFreezeUser = async (user: User) => {
    try {
      const updatedUser = {
        ...user,
        isActive: !user.isActive
      };
      
      const response = await userApi.updateUser(updatedUser);
      
      if (response && response.success) {
        // Update the user in the local state
        setUsers(prevUsers => 
          prevUsers.map(u => 
            u.userId === user.userId 
              ? { ...u, isActive: !u.isActive }
              : u
          )
        );
        
        // Show success message (you could add a toast notification here)
        console.log(`User ${user.isActive ? 'frozen' : 'unfrozen'} successfully`);
      }
    } catch (error) {
      console.error('Error updating user status:', error);
    }
  };

  const handleEmailUser = (user: User) => {
    // Open email client with pre-filled recipient
    window.location.href = `mailto:${user.email}`;
  };

  const handleDeleteUser = async (user: User) => {
    if (window.confirm(`Are you sure you want to delete user ${user.firstName} ${user.lastName}? This action cannot be undone.`)) {
      try {
        console.log('Attempting to delete user:', user.userId);
        const response = await userApi.deleteUser(user.userId);
        console.log('Delete user response:', response);
        
        if (response && response.success) {
          // Remove from local state only after successful deletion from database
          setUsers(prevUsers => prevUsers.filter(u => u.userId !== user.userId));
          console.log(`User ${user.firstName} ${user.lastName} deleted successfully`);
        } else {
          console.error('Failed to delete user:', response?.message);
          alert(`Failed to delete user: ${response?.message || 'Unknown error'}`);
        }
      } catch (error: any) {
        console.error('Error deleting user - Full error object:', error);
        console.error('Error message:', error.message);
        console.error('Error status:', error.status);
        console.error('Error response:', error.response);
        alert(`Error deleting user: ${error.message || error.status || 'Network error occurred'}`);
      }
    }
  };

  if (loading) {
    return (
      <div className="min-vh-100 bg-light" style={{overflowX: 'hidden'}}>
        <div className="container-fluid py-4" style={{maxWidth: '100%'}}>
          {/* Header */}
          <div className="mb-4">
            <div className="d-flex align-items-center justify-content-between mb-2">
              <div className="d-flex align-items-center">
                <Users className="text-primary me-3" size={32} />
                <div>
                  <h1 className="display-5 fw-bold text-dark mb-0">User Accounts</h1>
                  <p className="text-muted fs-6 mb-0">Manage user accounts and permissions</p>
                </div>
              </div>
              <div className="d-flex gap-2">
                <button className="btn btn-primary rounded-3 d-flex align-items-center" disabled>
                  <UserPlus className="me-2" size={18} />
                  Add User
                </button>
              </div>
            </div>
          </div>
          
          {/* Loading Content */}
          <div className="card shadow-sm border-0 rounded-4">
            <div className="card-body p-4 text-center">
              <div className="spinner-border text-primary mb-3" role="status">
                <span className="visually-hidden">Loading...</span>
              </div>
              <p className="text-muted">Loading accounts...</p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-vh-100 bg-light" style={{overflowX: 'hidden'}}>
      <div className="container-fluid py-4" style={{maxWidth: '100%'}}>
        {/* Header */}
        <div className="mb-4">
          <div className="d-flex align-items-center justify-content-between mb-2">
            <div className="d-flex align-items-center">
              <Users className="text-primary me-3" size={32} />
              <div>
                <h1 className="display-5 fw-bold text-dark mb-0">User Accounts</h1>
                <p className="text-muted fs-6 mb-0">Manage user accounts and permissions</p>
              </div>
            </div>
            <div className="d-flex gap-2">
              <button 
                className="btn btn-primary rounded-3 d-flex align-items-center"
                onClick={() => setShowAddUser(true)}
              >
                <UserPlus className="me-2" size={18} />
                Add User
              </button>
            </div>
          </div>
        </div>

        {/* Filters and Search */}
        <div className="row mb-4">
          <div className="col-12">
            <div className="card border-0 rounded-4 shadow-sm">
              <div className="card-body p-4">
                <div className="row g-3">
                  <div className="col-md-6">
                    <div className="position-relative">
                      <Search className="position-absolute top-50 start-0 translate-middle-y ms-3 text-muted" size={18} />
                      <input
                        type="text"
                        className="form-control form-control-lg rounded-3 ps-5"
                        placeholder="Search users by name, username, or email..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                      />
                    </div>
                  </div>
                  <div className="col-md-3">
                    <select
                      className="form-select form-select-lg rounded-3"
                      value={filterRole}
                      onChange={(e) => setFilterRole(e.target.value)}
                    >
                      <option value="all">All Roles</option>
                      {roles.map((role) => (
                        <option key={role.userRoleId} value={role.role.toLowerCase()}>
                          {role.role}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="col-md-3">
                    <div className="bg-light rounded-3 p-2 text-center">
                      <div className="fw-bold text-primary">{filteredUsers.length}</div>
                      <div className="small text-muted">Total Users</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Users Grid */}
        <div className="row g-4">
          {currentUsers.map((user) => (
            <div key={user.userId} className="col-xl-3 col-lg-4 col-md-6 col-sm-12">
              <div 
                className="card border-0 rounded-4 shadow-sm h-100 dashboard-card" 
                style={{ cursor: 'pointer', transition: 'transform 0.2s, box-shadow 0.2s' }}
                onClick={(e) => {
                  // Don't open modal if clicking on dropdown
                  if (!(e.target as HTMLElement).closest('.dropdown')) {
                    setUserDetailModal(user);
                  }
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = 'translateY(-2px)';
                  e.currentTarget.style.boxShadow = '0 8px 25px rgba(0,0,0,0.15)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = 'translateY(0)';
                  e.currentTarget.style.boxShadow = '';
                }}
              >
                <div className="card-body p-3">
                  <div className="d-flex align-items-start justify-content-between mb-2">
                    <div className="d-flex align-items-center">
                      <div className="rounded-circle bg-primary bg-opacity-10 p-2 me-2">
                        <Users className="text-primary" size={20} />
                      </div>
                      <div>
                        <h6 className="fw-bold text-dark mb-0">
                          {user.firstName} {user.lastName}
                        </h6>
                        <p className="text-muted mb-0 small">@{user.username}</p>
                      </div>
                    </div>
                    <div className="dropdown">
                      <button
                        className="btn btn-sm btn-outline-secondary rounded-3"
                        type="button"
                        data-bs-toggle="dropdown"
                        aria-expanded="false"
                        onClick={(e) => e.stopPropagation()}
                      >
                        <MoreVertical size={16} />
                      </button>
                      <ul className="dropdown-menu dropdown-menu-end" style={{ width: '190px' }}>
                        <li>
                          <button 
                            className="dropdown-item d-flex align-items-center w-100"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleFreezeUser(user);
                            }}
                            style={{ whiteSpace: 'nowrap', textAlign: 'left' }}
                          >
                            <span style={{ display: 'inline-flex', alignItems: 'center', width: '100%' }}>
                              {user.isActive ? (
                                <><Lock size={14} className="me-2 flex-shrink-0" /><span>Freeze Account</span></>
                              ) : (
                                <><Unlock size={14} className="me-2 flex-shrink-0" /><span>Unfreeze Account</span></>
                              )}
                            </span>
                          </button>
                        </li>
                        <li>
                          <button 
                            className="dropdown-item d-flex align-items-center w-100"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleEmailUser(user);
                            }}
                            style={{ whiteSpace: 'nowrap', textAlign: 'left' }}
                          >
                            <span style={{ display: 'inline-flex', alignItems: 'center', width: '100%' }}>
                              <Mail size={14} className="me-2 flex-shrink-0" /><span>Send Email</span>
                            </span>
                          </button>
                        </li>
                        <li><hr className="dropdown-divider" /></li>
                        <li>
                          <button 
                            className="dropdown-item text-danger d-flex align-items-center w-100"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleDeleteUser(user);
                            }}
                            style={{ whiteSpace: 'nowrap', textAlign: 'left' }}
                          >
                            <span style={{ display: 'inline-flex', alignItems: 'center', width: '100%' }}>
                              <Trash2 size={14} className="me-2 flex-shrink-0" /><span>Delete User</span>
                            </span>
                          </button>
                        </li>
                      </ul>
                    </div>
                  </div>

                  <div className="mb-2">
                    <span className={`badge bg-${getRoleBadgeColor(user.role)} bg-opacity-10 text-${getRoleBadgeColor(user.role)} fw-semibold small`}>
                      {user.role}
                    </span>
                    {user.isActive ? (
                      <span className="badge bg-success bg-opacity-10 text-success fw-semibold ms-1 small">
                        <CheckCircle2 size={10} className="me-1" />
                        Active
                      </span>
                    ) : (
                      <span className="badge bg-secondary bg-opacity-10 text-secondary fw-semibold ms-1 small">
                        <XCircle size={10} className="me-1" />
                        Inactive
                      </span>
                    )}
                  </div>

                  <div className="mb-2">
                    <div className="d-flex align-items-center mb-1">
                      <Mail className="text-muted me-2" size={12} />
                      <span className="small text-dark text-truncate" style={{maxWidth: '150px'}} title={user.email}>{user.email}</span>
                    </div>
                    {user.phoneNumber && (
                      <div className="d-flex align-items-center mb-1">
                        <Phone className="text-muted me-2" size={12} />
                        <span className="small text-dark">{user.phoneNumber}</span>
                      </div>
                    )}
                    <div className="d-flex align-items-center">
                      <Calendar className="text-muted me-2" size={12} />
                      <span className="small text-muted">Joined {formatDate(user.createdAt)}</span>
                    </div>
                  </div>

                  <div className="bg-light rounded-3 p-2">
                    <div className="d-flex justify-content-between align-items-center">
                      <span className="small fw-semibold text-muted">Last Login</span>
                      <span className="small text-dark">{formatLastLogin(user.lastLogin)}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          ))}

          {filteredUsers.length === 0 && !loading && (
            <div className="col-12">
              <div className="card border-0 rounded-4 shadow-sm">
                <div className="card-body p-5 text-center">
                  <Users size={64} className="text-muted mb-3 opacity-50" />
                  <h3 className="fw-bold text-muted mb-2">No Users Found</h3>
                  <p className="text-muted mb-4">
                    {searchTerm || filterRole !== 'all' 
                      ? 'Try adjusting your search or filter criteria.' 
                      : 'Get started by adding your first user.'}
                  </p>
                  {(!searchTerm && filterRole === 'all') && (
                    <button 
                      className="btn btn-primary rounded-3"
                      onClick={() => setShowAddUser(true)}
                    >
                      <UserPlus className="me-2" size={18} />
                      Add First User
                    </button>
                  )}
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Pagination */}
        {filteredUsers.length > 0 && totalPages > 1 && (
          <div className="d-flex justify-content-between align-items-center mt-4">
            <div className="d-flex align-items-center gap-3">
              <span className="text-muted">
                Showing {startIndex + 1} to {Math.min(endIndex, filteredUsers.length)} of {filteredUsers.length} users
              </span>
              <div className="d-flex align-items-center gap-2">
                <span className="text-muted small">Users per page:</span>
                <select 
                  className="form-select form-select-sm" 
                  style={{width: 'auto'}}
                  value={usersPerPage}
                  onChange={(e) => {
                    setUsersPerPage(Number(e.target.value));
                    setCurrentPage(1);
                  }}
                >
                  <option value={20}>20</option>
                  <option value={50}>50</option>
                  <option value={100}>100</option>
                </select>
              </div>
            </div>
            
            <nav>
              <ul className="pagination pagination-sm mb-0">
                <li className={`page-item ${currentPage === 1 ? 'disabled' : ''}`}>
                  <button 
                    className="page-link" 
                    onClick={() => setCurrentPage(1)}
                    disabled={currentPage === 1}
                  >
                    <ChevronsLeft size={16} />
                  </button>
                </li>
                <li className={`page-item ${currentPage === 1 ? 'disabled' : ''}`}>
                  <button 
                    className="page-link" 
                    onClick={() => setCurrentPage(currentPage - 1)}
                    disabled={currentPage === 1}
                  >
                    <ChevronLeft size={16} />
                  </button>
                </li>
                
                {/* Page numbers */}
                {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                  let pageNum: number;
                  if (totalPages <= 5) {
                    pageNum = i + 1;
                  } else if (currentPage <= 3) {
                    pageNum = i + 1;
                  } else if (currentPage >= totalPages - 2) {
                    pageNum = totalPages - 4 + i;
                  } else {
                    pageNum = currentPage - 2 + i;
                  }
                  
                  return (
                    <li key={pageNum} className={`page-item ${currentPage === pageNum ? 'active' : ''}`}>
                      <button 
                        className="page-link" 
                        onClick={() => setCurrentPage(pageNum)}
                      >
                        {pageNum}
                      </button>
                    </li>
                  );
                })}
                
                <li className={`page-item ${currentPage === totalPages ? 'disabled' : ''}`}>
                  <button 
                    className="page-link" 
                    onClick={() => setCurrentPage(currentPage + 1)}
                    disabled={currentPage === totalPages}
                  >
                    <ChevronRight size={16} />
                  </button>
                </li>
                <li className={`page-item ${currentPage === totalPages ? 'disabled' : ''}`}>
                  <button 
                    className="page-link" 
                    onClick={() => setCurrentPage(totalPages)}
                    disabled={currentPage === totalPages}
                  >
                    <ChevronsRight size={16} />
                  </button>
                </li>
              </ul>
            </nav>
          </div>
        )}

        {/* User Detail Modal */}
        {userDetailModal && (
          <div className="modal d-block" style={{backgroundColor: 'rgba(0,0,0,0.5)'}}>
            <div className="modal-dialog modal-dialog-centered modal-lg">
              <div className="modal-content border-0 rounded-4">
                <div className="modal-header border-0 pb-0">
                  <div className="d-flex align-items-center">
                    <div className="rounded-circle bg-primary bg-opacity-10 p-2 me-3">
                      <Users className="text-primary" size={20} />
                    </div>
                    <div>
                      <h5 className="modal-title fw-bold mb-0">
                        {userDetailModal.firstName} {userDetailModal.lastName}
                      </h5>
                      <p className="text-muted small mb-0">@{userDetailModal.username}</p>
                    </div>
                  </div>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={() => setUserDetailModal(null)}
                  ></button>
                </div>
                <div className="modal-body">
                  <div className="row g-4">
                    {/* Status and Role */}
                    <div className="col-12">
                      <div className="d-flex gap-2 mb-3">
                        <span className={`badge bg-${getRoleBadgeColor(userDetailModal.role)} bg-opacity-10 text-${getRoleBadgeColor(userDetailModal.role)} fw-semibold px-3 py-2`}>
                          <Shield size={14} className="me-1" />
                          {userDetailModal.role}
                        </span>
                        {userDetailModal.isActive ? (
                          <span className="badge bg-success bg-opacity-10 text-success fw-semibold px-3 py-2">
                            <CheckCircle2 size={14} className="me-1" />
                            Active
                          </span>
                        ) : (
                          <span className="badge bg-secondary bg-opacity-10 text-secondary fw-semibold px-3 py-2">
                            <XCircle size={14} className="me-1" />
                            Inactive
                          </span>
                        )}
                      </div>
                    </div>
                    
                    {/* Basic Information */}
                    <div className="col-md-6">
                      <div className="bg-primary bg-opacity-10 rounded-4 p-4">
                        <h6 className="fw-bold text-primary mb-3">
                          <Users size={16} className="me-2" />
                          Basic Information
                        </h6>
                        <div className="row g-3">
                          <div className="col-12">
                            <div className="small text-muted">User ID</div>
                            <div className="fw-semibold text-dark">{userDetailModal.userId}</div>
                          </div>
                          <div className="col-6">
                            <div className="small text-muted">First Name</div>
                            <div className="fw-semibold text-dark">{userDetailModal.firstName}</div>
                          </div>
                          <div className="col-6">
                            <div className="small text-muted">Last Name</div>
                            <div className="fw-semibold text-dark">{userDetailModal.lastName}</div>
                          </div>
                          <div className="col-12">
                            <div className="small text-muted">Username</div>
                            <div className="fw-semibold text-dark">@{userDetailModal.username}</div>
                          </div>
                        </div>
                      </div>
                    </div>
                    
                    {/* Contact Information */}
                    <div className="col-md-6">
                      <div className="bg-info bg-opacity-10 rounded-4 p-4">
                        <h6 className="fw-bold text-info mb-3">
                          <Mail size={16} className="me-2" />
                          Contact Information
                        </h6>
                        <div className="row g-3">
                          <div className="col-12">
                            <div className="d-flex align-items-center mb-3">
                              <Mail className="text-info me-3" size={16} />
                              <div>
                                <div className="small text-muted">Email Address</div>
                                <div className="fw-semibold text-dark">{userDetailModal.email}</div>
                              </div>
                            </div>
                            {userDetailModal.phoneNumber && (
                              <div className="d-flex align-items-center">
                                <Phone className="text-info me-3" size={16} />
                                <div>
                                  <div className="small text-muted">Phone Number</div>
                                  <div className="fw-semibold text-dark">{userDetailModal.phoneNumber}</div>
                                </div>
                              </div>
                            )}
                            {!userDetailModal.phoneNumber && (
                              <div className="d-flex align-items-center">
                                <Phone className="text-muted me-3" size={16} />
                                <div>
                                  <div className="small text-muted">Phone Number</div>
                                  <div className="text-muted">Not provided</div>
                                </div>
                              </div>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>
                    
                    {/* Activity Information */}
                    <div className="col-12">
                      <div className="bg-success bg-opacity-10 rounded-4 p-4">
                        <h6 className="fw-bold text-success mb-3">
                          <Calendar size={16} className="me-2" />
                          Activity Information
                        </h6>
                        <div className="row g-3">
                          <div className="col-md-4">
                            <div className="text-center">
                              <div className="small text-muted">Last Login</div>
                              <div className="fw-bold text-dark h5 mb-0">{formatLastLogin(userDetailModal.lastLogin)}</div>
                            </div>
                          </div>
                          <div className="col-md-4">
                            <div className="text-center">
                              <div className="small text-muted">Member Since</div>
                              <div className="fw-bold text-dark h5 mb-0">{formatDate(userDetailModal.createdAt)}</div>
                            </div>
                          </div>
                          <div className="col-md-4">
                            <div className="text-center">
                              <div className="small text-muted">Account Status</div>
                              <div className="fw-bold text-dark h5 mb-0">
                                {userDetailModal.isActive ? 'Active' : 'Inactive'}
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
                <div className="modal-footer border-0">
                  <div className="d-flex gap-2 w-100">
                    <button 
                      className="btn btn-outline-primary rounded-3 flex-fill"
                      onClick={() => {
                        setEditUserForm({
                          userId: userDetailModal.userId,
                          firstName: userDetailModal.firstName,
                          lastName: userDetailModal.lastName,
                          username: userDetailModal.username,
                          email: userDetailModal.email,
                          phoneNumber: userDetailModal.phoneNumber || '',
                          role: userDetailModal.role,
                          isActive: userDetailModal.isActive
                        });
                        setUserDetailModal(null);
                        setShowEditUser(true);
                      }}
                    >
                      <Edit3 size={16} className="me-2" />
                      Edit User
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
        
        {/* Edit User Modal */}
        {showEditUser && (
          <div className="modal d-block" style={{backgroundColor: 'rgba(0,0,0,0.5)'}}>
            <div className="modal-dialog modal-dialog-centered modal-lg">
              <div className="modal-content border-0 rounded-4">
                <div className="modal-header border-0 pb-0">
                  <h5 className="modal-title fw-bold">Edit User</h5>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={resetEditUserModal}
                  ></button>
                </div>
                <div className="modal-body">
                  {editSubmitSuccess ? (
                    <div className="text-center py-4">
                      <CheckCircle2 size={64} className="text-success mb-3" />
                      <h4 className="text-success fw-bold">User Updated Successfully!</h4>
                      <p className="text-muted">The user information has been updated.</p>
                    </div>
                  ) : (
                    <form onSubmit={handleEditUserSubmit}>
                      <div className="row g-3">
                        <div className="col-md-6">
                          <label className="form-label fw-semibold">First Name</label>
                          <input 
                            type="text" 
                            className={`form-control rounded-3 ${editFormErrors.firstName ? 'is-invalid' : ''}`}
                            placeholder="Enter first name" 
                            value={editUserForm.firstName}
                            onChange={(e) => handleEditInputChange('firstName', e.target.value)}
                          />
                          {editFormErrors.firstName && (
                            <div className="invalid-feedback d-flex align-items-center">
                              <AlertCircle size={16} className="me-1" />
                              {editFormErrors.firstName}
                            </div>
                          )}
                        </div>
                        <div className="col-md-6">
                          <label className="form-label fw-semibold">Last Name</label>
                          <input 
                            type="text" 
                            className={`form-control rounded-3 ${editFormErrors.lastName ? 'is-invalid' : ''}`}
                            placeholder="Enter last name" 
                            value={editUserForm.lastName}
                            onChange={(e) => handleEditInputChange('lastName', e.target.value)}
                          />
                          {editFormErrors.lastName && (
                            <div className="invalid-feedback d-flex align-items-center">
                              <AlertCircle size={16} className="me-1" />
                              {editFormErrors.lastName}
                            </div>
                          )}
                        </div>
                        <div className="col-12">
                          <label className="form-label fw-semibold">Username</label>
                          <input 
                            type="text" 
                            className={`form-control rounded-3 ${editFormErrors.username ? 'is-invalid' : ''}`}
                            placeholder="Enter username" 
                            value={editUserForm.username}
                            onChange={(e) => handleEditInputChange('username', e.target.value)}
                          />
                          {editFormErrors.username && (
                            <div className="invalid-feedback d-flex align-items-center">
                              <AlertCircle size={16} className="me-1" />
                              {editFormErrors.username}
                            </div>
                          )}
                        </div>
                        <div className="col-12">
                          <label className="form-label fw-semibold">Email</label>
                          <input 
                            type="email" 
                            className={`form-control rounded-3 ${editFormErrors.email ? 'is-invalid' : ''}`}
                            placeholder="Enter email address" 
                            value={editUserForm.email}
                            onChange={(e) => handleEditInputChange('email', e.target.value)}
                          />
                          {editFormErrors.email && (
                            <div className="invalid-feedback d-flex align-items-center">
                              <AlertCircle size={16} className="me-1" />
                              {editFormErrors.email}
                            </div>
                          )}
                        </div>
                        <div className="col-md-6">
                          <label className="form-label fw-semibold">Phone Number</label>
                          <input 
                            type="tel" 
                            className={`form-control rounded-3 ${editFormErrors.phoneNumber ? 'is-invalid' : ''}`}
                            placeholder="e.g., +1 (555) 123-4567 or 555-123-4567" 
                            value={editUserForm.phoneNumber}
                            onChange={(e) => handleEditInputChange('phoneNumber', e.target.value)}
                          />
                          {editFormErrors.phoneNumber && (
                            <div className="invalid-feedback d-flex align-items-center">
                              <AlertCircle size={16} className="me-1" />
                              {editFormErrors.phoneNumber}
                            </div>
                          )}
                        </div>
                        <div className="col-md-6">
                          <label className="form-label fw-semibold">Role</label>
                          <select 
                            className={`form-select rounded-3 ${editFormErrors.role ? 'is-invalid' : ''}`}
                            value={editUserForm.role}
                            onChange={(e) => handleEditInputChange('role', e.target.value)}
                          >
                            <option value="">Select a role</option>
                            {roles.map((role) => (
                              <option key={role.userRoleId} value={role.role}>
                                {role.role}
                              </option>
                            ))}
                          </select>
                          {editFormErrors.role && (
                            <div className="invalid-feedback d-flex align-items-center">
                              <AlertCircle size={16} className="me-1" />
                              {editFormErrors.role}
                            </div>
                          )}
                        </div>
                        <div className="col-12">
                          <div className="form-check">
                            <input 
                              className="form-check-input" 
                              type="checkbox" 
                              id="isActive"
                              checked={editUserForm.isActive}
                              onChange={(e) => handleEditInputChange('isActive', e.target.checked)}
                            />
                            <label className="form-check-label fw-semibold" htmlFor="isActive">
                              Active User
                            </label>
                          </div>
                        </div>
                      </div>
                    </form>
                  )}
                </div>
                {!editSubmitSuccess && (
                  <div className="modal-footer border-0 pt-0">
                    <button
                      type="button"
                      className="btn btn-secondary rounded-3"
                      onClick={resetEditUserModal}
                      disabled={isEditSubmitting}
                    >
                      Cancel
                    </button>
                    <button 
                      type="submit" 
                      className="btn btn-primary rounded-3"
                      onClick={handleEditUserSubmit}
                      disabled={isEditSubmitting}
                    >
                      {isEditSubmitting ? (
                        <>
                          <div className="spinner-border spinner-border-sm me-2" role="status">
                            <span className="visually-hidden">Loading...</span>
                          </div>
                          Updating User...
                        </>
                      ) : (
                        'Update User'
                      )}
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}
        
        {/* Add User Modal */}
        {showAddUser && (
          <div className="modal d-block" style={{backgroundColor: 'rgba(0,0,0,0.5)'}}>
            <div className="modal-dialog modal-dialog-centered modal-lg">
              <div className="modal-content border-0 rounded-4">
                <div className="modal-header border-0 pb-0">
                  <h5 className="modal-title fw-bold">Add New User</h5>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={resetAddUserModal}
                  ></button>
                </div>
                <div className="modal-body">
                  {submitSuccess ? (
                    <div className="text-center py-4">
                      <CheckCircle2 size={64} className="text-success mb-3" />
                      <h4 className="text-success fw-bold">User Created Successfully!</h4>
                      <p className="text-muted">The new user has been registered and can now login to the system.</p>
                    </div>
                  ) : (
                    <form onSubmit={handleSubmitNewUser}>
                      <div className="row g-3">
                        <div className="col-md-6">
                          <label className="form-label fw-semibold">First Name</label>
                          <input 
                            type="text" 
                            className={`form-control rounded-3 ${formErrors.firstName ? 'is-invalid' : ''}`}
                            placeholder="Enter first name" 
                            value={newUserForm.firstName}
                            onChange={(e) => handleInputChange('firstName', e.target.value)}
                          />
                          {formErrors.firstName && (
                            <div className="invalid-feedback d-flex align-items-center">
                              <AlertCircle size={16} className="me-1" />
                              {formErrors.firstName}
                            </div>
                          )}
                        </div>
                        <div className="col-md-6">
                          <label className="form-label fw-semibold">Last Name</label>
                          <input 
                            type="text" 
                            className={`form-control rounded-3 ${formErrors.lastName ? 'is-invalid' : ''}`}
                            placeholder="Enter last name" 
                            value={newUserForm.lastName}
                            onChange={(e) => handleInputChange('lastName', e.target.value)}
                          />
                          {formErrors.lastName && (
                            <div className="invalid-feedback d-flex align-items-center">
                              <AlertCircle size={16} className="me-1" />
                              {formErrors.lastName}
                            </div>
                          )}
                        </div>
                        <div className="col-12">
                          <label className="form-label fw-semibold">Username</label>
                          <input 
                            type="text" 
                            className={`form-control rounded-3 ${formErrors.username ? 'is-invalid' : ''}`}
                            placeholder="Enter username" 
                            value={newUserForm.username}
                            onChange={(e) => handleInputChange('username', e.target.value)}
                          />
                          {formErrors.username && (
                            <div className="invalid-feedback d-flex align-items-center">
                              <AlertCircle size={16} className="me-1" />
                              {formErrors.username}
                            </div>
                          )}
                        </div>
                        <div className="col-12">
                          <label className="form-label fw-semibold">Email</label>
                          <input 
                            type="email" 
                            className={`form-control rounded-3 ${formErrors.email ? 'is-invalid' : ''}`}
                            placeholder="Enter email address" 
                            value={newUserForm.email}
                            onChange={(e) => handleInputChange('email', e.target.value)}
                          />
                          {formErrors.email && (
                            <div className="invalid-feedback d-flex align-items-center">
                              <AlertCircle size={16} className="me-1" />
                              {formErrors.email}
                            </div>
                          )}
                        </div>
                        <div className="col-12">
                          <label className="form-label fw-semibold">Phone Number</label>
                          <input 
                            type="tel" 
                            className={`form-control rounded-3 ${formErrors.phoneNumber ? 'is-invalid' : ''}`}
                            placeholder="e.g., +1 (555) 123-4567 or 555-123-4567" 
                            value={newUserForm.phoneNumber}
                            onChange={(e) => handleInputChange('phoneNumber', e.target.value)}
                          />
                          {formErrors.phoneNumber && (
                            <div className="invalid-feedback d-flex align-items-center">
                              <AlertCircle size={16} className="me-1" />
                              {formErrors.phoneNumber}
                            </div>
                          )}
                        </div>
                        <div className="col-md-6">
                          <label className="form-label fw-semibold">Password</label>
                          <div className="position-relative">
                            <input 
                              type={showPassword ? 'text' : 'password'}
                              className={`form-control rounded-3 pe-5 ${formErrors.password ? 'is-invalid' : ''}`}
                              placeholder="Min 8 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special (@$!%*?&)" 
                              value={newUserForm.password}
                              onChange={(e) => handleInputChange('password', e.target.value)}
                            />
                            <button
                              type="button"
                              onClick={() => setShowPassword(!showPassword)}
                              className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                              style={{ zIndex: 10 }}
                            >
                              {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                            </button>
                            {formErrors.password && (
                              <div className="invalid-feedback d-flex align-items-center">
                                <AlertCircle size={16} className="me-1" />
                                {formErrors.password}
                              </div>
                            )}
                          </div>
                        </div>
                        <div className="col-md-6">
                          <label className="form-label fw-semibold">Confirm Password</label>
                          <div className="position-relative">
                            <input 
                              type={showReEnterPassword ? 'text' : 'password'}
                              className={`form-control rounded-3 pe-5 ${formErrors.reEnterPassword ? 'is-invalid' : ''}`}
                              placeholder="Re-enter password" 
                              value={newUserForm.reEnterPassword}
                              onChange={(e) => handleInputChange('reEnterPassword', e.target.value)}
                            />
                            <button
                              type="button"
                              onClick={() => setShowReEnterPassword(!showReEnterPassword)}
                              className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                              style={{ zIndex: 10 }}
                            >
                              {showReEnterPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                            </button>
                            {formErrors.reEnterPassword && (
                              <div className="invalid-feedback d-flex align-items-center">
                                <AlertCircle size={16} className="me-1" />
                                {formErrors.reEnterPassword}
                              </div>
                            )}
                          </div>
                        </div>
                      </div>
                    </form>
                  )}
                </div>
                {!submitSuccess && (
                  <div className="modal-footer border-0 pt-0">
                    <button
                      type="button"
                      className="btn btn-secondary rounded-3"
                      onClick={resetAddUserModal}
                      disabled={isSubmitting}
                    >
                      Cancel
                    </button>
                    <button 
                      type="submit" 
                      className="btn btn-primary rounded-3"
                      onClick={handleSubmitNewUser}
                      disabled={isSubmitting}
                    >
                      {isSubmitting ? (
                        <>
                          <div className="spinner-border spinner-border-sm me-2" role="status">
                            <span className="visually-hidden">Loading...</span>
                          </div>
                          Creating User...
                        </>
                      ) : (
                        'Create User'
                      )}
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
}