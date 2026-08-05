# Unity.AI

`Unity.AI` owns provider-neutral AI contracts, prompt/model/operation configuration,
runtime execution, and the generation API. Grant Manager owns the application data,
queue implementation, operation executors, and persistence of generated results.

## Boundaries

| Area | Responsibility |
| --- | --- |
| `Domain.Shared` | Features, permissions, localization, and prompt family names |
| `Application.Contracts` | Runtime, generation, queue, and DTO contracts |
| `Application` | Prompt/model/operation seeds, provider runtime, API, and status reads |
| `Runtime/Execution` | Prompt rendering, provider calls, response parsing, and prompt logging |
| Grant Manager | Request locking, background jobs, operation executors, and result persistence |
| `Web` | Menus, generate actions, and status polling |

## Operation catalog

`AIGenerationOperations` is the single catalog for operation type, prompt family,
feature, permissions, and form-version requirement.

| Operation | Type | Requires form version |
| --- | --- | --- |
| Application Analysis | `application-analysis` | No |
| Attachment Summary | `attachment-summary` | No |
| Application Scoring | `application-scoring` | No |
| Form Mapping | `form-mapping` | Yes |
| Form Worksheet | `form-worksheet` | Yes |
| Form Scoresheet | `form-scoresheet` | Yes |

Generation requires both the catalogued feature and generate permission. Status reads
require the corresponding view permission.

See [configuration](./configuration.md), [pipeline](./operation-pipeline.md), and the
[implementation playbook](./implementation-playbook.md).
