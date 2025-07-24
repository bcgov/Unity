# Crunchy Postgres chart

A chart to provision a [Crunchy Postgres](https://www.crunchydata.com/) cluster.

## Configuration
Apply base configuration from values.yaml and make the necessary overrides in custom-values-example.yaml
```Bash
helm upgrade --install new-hippo-ha . -f values.yaml -f custom-values-example.yaml
```
### Crunchy Options

| Parameter          | Description            | Default            |
| ------------------ | ---------------------- | ------------------ |
| `fullnameOverride` | Override release name  | `crunchy-postgres` |
| `crunchyImage`     | Crunchy Postgres image |                    |
| `postgresVersion`  | Postgres version       | `15`               |

---

### Instances

| Parameter                                   | Description                    | Default                  |
| ------------------------------------------- | ------------------------------ | ------------------------ |
| `instances.name`                            | Instance name                  | `ha` (high availability) |
| `instances.replicas`                        | Number of replicas             | `2`                      |
| `instances.dataVolumeClaimSpec.storage`     | Amount of storage for each PVC | `256Mi`                  |
| `instances.requests.cpu`                    | CPU requests                   | `1m`                     |
| `instances.requests.memory`                 | Memory requests                | `256Mi`                  |
| `instances.limits.cpu`                      | CPU limits                     | `100m`                   |
| `instances.limits.memory`                   | Memory limits                  | `512Mi`                  |
| `instances.replicaCertCopy.requests.cpu`    | replicaCertCopy CPU requests   | `1m`                     |
| `instances.replicaCertCopy.requests.memory` | replicaCertCopyMemory requests | `32Mi`                   |
| `instances.replicaCertCopy.limits.cpu`      | replicaCertCopyCPU limits      | `50m`                    |
| `instances.replicaCertCopy.limits.memory`   | replicaCertCopy Memory limits  | `64Mi`                   |

---

### pgBackRest - Reliable PostgreSQL Backup & Restore

[pgBackRest site](https://pgbackrest.org/)
[Crunchy pgBackRest docs](https://access.crunchydata.com/documentation/pgbackrest/latest/)

| Parameter                                            | Description                                                   | Default                |
| ---------------------------------------------------- | ------------------------------------------------------------- | ---------------------- |
| `pgBackRest.image`                                   | Crunchy pgBackRest                                            |                        |
| `pgBackRest.retention`                               | Number of backups/days to keep depending on retentionFullType | `2`                    |
| `pgBackRest.retentionFullType`                       | Either 'count' or 'time'                                      | `count`                |
| `pgBackRest.repos.schedules.full`                    | Full backup schedule                                          | `0 8 * * *`            |
| `pgBackRest.repos.schedules.incremental`             | Incremental backup schedule                                   | `0 0,4,12,16,20 * * *` |
| `pgBackRest.repos.schedules.volume.addessModes`      | Access modes                                                  | `ReadWriteOnce`        |
| `pgBackRest.repos.schedules.volume.storage`          | PVC size                                                  | `128Mi`                 |
| `pgBackRest.repos.schedules.volume.storageClassName` | Storage class name modes                                      | `netapp-file-backup`   |
| `pgBackRest.repoHost.requests.cpu`                   | CPU requests                                                  | `1m`                   |
| `pgBackRest.repoHost.requests.memory`                | Memory requests                                               | `64Mi`                 |
| `pgBackRest.repoHost.limits.cpu`                     | CPU limits                                                    | `50m`                  |
| `pgBackRest.repoHost.limits.memory`                  | Memory limits                                                 | `128Mi`                |
| `pgBackRest.sidecars.requests.cpu`                   | sidecars CPU requests                                         | `1m`                   |
| `pgBackRest.sidecars.requests.memory`                | sidecars Memory requests                                      | `64Mi`                 |
| `pgBackRest.sidecars.limits.cpu`                     | sidecars CPU limits                                           | `50m`                  |
| `pgBackRest.sidecars.limits.memory`                  | sidecars Memory limits                                        | `128Mi`                |
| `pgBackRest.s3.enabled`                  | Enables the s3 repo backups                                        | `false`                 |
| `pgBackRest.s3.createS3Secret`                  | Creates the s3 secret based on key and keySecret                                        | `true`                 |
| `pgBackRest.s3.s3Secret`                  | The secret name to be created or read from                                       | `s3-pgbackrest`                 |
| `pgBackRest.s3.s3Path`                  | The path inside the bucket where the backups will be saved to, set it to `/` to use the root of the bucket.                                        | `/dbbackup`                 |
| `pgBackRest.s3.s3UriStyle`                  | Style of URL to use for S3 communication. [More Info](https://pgbackrest.org/configuration.html#section-repository/option-repo-s3-uri-style)                                       | `path`                 |
| `pgBackRest.s3.bucket`                  | The bucket to use for backups                                        | `bucketName`                 |
| `pgBackRest.s3.endpoint`                  | The endpoint to use, for example s3.ca-central-1.amazonaws.com                                       | `endpointName`                 |
| `pgBackRest.s3.region`                  | The region to use, not necessary if your S3 system does not specify one                                       | `ca-central-1`                 |
| `pgBackRest.s3.key`                  | The key to use to access the bucket. MUST BE KEPT SECRET                                        | `s3KeyValue`                 |
| `pgBackRest.s3.keySecret`                  | The key secret for the key set above. MUST BE KEPT SECRET                                        | `s3SecretValue`                 |
---

### Patroni

[Patroni docs](https://patroni.readthedocs.io/en/latest/)
[Crunchy Patroni docs](https://access.crunchydata.com/documentation/patroni/latest/)

| Parameter                                   | Description                                                         | Default                           |
| ------------------------------------------- | ------------------------------------------------------------------- | --------------------------------- |
| `patroni.postgresql.pg_hba`                 | pg_hba permissions                                                  | `"host all all 0.0.0.0/0 md5"`    |
| `crunchyImage`                              | Crunchy Postgres image                                              | `...crunchy-postgres:ubi8-14.7-0` |
| `patroni.parameters.shared_buffers`         | The number of shared memory buffers used by the server              | `16MB`                            |
| `patroni.parameters.wal_buffers`            | The number of disk-page buffers in shared memory for WAL            | `64KB`                            |
| `patroni.parameters.min_wal_size`           | The minimum size to shrink the WAL to                               | `32MB`                            |
| `patroni.parameters.max_wal_size`           | Sets the WAL size that triggers a checkpoint                        | `64MB`                            |
| `patroni.parameters.max_slot_wal_keep_size` | Sets the maximum WAL size that can be reserved by replication slots | `128MB`                           |

---

### pgBouncer

A lightweight connection pooler for PostgreSQL

[pgBouncer site](https://www.pgbouncer.org/)
[Crunchy Postgres pgBouncer docs](https://access.crunchydata.com/documentation/pgbouncer/latest/)

| Parameter                         | Description             | Default |
| --------------------------------- | ----------------------- | ------- |
| `proxy.pgBouncer.image`           | Crunchy pgBouncer image |         |
| `proxy.pgBouncer.replicas`        | Number of replicas      | `2`     |
| `proxy.pgBouncer.requests.cpu`    | CPU requests            | `1m`    |
| `proxy.pgBouncer.requests.memory` | Memory requests         | `64Mi`  |
| `proxy.pgBouncer.limits.cpu`      | CPU limits              | `50m`   |
| `proxy.pgBouncer.limits.memory`   | Memory limits           | `128Mi` |

---

## PG Monitor

[Crunchy Postgres PG Monitor docs](https://access.crunchydata.com/documentation/pgmonitor/latest/)

| Parameter                            | Description                                    | Default |
| ------------------------------------ | ---------------------------------------------- | ------- |
| `pgmonitor.enabled`                  | Enable PG Monitor (currently only PG exporter) | `false` |
| `pgmonitor.exporter.requests.cpu`    | PG Monitor CPU requests                        | `1m`    |
| `pgmonitor.exporter.requests.memory` | PG Monitor Memory requests                     | `64Mi`  |
| `pgmonitor.exporter.limits.cpu`      | PG Monitor CPU limits                          | `50m`   |
| `pgmonitor.exporter.limits.memory`   | PG Monitor Memory limits                       | `128Mi` |

#### Postgres Exporter

A [Prometheus](https://prometheus.io/) exporter for PostgreSQL

[Postgres Exporter](https://github.com/prometheus-community/postgres_exporter)

| Parameter                            | Description               | Default |
| ------------------------------------ | ------------------------- | ------- |
| `pgmonitor.exporter.image`           | Crunchy PG Exporter image |         |
| `pgmonitor.exporter.requests.cpu`    | CPU requests              | `1m`    |
| `pgmonitor.exporter.requests.memory` | Memory requests           | `64Mi`  |
| `pgmonitor.exporter.limits.cpu`      | CPU limits                | `50m`   |
| `pgmonitor.exporterr.limits.memory`  | Memory limits             | `128Mi` |

---

## Data Restore CronJob

This feature allows you to set up a daily CronJob that restores data from a source S3 repository (e.g., from another database instance) into the current PostgreSQL cluster. This is useful for change data capture scenarios where you need to regularly sync data from a source database. The configuration reuses the same structure as `dataSource` and `pgBackRest.s3` for consistency.

### Configuration

| Parameter                                      | Description                                           | Default                |
| ---------------------------------------------- | ----------------------------------------------------- | ---------------------- |
| `dataRestore.enabled`                          | Enable the data restore CronJob                      | `false`                |
| `dataRestore.schedule`                         | Cron schedule for the restore job                    | `"0 2 * * *"`          |
| `dataRestore.image`                            | pgBackRest image to use for restore                  | `crunchy-pgbackrest`   |
| `dataRestore.secretName`                       | K8s secret containing S3 credentials (reuse existing)| `s3-pgbackrest`        |
| `dataRestore.repo.name`                        | Repository name (repo1, repo2, etc.)                 | `repo2`                |
| `dataRestore.repo.path`                        | S3 path prefix                                       | `/habackup`            |
| `dataRestore.repo.s3.bucket`                   | Source S3 bucket name                                | `bucketName`           |
| `dataRestore.repo.s3.endpoint`                 | S3 endpoint URL                                      | Object store endpoint  |
| `dataRestore.repo.s3.region`                   | S3 region                                            | `not-used`             |
| `dataRestore.repo.s3.uriStyle`                 | S3 URI style (path or host)                          | `path`                 |
| `dataRestore.stanza`                           | pgBackRest stanza name                               | `db`                   |
| `dataRestore.target.clusterName`               | Target cluster name (defaults to current cluster)    | `""`                   |
| `dataRestore.target.database`                  | Target database name                                 | `postgres`             |
| `dataRestore.resources.requests.cpu`           | CPU requests for restore job                         | `100m`                 |
| `dataRestore.resources.requests.memory`        | Memory requests for restore job                      | `256Mi`                |
| `dataRestore.resources.limits.cpu`             | CPU limits for restore job                           | `500m`                 |
| `dataRestore.resources.limits.memory`          | Memory limits for restore job                        | `512Mi`                |
| `dataRestore.successfulJobsHistoryLimit`       | Number of successful jobs to keep in history         | `3`                    |
| `dataRestore.failedJobsHistoryLimit`           | Number of failed jobs to keep in history             | `1`                    |
| `dataRestore.restartPolicy`                    | Pod restart policy for failed jobs                   | `OnFailure`            |
| `dataRestore.additionalArgs`                   | Additional pgbackrest arguments                      | `[]`                   |

### Usage Example

The configuration reuses existing S3 secrets and follows the same patterns as `dataSource`:

```yaml
dataRestore:
  enabled: true
  schedule: "0 2 * * *"  # Daily at 2 AM
  # Reuse existing S3 secret from dataSource or pgBackRest.s3
  secretName: "dev-s3-pgbackrest"
  repo:
    name: repo2
    path: "/habackup-source-database"
    s3:
      bucket: "source-database-backups"
      endpoint: "https://sector.objectstore.gov.bc.ca"
      region: "not-used"
      uriStyle: "path"
  stanza: db
  target:
    database: "myapp"
  additionalArgs:
    - "--log-level-console=debug"
    - "--process-max=2"
```

### Important Notes

- The restore uses `--delta` mode, which only restores changed files for efficiency
- Reuses existing S3 secrets from `dataSource` or `pgBackRest.s3` configuration
- The job runs with the specified S3 repository as the source
- Ensure the source S3 repository contains valid pgBackRest backups
- The target cluster must be accessible and have proper credentials
- Monitor CronJob logs for restore status and any errors
- Configuration follows the same patterns as `dataSource` for consistency

---
