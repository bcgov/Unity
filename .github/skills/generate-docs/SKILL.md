---
name: generate-docs
description: "Generate documentation for Unity Grant Manager components and APIs"
---

# Generate Documentation

Generate or update documentation for Unity Grant Manager components, APIs, and architectural decisions.

Ask for the following if not provided:
- The component or feature to document
- Documentation type (XML comments, markdown, API docs, architecture)

## Requirements

- Follow the project's documentation standards
- Use XML doc comments for all public C# APIs with `<summary>`, `<param>`, `<returns>`, `<exception>` tags
- Include `<example>` blocks for complex APIs
- Use Mermaid diagrams for architectural and data flow documentation
- Keep documentation close to the code it describes
- Use localization keys for user-facing content — add keys to `Localization/GrantManager/en.json`

## Documentation Types

- **XML Comments**: Public classes, methods, interfaces, and DTOs
- **Architecture Docs**: Update ARCHITECTURE.md for structural changes with Mermaid diagrams
- **API Documentation**: Swagger annotations, endpoint behavior, authorization requirements
- **Module Documentation**: Describe module purpose, integration points, and communication patterns
- **Migration Guides**: Document breaking changes and upgrade steps

## References

- [documentation.instructions.md](../../instructions/documentation.instructions.md) for documentation standards
- [ARCHITECTURE.md](../../../ARCHITECTURE.md) for existing architectural documentation
