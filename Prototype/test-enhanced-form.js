// Test the enhanced form with different enum combinations
const testEnhancedForm = async () => {
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

    const testCases = [
        {
            name: 'MySQL with UserPassword',
            dataSourceType: 'MySql',
            authenticationType: 'UserPassword',
            needsCredentials: true
        },
        {
            name: 'SQL Server with Azure AD Integrated',
            dataSourceType: 'MicrosoftSqlServer',
            authenticationType: 'AzureAdIntegrated',
            needsCredentials: false
        },
        {
            name: 'MongoDB with No Auth',
            dataSourceType: 'MongoDb',
            authenticationType: 'NoAuth',
            needsCredentials: false
        }
    ];

    for (const testCase of testCases) {
        console.log(`\nTesting: ${testCase.name}`);
        
        const timestamp = Date.now();
        const createPayload = {
            applicationName: `${testCase.name} ${timestamp}`,
            applicationDescription: `Testing ${testCase.name}`,
            dataSourceType: testCase.dataSourceType,
            connectionSource: {
                host: 'localhost',
                port: '1433',
                databaseName: `TestDb${timestamp}`,
                authenticationType: testCase.authenticationType,
                username: testCase.needsCredentials ? 'testuser' : '',
                password: testCase.needsCredentials ? 'testpass' : '',
                url: `Server=localhost,1433;Database=TestDb${timestamp};${testCase.needsCredentials ? 'User Id=testuser;Password=testpass;' : 'Integrated Security=true;'}TrustServerCertificate=true;`
            }
        };

        const createResponse = await fetch('http://localhost:8080/ApplicationSettings/new-application-connection', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${loginData.token}`
            },
            body: JSON.stringify(createPayload)
        });

        const createData = await createResponse.json();
        console.log(`Status: ${createResponse.status}, Success: ${createData.success}`);
        
        if (createData.success) {
            console.log(`✅ Created application with ID: ${createData.applicationId}`);
        } else {
            console.log(`❌ Failed: ${createData.message}`);
            if (createData.errors) {
                console.log('Validation errors:', Object.keys(createData.errors));
            }
        }
        
        await new Promise(resolve => setTimeout(resolve, 100));
    }

    console.log('\n🎉 Enhanced form testing complete!');
};

testEnhancedForm().catch(console.error);