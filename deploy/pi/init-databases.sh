#!/bin/bash
set -e

# Create all databases needed by the microservices on a single PostgreSQL instance.
# This script runs automatically on first container start.

for db in devicesdb personsdb monitoringdb rulesdb roomsdb dashboarddb; do
  echo "Creating database: $db"
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-SQL
    SELECT 'CREATE DATABASE $db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$db')\gexec
SQL
done
