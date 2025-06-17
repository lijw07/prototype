import React, { useState, ChangeEvent, FormEvent } from 'react';
import Button from '../shared/button';

interface SettingsProps {}

export interface User {
  userId: string;      
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber: string;
  createdAt: string;       
  updatedAt: string;
}

export interface AuditLog {
  id: number;
  userId: number;
  actionType: number;
  metadata: string;
  createdAt: string;

}
export default function Settings(props: SettingsProps) {
    const [logs, setLogs] = useState<AuditLog[] | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [token, setToken] = useState("")

    const handleGetCurrentUser = async () => {

        try {
            const response = await fetch('/ApplicationSettings/getCurrentUser', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({})  // empty object, because your DTO has no fields
            });

            if (response.ok) {
            console.log('Success');

            } else {
            console.log('Fail');
            }
        } catch (err: any) {
            console.log('Fail');
        }
    };
     const handleCreateApplicaiton = async () => {
        const dto = {
                applicationName: "My App",
                applicationDescription: "This is my test app",
                dataSourceType: 1,  // assuming DataSourceTypeEnum is an int/enum
                connectionSource: {
                  host: "localhost",
                  port: "5432",
                  instance: "myInstance",
                  authenticationType: 2,  // Replace with correct enum value
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
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(dto) 
            });

            if (response.ok) {
            console.log('Success created app');

            } else {
            console.log('Failed to make app');
            }
        } catch (err: any) {
            console.log('Failed to make app');
            console.log(err)
        }
    };


    const handleFetchAuditLogs = async () => {
      
        setError(null)
        setLogs(null)

        try {
            const response = await fetch('/AuditLogSettings', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            },
            });

            if (response.ok) {
            const data = await response.json();
            setLogs(logs)
            console.log('Audit logs:', data);
            } else {
                setError(`Failed to fetch logs: ${response.status} ${response.statusText}`);
            }
        } catch (error) {
            console.error('Exception:', error);
        }
    };

  return (
    <div>
      <h1>Settings</h1>
        <div>
          <Button 
            label="Get Current User"
            onClick={handleGetCurrentUser}
            color="blue"
            size="lg"
        />
        </div>
 
        <div>
          <Button 
            label="Create Application"
            onClick={handleCreateApplicaiton}
            color="blue"
            size="lg"
          />
        </div>
          
        <div>
          <Button 
            label="Get Audit Log"
            onClick={handleFetchAuditLogs}
            color="blue"
            size="lg"
          />
        </div>
        
        {error && (
            <div style={{ color: 'red', marginTop: '1rem' }}>
            Error: {error}
            </div>
        )}
        {logs && (
            <ul style={{ marginTop: '1rem' }}>
          {logs.map(log => (
            <li key={log.id}>
              <strong>{log.userId}</strong> at {new Date(log.createdAt).toLocaleString()}
            </li>
          ))}
        </ul>
        )}
    </div>
  );
}