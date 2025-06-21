// Test enum issue when creating application
const testEnumIssue = async () => {
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
    
    console.log('\nTesting application creation with enums...');
    const createPayload = {
        applicationName: `Enum Test ${timestamp}`,
        applicationDescription: 'Testing enum serialization',
        dataSourceType: 'MicrosoftSqlServer',
        connectionSource: {
            host: 'localhost',
            port: '1433',
            databaseName: `EnumTestDb${timestamp}`,
            authenticationType: 'UserPassword',
            username: 'sa',
            password: 'YourStrong!Passw0rd',
            url: `Server=localhost,1433;Database=EnumTestDb${timestamp};User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;`
        }
    };

    console.log('Request payload:', JSON.stringify(createPayload, null, 2));

    const createResponse = await fetch('http://localhost:8080/ApplicationSettings/new-application-connection', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${loginData.token}`
        },
        body: JSON.stringify(createPayload)
    });

    console.log('\nResponse status:', createResponse.status);
    const createData = await createResponse.json();
    console.log('Response:', JSON.stringify(createData, null, 2));

    if (!createData.success) {
        console.log('\nCreate failed. Error details:');
        if (createData.errors) {
            console.log('Validation errors:', createData.errors);
        }
        if (createData.message) {
            console.log('Error message:', createData.message);
        }
    } else {
        console.log('\nApplication created successfully!');
    }
};

testEnumIssue().catch(console.error);