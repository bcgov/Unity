# Runtime Configuration

AI behavior is split between host-owned database configuration and deployment
configuration. The database is the source of truth for which model, operation, and
prompt are used; appsettings holds deployment connectivity and operational settings.

## Database configuration

| Record | Owns |
| --- | --- |
| `AIModel` | Provider, deployment name (`Name`), active state, and model settings JSON |
| `AIOperation` | Prompt family (`Name`), selected model, execution mode, completion-token limit, and active state |
| `AIPrompt` | Versioned system/user templates, metadata, active state, and optional tenant ownership |

Host seeders create the built-in models, operations, and global prompts. Operations
select models by ID; `AIModel.Name` is the provider deployment identifier. The runtime
rejects inactive or unsupported configuration rather than choosing a fallback model.

## Prompt selection

For a prompt family, host requests use the newest active global prompt. Tenant requests
use the newest active tenant prompt, then fall back to the newest active global prompt.
Operations do not store a prompt ID or version.

## External configuration

Provider endpoint, API key, and authenticated-user cooldown remain deployment configuration:

```text
Azure:OpenAI:Endpoint
Azure:OpenAI:ApiKey
Azure:Generation:CooldownSeconds
```

Do not add operation defaults, profile maps, or prompt versions to appsettings.
