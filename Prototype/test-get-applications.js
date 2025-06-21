// Test get applications response to see enum values
const testGetApplications = async () => {
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
    if (!loginData.success || !loginData.token) {
        console.error('Login failed');
        return;
    }

    console.log('Getting applications...');
    const appsResponse = await fetch('http://localhost:8080/ApplicationSettings/get-applications', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        }
    });

    const appsData = await appsResponse.json();
    console.log('Response status:', appsResponse.status);
    console.log('Applications count:', appsData.data?.data?.length || 0);

    if (appsData.data?.data?.length > 0) {
        console.log('\nFirst application details:');
        const app = appsData.data.data[0];
        console.log('Application ID:', app.applicationId);
        console.log('Application Name:', app.applicationName);
        console.log('Application Data Source Type:', app.applicationDataSourceType);
        console.log('Connection:', JSON.stringify(app.connection, null, 2));
        
        // Check the specific enum values
        console.log('\nEnum values:');
        console.log('Data Source Type:', app.applicationDataSourceType, '(type:', typeof app.applicationDataSourceType, ')');
        console.log('Authentication Type:', app.connection.authenticationType, '(type:', typeof app.connection.authenticationType, ')');
    } else {
        console.log('No applications found');
    }
};

testGetApplications().catch(console.error);