# RabbitMQ Configuration

This directory contains a Docker Compose configuration for setting up RabbitMQ for local development.

## Overview

The setup provides:

- **RabbitMQ Server**: Message broker for Unity applications
- **Management Interface**: Web-based management and monitoring
- **Persistent Storage**: Data persistence across container restarts

## Getting Started

### Basic Usage

Start RabbitMQ:

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
| `RABBITMQ_DEFAULT_USER` | `admin` | Default RabbitMQ username |
| `RABBITMQ_DEFAULT_PASS` | `admin` | Default RabbitMQ password |

#### Custom Credentials

You can set custom RabbitMQ credentials:

```bash
# PowerShell
$env:RABBITMQ_DEFAULT_USER="myuser"; $env:RABBITMQ_DEFAULT_PASS="mypassword"; docker-compose up

# Bash/CMD
RABBITMQ_DEFAULT_USER=myuser RABBITMQ_DEFAULT_PASS=mypassword docker-compose up
```

Alternatively, create a `.env` file in the same directory:

```config
RABBITMQ_DEFAULT_USER=myuser
RABBITMQ_DEFAULT_PASS=mypassword
```

### Accessing RabbitMQ

#### Management Interface

- **URL**: http://localhost:15672
- **Username**: `admin` (or your custom user)
- **Password**: `admin` (or your custom password)

#### AMQP Connection

- **Host**: localhost
- **Port**: 5672
- **Username**: `admin` (or your custom user)
- **Password**: `admin` (or your custom password)

### Client Application Configuration

For Unity applications, configure your `appsettings.json` as follows:

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "admin",
    "Password": "admin",
    "VirtualHost": "/",
    "ExchangeName": "unity.exchange",
    "QueueName": "unity.queue"
  }
}
```

#### Configuration Examples

For local development:

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "admin",
    "Password": "admin"
  }
}
```

For Docker network communication:

```json
{
  "RabbitMQ": {
    "Host": "rabbitmq",
    "Port": 5672,
    "Username": "admin",
    "Password": "admin"
  }
}
```

For Kubernetes deployment:

```json
{
  "RabbitMQ": {
    "Host": "unity-rabbitmq.namespace.svc.cluster.local",
    "Port": 5672,
    "Username": "admin",
    "Password": "your-secure-password"
  }
}
```

## Verifying the Setup

Check RabbitMQ status:

```bash
# Check if RabbitMQ is running
docker ps | grep rabbitmq

# Check logs
docker-compose logs rabbitmq
```

Test connection using management API:

```bash
curl -u admin:admin http://localhost:15672/api/overview
```

## Stopping and Cleanup

Stop RabbitMQ:

```bash
docker-compose down
```

Remove volumes (this will delete all data):

```bash
docker-compose down -v
```

## Notes & Limitations

- This setup is designed for local development and testing
- For production deployments, use proper secrets management
- The management interface is exposed on all interfaces (0.0.0.0)
- Data persists in Docker volumes between restarts

## Troubleshooting

### Common Issues

1. **Port Already in Use**: If ports 5672 or 15672 are already in use, modify the port mappings in `docker-compose.yml`

2. **Permission Issues**: Ensure Docker has proper permissions to create volumes

3. **Connection Refused**: Check that RabbitMQ has fully started by monitoring the logs:
   ```bash
   docker-compose logs -f rabbitmq
   ```

### RabbitMQ Management Commands

Useful management commands via the web interface or CLI:

```bash
# List queues
docker exec rabbitmq rabbitmqctl list_queues

# List exchanges  
docker exec rabbitmq rabbitmqctl list_exchanges

# List users
docker exec rabbitmq rabbitmqctl list_users

# Add user
docker exec rabbitmq rabbitmqctl add_user newuser newpassword

# Set permissions
docker exec rabbitmq rabbitmqctl set_permissions -p / newuser ".*" ".*" ".*"
```

## Integration with Unity Applications

This RabbitMQ setup is designed to work seamlessly with Unity applications that require message queuing capabilities. The configuration matches the OpenShift deployment specifications for consistency across development and production environments.

### Message Patterns

Common RabbitMQ patterns used in Unity applications:

- **Work Queues**: Distributing tasks among workers
- **Publish/Subscribe**: Broadcasting messages to multiple consumers
- **Routing**: Selective message routing based on criteria
- **Topics**: Complex routing patterns with wildcards