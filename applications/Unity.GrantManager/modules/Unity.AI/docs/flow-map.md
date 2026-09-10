# Flow Map

```text
UI -> AIGenerationAppService -> IApplicationGenerationQueue
automation -------------------> IApplicationGenerationQueue
IApplicationGenerationQueue -> AIGenerationRequest + background job
  -> operation executor
  -> Unity.AI runtime
  -> operation-specific persisted result
```

The app service authorizes and feature-gates UI requests. Automatic intake checks its
own tenant, form, and feature preconditions before entering the queue. The Grant Manager
queue resolves the active database operation, prevents duplicate active requests,
validates prerequisites, and enqueues work. The background job establishes tenant scope
and records request state; its executor owns operation-specific input and persistence.
The runtime resolves the prompt and model configuration, renders the request, calls the
provider, and parses the response.

The form mapping, worksheet, and scoresheet operations require an application form
version. See [operation pipeline](./operation-pipeline.md) for ownership rules.
