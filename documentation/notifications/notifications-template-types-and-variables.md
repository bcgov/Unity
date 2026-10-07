# Template Types and Variables

## Purpose

Email templates currently use one global variable list. This proposal introduces a type-aware variable catalog so the TinyMCE Variables menu reflects the context in which a template is rendered.

Initial template types:

- `Application` — the default for all existing templates; may use application and applicant data reached through an application.
- `Applicant` — may use applicant-owned data that is stable across all applications for that applicant.

The design should use string type codes rather than a C# enum. A registered string-based type allows future types to be added without changing the database schema.

## Scope And Relationship Rules

`Application.ApplicantId` is required, while one `Applicant` can be associated with multiple applications. Therefore:

- Application templates may use application fields and applicant fields available through the selected application.
- Applicant templates may use only fields owned by `Applicant`, or explicitly approved related applicant data.
- Application-specific fields must not appear in Applicant templates because their values can differ between applications for the same applicant.
- Applicant templates rendered from an application still resolve the applicant through `Application -> Applicant`.
- A future applicant-only notification flow must provide an `ApplicantId` directly rather than manufacturing an application context.

## Persistence Changes

Add a required `TemplateType` field to `Notifications.EmailTemplates`:

```text
TemplateType varchar(64) NOT NULL DEFAULT 'Application'
```

Update the following:

- `EmailTemplate`
- `EmailTempateDto`
- `TemplatesService.CreateAsync`
- `TemplatesService.UpdateTemplate`
- template list and detail responses
- tenant EF Core migration and model snapshot

Existing rows must be backfilled to `Application`. New templates default to `Application` in both the server contract and the UI.

The existing template-name uniqueness check should become type-aware if templates with the same name are allowed in different contexts. The preferred uniqueness rule is `(TenantId, TemplateType, Name)`.

## Variable Catalog

Add `TemplateType` to `TemplateVariable`. The variable API should accept a type filter:

```text
GET /api/app/template/template-variables?templateType=Application
```

The API should return a DTO containing at least:

- `name`
- `token`
- `templateType`
- `mapTo`
- `isRequired`
- `availabilityDescription`

The seed process must be idempotent by `(TemplateType, Token)`, not by token alone. The existing variables become `Application` variables.

The type-specific variable catalog should be the source of truth for:

- seed data
- the TinyMCE Variables menu
- runtime token resolution
- template validation

## Template Editor Behavior

Add a required Template Type selector to `_TemplateDetails.cshtml` with the currently registered types. The selector should be data-driven so adding a future registered type does not require rewriting the editor markup.

Expected behavior:

1. New templates default to `Application`.
2. Existing templates load their persisted type.
3. Changing the type fetches the variables for the new type.
4. The TinyMCE Variables menu is rebuilt immediately from the new variable list.
5. If the editor contains unsaved content, changing type asks for confirmation before replacing the available menu.
6. Saving sends `templateType` with the other template fields.
7. Loading a template also uses its type when loading variables.
8. Switching type must not leave variables from the previous type in the menu.
9. `dropdownItems` must be cleared before repopulating so repeated edits do not duplicate menu entries.
10. The templates table should display Template Type and allow filtering if the number of types grows.

The current `initializeEditor` and `setupEditor` functions should receive the selected type and its variables instead of relying on one global list. The `variablesButton` remains available, but its contents are type-specific.

## Application Variables

These are the current application-template variables. They may include applicant values because the application is the rendering context.

