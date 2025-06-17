import React, { useState, ChangeEvent, FormEvent } from 'react';
import Button from '../shared/button';

interface SettingsProps {}

export interface User {
  userId: string;          // Guid from C# as string in JS
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber: string;
  createdAt: string;       // DateTime as ISO string
  updatedAt: string;

  // Optional collections — you can omit or type them if you use them
  applications?: any[];           // or create interface UserApplicationModel
  userActivityLogs?: any[];
  auditLogs?: any[];
  userRecoveryRequests?: any[];
  userPermissions?: any;          // or define UserPermissionModel interface
}

export interface AuditLog {
  id: number;
  userId: number;
  user: User;
  actionType: number;
  createdAt: string;
  // other properties you want to display
}
export default function Settings(props: SettingsProps) {
    const [logs, setLogs] = useState<AuditLog[] | null>(null);
    const [error, setError] = useState<string | null>(null);

    const handleCreateApplication = async () => {

        setError(null)
        setLogs(null)

        try {
            const response = await fetch('/ApplicationSettings', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({})  // empty object, because your DTO has no fields
            });

            if (response.ok) {
            console.log('Success');
            const data = await response.json();
            setLogs(data);
            } else {
            setError(`Failed to fetch logs: ${response.status} ${response.statusText}`);
            }
        } catch (err: any) {
            setError(`Exception: ${err.message || err.toString()}`);
        }
    };

    const handleFetchAuditLogs = async () => {
        try {
            const response = await fetch('/AuditLogSettings', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            },
            });

            if (response.ok) {
            const data = await response.json();
            console.log('Audit logs:', data);
            // You can update state here if inside a React component
            } else {
            console.error('Failed to fetch audit logs:', response.status, response.statusText);
            }
        } catch (error) {
            console.error('Exception:', error);
        }
    };

  return (
    <div>
      <h1>Settings</h1>

        <Button 
            label="Create Application"
            onClick={handleCreateApplication}
            color="blue"
            size="lg"
        />
        <Button 
            label="Get Audit Log"
            onClick={handleFetchAuditLogs}
            color="blue"
            size="lg"
        />
        {error && (
            <div style={{ color: 'red', marginTop: '1rem' }}>
            Error: {error}
            </div>
        )}
        {logs && (
            <ul style={{ marginTop: '1rem' }}>
          {logs.map(log => (
            <li key={log.id}>
              <strong>{log.user.firstName}</strong> at {new Date(log.createdAt).toLocaleString()}
            </li>
          ))}
        </ul>
        )}
    </div>
  );
}