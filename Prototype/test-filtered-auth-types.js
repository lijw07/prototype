// Test the filtered authentication types for each data source
const testFilteredAuthTypes = async () => {
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

    console.log('Testing filtered authentication types...\n');

    // Test Microsoft SQL Server authentication types
    const sqlServerAuthTypes = [
        'UserPassword', 'Kerberos', 'AzureAdPassword', 'AzureAdInteractive', 
        'AzureAdIntegrated', 'AzureAdDefault', 'AzureAdMsi', 'NoAuth'
    ];

    console.log('🔵 Microsoft SQL Server Authentication Types:');
    for (const authType of sqlServerAuthTypes) {
        const timestamp = Date.now();
        const result = await testApplication(loginData.token, 'MicrosoftSqlServer', authType, timestamp);
        console.log(`  ${authType}: ${result ? '✅' : '❌'}`);
        await new Promise(resolve => setTimeout(resolve, 50));
    }

    // Test MySQL authentication types
    const mysqlAuthTypes = ['UserPassword', 'NoAuth'];

    console.log('\n🟢 MySQL Authentication Types:');
    for (const authType of mysqlAuthTypes) {
        const timestamp = Date.now();
        const result = await testApplication(loginData.token, 'MySql', authType, timestamp);
        console.log(`  ${authType}: ${result ? '✅' : '❌'}`);
        await new Promise(resolve => setTimeout(resolve, 50));
    }

    // Test MongoDB authentication types
    const mongoAuthTypes = [
        'UserPassword', 'ScramSha1', 'ScramSha256', 'AwsIam', 
        'X509', 'GssapiKerberos', 'PlainLdap', 'NoAuth'
    ];

    console.log('\n🍃 MongoDB Authentication Types:');
    for (const authType of mongoAuthTypes) {
        const timestamp = Date.now();
        const result = await testApplication(loginData.token, 'MongoDb', authType, timestamp);
        console.log(`  ${authType}: ${result ? '✅' : '❌'}`);
        await new Promise(resolve => setTimeout(resolve, 50));
    }

    console.log('\n🎉 All authentication type filtering tests completed!');
};

async function testApplication(token, dataSourceType, authenticationType, timestamp) {
    const needsCredentials = [
        'UserPassword', 'AzureAdPassword', 'ScramSha1', 'ScramSha256', 'AwsIam', 'PlainLdap'
    ].includes(authenticationType);

    const createPayload = {
        applicationName: `${dataSourceType}-${authenticationType}-${timestamp}`,
        applicationDescription: `Testing ${dataSourceType} with ${authenticationType}`,
        dataSourceType: dataSourceType,
        connectionSource: {
            host: 'localhost',
            port: dataSourceType === 'MongoDb' ? '27017' : '1433',
            databaseName: `TestDb${timestamp}`,
            authenticationType: authenticationType,
            username: needsCredentials ? 'testuser' : '',
            password: needsCredentials ? 'testpass' : '',
            url: generateConnectionUrl(dataSourceType, authenticationType, `TestDb${timestamp}`, needsCredentials)
        }
    };

    try {
        const response = await fetch('http://localhost:8080/ApplicationSettings/new-application-connection', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(createPayload)
        });

        const data = await response.json();
        return data.success;
    } catch (error) {
        return false;
    }
}

function generateConnectionUrl(dataSourceType, authenticationType, dbName, needsCredentials) {
    switch (dataSourceType) {
        case 'MicrosoftSqlServer':
            return `Server=localhost,1433;Database=${dbName};${needsCredentials ? 'User Id=testuser;Password=testpass;' : 'Integrated Security=true;'}TrustServerCertificate=true;`;
        case 'MySql':
            return `Server=localhost;Port=3306;Database=${dbName};${needsCredentials ? 'Uid=testuser;Pwd=testpass;' : ''}`;
        case 'MongoDb':
            return `mongodb://localhost:27017/${dbName}`;
        default:
            return '';
    }
}

testFilteredAuthTypes().catch(console.error);