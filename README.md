# Unity Grant Management Enterprise Solution

[![Lifecycle:Stable](https://img.shields.io/badge/Lifecycle-Stable-97ca00)](https://github.com/bcgov/repomountie/blob/master/doc/lifecycle-badges.m)
The project is in a reliable state and major changes are unlikely to happen.

Enterprise Grant Management Solution is a Foundational Tool for Common Connected Cross Government Services that provides grant application intake, assessment, approvals, payments, reporting and notifications used across multiple ministries.  

## Directory Structure

    .github/                    - GitHub Actions and workflows
    applications/               - Application root containing all major components
    ├── Unity.AutoUI/           - Automated end-to-end UI testing (Cypress)
    ├── Unity.GrantManager/     - Grant management and adjudication solution
    ├── Unity.GrantManager.Angular/ - Angular front end for the strangler-fig UI migration
    └── Unity.Tools/            - Supporting tools and services
        ├── Unity.CHEFS/        - CHEFS regression test suite runner
        ├── Unity.Metabase/     - Reserved for Metabase integration
        ├── Unity.NginxData/    - Nginx HTTP server and reference files
        ├── Unity.RabbitMQ/     - RabbitMQ message broker configuration
        └── Unity.RedisSentinel/- Redis Sentinel high-availability setup
    database/                   - Database configuration scripts
    documentation/              - Solution documentation
    COMPLIANCE.yaml             - BCGov PIA/STRA compliance status
    CONTRIBUTING.md             - How to contribute
    LICENSE                     - License
    SECURITY.md                 - Security Policy and Reporting

## Technology

A cloud‑native platform built on common infrastructure, using modern open‑source technologies to deliver secure, scalable, and reusable government services.

**Platform & Hosting (BC Gov Standard)**
- Deployed on BC Gov OpenShift (Kubernetes) with high availability, self‑healing, and auto‑scaling
- Managed GitOps via ArgoCD, with environment promotion `dev` → `test` → `main`/uat → prod
- Access controlled via IDIR on OpenShift (gov‑standard internal access model)
- Zero‑downtime releases using rolling updates to maintain capacity and availability

**Application Stack (Core Components)**
- Core application: .NET + ABP Framework (MIT‑licensed, open source)
- Applicant portal: Angular front‑end with a .NET API backend
- Reporting / AI module: Angular front‑end with a Python Flask API backend

**Data Layer & Resilience (Enterprise Operations)**
- PostgreSQL High Availability (CrunchyDB operator) with automated failover
- Point‑in‑time recovery using Write‑Ahead Logging (WAL) and streaming replication (pgBackRest)
- Resilient backups retained to persistent storage and S3 object storage
- Redis Sentinel for caching high availability
- RabbitMQ for asynchronous messaging

## Documentation

- [Application Readme](applications/README.md)
- [Tenant GitOps Repository](https://github.com/bcgov-c/tenant-gitops-ce395f)
- [Analytics GitOps Repository](https://github.com/bcgov-c/tenant-gitops-b5805a)
