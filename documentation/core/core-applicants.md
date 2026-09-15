# Core — Applicants

An `Applicant` is the organisation or individual applying. Unlike an `Application`, it is **shared** — one applicant accumulates many applications over years, and the applicant record is what the Applicant Profile, funding history and payment supplier all hang off.

That sharing is what makes this area harder than it looks: applicants arrive from CHEFS with inconsistent names, get created twice, and then have to be merged back together without losing history.

## The aggregate

`Domain/Applications/Applicant.cs` — `AuditedAggregateRoot<Guid>`, `IMultiTenant`.

| Group | Properties |
|---|---|
| Identity | `ApplicantName`, `OrgName`, `NonRegisteredBusinessName`, `NonRegOrgName`, `UnityApplicantId`, `OrgNumber`, `BusinessNumber` |
| Classification | `OrganizationType`, `Sector`, `SubSector`, `SectorSubSectorIndustryDesc`, `IndigenousOrgInd`, `ApproxNumberOfEmployees` |
| Status | `Status`, `OrgStatus`, `RedStop` |
| Fiscal | `FiscalMonth`, `FiscalDay`, `FiscalYearEnd`, `StartedOperatingDate` |
| Payments | `SupplierId` — see [`payments/payments-suppliers-and-sites.md`](../payments/payments-suppliers-and-sites.md) |
| Duplicate detection | `MatchPercentage`, `IsDuplicated` |
| Free-text notes | `FundingHistoryComments`, `IssueTrackingComments`, `AuditComments`, `ReportsComments` |
| Soft delete | `IsDeleted`, `DeletionTime`, `DeleterId` — declared by hand, see below |

`RedStop` is a hard block flag surfaced through `GrantApplicationAppService.IsApplicantRedStopAsync`.

### Why `Applicant` is not `ISoftDelete`

The three soft-delete fields are declared manually rather than by implementing ABP's `ISoftDelete`, and the entity documents exactly why:

> `ISoftDelete` would register a global EF Core query filter that applies to `Include()` joins — `Application.Applicant` is a required navigation (`OnDelete NoAction`), so the filter would silently drop `Application` rows whose `Applicant` is soft-deleted, breaking the application and payment list pages. Filtering is applied explicitly only where needed (applicant list, lookup, autocomplete) via `.Where(a => !a.IsDeleted)`.

So: **deleting an applicant must not make its applications disappear.** Any new query that lists applicants needs its own `!a.IsDeleted` filter; any query that loads applications through applicants must *not* have one.

`DeleteApplicantAsync` is gated by `UnitySelector.ApplicantManagement.Applicant.Delete`, and `HasSubmissionsAsync` exists so the UI can warn before deleting an applicant that still has submissions.

## Applicant agents

`ApplicantAgent` is the **person who submitted**, as distinct from the organisation. One per application (`ApplicationId`), created during intake.

| Group | Properties |
|---|---|
| Link | `ApplicantId`, `ApplicationId` / `Application`, `OidcSubUser` |
| Contact | `Name`, `Title`, `Email`, `Phone`, `Phone2`, `PhoneExtension`, `Phone2Extension` |
| Role | `RoleForApplicant`, `ContactOrder`, `IsConfirmed`, `IsActive` |
| BCeID identity | `BceidBusinessGuid`, `BceidUserGuid`, `BceidUserName`, `BceidBusinessName`, `IdentityName`, `IdentityEmail`, `IdentityProvider` |

The BCeID block comes from the CHEFS login token and is what ties a portal user back to an applicant. `ContactOrder` drives the ordering of contacts on the application; `IsConfirmed` and `IsActive` control whether an agent still counts.

## Addresses

`ApplicantAddress` is an `AuditedAggregateRoot` linked to **both** an applicant and (optionally) an application, with an `AddressType` — `PhysicalAddress` or `MailingAddress`.

The invariant is primary-per-type, and `ApplicantAddressManager` owns it:

> Primary is scoped to `AddressType`: an applicant may hold at most one primary address **of each type**.

| Method | Role |
|---|---|
| `DemotePrimarySiblingsAsync(applicantId, addressType, …)` | Clears the primary flag on other addresses of the same type before setting a new one |
| `ElectPrimaryAsync(applicantId, addressType, …)` | Picks a replacement primary when the current one is removed |

The manager is written generically over the enum so adding a third address type needs no change to the rule.

Addresses matter beyond correspondence: `ApplicationForm.ElectoralDistrictAddressType` decides *which* address the electoral district is derived from at intake, and `DetermineElectoralDistrictHandler` performs that lookup.

`UpdateApplicantContactAddressesAsync` is gated by `UnitySelector.ApplicantManagement.Addresses.Update`.

## Contacts

A separate, more general model in `Domain/Contacts/` — `Contact`, `ContactLink`, `ContactManager`. `ContactLink` associates a contact with a subject, so the same contact can be reused across applicants and applications rather than duplicated per application the way `ApplicantAgent` is.

Surfaced by the **Applicant Contacts** and **Application Contacts** widgets, and by the `ApplicantContact` and `ApplicationContact` pages.

## How an applicant is resolved at intake

`ApplicantAppService.CreateOrRetrieveApplicantAsync(intakeMap, applicationId)` is called once per submission. It either finds the existing applicant or creates one, then intake stamps `application.ApplicantId` and calls `CreateApplicantAgentAsync`.

