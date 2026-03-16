---
name: unity-application-layer
description: ABP Application Services, DTOs, AutoMapper profiles, validation, and error handling for Unity. Use when creating or modifying app services, DTOs, or mapping profiles in Application or Application.Contracts projects.
---

# Unity Application Layer Patterns

## Application Service Contracts (Application.Contracts)

- Interface naming: `I*AppService` inheriting `IApplicationService`.
- Define DTOs in `*.Application.Contracts` — never in Domain or Web.
- All methods async, end with `Async`.
- Do NOT repeat entity name in method names: use `GetAsync`, not `GetGrantAsync`.

```csharp
public interface IGrantAppService : IApplicationService
{
    Task<GrantDto> GetAsync(Guid id);
    Task<PagedResultDto<GrantDto>> GetListAsync(GetGrantListInput input);
    Task<GrantDto> CreateAsync(CreateGrantDto input);
    Task<GrantDto> UpdateAsync(Guid id, UpdateGrantDto input); // ID separate from DTO
    Task DeleteAsync(Guid id);
}
```

## DTO Conventions

| Purpose | Convention | Example |
|---------|------------|---------|
| Query input | `Get{Entity}Input` | `GetGrantInput` |
| List query | `Get{Entity}ListInput` | `GetGrantListInput` |
| Create input | `Create{Entity}Dto` | `CreateGrantDto` |
| Update input | `Update{Entity}Dto` | `UpdateGrantDto` |
| Output | `{Entity}Dto` | `GrantDto` |

- Use data annotations for validation; reuse constants from Domain.Shared.
- Do NOT share input DTOs between methods.
- Do NOT put logic in DTOs (except `IValidatableObject` when necessary).

## Implementation (Application)

- Inherit from `ApplicationService`.
- Make all public methods `virtual`.
- Prefer `protected virtual` over `private` for helper methods.
- Use dedicated repositories, not inline LINQ in app services.
- Call `repository.UpdateAsync()` explicitly after mutations (don't assume change tracking).
- Do NOT use web types (`IFormFile`, `Stream`) — accept `byte[]` from controllers.
- Do NOT call other app services in the same module. Use domain services or repositories.

## Object Mapping (AutoMapper)

This project uses **AutoMapper** (not Mapperly). Mapping profiles are defined as:

```csharp
public class GrantManagerApplicationAutoMapperProfile : Profile
{
    public GrantManagerApplicationAutoMapperProfile()
    {
        CreateMap<Grant, GrantDto>();
        CreateMap<CreateGrantDto, Grant>();
    }
}
```

- Profile files follow `*AutoMapperProfile.cs` naming.
- Each Application and Web project has its own profile.
- Use `ObjectMapper.Map<TSource, TDest>(source)` in app services.

## Error Handling

```csharp
// Business rule violation — use namespaced error code
throw new BusinessException("GrantManager:DuplicateName")
    .WithData("Name", name);

// Entity not found
throw new EntityNotFoundException(typeof(Grant), id);

// User-facing message (use localized string)
throw new UserFriendlyException(L["GrantNotAvailable"]);
```

## Authorization

- Use `[Authorize(PermissionName)]` on service methods.
- Permission names defined as constants in `*Permissions` classes in Application.Contracts.

## Cross-Module Calls

- You MAY call other modules' app services via their Application.Contracts interfaces.
- Do NOT call app services within the same module — use domain services.
