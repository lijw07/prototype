// Create some application logs by performing actions
const createAppLogs = async () => {
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

    // Create a new application to generate logs
    const createResponse = await fetch('http://localhost:8080/ApplicationSettings/new-application-connection', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        },
        body: JSON.stringify({
            applicationName: 'Test Log Application',
            applicationDescription: 'Application to test logging functionality',
            dataSourceType: 'MicrosoftSqlServer',
            connectionSource: {
                host: 'localhost',
                port: '1433',
                databaseName: 'TestLogDb',
                authenticationType: 'UserPassword',
                username: 'sa',
                password: 'YourStrong!Passw0rd',
                url: 'Server=localhost,1433;Database=TestLogDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;'
            }
        })
    });

    const createData = await createResponse.json();
    console.log('Create application response:', createData.success ? 'SUCCESS' : 'FAILED');

    // Test the connection to generate more logs
    if (createData.success && createData.applicationId) {
        const testResponse = await fetch('http://localhost:8080/ApplicationSettings/test-application-connection', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${loginData.token}`
            },
            body: JSON.stringify({
                applicationId: createData.applicationId
            })
        });

        const testData = await testResponse.json();
        console.log('Test connection response:', testData.success ? 'SUCCESS' : 'FAILED');
    }

    // Check applications logs again
    const logsResponse = await fetch('http://localhost:8080/ApplicationLogSettings?page=1&pageSize=20', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        }
    });

    const logsData = await logsResponse.json();
    console.log('Application logs count:', logsData.data?.data?.length || 0);
    
    if (logsData.data?.data?.length > 0) {
        console.log('Latest log:', logsData.data.data[0]);
    }
};

createAppLogs().catch(console.error);