---
name: debug-issue
description: "Diagnose and resolve issues in Unity Grant Manager using structured debugging"
---

# Debug Issue

Systematically diagnose and resolve bugs, errors, and unexpected behavior in the Unity Grant Manager application.

Ask for the following if not provided:
- Error message or unexpected behavior description
- Steps to reproduce (if known)
- Affected layer or module (Domain, Application, Web, etc.)

## Requirements

- Follow a structured debugging methodology: reproduce, isolate, diagnose, fix, verify
- Search for similar patterns in existing codebase before proposing fixes
- Consider multi-tenancy implications — is the issue tenant-specific or global?
- Check ABP Framework conventions — many issues stem from convention violations
- Write a regression test before or alongside the fix
- Ensure the fix doesn't break existing tests

## Debugging Checklist

1. **Reproduce**: Confirm the issue and identify exact conditions
2. **Isolate**: Determine the affected layer (Domain, Application, EF Core, Web)
3. **Investigate**: Check logs (Serilog), database state, tenant context
4. **Root Cause**: Identify why the issue occurs — framework misuse, business logic error, data issue
5. **Fix**: Apply minimal, targeted fix following ABP conventions
6. **Test**: Write regression test, run full test suite
7. **Document**: Add comments explaining the fix if the root cause was non-obvious

## Common ABP Issues

- Missing `virtual` keyword on overridden methods
- Manual TenantId filtering instead of ABP automatic filtering
- Entity exposed from application service instead of DTO
- Wrong DbContext used (host vs tenant data)
- Missing `[Authorize]` attribute on new service methods
- Distributed event handler not registered

## References

- [copilot-instructions.md](../../copilot-instructions.md) for common mistakes
- [ARCHITECTURE.md](../../../ARCHITECTURE.md) for module communication patterns
