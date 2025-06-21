// Create new applications with unique names
const createNewApps = async () => {
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

    // Create a new application
    const createResponse = await fetch('http://localhost:8080/ApplicationSettings/new-application-connection', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        },
        body: JSON.stringify({
            applicationName: 'New Development Database',
            applicationDescription: 'Restored development SQL Server instance',
            dataSourceType: 'MicrosoftSqlServer',
            connectionSource: {
                host: 'localhost',
                port: '1433',
                databaseName: 'NewDevDb',
                authenticationType: 'UserPassword',
                username: 'sa',
                password: 'YourStrong!Passw0rd',
                url: 'Server=localhost,1433;Database=NewDevDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;'
            }
        })
    });

    const createData = await createResponse.json();
    console.log('Create application response:', JSON.stringify(createData, null, 2));

    // Create another application
    const createResponse2 = await fetch('http://localhost:8080/ApplicationSettings/new-application-connection', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        },
        body: JSON.stringify({
            applicationName: 'New Staging Database',
            applicationDescription: 'Restored staging environment database',
            dataSourceType: 'MicrosoftSqlServer',
            connectionSource: {
                host: 'localhost',
                port: '1433',
                databaseName: 'NewStagingDb',
                authenticationType: 'UserPassword',
                username: 'sa',
                password: 'YourStrong!Passw0rd',
                url: 'Server=localhost,1433;Database=NewStagingDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;'
            }
        })
    });

    const createData2 = await createResponse2.json();
    console.log('Create application 2 response:', JSON.stringify(createData2, null, 2));

    // Check applications again
    const appsResponse = await fetch('http://localhost:8080/ApplicationSettings/get-applications', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        }
    });

    const appsData = await appsResponse.json();
    console.log('Available applications after creation:', appsData.data?.data?.length || 0);
    if (appsData.data?.data?.length > 0) {
        console.log('Applications:', appsData.data.data.map(app => app.applicationName));
    }
};

createNewApps().catch(console.error);