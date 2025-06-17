import React, { useEffect, useState } from 'react';
import Button from '../shared/button';

interface SettingsProps {}

export interface User {
    userId: string;
    firstName: string;
    lastName: string;
    username: string;
    email: string;
    phoneNumber: string;
}

export interface AuditLog {
    auditLogId: string;
    userId: string;
    actionType: number;
    metadata: string;
    createdAt: string;
}

export interface Application {
    applicationId: string;
    applicationName: string;
    applicationDescription: string;
    createdAt: string;
    updatedAt: string;
}

export default function Settings(props: SettingsProps) {
    const [user, setUser] = useState<User | null>(null);
    const [logs, setLogs] = useState<AuditLog[] | null>(null);
    const [applications, setApplications] = useState<Application[]>([]);
    const [error, setError] = useState<string | null>(null);

    const [currentPassword, setCurrentPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [reTypeNewPassword, setReTypeNewPassword] = useState('');
    const [passwordMessage, setPasswordMessage] = useState<string | null>(null);

    const getAuthHeaders = () => {
        const token = localStorage.getItem('authToken');
        return {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        };
    };

    useEffect(() => {
        const fetchInitialData = async () => {
            try {
                const [userResponse, appsResponse] = await Promise.all([
                    fetch('/settings/user', { method: 'GET', headers: getAuthHeaders() }),
                    fetch('/ApplicationSettings/get-applications', { method: 'GET', headers: getAuthHeaders() })
                ]);

                if (userResponse.ok) {
                    const userData = await userResponse.json();
                    setUser(userData);
                } else {
                    console.error('Failed to get user:', userResponse.status);
                }

                if (appsResponse.ok) {
                    const appsData = await appsResponse.json();
                    setApplications(appsData);
                } else {
                    console.error('Failed to get applications:', appsResponse.status);
                }
            } catch (err) {
                console.error('Error during initial fetch:', err);
            }
        };

        fetchInitialData();
    }, []);

    const handleChangePassword = async () => {
        setPasswordMessage(null);

        if (newPassword !== reTypeNewPassword) {
            setPasswordMessage("New password and confirmation do not match.");
            return;
        }

        try {
            const response = await fetch('/settings/user/change-password', {
                method: 'PUT',
                headers: getAuthHeaders(),
                body: JSON.stringify({
                    currentPassword,
                    newPassword,
                    reTypeNewPassword
                })
            });

            const result = await response.json();

            if (response.ok) {
                setPasswordMessage(result.message || 'Password changed successfully.');
                setCurrentPassword('');
                setNewPassword('');
                setReTypeNewPassword('');
            } else {
                setPasswordMessage(result.message || 'Failed to change password.');
            }
        } catch (error) {
            console.error('Error changing password:', error);
            setPasswordMessage("An unexpected error occurred.");
        }
    };

    const handleCreateApplication = async () => {
        const dto = {
            applicationName: "My App",
            applicationDescription: "This is my test app",
            dataSourceType: 1,
            connectionSource: {
                host: "localhost",
                port: "5432",
                instance: "myInstance",
                authenticationType: 2,
                databaseName: "TestDb",
                url: "jdbc:postgresql://localhost:5432/TestDb",
                username: "myuser",
                password: "mypassword",
                authenticationDatabase: "",
                awsAccessKeyId: "",
                awsSecretAccessKey: "",
                awsSessionToken: "",
                principal: "",
                serviceName: "",
                serviceRealm: "",
                canonicalizeHostName: false
            }
        };

        try {
            const response = await fetch('/ApplicationSettings/new-application-connection', {
                method: 'POST',
                headers: getAuthHeaders(),
                body: JSON.stringify(dto)
            });

            if (response.ok) {
                console.log('Successfully created application');
                await handleGetApplications();
            } else {
                console.log('Failed to create application:', response.status);
            }
        } catch (err) {
            console.log('Error creating application:', err);
        }
    };

    const handleGetApplications = async () => {
        try {
            const response = await fetch('/ApplicationSettings/get-applications', {
                method: 'GET',
                headers: getAuthHeaders()
            });

            if (response.ok) {
                const data = await response.json();
                setApplications(data);
                console.log('Applications:', data);
            } else {
                console.log('Failed to fetch applications:', response.status);
            }
        } catch (err) {
            console.log('Error fetching applications:', err);
        }
    };

    const handleUpdateApplication = async (applicationId: string) => {
        const dto = {
            applicationName: "Updated App",
            applicationDescription: "This app has been updated",
            dataSourceType: 1,
            connectionSource: {
                host: "localhost",
                port: "5432",
                instance: "updatedInstance",
                authenticationType: 2,
                databaseName: "UpdatedDb",
                url: "jdbc:postgresql://localhost:5432/UpdatedDb",
                username: "updateduser",
                password: "updatedpassword",
                authenticationDatabase: "",
                awsAccessKeyId: "",
                awsSecretAccessKey: "",
                awsSessionToken: "",
                principal: "",
                serviceName: "",
                serviceRealm: "",
                canonicalizeHostName: false
            }
        };

        try {
            const response = await fetch(`/ApplicationSettings/update-application/${applicationId}`, {
                method: 'PUT',
                headers: getAuthHeaders(),
                body: JSON.stringify(dto)
            });

            if (response.ok) {
                console.log('Successfully updated application');
                await handleGetApplications();
            } else {
                console.log('Failed to update application:', response.status);
            }
        } catch (err) {
            console.log('Error updating application:', err);
        }
    };

    const handleDeleteApplication = async (applicationId: string) => {
        try {
            const response = await fetch(`/ApplicationSettings/delete-application/${applicationId}`, {
                method: 'DELETE',
                headers: getAuthHeaders()
            });

            if (response.ok) {
                console.log('Successfully deleted application');
                setApplications(prev => prev.filter(app => app.applicationId !== applicationId));
            } else {
                console.log('Failed to delete application:', response.status);
            }
        } catch (err) {
            console.log('Error deleting application:', err);
        }
    };

    const handleFetchAuditLogs = async () => {
        setError(null);
        setLogs(null);

        try {
            const response = await fetch('/AuditLogSettings', {
                method: 'GET',
                headers: getAuthHeaders()
            });

            if (response.ok) {
                const data = await response.json();
                setLogs(data);
                console.log('Audit logs:', data);
            } else {
                setError(`Failed to fetch logs: ${response.status} ${response.statusText}`);
            }
        } catch (error) {
            console.error('Exception:', error);
        }
    };

    const handleLogout = async () => {
        try {
            await fetch('/logout/logout', {
                method: 'POST',
                credentials: 'include'
            });

            localStorage.clear();
            sessionStorage.clear();

            window.location.href = '/login';
        } catch (error) {
            console.error('Logout failed:', error);
        }
    };

    return (
        <div>
            <h1>Settings</h1>

            {/* Section 1: User Info */}
            {user && (
                <div style={{ marginBottom: '2rem' }}>
                    <h2>User Info</h2>
                    <p>Name: {user.firstName} {user.lastName}</p>
                    <p>Username: {user.username}</p>
                    <p>Email: {user.email}</p>
                    <p>Phone: {user.phoneNumber}</p>
                </div>
            )}

            {/* Section 2: Change Password */}
            <div style={{ marginBottom: '2rem' }}>
                <h2>Change Password</h2>
                <div>
                    <label>Current Password:</label><br />
                    <input type="password" value={currentPassword} onChange={e => setCurrentPassword(e.target.value)} />
                </div>
                <div>
                    <label>New Password:</label><br />
                    <input type="password" value={newPassword} onChange={e => setNewPassword(e.target.value)} />
                </div>
                <div>
                    <label>Retype New Password:</label><br />
                    <input type="password" value={reTypeNewPassword} onChange={e => setReTypeNewPassword(e.target.value)} />
                </div>
                <Button label="Change Password" onClick={handleChangePassword} color="blue" size="lg" />
                {passwordMessage && <p style={{ color: passwordMessage.includes('success') ? 'green' : 'red' }}>{passwordMessage}</p>}
            </div>

            {/* Section 3: Create Application */}
            <div style={{ marginBottom: '2rem' }}>
                <Button label="Create Application" onClick={handleCreateApplication} color="green" size="lg" />
            </div>

            {/* Section 4: Audit Logs + Applications */}
            <div style={{ marginBottom: '2rem' }}>
                <Button label="Get Audit Log" onClick={handleFetchAuditLogs} color="gray" size="lg" />
            </div>

            <ul>
                {applications.map(app => (
                    <li key={app.applicationId}>
                        {app.applicationName}
                        <Button label="Update" onClick={() => handleUpdateApplication(app.applicationId)} color="orange" size="sm" />
                        <Button label="Delete" onClick={() => handleDeleteApplication(app.applicationId)} color="red" size="sm" />
                    </li>
                ))}
            </ul>

            {error && <div style={{ color: 'red' }}>Error: {error}</div>}

            {logs && (
                <ul>
                    {logs.map(log => (
                        <li key={log.auditLogId}>
                            <strong>{log.userId}</strong> at {new Date(log.createdAt).toLocaleString()}
                        </li>
                    ))}
                </ul>
            )}

            {/* Section 5: Logout */}
            <div style={{ marginTop: '2rem' }}>
                <Button label="Logout" onClick={handleLogout} color="red" size="lg" />
            </div>
        </div>
    );
}