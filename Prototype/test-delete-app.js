// Test delete application functionality
const testDeleteApp = async () => {
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

    // Get applications to find one to delete
    const appsResponse = await fetch('http://localhost:8080/ApplicationSettings/get-applications', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        }
    });

    const appsData = await appsResponse.json();
    console.log('Available applications:', appsData.data.data.length);
    
    if (appsData.data.data.length === 0) {
        console.log('No applications to test with');
        return;
    }

    const testApp = appsData.data.data[0];
    console.log('Attempting to delete app:', testApp.applicationName, testApp.applicationId);

    // Test delete application
    const deleteResponse = await fetch(`http://localhost:8080/ApplicationSettings/delete-application/${testApp.applicationId}`, {
        method: 'DELETE',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        }
    });

    console.log('Delete application status:', deleteResponse.status);
    const deleteResponseData = await deleteResponse.json();
    console.log('Delete application response:', JSON.stringify(deleteResponseData, null, 2));
};

testDeleteApp().catch(console.error);