`RelateDefaultSupplierAsync` follows, and is the one step allowed to fail silently — it is wrapped in a `try`/`catch` at the intake call site because of SSL certificate errors reaching CAS. It only runs when the applicant has no `MatchPercentage` yet, among other conditions.

## Duplicate detection

Two fields drive it: `MatchPercentage` and `IsDuplicated`.

`UpdateApplicantOrgMatchAsync(applicant)` computes a similarity score for the applicant's organisation name against existing records and stores it as `MatchPercentage`. `MatchApplicantOrgNamesAsync()` is the bulk sweep across the tenant.

The applicant list surfaces `IsDuplicated` so staff can find candidates to merge — which is the point of the whole feature.

## Merge and unmerge

`ApplicantMergeManager` (393 lines) is the most carefully built thing in this area, because merging is destructive and has to be reversible.

### Merging

`MergeAsync(principalApplicantId, secondaryApplicantId, source, …)` rejects up front:

| Guard | Error code |
|---|---|
| Principal and secondary are the same applicant | `ApplicantMergeSameApplicant` |
| The selection is not valid | `ApplicantMergeInvalidSelection` |
| The suppliers conflict | `ApplicantMergeInvalidSupplier` |

It then records **before and after JSON snapshots of both applicants** and moves the secondary's applications onto the principal. A comment explains a subtlety in how the applications are loaded:

> Merge updates applications, so load only the tracked application entities. `GetByApplicantIdAsync` is a read-oriented no-tracking query that includes the `Applicant` graph; attaching that graph for update conflicts with the principal and secondary `Applicant` instances already tracked above.

### The operation record

`ApplicantMergeOperation` is an aggregate in its own right, with private setters throughout:

| Property | Purpose |
|---|---|
| `PrincipalApplicantId`, `SecondaryApplicantId` | Who was merged |
| `Status` | `Completed` or `Reversed` (`ApplicantMergeStatus`) |
| `Source` | `ApplicantList` or `ApplicationDetails` (`ApplicantMergeSource`) — where the merge was initiated |
| `PrincipalStateBefore` / `After`, `SecondaryStateBefore` / `After` | Full JSON snapshots on both sides |
| `MergedAt`, `MergedById` | Audit |
| `ReversedAt`, `ReversedById`, `ReversalReason` | Populated on unmerge |
| `SnapshotVersion` | Schema version of the snapshots, so an old operation can still be interpreted |
| `ApplicationChanges` | The `ApplicantMergeApplicationChange` rows recording every application that moved |

### Reversibility

`GetReversibilityAsync(mergeOperationId)` returns an `ApplicantMergeReversibility` describing whether the merge can still be undone. The check is not just "was it merged?" — `RelatedRecordsMatchAfterStateAsync(operation)` compares the current state of the affected records against the `*StateAfter` snapshots. **If anything has changed since the merge, the merge is no longer safely reversible.**

`UnmergeAsync` then restores from the snapshots, and throws `ApplicantMergeApplicantUnavailable` or `ApplicantMergeInvalidHistory` if the records or the history needed are gone.

`GetActiveOperationsAsync(applicantId)` lists the merges currently affecting an applicant, which is what the UI uses to offer an unmerge.

## Unity applicant IDs

Distinct from the applicant's own name or business number, `UnityApplicantId` is Unity's stable human-readable identifier.

| Method | Role |
|---|---|
| `GetNextUnityApplicantIdAsync()` | Next available number |
| `IsUnityApplicantIdAvailableAsync(id, currentApplicantId)` | Uniqueness check that ignores the applicant being edited |
| `GetExistingApplicantAsync(unityApplicantId)` | Lookup by that id |

`ApplicantLookupService` (used by the applicant portal) resolves an applicant two ways — `ApplicantLookupByApplicantId(unityApplicantId)` and `ApplicantLookupByBceidBusinesName(bceidBusinessName, createIfNotExists)`. The second can create an applicant on the fly, and when the resulting applicant has both an `OrgNumber` and a `BusinessNumber` it triggers a CAS supplier lookup by BN9.

## Editing an applicant

`ApplicantAppService` is 738 lines. The pattern worth noting is `PartialUpdateApplicantSummaryAsync(applicantId, PartialUpdateDto<UpdateApplicantSummaryDto>)` — a partial-update DTO so the UI can save one field without the caller needing edit rights over every field in the summary. The same pattern appears on the application side as `UpdatePartialProjectInfoAsync`.

Permissions come from the `UnitySelector.ApplicantManagement` subtree:

| Operation | Permission |
|---|---|
| List, read, `HasSubmissionsAsync` | `ApplicantManagement.Applicant.Default` |
| Update status and summary | `ApplicantManagement.Applicant.Update` |
| Delete | `ApplicantManagement.Applicant.Delete` |
| Update contact addresses | `ApplicantManagement.Addresses.Update` |

## Where applicants surface

- **Applicant list** (`Web/Pages/Applicants/`) — search, duplicate flags, merge entry point.
- **Applicant detail** — the `ApplicantInfo`, `ApplicantOrganizationInfo`, `ApplicantAddresses`, `ApplicantContacts`, `ApplicantAttachments`, `ApplicantSubmissions`, `ApplicantHistory` and `ApplicantPayments` widgets.
- **Application detail** — the same applicant summary, plus the supplier widget owned by Payments.
- **Applicant Profile** — the portal-facing polymorphic endpoint, documented in [`applicant-portal/applicant-profile-data-providers.md`](../applicant-portal/applicant-profile-data-providers.md).
