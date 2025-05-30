# ProtoType

ProtoType is an application designed to serve as a centralized unit for managing multiple applications.

---

## 🚀 Getting Started

### Prerequisites

- Docker
- MySQL or SQL Server (other drivers like Oracle and PostgreSQL are untested but should have similar connection procedures)

---

## 🗄️ Database Specifications

#### MySQL Table

```sql
CREATE TABLE Employees(
    employeeId TEXT,
    firstName TEXT,
    lastName TEXT,
    email TEXT,
    role TEXT
);
```

#### SQL Server Table

```sql
CREATE TABLE Employees(
    employeeId NVARCHAR(255),
    firstName NVARCHAR(255),
    lastName NVARCHAR(255),
    email NVARCHAR(255),
    role NVARCHAR(255)
);
```

> **Note:** Other database drivers (e.g., Oracle, PostgreSQL) have not been tested yet, but the connection process should be similar.

---

## 🐳 Running Local Servers with Docker

### For MySQL

1. **Create a Docker volume**
   ```sh
   docker volume create mysql_appThree
   ```

2. **Start MySQL container**
   ```sh
   docker run -d --name AppThree -e MYSQL_ROOT_PASSWORD='Password0!' -e MYSQL_DATABASE=AppThree -p 1434:3306 -v mysql_appThree:/var/lib/mysql mysql:latest
   ```

### For SQL Server

1. **Create a Docker volume**
   ```sh
   docker volume create mysql_appThree
   ```

2. **Start SQL Server container**
   ```sh
   sudo docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password0!" --name appTwo -p 1435:1433 -v mysql_appThree:/var/opt/mssql -d --restart=always --hostname AppTwo --platform linux/amd64 mcr.microsoft.com/mssql/server:latest
   ```

---

## ⚙️ Miscellaneous

- **Verify containers are running:**
  ```sh
  docker ps
  docker logs AppThree | grep 'A MySQL'
  ```

- **Managing server data:**  
  Tools like DataGrip can be used to manage your database, but any SQL client should work.

- **Mock Data:**  
  You can generate mock data using [Mockaroo](https://mockaroo.com/) and import it into your server.  
  > Make sure that the field names and data types match your table specification.

---

## ⚠️ Notice

> **There is a good chance this project will be restructured and use .NET in the future.**
