---
applyTo: "**/*.cs,**/*.cshtml,**/*.js"
description: "Security best practices for Unity Grant Manager"
---

# Security Standards

## Authorization

- Apply `[Authorize(PermissionName)]` attributes on all application service methods
- Define permissions in `*Permissions` static class in Domain.Shared project
- Use `abp.auth.isGranted()` in JavaScript for UI permission checks
- Never rely solely on UI-level permission hiding — always enforce server-side

## Multi-Tenancy Security

- Never manually filter by `TenantId` — ABP handles tenant isolation automatically
- Ensure tenant-scoped entities implement `IMultiTenant`
- Test cross-tenant data isolation explicitly
- Use `GrantTenantDbContext` for tenant data, `GrantManagerDbContext` for host data
- Be cautious with `[IgnoreMultiTenancy]` — understand the security implications

## Input Validation

- Validate all inputs at the application service boundary using data annotations or FluentValidation
- Use ABP's `Check.*` methods for domain-level validation (e.g., `Check.NotNullOrWhiteSpace`)
- Sanitize user inputs before storage — prevent XSS and injection attacks
- Use parameterized queries — never concatenate user input into SQL

## Secrets Management

- Never commit secrets, connection strings, or API keys to source code
- Use environment variables or secure configuration providers
- Reference `.env.example` for required environment variables
- Sensitive configuration is stored in OpenShift secrects and Hashicorp Vault when deployed

## Authentication

- Authentication is handled via Keycloak (OpenID Connect)
- Do not implement custom authentication — use ABP's identity infrastructure
- Ensure all API endpoints require authentication unless explicitly public

## Data Protection

- Use Redis-backed data protection for key storage in distributed deployments
- Encrypt sensitive data at rest when required by compliance
- Follow government security standards (BC Government policies)
- Audit logging is enabled via ABP — ensure sensitive operations are captured