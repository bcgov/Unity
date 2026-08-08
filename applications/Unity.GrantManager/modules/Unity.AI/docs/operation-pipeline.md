# AI operation pipeline

AI generation uses a shared operation catalog and queued lifecycle. The catalog owns the operation key, seeded operation name, feature gate, permissions, and whether a form version is required. Submission enters `IAIGenerationAppService.SubmitAsync`, and the Grant Manager queue preserves the existing duplicate-request lock and operation-specific validation.

The generic background-job base owns tenant scope, structured logging, request state transitions, failure handling, and cooldown stamping. Operation-specific executors remain responsible for loading input, calling the AI contract, validating the response, and persisting the result.

## Adding an operation

1. Add an operation definition to `AIGenerationOperations`.
2. Add the operation's submission payload fields only when shared fields are insufficient.
3. Add the AI request/response contract and prompt/operation seed.
4. Implement an `IAIGenerationOperationExecutor` for Grant Manager-specific input and persistence.
5. Register the executor through the existing transient DI convention.
6. Add focused catalog, lifecycle, executor, and persistence tests.

Do not add another queue branch for shared lifecycle concerns. New operation behavior belongs in its executor; request locking, status transitions, tenant scope, logging, and cooldown behavior stay in the common pipeline.
