// Test script to create roles and application connections
const testSetup = async () => {
    // First login
    const loginResponse = await fetch('http://localhost:8080/login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            username: 'admin',
            password: 'Admin123!'
        })
    });

    const loginData = await loginResponse.json();
    console.log('Login success:', loginData.success);

    if (!loginData.token) {
        console.error('No token received');
        return;
    }

    // Create 3 Roles: Admin, User, and PlatformAdmin
    const roles = ['Admin', 'User', 'PlatformAdmin'];
    
    for (const roleName of roles) {
        console.log(`Creating role: ${roleName}`);
        
        const roleResponse = await fetch('http://localhost:8080/settings/roles', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${loginData.token}`
            },
            body: JSON.stringify({
                roleName: roleName
            })
        });

        const roleData = await roleResponse.json();
        console.log(`Role ${roleName} creation response:`, JSON.stringify(roleData, null, 2));
    }

    // Create application connections from restore-admin-access.js
    console.log('Creating Development Database application connection');
    
    const createDevResponse = await fetch('http://localhost:8080/settings/applications/new-application-connection', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        },
        body: JSON.stringify({
            applicationName: 'Development Database',
            applicationDescription: 'Local development SQL Server instance',
            dataSourceType: 'MicrosoftSqlServer',
            connectionSource: {
                host: 'db',
                port: '1433',
                databaseName: 'PrototypeDb',
                authenticationType: 'UserPassword',
                username: 'sa',
                password: 'YourStrong!Passw0rd',
                url: 'Server=db,1433;Database=PrototypeDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;'
            }
        })
    });

    console.log('Development Database response status:', createDevResponse.status);
    const devResponseText = await createDevResponse.text();
    console.log('Development Database raw response:', devResponseText);
    
    try {
        const createDevData = JSON.parse(devResponseText);
        console.log('Development Database creation response:', JSON.stringify(createDevData, null, 2));
    } catch (error) {
        console.error('Failed to parse Development Database response as JSON:', error.message);
    }

    console.log('Creating Staging Database application connection');
    
    const createStagingResponse = await fetch('http://localhost:8080/settings/applications/new-application-connection', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        },
        body: JSON.stringify({
            applicationName: 'Staging Database',
            applicationDescription: 'Staging environment database',
            dataSourceType: 'MicrosoftSqlServer',
            connectionSource: {
                host: 'db',
                port: '1433',
                databaseName: 'PrototypeDb',
                authenticationType: 'UserPassword',
                username: 'sa',
                password: 'YourStrong!Passw0rd',
                url: 'Server=db,1433;Database=PrototypeDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;'
            }
        })
    });

    const createStagingData = await createStagingResponse.json();
    console.log('Staging Database creation response:', JSON.stringify(createStagingData, null, 2));

    // Create PrototypeDb application connection
    console.log('Creating PrototypeDb application connection');
    
    const createAppResponse = await fetch('http://localhost:8080/settings/applications/new-application-connection', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        },
        body: JSON.stringify({
            applicationName: 'PrototypeDb',
            applicationDescription: 'Main prototype database connection',
            dataSourceType: 'MicrosoftSqlServer',
            connectionSource: {
                host: 'db',
                port: '1433',
                databaseName: 'PrototypeDb',
                authenticationType: 'UserPassword',
                username: 'sa',
                password: 'YourStrong!Passw0rd',
                url: 'Server=db,1433;Database=PrototypeDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;'
            }
        })
    });

    const createAppData = await createAppResponse.json();
    console.log('PrototypeDb application creation response:', JSON.stringify(createAppData, null, 2));

    // Get all roles to verify creation
    const rolesResponse = await fetch('http://localhost:8080/settings/roles', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        }
    });

    const rolesData = await rolesResponse.json();
    console.log('Available roles:', rolesData.data?.items?.length || 0);
    if (rolesData.data?.items) {
        rolesData.data.items.forEach(role => {
            console.log(`- ${role.role} (ID: ${role.userRoleId})`);
        });
    }

    // Get all applications to verify creation
    const appsResponse = await fetch('http://localhost:8080/settings/applications/get-applications', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        }
    });

    const appsData = await appsResponse.json();
    console.log('Available applications:', appsData.data?.data?.length || 0);
    if (appsData.data?.data) {
        appsData.data.data.forEach(app => {
            console.log(`- ${app.applicationName} (ID: ${app.applicationId})`);
        });
    }
};

testSetup().catch(console.error);