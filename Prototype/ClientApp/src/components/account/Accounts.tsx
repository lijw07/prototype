import React, { useState, useEffect } from 'react';
import { 
  Users, 
  Plus, 
  Search, 
  Filter, 
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
  EyeOff
} from 'lucide-react';
import { authApi } from '../../services/api';

interface User {
  userId: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber?: string;
  isActive: boolean;
  role: 'Admin' | 'User' | 'Manager';
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

export default function Accounts() {
  const [users, setUsers] = useState<User[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterRole, setFilterRole] = useState<string>('all');
  const [showAddUser, setShowAddUser] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  
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

  // Mock data - in real app, this would come from API
  useEffect(() => {
    const mockUsers: User[] = [
      {
        userId: '1',
        firstName: 'John',
        lastName: 'Doe',
        username: 'john.doe',
        email: 'john.doe@company.com',
        phoneNumber: '+1 (555) 123-4567',
        isActive: true,
        role: 'Admin',
        lastLogin: '2024-01-15T10:30:00Z',
        createdAt: '2023-06-15T09:00:00Z'
      },
      {
        userId: '2',
        firstName: 'Jane',
        lastName: 'Smith',
        username: 'jane.smith',
        email: 'jane.smith@company.com',
        phoneNumber: '+1 (555) 987-6543',
        isActive: true,
        role: 'Manager',
        lastLogin: '2024-01-14T16:45:00Z',
        createdAt: '2023-08-20T14:15:00Z'
      },
      {
        userId: '3',
        firstName: 'Mike',
        lastName: 'Johnson',
        username: 'mike.johnson',
        email: 'mike.johnson@company.com',
        isActive: false,
        role: 'User',
        lastLogin: '2024-01-10T11:20:00Z',
        createdAt: '2023-09-05T08:30:00Z'
      },
      {
        userId: '4',
        firstName: 'Sarah',
        lastName: 'Wilson',
        username: 'sarah.wilson',
        email: 'sarah.wilson@company.com',
        phoneNumber: '+1 (555) 456-7890',
        isActive: true,
        role: 'User',
        lastLogin: '2024-01-15T09:15:00Z',
        createdAt: '2023-10-12T13:45:00Z'
      },
      {
        userId: '5',
        firstName: 'David',
        lastName: 'Brown',
        username: 'david.brown',
        email: 'david.brown@company.com',
        isActive: true,
        role: 'Manager',
        lastLogin: '2024-01-15T12:00:00Z',
        createdAt: '2023-11-01T10:20:00Z'
      }
    ];

    setTimeout(() => {
      setUsers(mockUsers);
      setLoading(false);
    }, 1000);
  }, []);

  const filteredUsers = users.filter(user => {
    const matchesSearch = user.firstName.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         user.lastName.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         user.username.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         user.email.toLowerCase().includes(searchTerm.toLowerCase());
    
    const matchesRole = filterRole === 'all' || user.role.toLowerCase() === filterRole.toLowerCase();
    
    return matchesSearch && matchesRole;
  });

  const getRoleBadgeColor = (role: string) => {
    switch (role) {
      case 'Admin': return 'danger';
      case 'Manager': return 'warning';
      case 'User': return 'info';
      default: return 'secondary';
    }
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
                      <option value="admin">Admin</option>
                      <option value="manager">Manager</option>
                      <option value="user">User</option>
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
          {filteredUsers.map((user) => (
            <div key={user.userId} className="col-xl-4 col-lg-6">
              <div className="card border-0 rounded-4 shadow-sm h-100 dashboard-card">
                <div className="card-body p-4">
                  <div className="d-flex align-items-start justify-content-between mb-3">
                    <div className="d-flex align-items-center">
                      <div className="rounded-circle bg-primary bg-opacity-10 p-3 me-3">
                        <Users className="text-primary" size={24} />
                      </div>
                      <div>
                        <h5 className="fw-bold text-dark mb-1">
                          {user.firstName} {user.lastName}
                        </h5>
                        <p className="text-muted mb-0 small">@{user.username}</p>
                      </div>
                    </div>
                    <div className="dropdown">
                      <button
                        className="btn btn-sm btn-outline-secondary rounded-3"
                        type="button"
                        data-bs-toggle="dropdown"
                      >
                        <MoreVertical size={16} />
                      </button>
                      <ul className="dropdown-menu">
                        <li><a className="dropdown-item" href="#"><Edit3 size={14} className="me-2" />Edit</a></li>
                        <li><a className="dropdown-item" href="#"><Shield size={14} className="me-2" />Permissions</a></li>
                        <li><hr className="dropdown-divider" /></li>
                        <li><a className="dropdown-item text-danger" href="#"><Trash2 size={14} className="me-2" />Delete</a></li>
                      </ul>
                    </div>
                  </div>

                  <div className="row g-2 mb-3">
                    <div className="col-12">
                      <span className={`badge bg-${getRoleBadgeColor(user.role)} bg-opacity-10 text-${getRoleBadgeColor(user.role)} fw-semibold`}>
                        {user.role}
                      </span>
                      {user.isActive ? (
                        <span className="badge bg-success bg-opacity-10 text-success fw-semibold ms-2">
                          <CheckCircle2 size={12} className="me-1" />
                          Active
                        </span>
                      ) : (
                        <span className="badge bg-secondary bg-opacity-10 text-secondary fw-semibold ms-2">
                          <XCircle size={12} className="me-1" />
                          Inactive
                        </span>
                      )}
                    </div>
                  </div>

                  <div className="mb-3">
                    <div className="d-flex align-items-center mb-2">
                      <Mail className="text-muted me-2" size={14} />
                      <span className="small text-dark">{user.email}</span>
                    </div>
                    {user.phoneNumber && (
                      <div className="d-flex align-items-center mb-2">
                        <Phone className="text-muted me-2" size={14} />
                        <span className="small text-dark">{user.phoneNumber}</span>
                      </div>
                    )}
                    <div className="d-flex align-items-center mb-2">
                      <Calendar className="text-muted me-2" size={14} />
                      <span className="small text-muted">Joined {formatDate(user.createdAt)}</span>
                    </div>
                  </div>

                  <div className="bg-light rounded-3 p-3">
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