| Display name | Token | Source | Required variable? |
|---|---|---|---|
| Applicant name | `applicant_name` | `Application.Applicant.ApplicantName` | No |
| Applicant ID | `applicant_id` | `Application.Applicant.UnityApplicantId` | No |
| Registered organization name | `organization_name` | `Application.Applicant.OrgName` or `NonRegisteredBusinessName` | No |
| Submission number | `submission_number` | `Application.ReferenceNo` | Yes for application identification |
| Submission date | `submission_date` | `Application.SubmissionDate` | Yes for submitted applications |
| Category | `category` | `Application.ApplicationForm.Category` | No |
| Status | `status` | `Application.ApplicationStatus` | No |
| Approved amount | `approved_amount` | `Application.ApprovedAmount` | No |
| Approval date | `approval_date` | `Application.FinalDecisionDate` | No |
| Community | `community` | `Application.Community` | No |
| Contact full name | `contact_full_name` | application applicant agent | No |
| Contact title | `contact_title` | application applicant agent | No |
| Decline rationale | `decline_rationale` | `Application.DeclineRational` | No |
| Project name | `project_name` | `Application.ProjectName` | No |
| Project summary | `project_summary` | `Application.ProjectSummary` | No |
| Project start date | `project_start_date` | `Application.ProjectStartDate` | No |
| Project end date | `project_end_date` | `Application.ProjectEndDate` | No |
| Fiscal year end | `fiscal_year_end` | `Application.Applicant.FiscalYearEnd` | No |
| Signing authority full name | `signing_authority_full_name` | `Application.SigningAuthorityFullName` | No |
| Signing authority title | `signing_authority_title` | `Application.SigningAuthorityTitle` | No |
| Requested amount | `requested_amount` | `Application.RequestedAmount` | No |
| Recommended amount | `recommended_amount` | `Application.RecommendedAmount` | No |
| Unity application ID | `unity_application_id` | `Application.UnityApplicationId` | No |
| Today’s date | `today_date` | runtime-generated | No |

The exact existing token set should remain unchanged for Application templates to preserve existing template bodies.

## Applicant Variables

The following fields are owned by `Applicant` and are reasonable initial candidates for Applicant templates. They correspond to fields shown on the Applicant list or present on the Applicant aggregate.

| Display name | Token | Source | Applicant list visibility | Required variable? |
|---|---|---|---|---|
| Applicant name | `applicant_name` | `Applicant.ApplicantName` | Default | Recommended for applicant identification |
| Unity Applicant ID | `applicant_id` | `Applicant.UnityApplicantId` | Default | Recommended for applicant identification |
| Registered organization name | `organization_name` | `Applicant.OrgName` | Default | No |
| Non-registered business name | `non_registered_business_name` | `Applicant.NonRegisteredBusinessName` | Additional | No |
| Non-registered organization name | `non_registered_organization_name` | `Applicant.NonRegOrgName` | Additional | No |
| Organization number | `organization_number` | `Applicant.OrgNumber` | Default | No |
| Business number | `business_number` | `Applicant.BusinessNumber` | Additional | No |
| Organization status | `organization_status` | `Applicant.OrgStatus` | Default | No |
| Organization type | `organization_type` | `Applicant.OrganizationType` | Default | No |
| Applicant status | `applicant_status` | `Applicant.Status` | Default | No |
| Sector | `sector` | `Applicant.Sector` | Additional | No |
| Sub-sector | `sub_sector` | `Applicant.SubSector` | Additional | No |
| Industry description | `industry_description` | `Applicant.SectorSubSectorIndustryDesc` | Additional | No |
| Approximate number of employees | `approximate_number_of_employees` | `Applicant.ApproxNumberOfEmployees` | Additional | No |
| Indigenous organization indicator | `indigenous_organization` | `Applicant.IndigenousOrgInd` | Additional | No |
| Fiscal month | `fiscal_month` | `Applicant.FiscalMonth` | Additional | No |
| Fiscal day | `fiscal_day` | `Applicant.FiscalDay` | Additional | No |
| Fiscal year end | `fiscal_year_end` | `Applicant.FiscalYearEnd` | Additional | No |
| Started operating date | `started_operating_date` | `Applicant.StartedOperatingDate` | Additional | No |
| Today’s date | `today_date` | runtime-generated | Not a list field | No |

### Applicant list fields requiring a decision

These fields appear on the Applicant list but should not be included in the first Applicant variable release without an explicit business decision:

- `RedStop` — operational risk/workflow flag; potentially sensitive.
- `IsDuplicated` — data-quality flag rather than applicant business data.
- `CreationTime` and `LastModificationTime` — audit metadata.
- `SupplierId`, `SupplierNumber`, `SupplierName`, and `SupplierStatus` — related supplier data, not Applicant-owned fields. They may be added later as a separate `ApplicantSupplier` scope if notification requirements justify them.
- `FiscalYearEnd` is applicant-owned and is safe to include, but it is not displayed as a default Applicant list column.

