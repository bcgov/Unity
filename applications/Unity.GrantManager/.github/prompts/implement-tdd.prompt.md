---
agent: tdd
description: Implement a feature using test-driven development based on an implementation plan.
---

Please implement the feature described in the plan file: #{{planFile}}

Follow strict test-driven development (TDD) methodology:

1. **Red-Green-Refactor Cycle**: For each task, write the test first (failing), implement minimal code to make it pass, then refactor while keeping tests green.

2. **Implementation Order**: Follow the sequence outlined in the plan:
   - Domain Layer (entities, domain services) - test first
   - Database migrations
   - Application Layer (app services, DTOs) - test first
   - API Layer (controllers)
   - Integration tests
   - Web Layer (UI)

3. **ABP Framework Conventions**: Strictly follow patterns documented in CONTRIBUTING.md:
   - Inherit from proper base classes (`ApplicationService`, `DomainService`, `FullAuditedAggregateRoot`)
   - All public methods must be `virtual`
   - Application services return DTOs only, never entities
   - Use `Manager` suffix for domain services
   - Implement `IMultiTenant` for tenant-scoped entities
   - Apply `[Authorize]` attributes for permissions

4. **Testing Standards**: Use xUnit + Shouldly:
   - `Should_[Expected]_[Scenario]` naming
   - Arrange-Act-Assert pattern
   - Run tests after each implementation step
   - Ensure no regressions in full test suite

5. **Progress Updates**: Provide clear status on which tasks are complete and what's next.

6. **Quality Gates**: Don't move to the next task until:
   - All tests for current task pass
   - Code follows ABP conventions
   - No test regressions

Work through the plan systematically, one task at a time, ensuring quality through testing at every step.
