# Unity Portal

[![Lifecycle:Stable](https://img.shields.io/badge/Lifecycle-Stable-97ca00)](https://github.com/bcgov/repomountie/blob/master/doc/lifecycle-badges.m)
The project is in a reliable state and major changes are unlikely to happen.

## Directory Structure

    .github/                    - GitHub Actions and workflows
    applications/               - Application root containing all major components
    ├── Unity.AutoUI/           - Automated end-to-end UI testing (Cypress)
    ├── Unity.GrantManager/     - Grant management and adjudication solution
    └── Unity.Tools/            - Supporting tools and services
        ├── Unity.Metabase/     - Reserved for Metabase integration
        ├── Unity.NginxData/    - Nginx HTTP server and reference files
        ├── Unity.RabbitMQ/     - RabbitMQ message broker configuration
        └── Unity.RedisSentinel/- Redis Sentinel high-availability setup
    database/                   - Database configuration and scripts
    documentation/              - Solution documentation and assets
    openshift/                  - OpenShift deployment files and configs
    COMPLIANCE.yaml             - BCGov PIA/STRA compliance status
    CONTRIBUTING.md             - How to contribute
    LICENSE                     - License
    SECURITY.md                 - Security Policy and Reporting

## Documentation

- [Application Readme](applications/README.md)
