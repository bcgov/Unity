---
applyTo: "**/*.cs"
---

# C# Conventions for Unity Grant Manager

- Target framework: .NET 9.0 with `<LangVersion>latest</LangVersion>`.
- Nullable reference types are enabled in most projects.
- This is an ABP Framework project. Use ABP base classes, not raw ASP.NET Core.
- This is NOT the Unity game engine. Do not suggest UnityEngine APIs.

## Entities & Domain

- Entities use rich domain model: private/protected setters, behaviour via methods.
- Include `protected` parameterless constructor for EF Core deserialization.
- Do not generate `Guid` keys inside constructors; accept `id` from `IGuidGenerator`.
- Reference other aggregate roots by Id only, not navigation properties.
- Domain services use `*Manager` suffix.
- Throw `BusinessException` with namespaced error codes for rule violations.

## Application Services

- Interface naming: `I*AppService` inheriting `IApplicationService`.
- All methods `async`, name ends with `Async`.
- Accept/return DTOs only, never entities. Define DTOs in `*.Application.Contracts`.
- Make all public methods `virtual`.
- Use **AutoMapper** (`ObjectMapper.Map<>()`) for DTO mapping. Do NOT use Mapperly.
- Mapping profiles: `*AutoMapperProfile.cs` inheriting `Profile`.

## Code Style

- All user-facing text must be localized via `L["Key"]`. No hardcoded English strings.
- Permissions defined in `*PermissionDefinitionProvider` in Application.Contracts.
- Do not call other application services within the same module; push shared logic to domain services.
