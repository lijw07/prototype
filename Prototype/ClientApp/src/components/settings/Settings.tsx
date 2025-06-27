import React, { useState, useEffect } from 'react';
import { Settings, User, Shield, Eye, EyeOff, Edit, Save, X, Trash2, AlertTriangle, Bell, Moon, Globe, Key, Download, Activity, Clock, Monitor } from 'lucide-react';
import { userApi } from '../../services/api';

// Types based on your controllers
interface UserSettings {
    userId: string;
    firstName: string;
    lastName: string;
    username: string;
    email: string;
    phoneNumber: string;
}

const SettingsDashboard: React.FC = () => {
    const [userSettings, setUserSettings] = useState<UserSettings | null>(null);
    const [loading, setLoading] = useState(false);
    const [isEditingProfile, setIsEditingProfile] = useState(true);
    const [showDeleteModal, setShowDeleteModal] = useState(false);
    
    // Edit profile form state
    const [editForm, setEditForm] = useState({
        firstName: '',
        lastName: '',
        email: ''
    });

    // Password change form state
    const [passwordForm, setPasswordForm] = useState({
        currentPassword: '',
        newPassword: '',
        reTypeNewPassword: ''
    });

    const [showPasswords, setShowPasswords] = useState({
        current: false,
        new: false,
        reType: false
    });

    const fetchUserSettings = async () => {
        setLoading(true);
        try {
            const response = await userApi.getProfile();
            console.log('User profile response:', response);
            // Backend returns { success: true, user: userDto } directly
            if (response.success && response.user) {
                setUserSettings(response.user);
                setEditForm({
                    firstName: response.user.firstName,
                    lastName: response.user.lastName,
                    email: response.user.email
                });
            }
        } catch (error) {
            console.error('Failed to fetch user settings:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchUserSettings();
    }, []);

    const handlePasswordChange = async () => {
        try {
            const response = await userApi.changePassword(passwordForm);
            if (response.message && !response.message.toLowerCase().includes('error')) {
                alert('Password changed successfully!');
                setPasswordForm({ currentPassword: '', newPassword: '', reTypeNewPassword: '' });
            } else {
                alert(response.message || 'Failed to change password');
            }
        } catch (error: any) {
            console.error('Failed to change password:', error);
            alert(error.message || 'Failed to change password');
        }
    };


    const handleProfileUpdate = async () => {
        try {
            const response = await userApi.updateProfile(editForm);
            if (response.message && response.user) {
                alert('Profile updated successfully!');
                // Update local state with the fresh user data from response
                setUserSettings(response.user);
                setEditForm({
                    firstName: response.user.firstName,
                    lastName: response.user.lastName,
                    email: response.user.email
                });
                // Also fetch fresh data to ensure consistency
                fetchUserSettings();
            } else {
                alert(response.message || 'Failed to update profile');
            }
        } catch (error: any) {
            console.error('Failed to update profile:', error);
            alert(error.message || 'Failed to update profile');
        }
    };

    const handleDeleteRequest = () => {
        // For now, just show an alert. In a real implementation, 
        // you would create a delete request record in the database
        alert('Account deletion request submitted. An administrator will review your request.');
        setShowDeleteModal(false);
    };

    const togglePasswordVisibility = (field: keyof typeof showPasswords) => {
        setShowPasswords(prev => ({ ...prev, [field]: !prev[field] }));
    };

    return (
        <div className="min-vh-100 bg-light">
            <div className="container-fluid py-4">
                {/* Header */}
                <div className="mb-4">
                    <div className="d-flex align-items-center mb-2">
                        <Settings className="text-primary me-3" size={32} />
                        <h1 className="display-5 fw-bold text-dark mb-0">User Settings</h1>
                    </div>
                    <p className="text-muted fs-6">Manage your personal account and security settings</p>
                </div>

                {/* User Settings Content */}
                <div>
                        <div className="card shadow-sm border-0 rounded-4 mb-4">
                            <div className="card-body p-4">
                                <div className="mb-4">
                                    <h2 className="card-title fw-bold text-dark mb-0 d-flex align-items-center">
                                        <User className="text-primary me-2" size={24} />
                                        User Information
                                    </h2>
                                </div>
                                
                                {userSettings ? (
                                        <div className="row g-4">
                                            <div className="col-md-6">
                                                <label className="form-label fw-semibold">First Name</label>
                                                <input
                                                    type="text"
                                                    value={editForm.firstName}
                                                    onChange={(e) => setEditForm({...editForm, firstName: e.target.value})}
                                                    className="form-control rounded-3"
                                                    placeholder="Enter first name"
                                                />
                                            </div>
                                            <div className="col-md-6">
                                                <label className="form-label fw-semibold">Last Name</label>
                                                <input
                                                    type="text"
                                                    value={editForm.lastName}
                                                    onChange={(e) => setEditForm({...editForm, lastName: e.target.value})}
                                                    className="form-control rounded-3"
                                                    placeholder="Enter last name"
                                                />
                                            </div>
                                            <div className="col-md-6">
                                                <label className="form-label fw-semibold text-muted small">USERNAME (READ-ONLY)</label>
                                                <p className="fw-semibold text-muted mb-0">{userSettings.username}</p>
                                            </div>
                                            <div className="col-md-6">
                                                <label className="form-label fw-semibold">Email</label>
                                                <input
                                                    type="email"
                                                    value={editForm.email}
                                                    onChange={(e) => setEditForm({...editForm, email: e.target.value})}
                                                    className="form-control rounded-3"
                                                    placeholder="Enter email address"
                                                />
                                            </div>
                                            <div className="col-md-6">
                                                <label className="form-label fw-semibold text-muted small">PHONE NUMBER (READ-ONLY)</label>
                                                <p className="fw-semibold text-muted mb-0">{userSettings.phoneNumber || 'Not provided'}</p>
                                            </div>
                                            
                                            {/* Profile Save Button */}
                                            <div className="col-12 d-flex gap-2 pt-2">
                                                <button
                                                    onClick={handleProfileUpdate}
                                                    className="btn btn-success rounded-3 fw-semibold d-flex align-items-center"
                                                >
                                                    <Save size={16} className="me-2" />
                                                    Save Profile Changes
                                                </button>
                                                <button
                                                    onClick={() => {
                                                        setEditForm({
                                                            firstName: userSettings.firstName,
                                                            lastName: userSettings.lastName,
                                                            email: userSettings.email
                                                        });
                                                    }}
                                                    className="btn btn-secondary rounded-3 fw-semibold d-flex align-items-center"
                                                >
                                                    <X size={16} className="me-2" />
                                                    Reset Changes
                                                </button>
                                            </div>
                                            
                                            {/* Password Change Section in Edit Mode */}
                                            <div className="col-12">
                                                <hr className="my-4" />
                                                <h5 className="fw-bold text-dark mb-3 d-flex align-items-center">
                                                    <Shield className="text-primary me-2" size={20} />
                                                    Change Password
                                                </h5>
                                                <div className="row g-3">
                                                    <div className="col-md-4">
                                                        <label className="form-label fw-semibold">Current Password</label>
                                                        <div className="position-relative">
                                                            <input
                                                                type={showPasswords.current ? "text" : "password"}
                                                                value={passwordForm.currentPassword}
                                                                onChange={(e) => setPasswordForm({...passwordForm, currentPassword: e.target.value})}
                                                                className="form-control rounded-3 pe-5"
                                                                placeholder="Enter current password"
                                                            />
                                                            <button
                                                                type="button"
                                                                onClick={() => togglePasswordVisibility('current')}
                                                                className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                                                            >
                                                                {showPasswords.current ? <EyeOff size={16} /> : <Eye size={16} />}
                                                            </button>
                                                        </div>
                                                    </div>
                                                    <div className="col-md-4">
                                                        <label className="form-label fw-semibold">New Password</label>
                                                        <div className="position-relative">
                                                            <input
                                                                type={showPasswords.new ? "text" : "password"}
                                                                value={passwordForm.newPassword}
                                                                onChange={(e) => setPasswordForm({...passwordForm, newPassword: e.target.value})}
                                                                className="form-control rounded-3 pe-5"
                                                                placeholder="Enter new password"
                                                            />
                                                            <button
                                                                type="button"
                                                                onClick={() => togglePasswordVisibility('new')}
                                                                className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                                                            >
                                                                {showPasswords.new ? <EyeOff size={16} /> : <Eye size={16} />}
                                                            </button>
                                                        </div>
                                                    </div>
                                                    <div className="col-md-4">
                                                        <label className="form-label fw-semibold">Confirm New Password</label>
                                                        <div className="position-relative">
                                                            <input
                                                                type={showPasswords.reType ? "text" : "password"}
                                                                value={passwordForm.reTypeNewPassword}
                                                                onChange={(e) => setPasswordForm({...passwordForm, reTypeNewPassword: e.target.value})}
                                                                className="form-control rounded-3 pe-5"
                                                                placeholder="Confirm new password"
                                                            />
                                                            <button
                                                                type="button"
                                                                onClick={() => togglePasswordVisibility('reType')}
                                                                className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                                                            >
                                                                {showPasswords.reType ? <EyeOff size={16} /> : <Eye size={16} />}
                                                            </button>
                                                        </div>
                                                    </div>
                                                    <div className="col-12 d-flex gap-2 pt-2">
                                                        <button
                                                            onClick={handlePasswordChange}
                                                            className="btn btn-primary rounded-3 fw-semibold d-flex align-items-center"
                                                        >
                                                            <Shield size={16} className="me-2" />
                                                            Update Password
                                                        </button>
                                                        <button
                                                            onClick={() => setPasswordForm({ currentPassword: '', newPassword: '', reTypeNewPassword: '' })}
                                                            className="btn btn-outline-secondary rounded-3 fw-semibold"
                                                        >
                                                            Clear Password Fields
                                                        </button>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                ) : (
                                    <div className="d-flex align-items-center text-muted">
                                        <div className="spinner-border spinner-border-sm me-2" role="status">
                                            <span className="visually-hidden">Loading...</span>
                                        </div>
                                        Loading user information...
                                    </div>
                                )}
                            </div>
                        </div>

                        {/* Notification Settings Card */}
                        <div className="card shadow-sm border-0 rounded-4 mb-4">
                            <div className="card-body p-4">
                                <h2 className="card-title fw-bold text-dark mb-3 d-flex align-items-center">
                                    <Bell className="text-primary me-2" size={24} />
                                    Notification Preferences
                                </h2>
                                <div className="row g-3">
                                    <div className="col-md-6">
                                        <div className="form-check form-switch">
                                            <input className="form-check-input" type="checkbox" id="emailNotifications" defaultChecked />
                                            <label className="form-check-label fw-semibold" htmlFor="emailNotifications">
                                                Email Notifications
                                            </label>
                                            <div className="small text-muted">Receive updates via email</div>
                                        </div>
                                    </div>
                                    <div className="col-md-6">
                                        <div className="form-check form-switch">
                                            <input className="form-check-input" type="checkbox" id="browserNotifications" defaultChecked />
                                            <label className="form-check-label fw-semibold" htmlFor="browserNotifications">
                                                Browser Notifications
                                            </label>
                                            <div className="small text-muted">Show desktop notifications</div>
                                        </div>
                                    </div>
                                    <div className="col-md-6">
                                        <div className="form-check form-switch">
                                            <input className="form-check-input" type="checkbox" id="securityAlerts" defaultChecked />
                                            <label className="form-check-label fw-semibold" htmlFor="securityAlerts">
                                                Security Alerts
                                            </label>
                                            <div className="small text-muted">Important security notifications</div>
                                        </div>
                                    </div>
                                    <div className="col-md-6">
                                        <div className="form-check form-switch">
                                            <input className="form-check-input" type="checkbox" id="systemUpdates" />
                                            <label className="form-check-label fw-semibold" htmlFor="systemUpdates">
                                                System Updates
                                            </label>
                                            <div className="small text-muted">New feature announcements</div>
                                        </div>
                                    </div>
                                </div>
                                <div className="mt-3">
                                    <button className="btn btn-primary rounded-3 fw-semibold">
                                        <Save size={16} className="me-2" />
                                        Save Notification Settings
                                    </button>
                                </div>
                            </div>
                        </div>

                        {/* Appearance Settings Card */}
                        <div className="card shadow-sm border-0 rounded-4 mb-4">
                            <div className="card-body p-4">
                                <h2 className="card-title fw-bold text-dark mb-3 d-flex align-items-center">
                                    <Monitor className="text-primary me-2" size={24} />
                                    Appearance & Preferences
                                </h2>
                                <div className="row g-3">
                                    <div className="col-md-4">
                                        <label className="form-label fw-semibold">Theme</label>
                                        <select className="form-select rounded-3">
                                            <option value="light">Light Mode</option>
                                            <option value="dark">Dark Mode</option>
                                            <option value="auto">Auto (System)</option>
                                        </select>
                                    </div>
                                    <div className="col-md-4">
                                        <label className="form-label fw-semibold">Language</label>
                                        <select className="form-select rounded-3">
                                            <option value="en-US">English (US)</option>
                                            <option value="en-GB">English (UK)</option>
                                            <option value="es-ES">Español</option>
                                            <option value="fr-FR">Français</option>
                                        </select>
                                    </div>
                                    <div className="col-md-4">
                                        <label className="form-label fw-semibold">Timezone</label>
                                        <select className="form-select rounded-3">
                                            <option value="UTC-8">Pacific Time (UTC-8)</option>
                                            <option value="UTC-5">Eastern Time (UTC-5)</option>
                                            <option value="UTC+0">UTC</option>
                                            <option value="UTC+1">Central European (UTC+1)</option>
                                        </select>
                                    </div>
                                </div>
                                <div className="mt-3">
                                    <button className="btn btn-primary rounded-3 fw-semibold">
                                        <Save size={16} className="me-2" />
                                        Save Preferences
                                    </button>
                                </div>
                            </div>
                        </div>

                        {/* Security & Sessions Card */}
                        <div className="card shadow-sm border-0 rounded-4 mb-4">
                            <div className="card-body p-4">
                                <h2 className="card-title fw-bold text-dark mb-3 d-flex align-items-center">
                                    <Key className="text-primary me-2" size={24} />
                                    Security & Sessions
                                </h2>
                                <div className="row g-3">
                                    <div className="col-12">
                                        <div className="bg-light rounded-3 p-3">
                                            <div className="d-flex justify-content-between align-items-center mb-2">
                                                <div>
                                                    <div className="fw-semibold">Current Session</div>
                                                    <div className="small text-muted">Chrome on Windows • Active now</div>
                                                </div>
                                                <span className="badge bg-success">Current</span>
                                            </div>
                                        </div>
                                    </div>
                                    <div className="col-12">
                                        <div className="bg-light rounded-3 p-3">
                                            <div className="d-flex justify-content-between align-items-center mb-2">
                                                <div>
                                                    <div className="fw-semibold">Mobile Session</div>
                                                    <div className="small text-muted">Safari on iPhone • 2 hours ago</div>
                                                </div>
                                                <button className="btn btn-outline-danger btn-sm">
                                                    <X size={14} className="me-1" />
                                                    Revoke
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                    <div className="col-12">
                                        <div className="d-flex align-items-center justify-content-between">
                                            <div>
                                                <div className="fw-semibold">Two-Factor Authentication</div>
                                                <div className="small text-muted">Add an extra layer of security</div>
                                            </div>
                                            <button className="btn btn-outline-primary btn-sm">
                                                <Shield size={14} className="me-1" />
                                                Enable 2FA
                                            </button>
                                        </div>
                                    </div>
                                </div>
                                <div className="mt-3">
                                    <button className="btn btn-outline-danger rounded-3 fw-semibold">
                                        <AlertTriangle size={16} className="me-2" />
                                        Sign Out All Sessions
                                    </button>
                                </div>
                            </div>
                        </div>

                        {/* Data & Privacy Card */}
                        <div className="card shadow-sm border-0 rounded-4 mb-4">
                            <div className="card-body p-4">
                                <h2 className="card-title fw-bold text-dark mb-3 d-flex align-items-center">
                                    <Download className="text-primary me-2" size={24} />
                                    Data & Privacy
                                </h2>
                                <div className="row g-3">
                                    <div className="col-md-6">
                                        <div className="form-check form-switch">
                                            <input className="form-check-input" type="checkbox" id="activityTracking" defaultChecked />
                                            <label className="form-check-label fw-semibold" htmlFor="activityTracking">
                                                Activity Tracking
                                            </label>
                                            <div className="small text-muted">Track usage for analytics</div>
                                        </div>
                                    </div>
                                    <div className="col-md-6">
                                        <div className="form-check form-switch">
                                            <input className="form-check-input" type="checkbox" id="dataCollection" />
                                            <label className="form-check-label fw-semibold" htmlFor="dataCollection">
                                                Data Collection
                                            </label>
                                            <div className="small text-muted">Help improve our services</div>
                                        </div>
                                    </div>
                                    <div className="col-12">
                                        <hr className="my-3" />
                                        <div className="d-flex gap-2">
                                            <button className="btn btn-outline-primary rounded-3 fw-semibold">
                                                <Download size={16} className="me-2" />
                                                Download My Data
                                            </button>
                                            <button className="btn btn-outline-secondary rounded-3 fw-semibold">
                                                <Activity size={16} className="me-2" />
                                                View Activity Log
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        {/* Account Deletion Card */}
                        <div className="card shadow-sm border-0 rounded-4 mt-4 border-danger">
                            <div className="card-body p-4">
                                <h2 className="card-title fw-bold text-danger mb-3 d-flex align-items-center">
                                    <Trash2 className="text-danger me-2" size={24} />
                                    Delete Account
                                </h2>
                                <div className="bg-danger bg-opacity-10 p-4 rounded-3 mb-4">
                                    <div className="d-flex align-items-start">
                                        <AlertTriangle className="text-danger me-3 mt-1 flex-shrink-0" size={20} />
                                        <div>
                                            <h6 className="fw-bold text-danger mb-2">Permanent Account Deletion</h6>
                                            <p className="text-danger mb-2 small">
                                                This action cannot be undone. Requesting account deletion will:
                                            </p>
                                            <ul className="text-danger small mb-0">
                                                <li>Submit a request to administrators for review</li>
                                                <li>Permanently delete all your personal data</li>
                                                <li>Remove access to all applications and services</li>
                                                <li>Cannot be reversed once approved</li>
                                            </ul>
                                        </div>
                                    </div>
                                </div>
                                <button
                                    onClick={() => setShowDeleteModal(true)}
                                    className="btn btn-danger rounded-3 fw-semibold d-flex align-items-center"
                                >
                                    <Trash2 size={16} className="me-2" />
                                    Request Account Deletion
                                </button>
                            </div>
                        </div>

                        {/* Delete Confirmation Modal */}
                        {showDeleteModal && (
                            <div className="modal d-block" style={{backgroundColor: 'rgba(0,0,0,0.5)', position: 'fixed', top: 0, left: 0, width: '100%', height: '100%', zIndex: 1050}}>
                                <div className="modal-dialog modal-dialog-centered">
                                    <div className="modal-content border-0 rounded-4">
                                        <div className="modal-header border-0 pb-0">
                                            <h3 className="modal-title fw-bold text-danger d-flex align-items-center">
                                                <AlertTriangle className="me-2" size={24} />
                                                Confirm Account Deletion
                                            </h3>
                                            <button
                                                type="button"
                                                className="btn-close"
                                                onClick={() => setShowDeleteModal(false)}
                                            ></button>
                                        </div>
                                        <div className="modal-body">
                                            <div className="bg-danger bg-opacity-10 p-4 rounded-3 mb-4">
                                                <p className="text-danger fw-semibold mb-3">
                                                    Are you absolutely sure you want to request account deletion?
                                                </p>
                                                <p className="text-danger small mb-0">
                                                    This will submit a permanent deletion request to the administrators. 
                                                    Once approved, all your data will be permanently removed and cannot be recovered.
                                                </p>
                                            </div>
                                            <div className="form-check mb-3">
                                                <input className="form-check-input" type="checkbox" id="confirmDelete" />
                                                <label className="form-check-label text-danger fw-semibold" htmlFor="confirmDelete">
                                                    I understand this action cannot be undone
                                                </label>
                                            </div>
                                        </div>
                                        <div className="modal-footer border-0">
                                            <div className="d-flex gap-2 w-100">
                                                <button
                                                    onClick={() => setShowDeleteModal(false)}
                                                    className="btn btn-secondary rounded-3 fw-semibold flex-fill"
                                                >
                                                    Cancel
                                                </button>
                                                <button
                                                    onClick={handleDeleteRequest}
                                                    className="btn btn-danger rounded-3 fw-semibold flex-fill d-flex align-items-center justify-content-center"
                                                >
                                                    <Trash2 size={16} className="me-2" />
                                                    Submit Deletion Request
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        )}
                </div>
            </div>
        </div>
    );
};

export default SettingsDashboard;