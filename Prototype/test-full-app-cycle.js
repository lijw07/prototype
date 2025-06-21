// Test full application lifecycle to generate comprehensive logs
const testFullAppCycle = async () => {
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

    // 1. Create application
    console.log('\n1. Creating application...');
    const createResponse = await fetch('http://localhost:8080/ApplicationSettings/new-application-connection', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...authHeader },
        body: JSON.stringify({
            applicationName: `Lifecycle Test ${timestamp}`,
            applicationDescription: 'Testing full application lifecycle',
            dataSourceType: 'MicrosoftSqlServer',
            connectionSource: {
                host: 'localhost',
                port: '1433',
                databaseName: `LifecycleDb${timestamp}`,
                authenticationType: 'UserPassword',
                username: 'sa',
                password: 'YourStrong!Passw0rd',
                url: `Server=localhost,1433;Database=LifecycleDb${timestamp};User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;`
            }
        })
    });
    const createData = await createResponse.json();
    console.log('Create:', createData.success ? 'SUCCESS' : 'FAILED');
    
    if (!createData.success || !createData.applicationId) {
        console.error('Failed to create application');
        return;
    }

    const appId = createData.applicationId;

    // 2. Test connection
    console.log('\n2. Testing connection...');
    const testResponse = await fetch('http://localhost:8080/ApplicationSettings/test-application-connection', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...authHeader },
        body: JSON.stringify({ applicationId: appId })
    });
    const testData = await testResponse.json();
    console.log('Test:', testData.success ? 'SUCCESS' : 'FAILED');

    // 3. Update application
    console.log('\n3. Updating application...');
    const updateResponse = await fetch(`http://localhost:8080/ApplicationSettings/update-application/${appId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', ...authHeader },
        body: JSON.stringify({
            applicationName: `Updated Lifecycle Test ${timestamp}`,
            applicationDescription: 'Updated description for lifecycle testing',
            dataSourceType: 'MicrosoftSqlServer',
            connectionSource: {
                host: 'localhost',
                port: '1433',
                databaseName: `UpdatedLifecycleDb${timestamp}`,
                authenticationType: 'UserPassword',
                username: 'sa',
                password: 'YourStrong!Passw0rd',
                url: `Server=localhost,1433;Database=UpdatedLifecycleDb${timestamp};User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;`
            }
        })
    });
    const updateData = await updateResponse.json();
    console.log('Update:', updateData.success ? 'SUCCESS' : 'FAILED');

    // 4. Test connection again after update
    console.log('\n4. Testing connection after update...');
    const testResponse2 = await fetch('http://localhost:8080/ApplicationSettings/test-application-connection', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...authHeader },
        body: JSON.stringify({ applicationId: appId })
    });
    const testData2 = await testResponse2.json();
    console.log('Test 2:', testData2.success ? 'SUCCESS' : 'FAILED');

    // 5. Delete application
    console.log('\n5. Deleting application...');
    const deleteResponse = await fetch(`http://localhost:8080/ApplicationSettings/delete-application/${appId}`, {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json', ...authHeader }
    });
    const deleteData = await deleteResponse.json();
    console.log('Delete:', deleteData.success ? 'SUCCESS' : 'FAILED');

    // 6. Check application logs
    console.log('\n6. Checking application logs...');
    await new Promise(resolve => setTimeout(resolve, 1000)); // Wait for logs to be written

    const logsResponse = await fetch('http://localhost:8080/ApplicationLogSettings?page=1&pageSize=50', {
        method: 'GET',
        headers: { 'Content-Type': 'application/json', ...authHeader }
    });

    const logsData = await logsResponse.json();
    console.log(`\nApplication logs count: ${logsData.data?.data?.length || 0}`);
    
    if (logsData.data?.data?.length > 0) {
        console.log('\nRecent application logs:');
        logsData.data.data.slice(0, 10).forEach((log, i) => {
            console.log(`${i + 1}. [${log.actionType}] ${log.applicationName}: ${log.metadata}`);
        });
    }
};

testFullAppCycle().catch(console.error);