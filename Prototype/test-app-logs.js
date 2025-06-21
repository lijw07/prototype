// Test application logs functionality
const testAppLogs = async () => {
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

    // Get application logs
    const logsResponse = await fetch('http://localhost:8080/ApplicationLogSettings?page=1&pageSize=20', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        }
    });

    console.log('Application logs response status:', logsResponse.status);
    const logsData = await logsResponse.json();
    console.log('Application logs response:', JSON.stringify(logsData, null, 2));
};

testAppLogs().catch(console.error);