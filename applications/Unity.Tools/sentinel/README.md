# Redis Sentinel Configuration

This repository contains a Docker Compose configuration for setting up Redis with Sentinel for high availability.

## Overview

The setup consists of:

- 1 Redis master
- 1 Redis replica (slave)
- 3 Redis sentinels for high availability

## Getting Started

### Basic Usage

Start the Redis cluster with Sentinel:

```bash
docker-compose up
```

To run in detached mode:

```bash
docker-compose up -d
```

### Configuration Options

This setup supports three environment variables to customize your Redis cluster:

| Variable | Default | Description |
|----------|---------|-------------|
| `REDIS_MASTER_HOST` | `redis-master` | Hostname or IP address of the Redis master. Use your LAN IP when accessing from the host machine. |
| `REDIS_SLAVE_HOST` | `redis-slave` | Hostname or IP address of the Redis slave. Use your LAN IP when accessing from the host machine. |
| `REDIS_PASSWORD` | `MySecurePassword` | Password for Redis authentication. |

#### Default Mode

Uses internal Docker networking with service names:

```bash
docker-compose up
```

#### Local Testing Mode

When testing from an application on the host machine to the Docker environment, you need to use your LAN IP:

```bash
# PowerShell
$env:REDIS_MASTER_HOST="192.168.1.x"; $env:REDIS_SLAVE_HOST="192.168.1.x"; docker-compose up

# Bash/CMD
REDIS_MASTER_HOST=192.168.1.x REDIS_SLAVE_HOST=192.168.1.x docker-compose up
```

#### Custom Password

You can set a custom Redis password:

```bash
# PowerShell
$env:REDIS_PASSWORD="MySecurePassword"; docker-compose up

# Bash/CMD
REDIS_PASSWORD=MySecurePassword docker-compose up
```

#### Multiple Configuration Options

You can combine multiple configuration options:

```bash
# PowerShell
$env:REDIS_MASTER_HOST="192.168.1.x"; $env:REDIS_SLAVE_HOST="192.168.1.x"; $env:REDIS_PASSWORD="MySecurePassword"; docker-compose up

# Bash/CMD
REDIS_MASTER_HOST=192.168.1.x REDIS_SLAVE_HOST=192.168.1.x REDIS_PASSWORD=MySecurePassword docker-compose up
```

Alternatively, create a `.env` file in the same directory:

``` config
REDIS_MASTER_HOST=192.168.1.x
REDIS_SLAVE_HOST=192.168.1.x
REDIS_PASSWORD=MySecurePassword
```

### Client Application Configuration

For application using Sentinel, configure your `appsettings.json` as follows:

```json
"Redis": {
  "IsEnabled": true,
  "UseSentinel": true,
  
  /* Used if UseSentinel is true - omit if UseSentinel is false */
  "SentinelMasterName": "mymaster",
  "Configuration": "192.168.1.x:26379,192.168.1.x:26380,192.168.1.x:26381",
  "DatabaseId": 0,
  
  /* Used if UseSentinel is false - omit if UseSentinel is true */
  "Host": "localhost",
  "Port": 6379,
  "InstanceName": "redis",
  
  /* Used if IsEnabled is true and for both UseSentinel true or false */
  "KeyPrefix": "Unity",
  "Password": "YourRedisPassword"
}
```

#### Configuration Examples

For internal Docker network communication:

```json
"Redis": {
  "IsEnabled": true,
  "UseSentinel": true,
  "SentinelMasterName": "mymaster",
  "Configuration": "redis-sentinel1:26379,redis-sentinel2:26380,redis-sentinel3:26381",
  "DatabaseId": 0,
  "KeyPrefix": "Unity",
  "Password": "YourRedisPassword"
}
```

For host machine to Docker communication:

```json
"Redis": {
  "IsEnabled": true,
  "UseSentinel": true,
  "SentinelMasterName": "mymaster",
  "Configuration": "192.168.1.x:26379,192.168.1.x:26380,192.168.1.x:26381",
  "DatabaseId": 0,
  "KeyPrefix": "Unity",
  "Password": "YourRedisPassword"
}
```

For Kubernetes deployment:

