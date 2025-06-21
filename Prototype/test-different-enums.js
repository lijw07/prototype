// Test different enum values
const testDifferentEnums = async () => {
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
            name: 'Test 1: MicrosoftSqlServer + UserPassword',
            dataSourceType: 'MicrosoftSqlServer',
            authenticationType: 'UserPassword'
        },
        {
            name: 'Test 2: MySql + UserPassword',
            dataSourceType: 'MySql',
            authenticationType: 'UserPassword'
        },
        {
            name: 'Test 3: MicrosoftSqlServer + AzureAdPassword',
            dataSourceType: 'MicrosoftSqlServer',
            authenticationType: 'AzureAdPassword'
        },
        {
            name: 'Test 4: Invalid DataSourceType',
            dataSourceType: 'InvalidType',
            authenticationType: 'UserPassword'
        },
        {
            name: 'Test 5: Invalid AuthenticationType',
            dataSourceType: 'MicrosoftSqlServer',
            authenticationType: 'InvalidAuth'
        }
    ];

    for (const testCase of testCases) {
        console.log(`\n${testCase.name}`);
        
        const timestamp = Date.now();
        const createPayload = {
            applicationName: `${testCase.name} ${timestamp}`,
            applicationDescription: 'Testing different enums',
            dataSourceType: testCase.dataSourceType,
            connectionSource: {
                host: 'localhost',
                port: '1433',
                databaseName: `TestDb${timestamp}`,
                authenticationType: testCase.authenticationType,
                username: 'sa',
                password: 'YourStrong!Passw0rd',
                url: `Server=localhost,1433;Database=TestDb${timestamp};User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;`
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
        
        if (!createData.success) {
            console.log('Error:', createData.message);
            if (createData.errors) {
                console.log('Validation errors:', JSON.stringify(createData.errors, null, 2));
            }
        }
        
        // Small delay between requests
        await new Promise(resolve => setTimeout(resolve, 100));
    }
};

testDifferentEnums().catch(console.error);