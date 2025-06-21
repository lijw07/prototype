// Create application logs by performing actions with unique names
const testAppLogsCreation = async () => {
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

    // Create a new application with unique name
    const timestamp = Date.now();
    const createResponse = await fetch('http://localhost:8080/ApplicationSettings/new-application-connection', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        },
        body: JSON.stringify({
            applicationName: `Log Test App ${timestamp}`,
            applicationDescription: 'Application to test logging functionality',
            dataSourceType: 'MicrosoftSqlServer',
            connectionSource: {
                host: 'localhost',
                port: '1433',
                databaseName: `TestLogDb${timestamp}`,
                authenticationType: 'UserPassword',
                username: 'sa',
                password: 'YourStrong!Passw0rd',
                url: `Server=localhost,1433;Database=TestLogDb${timestamp};User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;`
            }
        })
    });

    const createData = await createResponse.json();
    console.log('Create application response:', createData.success ? 'SUCCESS' : 'FAILED', createData.message || '');

    // Test the connection to generate more logs
    if (createData.success && createData.applicationId) {
        console.log('Testing connection for application:', createData.applicationId);
        
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
        console.log('Test connection response:', testData.success ? 'SUCCESS' : 'FAILED', testData.message || '');
    }

    // Wait a moment for logs to be written
    await new Promise(resolve => setTimeout(resolve, 1000));

    // Check applications logs again
    const logsResponse = await fetch('http://localhost:8080/ApplicationLogSettings?page=1&pageSize=20', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        }
    });

    const logsData = await logsResponse.json();
    console.log('Application logs response status:', logsResponse.status);
    console.log('Application logs count:', logsData.data?.data?.length || 0);
    
    if (logsData.data?.data?.length > 0) {
        console.log('Sample logs:');
        logsData.data.data.slice(0, 3).forEach((log, i) => {
            console.log(`${i + 1}. ${log.actionType}: ${log.metadata}`);
        });
    }
};

testAppLogsCreation().catch(console.error);