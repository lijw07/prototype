import React, { useState, useEffect } from 'react';
import { Settings, User, Shield, Eye, EyeOff } from 'lucide-react';
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
    const [showPasswordForm, setShowPasswordForm] = useState(false);

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
            if (response.success && response.data?.user) {
                setUserSettings(response.data.user);
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
            if (response.success) {
                alert('Password changed successfully!');
                setPasswordForm({ currentPassword: '', newPassword: '', reTypeNewPassword: '' });
                setShowPasswordForm(false);
            } else {
                alert(response.message || 'Failed to change password');
            }
        } catch (error: any) {
            console.error('Failed to change password:', error);
            alert(error.message || 'Failed to change password');
        }
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
                                <h2 className="card-title fw-bold text-dark mb-4 d-flex align-items-center">
                                    <User className="text-primary me-2" size={24} />
                                    User Information
                                </h2>
                                {userSettings ? (
                                    <div className="row g-4">
                                        <div className="col-md-6">
                                            <label className="form-label fw-semibold text-muted small">FIRST NAME</label>
                                            <p className="fw-semibold text-dark mb-0">{userSettings.firstName}</p>
                                        </div>
                                        <div className="col-md-6">
                                            <label className="form-label fw-semibold text-muted small">LAST NAME</label>
                                            <p className="fw-semibold text-dark mb-0">{userSettings.lastName}</p>
                                        </div>
                                        <div className="col-md-6">
                                            <label className="form-label fw-semibold text-muted small">USERNAME</label>
                                            <p className="fw-semibold text-dark mb-0">{userSettings.username}</p>
                                        </div>
                                        <div className="col-md-6">
                                            <label className="form-label fw-semibold text-muted small">EMAIL</label>
                                            <p className="fw-semibold text-dark mb-0">{userSettings.email}</p>
                                        </div>
                                        <div className="col-md-6">
                                            <label className="form-label fw-semibold text-muted small">PHONE NUMBER</label>
                                            <p className="fw-semibold text-dark mb-0">{userSettings.phoneNumber || 'Not provided'}</p>
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

                        <div className="card shadow-sm border-0 rounded-4">
                            <div className="card-body p-4">
                                <div className="d-flex justify-content-between align-items-center mb-4">
                                    <h2 className="card-title fw-bold text-dark mb-0 d-flex align-items-center">
                                        <Shield className="text-primary me-2" size={24} />
                                        Password Security
                                    </h2>
                                    <button
                                        onClick={() => setShowPasswordForm(!showPasswordForm)}
                                        className="btn btn-primary rounded-3 fw-semibold"
                                    >
                                        {showPasswordForm ? 'Cancel' : 'Change Password'}
                                    </button>
                                </div>

                                {showPasswordForm && (
                                    <div className="bg-light p-4 rounded-3">
                                        <div className="row g-3">
                                            <div className="col-12">
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
                                            <div className="col-12">
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
                                            <div className="col-12">
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
                                                    className="btn btn-success rounded-3 fw-semibold"
                                                >
                                                    Update Password
                                                </button>
                                                <button
                                                    onClick={() => setShowPasswordForm(false)}
                                                    className="btn btn-secondary rounded-3 fw-semibold"
                                                >
                                                    Cancel
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                )}
                            </div>
                        </div>
                </div>
            </div>
        </div>
    );
};

export default SettingsDashboard;