# 🔐 Centralized Application Management System (CAMS)

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18.2-61DAFB)](https://reactjs.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED)](https://docker.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927)](https://www.microsoft.com/sql-server)

A modern, scalable web application for centralized management of user permissions and database connections across multiple client applications. Built with ASP.NET Core 8.0, React 18.2, and comprehensive database connectivity support.

## 🚀 Features

### 🔌 **Universal Database Connectivity**
- **Direct Connections**: SQL Server (Local/Azure), MySQL, PostgreSQL, Oracle, MariaDB, SQLite
- **NoSQL Support**: MongoDB, Redis, Cassandra, ElasticSearch
- **Cloud Databases**: Azure SQL, AWS RDS, Google Cloud SQL
- **Connection Testing**: Built-in connection validation for all database types

### 🌐 **External API Integration**
- **REST APIs**: Full HTTP method support with comprehensive authentication
- **GraphQL**: Query/mutation support with introspection testing
- **SOAP Services**: WSDL discovery and SOAP envelope handling
- **Authentication**: API Key, Bearer Token, Basic Auth, OAuth 2.0, Azure AD, JWT

### 📁 **File-Based Data Sources**
- **Formats**: CSV, JSON, XML, Excel, Parquet, YAML, Text files
- **Processing**: Automated parsing and validation
- **Storage**: Local file system and cloud storage support

### 🛡️ **Security & Authentication**
- **JWT-based Authentication**: Secure token management
- **Role-based Access Control**: Granular permission system
- **Password Encryption**: BCrypt hashing for all stored passwords
- **Azure AD Integration**: Enterprise authentication support
- **Audit Logging**: Comprehensive activity tracking

### 📊 **Management & Monitoring**
- **User Management**: Create, update, delete users with role assignments
- **Application Management**: Centralized application connection management
- **Activity Logging**: Real-time monitoring of all system activities
- **Connection Testing**: Validate all connections before deployment

## 🏗️ Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   React SPA     │    │  ASP.NET Core    │    │   SQL Server    │
│   (Frontend)    │◄──►│     API          │◄──►│   (Primary DB)  │
│                 │    │   (Backend)      │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌──────────────────┐
                       │ External Sources │
                       ├──────────────────┤
                       │ • Client DBs     │
                       │ • REST APIs      │
                       │ • GraphQL APIs   │
                       │ • SOAP Services  │
                       │ • File Sources   │
                       │ • Cloud Services │
                       └──────────────────┘
```

### 🗂️ **Project Structure**
```
Prototype/
├── 📁 Controllers/          # API endpoints and business logic
│   ├── Settings/           # Application and user management
│   ├── Login/              # Authentication endpoints
│   └── Dashboard/          # Dashboard and analytics
├── 📁 Database/            # Connection strategies
│   ├── MicrosoftSQLServer/ # SQL Server connections
│   ├── MySql/              # MySQL connections
│   ├── Api/                # REST/GraphQL/SOAP strategies
│   ├── Cloud/              # Cloud database strategies
│   └── Interface/          # Connection interfaces
├── 📁 Services/            # Business logic services
│   ├── Factory/            # Factory pattern services
│   └── Interfaces/         # Service contracts
├── 📁 Models/              # Entity Framework models
├── 📁 DTOs/                # Data transfer objects
├── 📁 Enum/                # Application enumerations
├── 📁 ClientApp/           # React frontend application
│   ├── src/
│   │   ├── components/     # React components
│   │   ├── services/       # API service calls
│   │   └── utils/          # Utility functions
│   └── public/
└── 📁 Migrations/          # Entity Framework migrations
```

## 🛠️ Technology Stack

### **Backend**
- **Framework**: ASP.NET Core 8.0 Web API
- **Database**: Entity Framework Core with SQL Server
- **Authentication**: JWT Bearer tokens with BCrypt encryption
- **Connectivity**: ADO.NET with ODBC/Native drivers
- **Logging**: Built-in ASP.NET Core logging with Serilog
- **Caching**: Redis for distributed caching

### **Frontend**
- **Framework**: React 18.2 with TypeScript
- **UI Library**: Bootstrap 5.3 with Reactstrap
- **State Management**: React Context API
- **HTTP Client**: Axios for API communication
- **Routing**: React Router DOM with protected routes

### **Infrastructure**
- **Containerization**: Docker & Docker Compose
- **Database**: SQL Server 2022 (containerized)
- **Caching**: Redis 7 Alpine
- **Reverse Proxy**: Nginx (production)
- **Database Admin**: CloudBeaver web interface

## 🚀 Quick Start

### **Prerequisites**
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Recommended)
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (for local development)
- [Node.js 18+](https://nodejs.org/) (for local development)

### **🐳 Docker Setup (Recommended)**

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Prototype
   ```

2. **Start all services**
   ```bash
   docker compose up -d
   ```

3. **Access the application**
   - **Frontend**: http://localhost:3000
   - **Backend API**: http://localhost:8080
   - **Database Admin**: http://localhost:8978 (CloudBeaver)
   - **API Documentation**: http://localhost:8080/swagger

4. **Stop services**
   ```bash
   docker compose down
   ```

### **🔧 Local Development Setup**

1. **Backend Setup**
   ```bash
   # Restore dependencies
   dotnet restore
   
   # Run migrations
   dotnet ef database update
   
   # Start backend
   dotnet run
   ```

2. **Frontend Setup**
   ```bash
   # Navigate to React app
   cd Prototype/ClientApp
   
   # Install dependencies
   npm install
   
   # Start development server
   npm start
   ```

## 🔌 Connection Testing

### **Available Connection Types**

| Type | Status | Authentication | Use Cases |
|------|--------|---------------|-----------|
| **SQL Server** | ✅ Ready | User/Pass, Windows, Azure AD | Local & Azure SQL databases |
| **MySQL** | ✅ Ready | User/Pass, No Auth | MySQL & MariaDB databases |
| **PostgreSQL** | ✅ Ready | User/Pass, Windows | PostgreSQL databases |
| **MongoDB** | ✅ Ready | User/Pass, No Auth | Document databases |
| **Redis** | ✅ Ready | Password, No Auth | Caching & session storage |
| **REST API** | ✅ Ready | API Key, Bearer, Basic, OAuth | RESTful web services |
| **GraphQL** | ✅ Ready | API Key, Bearer, JWT | GraphQL endpoints |
| **SOAP API** | ✅ Ready | Basic, Bearer, Custom | Legacy SOAP services |
| **Oracle** | ✅ Ready | User/Pass | Oracle databases |
| **SQLite** | ✅ Ready | Password, No Auth | Embedded databases |
| **Cassandra** | ✅ Ready | User/Pass | Wide-column databases |
| **ElasticSearch** | ✅ Ready | Basic, API Key | Search & analytics |

### **🧪 Test Connections**

#### **Via Frontend (Recommended)**
1. Login to http://localhost:3000
2. Navigate to **Applications** → **Add New Application**
3. Select connection type and fill in details
4. Click **"Test Connection"**

#### **Via API**
```bash
# Test GraphQL connection
curl -X POST "http://localhost:8080/settings/applications/test-application-connection" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "applicationName": "Countries API",
    "dataSourceType": "GraphQL",
    "connectionSource": {
      "url": "https://countries.trevorblades.com/graphql",
      "authenticationType": "NoAuth",
      "requestBody": "{\"query\": \"{ countries { code name } }\"}"
    }
  }'
```

#### **Quick Test Examples**
| Service | URL | Type | Auth |
|---------|-----|------|------|
| Countries GraphQL | `https://countries.trevorblades.com/graphql` | GraphQL | None |
| JSONPlaceholder | `https://jsonplaceholder.typicode.com/users` | REST | None |
| Local SQL Server | `localhost:1433` | SQL Server | sa/YourStrong!Passw0rd |

## 📊 Database Schema

### **Core Tables**
- **Users**: User accounts and profiles
- **Applications**: Managed applications
- **ApplicationConnections**: Connection configurations
- **UserApplications**: User-application relationships
- **UserActivityLogs**: User action tracking
- **ApplicationLogs**: Application-specific logs
- **AuditLogs**: System audit trail

### **🗃️ Database Management**

#### **Migrations**
```bash
# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

#### **Database Access**
```bash
# Connect via CloudBeaver
http://localhost:8978

# Connect via sqlcmd
docker exec -it prototype-database /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong!Passw0rd" -C
```

## 🔒 Security Features

### **Authentication & Authorization**
- **JWT Tokens**: Secure, stateless authentication
- **Password Hashing**: BCrypt with salt
- **Role-based Access**: Granular permission control
- **Azure AD Integration**: Enterprise SSO support
- **Rate Limiting**: API protection against abuse

### **Data Protection**
- **Encryption**: All passwords encrypted at rest
- **SSL/TLS**: Encrypted data transmission
- **Connection String Security**: Secure credential storage
- **Audit Logging**: Complete activity tracking

## 🔧 Configuration

### **Environment Variables**
```bash
# Database Configuration
DB_HOST=localhost
DB_PORT=1433
DB_NAME=PrototypeDb
DB_USER=sa
DB_PASSWORD=YourStrong!Passw0rd

# JWT Configuration
JWT_SECRET_KEY=your-secret-key
JWT_ISSUER=PrototypeApp
JWT_AUDIENCE=PrototypeUsers

# SMTP Configuration
SMTP_HOST=smtp.example.com
SMTP_PORT=587
SMTP_USERNAME=user
SMTP_PASSWORD=pass
```

### **Docker Services**
```yaml
services:
  frontend:    # React app (port 3000)
  backend:     # ASP.NET Core API (port 8080)
  db:          # SQL Server (port 1433)
  redis:       # Redis cache (port 6379)
  cloudbeaver: # DB admin (port 8978)
```

## 🚨 Troubleshooting

### **Common Issues**

#### **Docker Issues**
```bash
# Clean restart
docker compose down
docker system prune -f
docker compose up --build

# Check logs
docker logs prototype-backend --tail 50
docker logs prototype-frontend --tail 50
```

#### **Database Connection Issues**
```bash
# Test database connectivity
docker exec prototype-database /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong!Passw0rd" -C -Q "SELECT 1"

# Check connection logs
docker logs prototype-backend 2>&1 | grep -i "connection"
```

#### **Authentication Issues**
- Verify JWT token in browser developer tools
- Check token expiration (default: 60 minutes)
- Ensure correct username/password combination

### **Debug Mode**
```bash
# Enable detailed logging
export ASPNETCORE_ENVIRONMENT=Development
export Logging__LogLevel__Default=Debug

# Run with debug output
docker compose up
```

## 📈 Monitoring & Logging

### **Application Logs**
- **User Activities**: All user actions tracked
- **Connection Tests**: Success/failure logging
- **Application Events**: Create/update/delete operations
- **System Events**: Startup, shutdown, errors

### **Log Queries**
```sql
-- Recent connection attempts
SELECT TOP 10 Description, Timestamp, 
  CASE WHEN Description LIKE '%Success%' THEN 'SUCCESS' ELSE 'FAILED' END AS Result
FROM UserActivityLogs 
WHERE ActionType = 5 -- ConnectionAttempt
ORDER BY Timestamp DESC;

-- Application usage statistics
SELECT COUNT(*) as TotalApplications, 
  AVG(DATEDIFF(day, CreatedAt, GETDATE())) as AvgAgeInDays
FROM Applications;
```

## 🤝 Contributing

### **Development Workflow**
1. **Fork** the repository
2. **Create** a feature branch: `git checkout -b feature/amazing-feature`
3. **Commit** changes: `git commit -m 'Add amazing feature'`
4. **Push** to branch: `git push origin feature/amazing-feature`
5. **Create** a Pull Request

### **Branch Naming Convention**
```
<type>/<short-description>

Types:
- feature/    # New features
- bugfix/     # Bug fixes
- hotfix/     # Urgent fixes
- docs/       # Documentation
- chore/      # Maintenance

Examples:
- feature/graphql-support
- bugfix/connection-timeout
- docs/api-documentation
```

### **Code Standards**
- Follow .NET naming conventions
- Use TypeScript for React components
- Write unit tests for new features
- Document public APIs
- Ensure Docker compatibility

## 📝 API Documentation

### **Authentication Endpoints**
- `POST /login` - User authentication
- `POST /logout` - User logout
- `GET /user/profile` - Get user profile

### **Application Management**
- `POST /settings/applications/new-application-connection` - Create application
- `GET /settings/applications/get-applications` - List applications
- `PUT /settings/applications/update-application/{id}` - Update application
- `DELETE /settings/applications/delete-application/{id}` - Delete application
- `POST /settings/applications/test-application-connection` - Test connection

### **User Management**
- `POST /settings/user/create-user` - Create user
- `GET /settings/user/get-users` - List users
- `PUT /settings/user/update-user/{id}` - Update user
- `DELETE /settings/user/delete-user/{id}` - Delete user

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

## 🔗 Quick Links

- **Live Demo**: [Coming Soon]
- **Documentation**: [Coming Soon]
- **Issues**: [Coming Soon]
- **Discussions**: [Coming Soon]