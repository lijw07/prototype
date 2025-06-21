// Debug application logs creation
const debugAppLogs = async () => {
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

    const timestamp = Date.now();
    
    // Create a simple application to test logging
    console.log('\nCreating application...');
    const createResponse = await fetch('http://localhost:8080/ApplicationSettings/new-application-connection', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        },
        body: JSON.stringify({
            applicationName: `Debug App ${timestamp}`,
            applicationDescription: 'Debug application for logging test',
            dataSourceType: 'MicrosoftSqlServer',
            connectionSource: {
                host: 'localhost',
                port: '1433',
                databaseName: `DebugDb${timestamp}`,
                authenticationType: 'UserPassword',
                username: 'sa',
                password: 'YourStrong!Passw0rd',
                url: `Server=localhost,1433;Database=DebugDb${timestamp};User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;`
            }
        })
    });

    const createData = await createResponse.json();
    console.log('Create response:', createData);

    // Wait a moment then check logs
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    const logsResponse = await fetch('http://localhost:8080/ApplicationLogSettings?page=1&pageSize=50', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        }
    });

    const logsData = await logsResponse.json();
    console.log('\nApplication logs count:', logsData.data?.data?.length || 0);
    
    if (logsData.data?.data?.length > 0) {
        console.log('Latest logs:');
        logsData.data.data.slice(0, 5).forEach((log, i) => {
            console.log(`${i + 1}. [${log.actionType}] ${log.applicationName}: ${log.metadata} (${new Date(log.createdAt).toLocaleTimeString()})`);
        });
    } else {
        console.log('No application logs found');
    }
};

debugAppLogs().catch(console.error);