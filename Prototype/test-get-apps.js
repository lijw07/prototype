// Test get applications functionality
const testGetApps = async () => {
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

    // Get applications
    const appsResponse = await fetch('http://localhost:8080/ApplicationSettings/get-applications', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        }
    });

    const appsData = await appsResponse.json();
    console.log('Applications response:', JSON.stringify(appsData, null, 2));
    console.log('Available applications count:', appsData.data?.data?.length || 0);
};

testGetApps().catch(console.error);