Applicant addresses should also remain out of the initial list. `ApplicantAddress` can contain both applicant-level and application-specific address records, so the system needs an explicit deterministic rule for selecting physical and mailing addresses before exposing address variables.

## Mandatory Versus Optional Variables

There are two different meanings of “mandatory” and they must not be conflated:

1. **Mandatory source field** — the database or business process requires the field to be populated.
2. **Mandatory template variable** — the template author is required to use the variable.

A required database field does not automatically mean a template must use it. Conversely, an optional database field may be essential to a particular notification.

Represent this explicitly in variable metadata:

```text
IsRequired: boolean
RequiredFor: Application | Applicant | None
```

Recommended rules:

- `ApplicantName` and `UnityApplicantId` are recommended identification variables, but should not block saving a template because legacy or imported records may lack them.
- `ReferenceNo` and `SubmissionDate` are mandatory for normal submitted applications and may be marked required for `Application` notifications.
- Applicant organization, sector, supplier, address, and fiscal fields are optional because they can be absent or not applicable.
- `today_date` is always resolvable and does not need source data.
- A missing optional value renders as an empty string.
- A missing required value should be surfaced at send time with a clear validation/logging result, rather than silently generating misleading content.

The initial implementation should use `IsRequired` for runtime validation metadata, while the editor may display a visual “required” indicator or description. It should not force every template to contain every required variable unless the business explicitly wants structural template validation.

## Runtime Rendering

Refactor the current application-centric resolver into separate scopes:

```text
BuildApplicationTokenValues(application, applicantAgent)
BuildApplicantTokenValues(applicant)
```

Select the resolver from `template.TemplateType`.

For an Applicant template applied while composing an email for an application:

```text
Application -> Applicant -> applicant token values
```

Applicant variables must be resolved from the `Applicant` entity, not from a copied application projection. This prevents values such as project name, submission number, or approval amount from leaking into Applicant templates.

Application-only tokens should remain unavailable to Applicant templates and should not be inserted by the TinyMCE menu. Server-side validation should also reject or flag tokens that do not belong to the template’s type.

## API And Validation Changes

Add a type-aware variable endpoint and, preferably, a type endpoint:

```text
GET /api/app/template/template-types
GET /api/app/template/template-variables?templateType=Applicant
```

Server-side validation should ensure:

- Template Type is non-empty.
- Template Type is registered.
- Variables belong to the selected Template Type.
- A template cannot save an application-only token as an Applicant template.
- Unknown tokens are either rejected with a validation message or deliberately preserved as literal text according to the product decision.

The browser menu is a usability feature, not the security boundary. Validation must occur on the server.

## Migration And Rollout

1. Add `TemplateType` to `EmailTemplate` with an `Application` default.
2. Backfill existing templates to `Application`.
3. Add `TemplateType` to `TemplateVariable` and migrate existing variables to `Application`.
4. Seed Applicant variables idempotently.
5. Add type-aware DTOs, APIs, and validation.
6. Add the Template Type selector and type-aware TinyMCE menu.
7. Refactor runtime token resolution into Application and Applicant scopes.
8. Add unit, integration, and Cypress coverage.
9. Review existing templates for tokens that are not valid in their stored type before enabling strict send-time validation.

## Testing Requirements

Cover at least:

- Existing templates read as `Application` after migration.
- New templates default to `Application`.
- Applicant templates persist and reload as `Applicant`.
- Variable lookup returns only the selected type.
- Switching type changes the TinyMCE menu and removes old variables.
- Reopening several templates does not duplicate menu entries.
- Applicant templates render applicant-owned values consistently across two applications for the same applicant.
- Applicant templates cannot use project, submission, funding, status, signing-authority, or application-contact variables.
- Missing optional values render as empty strings.
- Missing required values produce the chosen validation/logging behavior.
- Same-name templates follow the selected type-aware uniqueness rule.
- Applicant list fields marked as excluded do not appear in the Applicant menu.
