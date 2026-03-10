---
applyTo: "**/*.cs,**/*.md"
description: "Documentation standards for Unity Grant Manager"
---

# Documentation Standards

Apply the repository-wide guidance from `../copilot-instructions.md` to all documentation.

## XML Documentation Comments

- Create XML doc comments for all public APIs, classes, and interfaces
- Include `<summary>`, `<param>`, `<returns>`, and `<exception>` tags as appropriate
- When applicable, include `<example>` and `<code>` blocks for complex APIs
- Document business rules and domain-specific behavior in entity and domain service comments

## Markdown Documentation

- Use clear headings with proper hierarchy (H1 → H2 → H3)
- Include Mermaid diagrams for architectural and flow documentation
- Keep documentation close to the code it describes
- Update ARCHITECTURE.md when making structural changes
- Update CONTRIBUTING.md when changing conventions or patterns

## Code Comments

- Comment the "why", not the "what" — code should be self-documenting
- Document complex business rules, non-obvious design decisions, and workarounds
- Add TODO comments with context for deferred work
- Reference ABP documentation links for framework-specific patterns

## API Documentation

- Document endpoint behavior, expected inputs, and response shapes
- Include authorization requirements and permission names
- Document error codes and exception scenarios
- Keep Swagger/OpenAPI annotations up to date

## Localization

- All user-facing strings must use localization keys
- Add new keys to `Localization/GrantManager/en.json`
- Use descriptive, hierarchical key names (e.g., `Menu:Applications`, `Permissions:Edit`)
