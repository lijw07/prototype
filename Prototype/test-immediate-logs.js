// Test immediate application logs after each action
const testImmediateLogs = async () => {
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
    const authHeader = { 'Authorization': `Bearer ${loginData.token}` };

    // Function to check logs
    const checkLogs = async (step) => {
        await new Promise(resolve => setTimeout(resolve, 500)); // Brief pause
        const logsResponse = await fetch('http://localhost:8080/ApplicationLogSettings?page=1&pageSize=20', {
            method: 'GET',
            headers: { 'Content-Type': 'application/json', ...authHeader }
        });
        const logsData = await logsResponse.json();
        console.log(`${step}: ${logsData.data?.data?.length || 0} logs`);
        return logsData.data?.data || [];
    };

    // Initial state
    await checkLogs("Initial");

    // 1. Create application
    console.log('\n1. Creating application...');
    const createResponse = await fetch('http://localhost:8080/ApplicationSettings/new-application-connection', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...authHeader },
        body: JSON.stringify({
            applicationName: `Immediate Test ${timestamp}`,
            applicationDescription: 'Testing immediate logging',
            dataSourceType: 'MicrosoftSqlServer',
            connectionSource: {
                host: 'localhost',
                port: '1433',
                databaseName: `ImmediateDb${timestamp}`,
                authenticationType: 'UserPassword',
                username: 'sa',
                password: 'YourStrong!Passw0rd',
                url: `Server=localhost,1433;Database=ImmediateDb${timestamp};User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;`
            }
        })
    });
    const createData = await createResponse.json();
    console.log('Create:', createData.success ? 'SUCCESS' : 'FAILED');
    
    if (!createData.success) return;
    
    const appId = createData.applicationId;
    const logs1 = await checkLogs("After create");

    // 2. Test connection
    console.log('\n2. Testing connection...');
    const testResponse = await fetch('http://localhost:8080/ApplicationSettings/test-application-connection', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...authHeader },
        body: JSON.stringify({ applicationId: appId })
    });
    const testData = await testResponse.json();
    console.log('Test:', testData.success ? 'SUCCESS' : 'FAILED');
    
    const logs2 = await checkLogs("After test connection");

    // 3. Update application
    console.log('\n3. Updating application...');
    const updateResponse = await fetch(`http://localhost:8080/ApplicationSettings/update-application/${appId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', ...authHeader },
        body: JSON.stringify({
            applicationName: `Updated Immediate Test ${timestamp}`,
            applicationDescription: 'Updated immediate testing',
            dataSourceType: 'MicrosoftSqlServer',
            connectionSource: {
                host: 'localhost',
                port: '1433',
                databaseName: `UpdatedImmediateDb${timestamp}`,
                authenticationType: 'UserPassword',
                username: 'sa',
                password: 'YourStrong!Passw0rd',
                url: `Server=localhost,1433;Database=UpdatedImmediateDb${timestamp};User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;`
            }
        })
    });
    const updateData = await updateResponse.json();
    console.log('Update:', updateData.success ? 'SUCCESS' : 'FAILED');
    
    const logs3 = await checkLogs("After update");

    // Show latest logs
    if (logs3.length > 0) {
        console.log('\nCurrent logs:');
        logs3.slice(0, 10).forEach((log, i) => {
            console.log(`${i + 1}. [${log.actionType}] ${log.applicationName}: ${log.metadata}`);
        });
    }
};

testImmediateLogs().catch(console.error);