# Unity Portal
[![Lifecycle:Maturing](https://img.shields.io/badge/Lifecycle-Maturing-007EC6)](https://github.com/bcgov/repomountie/blob/master/doc/lifecycle-badges.md)
The codebase is being roughed out, but finer details are likely to change.

## Directory Structure

    .github                    - GitHub Actions
    applications/              - Application Root
    ├── Unity.ApplicantPortal/ - Applicant Information solution
    ├── Unity.AutoUI/          - Automated User Interface testing
    ├── Unity.GrantManager/    - Grant manager and adjudication solution
    ├── Unity.Orchestrator/    - Workflow orchestrator solution
    ├── Unity.RabbitMQ/        - Messaging and streaming broker configuration
    ├── Unity.Tools/           - DevOps tools
    database/                  - Database configuration files
    documentation/             - Solution documentation and assets
    openshift/                 - OpenShift deployment files
    COMPLIANCE.yaml            - BCGov PIA/STRA compliance status
    CONTRIBUTING.md            - How to contribute
    LICENSE                    - License
    SECURITY.md                - Security Policy and Reporting

## Documentation

- [Application Readme](applications/README.md)
