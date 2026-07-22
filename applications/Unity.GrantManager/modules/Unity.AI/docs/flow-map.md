# Flow Map

## Standard path
UI -> API app service -> queue -> background job -> AI runtime -> persisted result

## Operation families
- Application Analysis: submission -> analysis
- Attachment Summary: attachment ids -> summaries
- Application Scoring: application + scoresheet -> scoring
- Form Mapping: form version -> mapping
- Form Worksheet: form version -> worksheet

## Build Rule
See [`implementation-playbook.md`](./implementation-playbook.md) for the canonical add-a-new-operation sequence.
