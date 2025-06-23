#!/bin/bash

# Test API Connection Examples for Prototype Application
# Usage: ./test-connections.sh

API_BASE="http://localhost:8080"
AUTH_TOKEN="YOUR_JWT_TOKEN_HERE"  # Get this from login

echo "🧪 Testing External API and Cloud Database Connections"
echo "=================================================="

# 1. Test GraphQL Connection (using a public GraphQL API)
echo "📊 Testing GraphQL Connection..."
curl -X POST "$API_BASE/settings/applications/test-application-connection" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -d '{
    "applicationName": "Test GraphQL API",
    "dataSourceType": "GraphQL",
    "connectionSource": {
      "url": "https://countries.trevorblades.com/graphql",
      "apiEndpoint": "https://countries.trevorblades.com/graphql",
      "authenticationType": "NoAuth",
      "httpMethod": "POST",
      "requestBody": "{\"query\": \"{ countries { code name } }\"}"
    }
  }' | jq '.'

echo -e "\n"

# 2. Test SOAP Connection (using a public SOAP service)
echo "🧼 Testing SOAP API Connection..."
curl -X POST "$API_BASE/settings/applications/test-application-connection" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -d '{
    "applicationName": "Test SOAP Service",
    "dataSourceType": "SoapApi",
    "connectionSource": {
      "url": "http://webservices.oorsprong.org/websamples.countryinfo/CountryInfoService.wso",
      "apiEndpoint": "http://webservices.oorsprong.org/websamples.countryinfo/CountryInfoService.wso",
      "authenticationType": "NoAuth",
      "httpMethod": "POST",
      "headers": "{\"SOAPAction\": \"http://www.oorsprong.org/websamples.countryinfo/CountryName\"}"
    }
  }' | jq '.'

echo -e "\n"

# 3. Test REST API Connection (using JSONPlaceholder)
echo "🌐 Testing REST API Connection..."
curl -X POST "$API_BASE/settings/applications/test-application-connection" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -d '{
    "applicationName": "Test REST API",
    "dataSourceType": "RestApi",
    "connectionSource": {
      "url": "https://jsonplaceholder.typicode.com/posts/1",
      "apiEndpoint": "https://jsonplaceholder.typicode.com/posts/1",
      "authenticationType": "NoAuth",
      "httpMethod": "GET"
    }
  }' | jq '.'

echo -e "\n"

# 4. Test Azure SQL Database Connection (requires your Azure SQL details)
echo "☁️ Testing Azure SQL Database Connection..."
echo "⚠️  Replace with your actual Azure SQL credentials"
# curl -X POST "$API_BASE/settings/applications/test-application-connection" \
#   -H "Content-Type: application/json" \
#   -H "Authorization: Bearer $AUTH_TOKEN" \
#   -d '{
#     "applicationName": "Test Azure SQL",
#     "dataSourceType": "MicrosoftSqlServer",
#     "connectionSource": {
#       "host": "your-server.database.windows.net",
#       "port": "1433",
#       "databaseName": "your-database",
#       "authenticationType": "UserPassword",
#       "username": "your-username",
#       "password": "your-password",
#       "url": "azure-sql-connection"
#     }
#   }' | jq '.'

echo -e "\n"

# 5. Test Local SQL Server Connection
echo "🗄️ Testing Local SQL Server Connection..."
curl -X POST "$API_BASE/settings/applications/test-application-connection" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -d '{
    "applicationName": "Test Local SQL Server",
    "dataSourceType": "MicrosoftSqlServer",
    "connectionSource": {
      "host": "localhost",
      "port": "1433",
      "databaseName": "PrototypeDb",
      "authenticationType": "UserPassword",
      "username": "sa",
      "password": "YourStrong!Passw0rd",
      "url": "local-sql-connection"
    }
  }' | jq '.'

echo -e "\n"

echo "✅ Connection testing complete!"
echo "Check the logs in UserActivityLogs table to see results"