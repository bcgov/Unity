# Metabase Configuration

This directory contains a Docker Compose configuration for setting up Metabase for local development and analytics.

## Overview

The setup provides:

- **Metabase Analytics Platform**: Business intelligence and data visualization
- **PostgreSQL Database**: Metabase application database for storing dashboards, users, etc.
- **Persistent Storage**: Data persistence across container restarts

## Getting Started

### Basic Usage

Start Metabase:

```bash
docker-compose up
```

To run in detached mode:

```bash
docker-compose up -d
```

### Configuration Options

This setup supports environment variables for customization:

| Variable | Default | Description |
|----------|---------|-------------|
| `MB_DB_DBNAME` | `metabase` | Metabase application database name |
| `MB_DB_USER` | `metabase` | Metabase database username |
| `MB_DB_PASS` | `metabase123` | Metabase database password |
| `POSTGRES_USER` | `metabase` | PostgreSQL superuser username |
| `POSTGRES_PASSWORD` | `metabase123` | PostgreSQL superuser password |

#### Custom Database Configuration

You can set custom database credentials:

```bash
# PowerShell
$env:MB_DB_PASS="mysecurepassword"; $env:POSTGRES_PASSWORD="mysecurepassword"; docker-compose up

# Bash/CMD
MB_DB_PASS=mysecurepassword POSTGRES_PASSWORD=mysecurepassword docker-compose up
```

Alternatively, create a `.env` file in the same directory:

```config
MB_DB_DBNAME=metabase
MB_DB_USER=metabase
MB_DB_PASS=mysecurepassword
POSTGRES_USER=metabase
POSTGRES_PASSWORD=mysecurepassword
```

### Accessing Metabase

#### Web Interface

- **URL**: http://localhost:3000
- **First-time setup**: You'll be prompted to create an admin account
- **Default admin**: Create during initial setup

#### Database Connection for Unity Data

When setting up data sources in Metabase to connect to your Unity databases:

**For Unity PostgreSQL (from Unity.GrantManager docker-compose):**
- **Host**: `host.docker.internal` (Windows/Mac) or your machine's IP
- **Port**: `5432` (or your Unity DB port)
- **Database**: Your Unity database name
- **Username/Password**: Your Unity database credentials

## Initial Setup

### First-Time Configuration

1. Start Metabase: `docker-compose up`
2. Wait for services to fully start (check logs)
3. Navigate to http://localhost:3000
4. Complete the initial setup wizard:
   - Create admin account
   - Skip adding data source (or add Unity database)
   - Finish setup

### Connecting to Unity Data

To analyze Unity application data:

1. **Add Database** in Metabase
2. **Select PostgreSQL** 
3. **Connection details**:
   ```
   Host: host.docker.internal
   Port: 5432 (or your Unity DB port)
   Database name: [Your Unity DB name]
   Username: [Your Unity DB user]
   Password: [Your Unity DB password]
   ```

### Example Unity Database Connection

If using the Unity.GrantManager docker-compose setup:

```json
{
  "host": "host.docker.internal",
  "port": 5432,
  "database": "postgres",
  "username": "postgres", 
  "password": "admin"
}
```

## Verifying the Setup

Check Metabase status:

```bash
# Check if services are running
docker ps | grep metabase

# Check Metabase logs
docker-compose logs metabase

# Check PostgreSQL logs  
docker-compose logs metabase-db
```

Test web interface:

```bash
curl http://localhost:3000/api/health
```

## Stopping and Cleanup

Stop Metabase:

```bash
docker-compose down
```

Remove volumes (this will delete all dashboards and configuration):

```bash
docker-compose down -v
```

## Notes & Limitations

- This setup is designed for local development and testing
- For production deployments, use proper secrets management
- The first startup takes longer as Metabase initializes its database
- Dashboards and questions are stored in the PostgreSQL database

## Troubleshooting

### Common Issues

1. **Slow startup**: Metabase can take 2-3 minutes to fully initialize on first run
2. **Port conflicts**: If port 3000 is in use, modify the port mapping in `docker-compose.yml`
3. **Database connection issues**: Ensure your Unity database is accessible from Docker

### Useful Commands

```bash
# Check Metabase initialization status
docker-compose logs -f metabase | grep -i "metabase initialization"

# Reset Metabase (removes all dashboards/config)
docker-compose down -v && docker-compose up

# Access PostgreSQL directly
docker exec -it metabase-db psql -U metabase -d metabase
```

## Integration with Unity Applications

This Metabase setup is designed to work with Unity applications for:

- **Analytics Dashboards**: Visualize Unity application data
- **Business Intelligence**: Generate reports from Unity databases
- **Data Monitoring**: Track application metrics and KPIs
- **User Insights**: Analyze user behavior and application usage

### Common Unity Analytics Use Cases

- Grant application metrics and trends
- User engagement and portal usage
- Application performance monitoring  
- Business process analytics
- Compliance and audit reporting

## Production Considerations

When deploying to production environments:

- Use external PostgreSQL database instead of containerized one
- Implement proper backup strategies for dashboards and configuration
- Set up proper authentication integration (LDAP, SAML, etc.)
- Configure SSL/TLS for secure connections
- Use environment-specific connection strings
