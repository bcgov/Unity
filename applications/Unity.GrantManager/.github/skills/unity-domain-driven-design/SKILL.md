---
name: unity-domain-driven-design
description: DDD patterns for Unity - Entities, Aggregate Roots, Repositories, Domain Services, Domain Events. Use when creating or modifying entities, repositories, or domain services in Domain or Domain.Shared projects.
---

# Unity ABP DDD Patterns

> Based on ABP Framework DDD conventions. This project uses ABP 9.1.3 with PostgreSQL 17 and EF Core 9.0.

## Entities

- Define entities in `*.Domain` projects.
- Use **rich domain model**: private/protected setters with methods that enforce invariants.
- Always provide a `protected` parameterless constructor for EF Core.
- Accept `Guid id` in the primary constructor; do NOT generate GUIDs inside constructors. Use `IGuidGenerator` from calling code.
- Make members `virtual` for ORM proxy compatibility.
- Initialize sub-collections in the primary constructor.

```csharp
public class Grant : AuditedAggregateRoot<Guid>
{
    public string Name { get; private set; }
    public GrantStatus Status { get; private set; }
    public ICollection<GrantApplication> Applications { get; private set; }

    protected Grant() { } // For EF Core

    public Grant(Guid id, string name) : base(id)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name));
        Status = GrantStatus.Draft;
        Applications = new List<GrantApplication>();
    }

    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name));
    }
}
```

## Aggregate Roots

- Use a single `Id` property, prefer `Guid` keys.
- Inherit from `AggregateRoot<Guid>` or audited base classes (`AuditedAggregateRoot<Guid>`, `FullAuditedAggregateRoot<Guid>`).
- Reference other aggregate roots **by Id only** — no cross-aggregate navigation properties.
- Keep aggregates small.

## Repositories

- Define repository interfaces in the Domain layer.
- One repository per aggregate root only. Never create repositories for child entities.
- Custom repository interface should inherit `IRepository<TEntity, TKey>`.
- All methods async with `CancellationToken cancellationToken = default`.
- Single-entity methods: `includeDetails = true` by default.
- List methods: `includeDetails = false` by default.

```csharp
public interface IGrantRepository : IRepository<Grant, Guid>
{
    Task<Grant?> FindByNameAsync(string name, bool includeDetails = true, CancellationToken cancellationToken = default);
    Task<List<Grant>> GetListByStatusAsync(GrantStatus status, bool includeDetails = false, CancellationToken cancellationToken = default);
}
```

## Domain Services

- Naming: `*Manager` suffix (e.g., `GrantManager`).
- No interface by default unless multiple implementations are needed.
- Accept/return domain objects, not DTOs.
- Do NOT depend on authenticated user; accept required values from application layer.
- Use `GuidGenerator`, `Clock` from base class properties.

```csharp
public class GrantManager : DomainService
{
    private readonly IGrantRepository _grantRepository;

    public GrantManager(IGrantRepository grantRepository)
    {
        _grantRepository = grantRepository;
    }

    public async Task<Grant> CreateAsync(string name)
    {
        var existing = await _grantRepository.FindByNameAsync(name);
        if (existing != null)
            throw new BusinessException("GrantManager:NameAlreadyExists").WithData("Name", name);

        return new Grant(GuidGenerator.Create(), name);
    }
}
```

## Domain Events

- `AddLocalEvent()` — same transaction, can access full entity state.
- `AddDistributedEvent()` — async, use ETOs defined in Domain.Shared.
- This project uses **RabbitMQ** for distributed events via `IDistributedEventBus`.

## Shared Constants

- Define constants, enums, and error codes in `*.Domain.Shared`.
- Localization resources (JSON) live under `Domain.Shared/Localization/*/en.json`.
- Error codes: namespaced as `ModuleName:ErrorCode`.
