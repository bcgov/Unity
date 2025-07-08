# Unity RabbitMQ

This directory contains the setup for RabbitMQ message broker in an OpenShift container. It includes configuration for administrator and client users, as well as virtual hosts for development environments.

## Contents
- RabbitMQ configuration files
- User and vhost setup instructions

See the README for setup and usage instructions.

Setup of RabbitMQ message broker in an OpenShift container requires an administrator user (`unity-admin`) and two client users each associated with their own virtual hosts (`/dev` and `/dev2`).

## Prerequisites

- OpenShift cluster access
- RabbitMQ installed on your OpenShift cluster
- RabbitMQ CLI tools (`rabbitmqctl`)

## Setup

### Creating Virtual Hosts

To create the virtual hosts `/dev` and `/dev2`, use the following commands:

```sh
rabbitmqctl add_vhost /dev
rabbitmqctl add_vhost /dev2
```

### Adding Users and Setting Permissions

Create the administrator user `unity-admin`:

```sh
rabbitmqctl add_user unity-admin 'your_admin_password'
rabbitmqctl set_permissions -p / unity-admin ".*" ".*" ".*"
rabbitmqctl set_user_tags unity-admin administrator
```

Create the client user `unity-rabbitmq-user-dev` for the `/dev` vhost:

```sh
rabbitmqctl add_user unity-rabbitmq-user-dev 'your_dev_password'
rabbitmqctl set_permissions -p /dev unity-rabbitmq-user-dev ".*" ".*" ".*"
```

Create the client user `unity-rabbitmq-user-dev2` for the `/dev2` vhost:

```sh
rabbitmqctl add_user unity-rabbitmq-user-dev2 'your_dev2_password'
rabbitmqctl set_permissions -p /dev2 unity-rabbitmq-user-dev2 ".*" ".*" ".*"
```

## Volume Mounts

To persist RabbitMQ data a container volume mount is required with backup to offsite S3 storage.

```yaml
volumeMounts:
  - mountPath: /var/lib/rabbitmq
```
