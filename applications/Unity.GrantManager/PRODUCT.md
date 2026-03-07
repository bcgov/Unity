# Unity Grant Manager - Product Vision

## Overview

Unity Grant Manager is a comprehensive grant management platform designed for the Government of British Columbia to streamline and automate the entire grants lifecycle. The application enables government staff to manage grant programs, review applications, conduct assessments, and process payments, while providing applicants with a user-friendly portal to submit and track their grant applications.

## Product Goals

1. **Streamline Grant Administration**: Reduce administrative overhead by automating grant program management, application intake, assessment workflows, and payment processing.

2. **Enhance Transparency**: Provide applicants and stakeholders with real-time visibility into application status, assessment progress, and payment tracking.

3. **Ensure Compliance**: Maintain audit trails, enforce business rules, and integrate with government systems (CHES email service, CAS payment system) to ensure regulatory compliance.

4. **Enable Flexibility**: Support dynamic form creation for diverse grant programs with varying requirements through the Unity.Flex module.

5. **Support Multi-Tenancy**: Enable multiple government organizations or programs to operate independently within a shared platform instance.

## Key Features

### Grant Program Management
- Configure and manage multiple grant programs with unique requirements, eligibility criteria, and assessment workflows
- Define program intake periods, budget allocations, and reporting requirements
- Track program performance metrics and funding distribution

### Applicant Portal
- Self-service application submission with dynamic forms tailored to each grant program
- Document upload and management for supporting materials
- Real-time application status tracking and notifications
- Application editing and resubmission capabilities during intake periods

### Application Assessment & Scoring
- Configurable assessment workflows with multiple review stages
- Collaborative review process with scoring rubrics and criteria
- Assignment of applications to assessors and review teams
- Consolidated scoring and recommendation reporting
- Comment threads and internal discussions on applications

### Payment Processing
- Integration with CAS (Common Accounting System) for government payment processing
- Payment milestone tracking and approval workflows
- Payment history and reconciliation reporting
- Support for installment-based and milestone-based payment schedules

### Notifications & Communications
- Automated email notifications via CHES (Common Hosted Email Service)
- Configurable notification templates for application status changes
- Event-driven notifications for assessments, payments, and program updates
- Communication history tracking

### Reporting & Analytics
- Customizable reports on application volumes, assessment outcomes, and payment distributions
- Program performance dashboards and metrics
- Export capabilities for external analysis and compliance reporting
- Integration with Unity.Reporting module for advanced reporting features

### User & Role Management
- Role-based access control for staff, assessors, and applicants
- Integration with Keycloak for authentication and single sign-on
- Organization and team-based permission structures
- Audit logging for all user actions

## Target Users

### Government Program Staff
- Grant program administrators who configure programs and manage intake periods
- Grant officers who oversee application processing and assessment coordination
- Finance staff who manage payment processing and budget tracking

### Assessors & Reviewers
- Internal and external subject matter experts who evaluate applications
- Review panel members who participate in scoring and recommendation processes

### Applicants
- Individuals, organizations, or businesses applying for government grants
- Grant recipients tracking payment schedules and reporting requirements

## Integration Points

### Unity Platform Modules
- **Unity.Flex**: Dynamic form definitions for customizable application forms
- **Unity.Notifications**: Email notifications via CHES integration
- **Unity.Payments**: Payment processing through CAS integration
- **Unity.Reporting**: Advanced reporting and data visualization
- **Unity.Identity.Web**: User authentication and authorization
- **Unity.TenantManagement**: Multi-tenant configuration and isolation
- **Unity.Theme.UX2**: Consistent user interface and experience

### External Systems
- **CHES (Common Hosted Email Service)**: Government email notification service
- **CAS (Common Accounting System)**: Government payment processing system
- **Keycloak**: Enterprise identity and access management
- **COMS (Common Object Management Service)**: Document storage and blob management via S3-compatible APIs

## Success Criteria

1. **Efficiency**: Reduce grant processing time by 40% through automation and streamlined workflows
2. **User Satisfaction**: Achieve 85%+ satisfaction ratings from both applicants and staff users
3. **Transparency**: Provide real-time status updates for 100% of applications
4. **Compliance**: Maintain complete audit trails and pass all security/privacy audits
5. **Scalability**: Support multiple concurrent grant programs with thousands of applications
