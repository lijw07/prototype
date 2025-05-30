# ProtoType
This is an application meant to be build for the sole purpose of having a centralized unit for managing multiple applicaitons. 

## Instructions on how to use the current state of this project.
This application currently only supports a database with the following specificaitons.


MYSQL:
```
CREATE TABLE Employees(
    employeeId text,
    firstName text,
    lastName text,
    email text,
    role text
);
```
SQL SERVER:
```
CREATE TABLE Employees(
    employeeId NVARCHAR(255),
    firstName NVARCHAR(255),
    lastName NVARCHAR(255),
    email NVARCHAR(255),
    role NVARCHAR(255)
);
```
*All other Driver classes like oracle and PostgreSQL have not been tested yet but the means of establishing a connection should be the same.*

## Steps for running local servers in docker for MySQL

### Create volume
```docker volume create mysql_appThree```

### Spin up container
```docker run -d --name AppThree -e MYSQL_ROOT_PASSWORD='Password0!' \ -e MYSQL_DATABASE=AppThree -p 1434:3306 -v mysql_appThree:/var/lib/mysql mysql:latest```

## Steps for running local servers in docker for SQL Server

### Create volume
```docker volume create mysql_appThree```

### Spin up container
```sudo docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password0\!" --name appTwo -p 1435:1433 -v mysql_appThree:/var/opt/mssql -d --restart=always --hostname AppTwo --platform linux/amd64 mcr.microsoft.com/mssql/server:2022-latest```

### MISC
You can check to see if the contains had spun up correct
```docker ps```
```docker logs AppThree | grep 'A MySQL'```

### Managing Server Data
I just use Datagrip but others can also be used


### Mock Data
I just used this website https://mockaroo.com/ and imported the data into my server. 

*Make sure that the fields follow the same format as the table*


# THERE WILL BE A GOOD CHANCE THAT THIS PROJECT WILL BE RESTRUCTURED AND USE .NET
