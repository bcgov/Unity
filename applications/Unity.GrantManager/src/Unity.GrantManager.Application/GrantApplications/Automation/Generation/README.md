# Grant Manager AI Generation

This folder owns the Grant Manager side of AI generation. Unity.AI owns shared
contracts, runtime execution, and database configuration; Grant Manager owns request
queueing, background execution, application-specific input, and result persistence.

`ApplicationAIGenerationQueue` serializes queueing per tenant, application, and
operation, then records one active request before enqueuing its job.
`AIGenerationBackgroundJob` owns tenant scope and request lifecycle state.
Operation executors own their input and persistence; they must not duplicate queue,
status, or cooldown behavior.

See the shared [Unity.AI operation pipeline](../../../../../modules/Unity.AI/docs/operation-pipeline.md).
