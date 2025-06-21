// Test the complete enhanced form with data source specific authentication
const testCompleteEnhancedForm = async () => {
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

    console.log('🧪 Testing Complete Enhanced Form with Data Source Specific Authentication\n');

    const testCases = [
        // Microsoft SQL Server tests
        {
            name: 'SQL Server with Azure AD Integrated',
            dataSourceType: 'MicrosoftSqlServer',
            authenticationType: 'AzureAdIntegrated',
            expectedPort: '1433',
            needsCredentials: false
        },
        {
            name: 'SQL Server with Kerberos',
            dataSourceType: 'MicrosoftSqlServer',
            authenticationType: 'Kerberos',
            expectedPort: '1433',
            needsCredentials: false
        },
        
        // MySQL tests
        {
            name: 'MySQL with Username/Password',
            dataSourceType: 'MySql',
            authenticationType: 'UserPassword',
            expectedPort: '3306',
            needsCredentials: true
        },
        {
            name: 'MySQL with No Auth',
            dataSourceType: 'MySql',
            authenticationType: 'NoAuth',
            expectedPort: '3306',
            needsCredentials: false
        },
        
        // MongoDB tests
        {
            name: 'MongoDB with SCRAM-SHA-256',
            dataSourceType: 'MongoDb',
            authenticationType: 'ScramSha256',
            expectedPort: '27017',
            needsCredentials: true
        },
        {
            name: 'MongoDB with X.509',
            dataSourceType: 'MongoDb',
            authenticationType: 'X509',
            expectedPort: '27017',
            needsCredentials: false
        },
        {
            name: 'MongoDB with GSSAPI Kerberos',
            dataSourceType: 'MongoDb',
            authenticationType: 'GssapiKerberos',
            expectedPort: '27017',
            needsCredentials: false
        }
    ];

    for (const testCase of testCases) {
        console.log(`🔧 ${testCase.name}`);
        
        const timestamp = Date.now();
        const createPayload = {
            applicationName: `${testCase.name} ${timestamp}`,
            applicationDescription: `Testing ${testCase.name} configuration`,
            dataSourceType: testCase.dataSourceType,
            connectionSource: {
                host: 'localhost',
                port: testCase.expectedPort,
                databaseName: `TestDb${timestamp}`,
                authenticationType: testCase.authenticationType,
                username: testCase.needsCredentials ? 'testuser' : '',
                password: testCase.needsCredentials ? 'testpass123' : '',
                url: generateConnectionUrl(testCase, `TestDb${timestamp}`)
            }
        };

        console.log(`   📋 Data Source: ${testCase.dataSourceType}`);
        console.log(`   🔐 Auth Type: ${testCase.authenticationType}`);
        console.log(`   🔌 Port: ${testCase.expectedPort}`);
        console.log(`   🔑 Needs Credentials: ${testCase.needsCredentials ? 'Yes' : 'No'}`);

        const createResponse = await fetch('http://localhost:8080/ApplicationSettings/new-application-connection', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${loginData.token}`
            },
            body: JSON.stringify(createPayload)
        });

        const createData = await createResponse.json();
        
        if (createData.success) {
            console.log(`   ✅ SUCCESS - Application ID: ${createData.applicationId}`);
        } else {
            console.log(`   ❌ FAILED - ${createData.message}`);
            if (createData.errors) {
                console.log(`   🐛 Errors: ${Object.keys(createData.errors).join(', ')}`);
            }
        }
        
        console.log(''); // Empty line for readability
        await new Promise(resolve => setTimeout(resolve, 100));
    }

    console.log('🎉 Complete enhanced form testing finished!');
};

function generateConnectionUrl(testCase, dbName) {
    switch (testCase.dataSourceType) {
        case 'MicrosoftSqlServer':
            if (testCase.needsCredentials) {
                return `Server=localhost,${testCase.expectedPort};Database=${dbName};User Id=testuser;Password=testpass123;TrustServerCertificate=true;`;
            } else {
                return `Server=localhost,${testCase.expectedPort};Database=${dbName};Integrated Security=true;TrustServerCertificate=true;`;
            }
            
        case 'MySql':
            if (testCase.needsCredentials) {
                return `Server=localhost;Port=${testCase.expectedPort};Database=${dbName};Uid=testuser;Pwd=testpass123;`;
            } else {
                return `Server=localhost;Port=${testCase.expectedPort};Database=${dbName};`;
            }
            
        case 'MongoDb':
            if (testCase.needsCredentials) {
                return `mongodb://testuser:testpass123@localhost:${testCase.expectedPort}/${dbName}`;
            } else {
                return `mongodb://localhost:${testCase.expectedPort}/${dbName}`;
            }
            
        default:
            return '';
    }
}

testCompleteEnhancedForm().catch(console.error);