```json
"Redis": {
  "IsEnabled": true,
  "UseSentinel": true,
  "SentinelMasterName": "mymaster",
  "Configuration": "env-redis-ha.namespace.svc.cluster.local:26379",
  "DatabaseId": 0,
  "KeyPrefix": "Unity", 
  "Password": "YourRedisPassword"
}
```

## Verifying the Setup

Check Sentinel status:

```bash
redis-cli -p 26379 sentinel masters
```

Expected output:

```bash
1)  1) "name"
    2) "mymaster"
    3) "ip"
    4) "redis-master"
    5) "port"
    6) "6379"
    ...
```

Find the Redis master container IP:

```bash
docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' redis-master
```

## Testing Failover

### Simulated Failover

1. Stop the Redis master:

   ```bash
   docker stop redis-master
   ```

2. Check if the Sentinel promotes the slave to master:

   ```bash
   redis-cli -p 26379 sentinel masters
   ```

3. Restart the Redis master (it will rejoin as a replica):

   ```bash
   docker start redis-master
   ```

4. To restore original roles (if desired):

   ```bash
   # Option 1: Stop all containers, then start them in the correct order
   docker stop redis-master redis-slave redis-sentinel1 redis-sentinel2 redis-sentinel3
   docker start redis-master
   # Wait a few seconds for master to initialize
   Start-Sleep -Seconds 5
   docker start redis-slave redis-sentinel1 redis-sentinel2 redis-sentinel3
   ```

### Manual Failover

You can also trigger a manual failover using Redis Sentinel commands:

1. Check current master status:

   ```bash
   redis-cli -p 26379 sentinel masters
   ```

2. Initiate a manual failover:

   ```bash
   redis-cli -p 26379 -a YourRedisPassword sentinel failover mymaster
   ```

   > Note: If you receive a `NOGOODSLAVE No suitable replica to promote` error, this is likely due to conflicts with local Redis instances as mentioned in the Troubleshooting section. Ensure no other Redis instances are running on your machine.

3. Verify the failover occurred:

   ```bash
   redis-cli -p 26379 sentinel masters
   ```

### Additional Sentinel Commands

Useful commands for monitoring and managing your Redis Sentinel setup:

```bash
# List all monitored masters
redis-cli -p 26379 sentinel masters

# Get master by name
redis-cli -p 26379 sentinel master mymaster

# Get slaves for a master
redis-cli -p 26379 sentinel slaves mymaster

# Get sentinels for a master
redis-cli -p 26379 sentinel sentinels mymaster

# Check if a specific instance is a master
redis-cli -p 6379 -a YourRedisPassword info replication

# Reset a sentinel (useful during testing)
redis-cli -p 26379 sentinel reset mymaster
```

## Notes & Limitations

- This setup is not suitable for production because it does not persist Sentinel configuration.
- For production, you should use persistent sentinel.conf files and ensure they are properly mounted.
- Environment variables can be used to override the default Redis master hostname.
- The sentinel quorum is set to 2, meaning at least 2 sentinels must agree to trigger a failover.

## Troubleshooting

### Multiple Redis Instances

If you encounter issues with Redis Sentinel reporting `num-slaves=0` or getting `NOGOODSLAVE No suitable replica to promote` errors during failover attempts:

- Check if you have Redis running both in Docker and locally on your host machine.
- Having multiple Redis instances can cause conflicts and misleading behavior.
- Verify all instances with: `redis-cli ping` (returns PONG if Redis is running)
- If needed, stop local Redis instances before testing your Docker setup.
- On Windows: Check Services for Redis, or use `Stop-Service redis` if installed as a service.
- On Linux/Mac: `sudo service redis-server stop` or `brew services stop redis`

## Kubernetes Deployment

When deploying to Kubernetes:

- The "Configuration" parameter should point to the Kubernetes service endpoint for your Redis Sentinel service.
- Service endpoints in Kubernetes typically follow the format `service-name.namespace.svc.cluster.local`.
- Using a single service endpoint is sufficient as Kubernetes will handle load balancing to the appropriate sentinel pods.
- If using Helm charts for Redis HA, the service endpoint is usually exposed as part of the deployment.
