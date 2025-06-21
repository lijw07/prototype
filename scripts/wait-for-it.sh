#!/bin/bash
# wait-for-it.sh - Wait for SQL Server to be ready and then initialize the database

set -e

host="$1"
port="$2"
password="$3"
shift 3
cmd="$@"

echo "Waiting for SQL Server at $host:$port to be ready..."

# Wait for SQL Server to be responsive
until /opt/mssql-tools/bin/sqlcmd -S "$host,$port" -U sa -P "$password" -Q "SELECT 1" > /dev/null 2>&1; do
  echo "SQL Server is unavailable - sleeping"
  sleep 2
done

echo "SQL Server is up - executing database initialization"

# Run the initialization script
/opt/mssql-tools/bin/sqlcmd -S "$host,$port" -U sa -P "$password" -i /scripts/init-db.sql

echo "Database initialization completed"

# Execute the original command if provided
if [ ${#cmd} -gt 0 ]; then
  exec $cmd
fi