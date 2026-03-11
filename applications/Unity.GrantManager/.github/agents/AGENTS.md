# ABP Workflow Agents

This folder contains reusable Copilot agent definitions tailored to Unity Grant Manager ABP workflows.

## Agent Catalog

- `abp-feature-planner.agent.md` - Breaks a feature request into ABP-aligned implementation steps.
- `abp-ddd-modeler.agent.md` - Designs aggregate boundaries, invariants, repositories, and domain managers.
- `abp-application-service-designer.agent.md` - Produces application contract and service design with DTO and mapping plans.
- `abp-efcore-migration-planner.agent.md` - Plans host vs tenant EF Core changes and migration steps.
- `abp-permissions-localization-auditor.agent.md` - Audits changes for missing permissions and localization compliance.
- `abp-test-strategy.agent.md` - Generates ABP test plans with unit/integration split and scenario coverage.
- `abp-test-triage.agent.md` - Diagnoses failing tests and proposes minimal-risk fix sequences.
- `abp-pr-readiness.agent.md` - Runs a final ABP policy and quality gate before PR creation.

## Usage

Pick the agent that matches your workflow stage and provide:

1. Feature or bug context.
2. Target module(s) and files.
3. Constraints (tenant scope, security, deadline, non-functional requirements).

Each agent enforces ABP layering, AutoMapper usage, localization, and test conventions from repository instructions and skills.