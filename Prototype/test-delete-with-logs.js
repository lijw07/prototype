// Test delete operation and its effect on logs
const testDeleteWithLogs = async () => {
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

    const authHeader = { 'Authorization': `Bearer ${loginData.token}` };

    // Function to check logs
    const checkLogs = async (step) => {
        await new Promise(resolve => setTimeout(resolve, 500));
        const logsResponse = await fetch('http://localhost:8080/ApplicationLogSettings?page=1&pageSize=20', {
            method: 'GET',
            headers: { 'Content-Type': 'application/json', ...authHeader }
        });
        const logsData = await logsResponse.json();
        console.log(`${step}: ${logsData.data?.data?.length || 0} logs`);
        if (logsData.data?.data?.length > 0) {
            console.log('Latest log:', logsData.data.data[0].actionType, '-', logsData.data.data[0].metadata);
        }
        return logsData.data?.data || [];
    };

    // Get current applications
    const appsResponse = await fetch('http://localhost:8080/ApplicationSettings/get-applications', {
        method: 'GET',
        headers: { 'Content-Type': 'application/json', ...authHeader }
    });
    const appsData = await appsResponse.json();
    
    if (!appsData.success || !appsData.data?.data?.length) {
        console.log('No applications found to delete');
        return;
    }

    const appToDelete = appsData.data.data[0];
    console.log(`\nDeleting application: ${appToDelete.applicationName} (${appToDelete.applicationId})`);

    // Check logs before delete
    await checkLogs("Before delete");

    // Delete the application
    const deleteResponse = await fetch(`http://localhost:8080/ApplicationSettings/delete-application/${appToDelete.applicationId}`, {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json', ...authHeader }
    });

    const deleteData = await deleteResponse.json();
    console.log('Delete result:', deleteData.success ? 'SUCCESS' : 'FAILED');
    
    if (!deleteData.success) {
        console.log('Delete error:', deleteData.message);
    }

    // Check logs after delete
    await checkLogs("After delete");

    // Show final logs
    const finalLogs = await checkLogs("Final check");
    if (finalLogs.length > 0) {
        console.log('\nFinal application logs:');
        finalLogs.slice(0, 8).forEach((log, i) => {
            console.log(`${i + 1}. [${log.actionType}] ${log.applicationName}: ${log.metadata}`);
        });
    }
};

testDeleteWithLogs().catch(console.error);