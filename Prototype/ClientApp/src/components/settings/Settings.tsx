import React, { useEffect, useState, CSSProperties } from 'react';
import Button from '../shared/button';

interface SettingsProps {}

export interface User { userId: string; firstName: string; lastName: string; username: string; email: string; phoneNumber: string; }
export interface AuditLog { auditLogId: string; userId: string; actionType: number; metadata: string; createdAt: string; }
export interface Application { applicationId: string; applicationName: string; applicationDescription: string; createdAt: string; updatedAt: string; }

enum DataSourceTypeEnum { MicrosoftSqlServer = 0, MySql = 1, MongoDb = 2 }
enum AuthTypeEnum {
    UserPassword = 0,
    Kerberos = 1,
    AzureAdPassword = 2,
    AzureAdInteractive = 3,
    AzureAdIntegrated = 4,
    AzureAdDefault = 5,
    AzureAdMsi = 6,
    ScramSha1 = 7,
    ScramSha256 = 8,
    AwsIam = 9,
    X509 = 10,
    GssapiKerberos = 11,
    PlainLdap = 12,
    NoAuth = 13
}

export default function Settings(props: SettingsProps) {
    const [user, setUser] = useState<User | null>(null);
    const [logs, setLogs] = useState<AuditLog[] | null>(null);
    const [applications, setApplications] = useState<Application[]>([]);
    const [error, setError] = useState<string | null>(null);
    const [showForm, setShowForm] = useState(false);
    const [isEditMode, setIsEditMode] = useState(false);
    const [editingAppId, setEditingAppId] = useState<string | null>(null);

    const [currentPassword, setCurrentPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [reTypeNewPassword, setReTypeNewPassword] = useState('');
    const [passwordMessage, setPasswordMessage] = useState<string | null>(null);

    const [dataSourceType, setDataSourceType] = useState<DataSourceTypeEnum>(DataSourceTypeEnum.MicrosoftSqlServer);
    const [authType, setAuthType] = useState<AuthTypeEnum>(AuthTypeEnum.UserPassword);
    const [appName, setAppName] = useState('');
    const [appDesc, setAppDesc] = useState('');
    const [host, setHost] = useState('');
    const [port, setPort] = useState('');
    const [databaseName, setDatabaseName] = useState('');
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [connectionMessage, setConnectionMessage] = useState<string | null>(null);

    const [instance, setInstance] = useState('');
    const [authenticationDatabase, setAuthenticationDatabase] = useState('');
    const [awsAccessKeyId, setAwsAccessKeyId] = useState('');
    const [awsSecretAccessKey, setAwsSecretAccessKey] = useState('');
    const [awsSessionToken, setAwsSessionToken] = useState('');
    const [principal, setPrincipal] = useState('');
    const [serviceName, setServiceName] = useState('');
    const [serviceRealm, setServiceRealm] = useState('');
    const [canonicalizeHostName, setCanonicalizeHostName] = useState(false);

    const authOptionsMap: Record<DataSourceTypeEnum, AuthTypeEnum[]> = {
        [DataSourceTypeEnum.MicrosoftSqlServer]: [
            AuthTypeEnum.UserPassword, 
            AuthTypeEnum.Kerberos,
            AuthTypeEnum.AzureAdPassword, 
            AuthTypeEnum.AzureAdInteractive,
            AuthTypeEnum.AzureAdIntegrated,
            AuthTypeEnum.AzureAdDefault,
            AuthTypeEnum.AzureAdMsi,
            AuthTypeEnum.NoAuth],
        [DataSourceTypeEnum.MySql]: [
            AuthTypeEnum.UserPassword, 
            AuthTypeEnum.NoAuth],
        [DataSourceTypeEnum.MongoDb]: [
            AuthTypeEnum.UserPassword, 
            AuthTypeEnum.ScramSha1, 
            AuthTypeEnum.ScramSha256, 
            AuthTypeEnum.AwsIam, 
            AuthTypeEnum.X509, 
            AuthTypeEnum.GssapiKerberos, 
            AuthTypeEnum.PlainLdap, 
            AuthTypeEnum.NoAuth],
    };

    const styles: Record<string, CSSProperties> = {
        container: { maxWidth: '800px', margin: '0 auto', fontFamily: 'sans-serif', padding: '1rem' },
        section: { marginBottom: '2rem', padding: '1rem', borderRadius: '8px', boxShadow: '0 2px 5px rgba(0,0,0,0.1)' },
        formRow: { display: 'flex', flexWrap: 'wrap', gap: '1rem', marginBottom: '1rem' },
        input: { flex: '1 1 200px', padding: '0.5rem', borderRadius: '4px', border: '1px solid #ccc' },
    };

    const getAuthHeaders = () => ({ 'Content-Type': 'application/json', 'Authorization': `Bearer ${localStorage.getItem('authToken')}` });

    useEffect(() => {
        (async () => {
            try {
                const [uRes, aRes] = await Promise.all([
                    fetch('/settings/user', { headers: getAuthHeaders() }),
                    fetch('/ApplicationSettings/get-applications', { headers: getAuthHeaders() }),
                ]);
                if (uRes.ok) setUser(await uRes.json());
                if (aRes.ok) setApplications(await aRes.json());
            } catch (err) {
                console.error(err);
            }
        })();
    }, []);

    const handleTestConnection = async () => {
        const url = dataSourceType === DataSourceTypeEnum.MongoDb
            ? `mongodb://${username}:${password}@${host}:${port}/${databaseName}`
            : dataSourceType === DataSourceTypeEnum.MySql
            ? `Server=${host};Port=${port};Database=${databaseName};`
            : `Server=${host},${port};Database=${databaseName};`;

        const dto = {
            applicationName: appName,
            applicationDescription: appDesc,
            dataSourceType,
            connectionSource: {
                host,
                port,
                instance,
                authenticationType: authType,
                databaseName,
                url,
                username,
                password,
                authenticationDatabase,
                awsAccessKeyId,
                awsSecretAccessKey,
                awsSessionToken,
                principal,
                serviceName,
                serviceRealm,
                canonicalizeHostName
            }
        };

        try {
            const res = await fetch('/ApplicationSettings/test-application-connection', {
                method: 'POST',
                headers: getAuthHeaders(),
                body: JSON.stringify(dto)
            });
            console.log(res);
            const body = await res.json();
            console.log(body);
            setConnectionMessage(body.message);
            console.log(body.message);
        } catch {
            setConnectionMessage('Error testing connection');
        }
    };

    const handleCreateApplication = async () => {
        const url = dataSourceType === DataSourceTypeEnum.MongoDb
            ? `mongodb://${username}:${password}@${host}:${port}/${databaseName}`
            : dataSourceType === DataSourceTypeEnum.MySql
            ? `Server=${host};Port=${port};Database=${databaseName};`
            : `Server=${host},${port};Database=${databaseName};`;

        const dto = {
            applicationName: appName,
            applicationDescription: appDesc,
            dataSourceType,
            connectionSource: {
                host,
                port,
                instance,
                authenticationType: authType,
                databaseName,
                url,
                username,
                password,
                authenticationDatabase,
                awsAccessKeyId,
                awsSecretAccessKey,
                awsSessionToken,
                principal,
                serviceName,
                serviceRealm,
                canonicalizeHostName
            }
        };

        try {
            if (isEditMode && editingAppId) {
                // Update application
                const res = await fetch(`/ApplicationSettings/update-application/${editingAppId}`, {
                    method: 'PUT',
                    headers: getAuthHeaders(),
                    body: JSON.stringify(dto)
                });
                console.log(res);
                if (res.ok) {
                    setConnectionMessage('Updated successfully');
                    setEditingAppId(null);
                    setIsEditMode(false);
                    setShowForm(false);
                    setAppName('');
                    setAppDesc('');
                    setHost('');
                    setPort('');
                    setDatabaseName('');
                    setUsername('');
                    setPassword('');
                    setInstance('');
                    setAuthenticationDatabase('');
                    setAwsAccessKeyId('');
                    setAwsSecretAccessKey('');
                    setAwsSessionToken('');
                    setPrincipal('');
                    setServiceName('');
                    setServiceRealm('');
                    setCanonicalizeHostName(false);
                    await fetchApplications();
                } else {
                    setConnectionMessage('Update failed');
                }
            } else {
                // Create application
                const res = await fetch('/ApplicationSettings/new-application-connection', {
                    method: 'POST',
                    headers: getAuthHeaders(),
                    body: JSON.stringify(dto)
                });
                if (res.ok) {
                    setConnectionMessage('Created successfully');
                    await fetchApplications();
                    setShowForm(false);
                    setAppName('');
                    setAppDesc('');
                    setHost('');
                    setPort('');
                    setDatabaseName('');
                    setUsername('');
                    setPassword('');
                    setInstance('');
                    setAuthenticationDatabase('');
                    setAwsAccessKeyId('');
                    setAwsSecretAccessKey('');
                    setAwsSessionToken('');
                    setPrincipal('');
                    setServiceName('');
                    setServiceRealm('');
                    setCanonicalizeHostName(false);
                } else {
                    setConnectionMessage('Create failed');
                }
            }
        } catch {
            setConnectionMessage(isEditMode ? 'Error updating application' : 'Error creating application');
        }
    };

    const fetchApplications = async () => {
        try {
            const res = await fetch('/ApplicationSettings/get-applications', { headers: getAuthHeaders() });
            if (res.ok) setApplications(await res.json());
        } catch {}
    };

    const handleDeleteApplication = async (id: string) => {
        try {
            const res = await fetch(`/ApplicationSettings/delete-application/${id}`, { method: 'DELETE', headers: getAuthHeaders() });
            if (res.ok) setApplications(prev => prev.filter(a => a.applicationId !== id));
        } catch {}
    };

    const handleFetchAuditLogs = async () => {
        setError(null);
        setLogs(null);
        try {
            const res = await fetch('/AuditLogSettings', { headers: getAuthHeaders() });
            if (res.ok) setLogs(await res.json()); else setError('Failed to fetch logs');
        } catch {
            setError('Error fetching logs');
        }
    };

    const handleChangePassword = async () => {
        setPasswordMessage(null);
        if (newPassword !== reTypeNewPassword) { setPasswordMessage('Passwords do not match'); return; }
        try {
            const res = await fetch('/settings/user/change-password', { method: 'PUT', headers: getAuthHeaders(), body: JSON.stringify({ currentPassword, newPassword, reTypeNewPassword }) });
            const body = await res.json(); setPasswordMessage(body.message);
        } catch {
            setPasswordMessage('Error changing password');
        }
    };

    const handleLogout = async () => {
        try { await fetch('/logout/logout', { method: 'POST', credentials: 'include' }); localStorage.clear(); sessionStorage.clear(); window.location.href = '/login'; } catch {}
    };

    return (
        <div style={styles.container}>
            <h1 style={{ textAlign: 'center', marginBottom: '1.5rem' }}>Settings</h1>

            {/* User Info First */}
            <section style={styles.section}>
                <h2>User Info</h2>
                {user && (
                    <>
                        <p><strong>{user.firstName} {user.lastName}</strong> (@{user.username})</p>
                        <p>{user.email}</p>
                        <p>{user.phoneNumber}</p>
                    </>
                )}
                <div style={{ marginTop: '1rem' }}>
                    <Button label="Logout" onClick={handleLogout} color="red" size="lg" />
                </div>
            </section>

            {/* Applications List */}
            <section style={styles.section}>
                <h2>Applications</h2>
                <ul style={{ listStyle: 'none', padding: 0 }}>
                    {applications.map(app => (
                        <li key={app.applicationId} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0.5rem 0' }}>
                            <span>{app.applicationName}</span>
                            <div style={{ display: 'flex', gap: '0.5rem' }}>
                                <Button
                                    label="Test"
                                    onClick={async () => {
                                        const res = await fetch(`/ApplicationSettings/test-application-connection/${app.applicationId}`, {
                                            method: 'POST',
                                            headers: getAuthHeaders()
                                        });

                                        let body;
                                        const contentType = res.headers.get('Content-Type') || '';

                                        if (contentType.includes('application/json')) {
                                            body = await res.json();
                                        } else {
                                            body = { message: await res.text() };
                                        }

                                        alert(body.message || 'Unknown response');
                                    }}
                                    color="gray"
                                    size="sm"
                                />
                                <Button
                                    label="Edit"
                                    onClick={async () => {
                                        setShowForm(true);
                                        setAppName(app.applicationName);
                                        setAppDesc(app.applicationDescription);
                                        // Populate connection-level fields from the application's connection, if available.
                                        // This assumes that your backend returns ApplicationConnections as a property on each app.
                                        // If not, you may need to fetch it separately.
                                        let connection = null;
                                        // Try to get connection details from app if available.
                                        if ((app as any).applicationConnections && Array.isArray((app as any).applicationConnections) && (app as any).applicationConnections.length > 0) {
                                            connection = (app as any).applicationConnections[0];
                                        }
                                        // If not present, optionally fetch from backend (pseudo-code):
                                        // if (!connection) {
                                        //   const res = await fetch(`/ApplicationSettings/get-application-connection/${app.applicationId}`, { headers: getAuthHeaders() });
                                        //   if (res.ok) connection = await res.json();
                                        // }
                                        // Populate fields from connection or clear if not present.
                                        setHost(connection?.host || '');
                                        setPort(connection?.port || '');
                                        setDatabaseName(connection?.databaseName || '');
                                        setUsername(connection?.username || '');
                                        setPassword(connection?.password || '');
                                        setInstance(connection?.instance || '');
                                        setAuthenticationDatabase(connection?.authenticationDatabase || '');
                                        setAwsAccessKeyId(connection?.awsAccessKeyId || '');
                                        setAwsSecretAccessKey(connection?.awsSecretAccessKey || '');
                                        setAwsSessionToken(connection?.awsSessionToken || '');
                                        setPrincipal(connection?.principal || '');
                                        setServiceName(connection?.serviceName || '');
                                        setServiceRealm(connection?.serviceRealm || '');
                                        setCanonicalizeHostName(connection?.canonicalizeHostName || false);
                                        // Optionally set dataSourceType and authType if present
                                        if (typeof (app as any).dataSourceType !== "undefined") setDataSourceType((app as any).dataSourceType);
                                        if (typeof connection?.authenticationType !== "undefined") setAuthType(connection.authenticationType);
                                        setIsEditMode(true);
                                        setEditingAppId(app.applicationId);
                                    }}
                                    color="orange"
                                    size="sm"
                                />
                                <Button label="Delete" onClick={() => handleDeleteApplication(app.applicationId)} color="red" size="sm" />
                            </div>
                        </li>
                    ))}
                </ul>
                <Button label={showForm ? 'Cancel' : 'New Connection'} onClick={() => {
                    setShowForm(prev => !prev);
                    if (showForm) {
                        setIsEditMode(false);
                        setEditingAppId(null);
                        setAppName('');
                        setAppDesc('');
                        setHost('');
                        setPort('');
                        setDatabaseName('');
                        setUsername('');
                        setPassword('');
                        setInstance('');
                        setAuthenticationDatabase('');
                        setAwsAccessKeyId('');
                        setAwsSecretAccessKey('');
                        setAwsSessionToken('');
                        setPrincipal('');
                        setServiceName('');
                        setServiceRealm('');
                        setCanonicalizeHostName(false);
                    }
                }} color="blue" size="lg" />
            </section>

            {/* New Connection Form */}
            {showForm && (
                <section style={styles.section}>
                    <h2>{isEditMode ? 'Edit Application Connection' : 'New Application Connection'}</h2>
                    <div style={styles.formRow}>
                        <select value={dataSourceType} onChange={e => setDataSourceType(Number(e.target.value))} style={styles.input}>
                            <option value={DataSourceTypeEnum.MicrosoftSqlServer}>SQL Server</option>
                            <option value={DataSourceTypeEnum.MySql}>MySQL</option>
                            <option value={DataSourceTypeEnum.MongoDb}>MongoDB</option>
                        </select>
                        <select value={authType} onChange={e => setAuthType(Number(e.target.value))} style={styles.input}>
                            {authOptionsMap[dataSourceType].map(opt => (<option key={opt} value={opt}>{AuthTypeEnum[opt]}</option>))}
                        </select>
                    </div>
                    <div style={styles.formRow}>
                        <input placeholder="App Name" value={appName} onChange={e => setAppName(e.target.value)} style={styles.input} />
                        <input placeholder="Description" value={appDesc} onChange={e => setAppDesc(e.target.value)} style={styles.input} />
                    </div>
                    <div style={styles.formRow}>
                        <input placeholder="Host" value={host} onChange={e => setHost(e.target.value)} style={styles.input} />
                        <input placeholder="Port" value={port} onChange={e => setPort(e.target.value)} style={styles.input} />
                        <input placeholder="Database" value={databaseName} onChange={e => setDatabaseName(e.target.value)} style={styles.input} />
                    </div>
                    {/* Username and Password Inputs */}
                    {(() => {
                        // Define which fields to show based on authType
                        const USERNAME_AUTH_TYPES = [
                            AuthTypeEnum.UserPassword,
                            AuthTypeEnum.Kerberos,
                            AuthTypeEnum.AzureAdPassword,
                            AuthTypeEnum.AzureAdInteractive,
                            AuthTypeEnum.AzureAdMsi,
                            AuthTypeEnum.PlainLdap
                        ];
                        const PASSWORD_AUTH_TYPES = [
                            AuthTypeEnum.UserPassword,
                            AuthTypeEnum.Kerberos,
                            AuthTypeEnum.AzureAdPassword,
                            AuthTypeEnum.PlainLdap
                        ];
                        const showUsernameInput = USERNAME_AUTH_TYPES.includes(authType);
                        const showPasswordInput = PASSWORD_AUTH_TYPES.includes(authType);
                        return (showUsernameInput || showPasswordInput) ? (
                            <div style={styles.formRow}>
                                {showUsernameInput && (
                                    <input
                                        placeholder="Username"
                                        value={username}
                                        onChange={e => setUsername(e.target.value)}
                                        style={styles.input}
                                    />
                                )}
                                {showPasswordInput && (
                                    <input
                                        type="password"
                                        placeholder="Password"
                                        value={password}
                                        onChange={e => setPassword(e.target.value)}
                                        style={styles.input}
                                    />
                                )}
                            </div>
                        ) : null;
                    })()}
                    <div style={styles.formRow}>
                        {dataSourceType === DataSourceTypeEnum.MicrosoftSqlServer && (
                            <input
                                placeholder="Instance"
                                value={instance}
                                onChange={e => setInstance(e.target.value)}
                                style={styles.input}
                            />
                        )}
                        {[AuthTypeEnum.ScramSha1, AuthTypeEnum.ScramSha256].includes(authType) && (
                            <input 
                                placeholder="Auth DB" 
                                value={authenticationDatabase} 
                                onChange={e => setAuthenticationDatabase(e.target.value)}
                                style={styles.input}
                            />
                        )}
                        {[AuthTypeEnum.AwsIam].includes(authType) && (
                            <input 
                                placeholder="AWS Access Key ID" 
                               value={awsAccessKeyId} 
                               onChange={e => setAwsAccessKeyId(e.target.value)} 
                               style={styles.input} 
                            />
                        )}
                    </div>
                    <div style={styles.formRow}>
                        {[AuthTypeEnum.AwsIam].includes(authType) && (
                            <input 
                                placeholder="AWS Secret Access Key" 
                                   value={awsSecretAccessKey} 
                                   onChange={e => setAwsSecretAccessKey(e.target.value)} 
                                   style={styles.input} 
                            />
                        )}
                        {[AuthTypeEnum.AwsIam].includes(authType) && (
                            <input 
                                placeholder="AWS Session Token" 
                                value={awsSessionToken} 
                                onChange={e => setAwsSessionToken(e.target.value)} 
                                style={styles.input} 
                            />
                        )}
                    </div>
                    <div style={styles.formRow}>
                        {[AuthTypeEnum.GssapiKerberos].includes(authType) && (
                            <input 
                                placeholder="Principal" 
                                value={principal} 
                                onChange={e => setPrincipal(e.target.value)} 
                                style={styles.input} 
                            />
                        )}
                        {[AuthTypeEnum.GssapiKerberos].includes(authType) && (
                            <input 
                                placeholder="Service Name" 
                                value={serviceName} 
                                onChange={e => setServiceName(e.target.value)} 
                                style={styles.input} 
                            />
                        )}
                        {[AuthTypeEnum.GssapiKerberos].includes(authType) && (
                            <input 
                                placeholder="Service Realm" 
                                value={serviceRealm} 
                                onChange={e => setServiceRealm(e.target.value)} 
                                style={styles.input} 
                            />
                        )}
                    </div>
                    {[AuthTypeEnum.GssapiKerberos].includes(authType) && (
                        <div style={styles.formRow}>
                        <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                            <input 
                                type="checkbox" 
                                checked={canonicalizeHostName} 
                                onChange={e => setCanonicalizeHostName(e.target.checked)} 
                            />
                            Canonicalize Host Name
                        </label>
                    </div>
                    )}
                    <div style={styles.formRow}>
                        <Button label="Test" onClick={handleTestConnection} color="gray" size="lg" />
                        <Button label={isEditMode ? 'Update' : 'Create'} onClick={handleCreateApplication} color={isEditMode ? 'orange' : 'green'} size="lg" />
                    </div>
                    {connectionMessage && <p>{connectionMessage}</p>}
                </section>
            )}

            {/* Audit Logs */}
            <section style={styles.section}>
                <h2>Audit Logs</h2>
                <Button label="Get Logs" onClick={handleFetchAuditLogs} color="gray" size="lg" />
                {error && <p>{error}</p>}
                {logs && (
                    <ul style={{ listStyle: 'none', padding: 0 }}>
                        {logs.map(log => (
                            <li key={log.auditLogId} style={{ padding: '0.5rem 0' }}>
                                <strong>{log.userId}</strong> at {new Date(log.createdAt).toLocaleString()}
                            </li>
                        ))}
                    </ul>
                )}
            </section>
        </div>
    );
}