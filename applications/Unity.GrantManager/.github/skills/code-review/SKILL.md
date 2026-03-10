---
name: code-review
description: "Review code changes for ABP Framework compliance and project standards"
---

# Code Review

Review code changes in the Unity Grant Manager for compliance with ABP Framework conventions, DDD principles, security requirements, and project standards.

Ask for the following if not provided:
- The files or pull request to review
- Specific areas of concern (architecture, security, performance, etc.)

## Requirements

- Check ABP Framework compliance: base classes, virtual methods, DTOs, naming conventions
- Verify layer boundary integrity (Domain → Application → Web dependency direction)
- Confirm multi-tenancy patterns: `IMultiTenant`, correct DbContext, no manual TenantId filtering
- Validate authorization: `[Authorize]` attributes with permission names
- Check nullable reference type handling
- Verify test coverage for new/changed code
- Review JavaScript for IIFE wrapping, ABP proxy usage, localization
- Flag security concerns: secrets, injection risks, missing validation
- Assess performance: N+1 queries, missing indexes, unnecessary loading

## Review Output

Provide findings organized by severity:
1. **Critical**: Security vulnerabilities, data leaks, architectural violations
2. **Important**: Missing tests, ABP convention violations, performance issues
3. **Suggestion**: Code style improvements, refactoring opportunities

## References

- [code-review.instructions.md](../../instructions/code-review.instructions.md) for review standards
- [security.instructions.md](../../instructions/security.instructions.md) for security checklist
- [CONTRIBUTING.md](../../../CONTRIBUTING.md) for project conventions
