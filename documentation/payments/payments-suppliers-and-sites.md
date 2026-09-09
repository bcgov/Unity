# Payments — Suppliers and Sites

CAS addresses a payment by **supplier number plus site code**. Unity never invents either: `Supplier` and `Site` rows in the `Payments` schema are a mirror of CAS master data, refreshed opportunistically whenever staff touch supplier information. This document covers how that mirror is built and kept current, and how an applicant ends up bound to a supplier and a default site.

## The players

| Component | Location | Role |
|---|---|---|
| `SupplierService` (`ISupplierService`) | `Unity.Payments.Application/Integrations/Cas/` | Calls CAS, parses the JSON, publishes `UpsertSupplierEto` |
| `UpsertSupplierHandler` | `Unity.Payments.Application/Handlers/` | Creates/updates the `Supplier` and its `Site` rows, then publishes `ApplicantSupplierEto` |
| `SupplierCreatedHandler` | `src/Unity.GrantManager.Application/Handlers/UpsertSupplierHandler.cs` | Host side: links the supplier to the applicant, and deletes or tombstones sites CAS no longer returns |
| `SupplierAppService` / `SiteAppService` | `Unity.Payments.Application/Suppliers/` | CRUD plus the site-comparison logic used by the Supplier Info widget |
| `ApplicantSupplierAppService` | `src/Unity.GrantManager.Application/Applicants/` | Host-facing entry point used by the widget: set/clear an applicant's supplier number, look up by BN9 |

## Reading a supplier from CAS

`SupplierService` builds its base URL lazily (`Lazy<Task<string>>`) from the `PAYMENT_API_BASE` dynamic URL, and authenticates with `ICasTokenService` (see [payments-cas-integration.md](payments-cas-integration.md#authentication)).

| Method | CAS resource |
|---|---|
| `GetCasSupplierInformationAsync(supplierNumber)` | `GET {base}/cfs/supplier/{supplierNumber}` |
| `GetCasSupplierInformationByBn9Async(bn9)` | `GET {base}/cfs/supplier/{bn9}/businessnumber` |

Both funnel through `GetCasSupplierInformationByResourceAsync`, which raises a `UserFriendlyException` for a 404 ("Supplier not Found."), for any non-OK status, and for a null response. The BN9 lookup returns an `items` array; `UpdateApplicantSupplierInfoByBn9` takes the **first** entry.

A CAS supplier response looks like:

```json
{
  "suppliernumber": "2002492",
  "suppliername": "GENSUPPOSP2016, ONE",
  "subcategory": "Individual",
  "sin": null, "providerid": null, "businessnumber": null,
  "status": "INACTIVE", "supplierprotected": null,
  "standardindustryclassification": null,
  "lastupdated": "2024-05-10 12:21:53",
  "supplieraddress": [
    { "suppliersitecode": "001", "addressline1": "100-3350 DOUGLAS ST",
      "city": "VICTORIA", "province": "BC", "country": "CA", "postalcode": "V8Z3L1",
      "emailaddress": null, "accountnumber": null, "eftadvicepref": null,
      "providerid": null, "status": "INACTIVE", "siteprotected": null,
      "lastupdated": "2021-03-18 14:46:32" }
  ]
}
```

`GetEventDtoFromCasResponse` maps this to `UpsertSupplierEto`. Two details worth knowing:

- An **empty `suppliernumber`** raises `UserFriendlyException` — Unity refuses to create a nameless payee.
- `GetSiteEto` **masks the bank account**: `accountnumber` is stored as `****1234` (all but the last four characters replaced with `*`). The unmasked number never enters the Unity database.
- The `sin` field is read into no property — `Supplier.SIN` exists on the entity but nothing populates it from CAS.

`UpdateSupplierInfo` re-parses the response as a `JsonDocument`, raises `UserFriendlyException` if CAS returned `{"code": "Unauthorized"}`, stamps the `ApplicantId` and optional `ApplicationId` onto the ETO, and publishes it on the **local** event bus.

`UpdateApplicantSupplierInfo` is a no-op unless the `Unity.Payments` feature is enabled and a supplier number was supplied.

## Upserting the supplier and its sites

`UpsertSupplierHandler : ILocalEventHandler<UpsertSupplierEto>` (payments module):

1. **`GetSupplierFromEvent`** — looks the supplier up by number. Found → `UpdateAsync`; not found → `CreateAsync`. Both go through `SupplierAppService`, which routes the fields into the value-object update methods (`UpdateBasicInfo`, `UpdateProviderInfo`, `UpdateStatus`, `UpdateCasMetadata`).
2. **Existing sites** are loaded and indexed by site number into a dictionary (duplicates collapsed with `GroupBy(...).First()`).
3. **`ResolveDefaultPaymentGroupAsync`** — if the ETO carries an `ApplicationId`, the application's `ApplicationForm.DefaultPaymentGroup` decides the pay group for **new** sites; otherwise it falls back to `PaymentGroup.EFT`. Failures are logged as a warning and fall back to EFT.
4. **`UpsertSitesFromEventDtoAsync`** — deduplicates the incoming `SiteEto`s by `SupplierSiteCode` (CAS can return duplicates), then per site: existing → `SiteAppService.UpdateAsync`, new → `SiteAppService.InsertAsync`.
5. Publishes `ApplicantSupplierEto { SupplierId, ApplicantId, ExistingSitesDictionary, SiteEtos }`.

`SiteAppService` adds two guards:

- **`InsertAsync`** checks for an existing site with the same `Number` under the same supplier and updates it instead of inserting a duplicate.
- **`UpdateAsync`** compares field-by-field with `Site.SiteMatchesSiteDto` and skips the write entirely when nothing changed. When it does write, it clears `MarkDeletedInUse`.

## The host reaction: linking and tombstoning

`SupplierCreatedHandler : ILocalEventHandler<ApplicantSupplierEto>` (host, in `src/Unity.GrantManager.Application/Handlers/UpsertSupplierHandler.cs`):

1. `IApplicantAppService.RelateSupplierToApplicant(eto)` — sets the applicant's `SupplierId`.
2. Computes `sitesToDelete` = site numbers Unity has that CAS no longer returns. For each:
   - if any `Application` references it as `DefaultSiteId` → `SiteAppService.MarkDeletedInUseAsync(siteId)`;
   - else if any payment request references it (`GetPaymentRequestCountBySiteIdAsync`) → `MarkDeletedInUseAsync`;
   - else → `SiteAppService.DeleteAsync(siteId)`.

`MarkDeletedInUse` is the tombstone: the site is gone from CAS but is still referenced by Unity history, so it is kept and flagged rather than removed. Any later CAS refresh that returns the site again clears the flag (via `SiteAppService.UpdateAsync`).

## Refreshing sites from the UI

The Supplier Info widget lists a supplier's sites through `SupplierAppService.GetSitesBySupplierNumberAsync(supplierNumber, applicantId, applicationId)`, which does a **live comparison against CAS on every call**:

1. Resolve the default pay group for the applicant/application (`ResolveDefaultPaymentGroupForApplicantAsync` — same rule as the handler: the application's form's `DefaultPaymentGroup`, else EFT).
2. Fetch the supplier from CAS and project its `supplieraddress` array into `SiteDto`s.
3. Load the sites Unity already has.
4. `hasChanges` is true if the counts differ, or if any CAS site is missing locally, or if any matched site differs on pay group, country, EFT advice preference, email, postal code, provider id, province, site-protected flag, city, address lines 1–2, bank account, or status.
5. If `hasChanges`, call `SupplierService.UpdateSupplierInfo(...)` — which re-enters the upsert path above — then re-read the sites so the caller gets rows with real ids.
6. Return `{ sites, hasChanges }`.

The widget uses `hasChanges` to tell the user the list was refreshed.

## Binding an applicant to a supplier

`ApplicantSupplierAppService` (host) is what the Supplier Info widget calls:

| Method | Behaviour |
|---|---|
| `GetSupplierByNumber(supplierNumber)` / `GetSupplierByBusinessNumber(bn9)` | Straight passthrough to `SupplierService` — used to preview a supplier before committing to it |
| `UpdateApplicantSupplierNumberAsync(applicantId, supplierNumber, applicationId?)` | `[Authorize(UnitySelector.Payment.Supplier.Update)]`. No-ops when the feature is off or the number is unchanged; an empty number routes to `ClearApplicantSupplierAsync`; otherwise triggers `SupplierService.UpdateApplicantSupplierInfo`, which runs the whole CAS→upsert→link chain |
| `ClearApplicantSupplierAsync(applicantId)` | Clears `Applicant.SupplierId` **and** cascades: every application for that applicant has its `DefaultSiteId` cleared, because the sites belonged to the now-unlinked supplier |
| `GetSitesBySupplierIdAsync(supplierId)` | Direct repository read |

The **default site** lives on the `Application` (`Application.DefaultSiteId`), not on the applicant — different applications from the same applicant can be paid to different sites. `SupplierInfoViewComponent` reads it to seed the widget, and `CreatePaymentRequestsModel` reads it to stamp `SiteId` on each new payment request. A payment cannot be created without one (`ValidatePaymentRequest` throws `MissingSite`).

## Pay group

`Site.PaymentGroup` (`EFT` or `Cheque`) decides the CAS `payGroup` field (`"GEN EFT"` / `"GEN CHQ"`). It has three inputs:

1. **Default on creation** — the application's form's `DefaultPaymentGroup`, else `EFT`.
2. **Manual override** — `SiteAppService.UpdatePaygroupAsync(paymentGroup, siteId)` from the Supplier Info widget.
3. **CAS refresh** — a differing pay group counts as a change and triggers a re-sync, which rewrites the site from the CAS projection.

An **EFT site with no bank account** blocks payment creation: `PaymentRequestPageHelperService.GetErrorListAsync` adds "Payment cannot be submitted because the default site's pay group is set to EFT, but no bank account is configured."

## Where supplier data surfaces

- **Supplier Info widget** (`Views/Shared/Components/SupplierInfo/`) on the application detail page — supplier number/name/status, site list, default-site selection, pay group editing. Gated by the `Unity.Payments` feature and `UnitySelector.Payment.Supplier.Update` for edits.
- **Payments list** — `supplierNumber`, `supplierName`, `site` (site number) and `payGroup` columns, eager-loaded only when those columns are requested.
- **CAS invoice** — `SupplierNumber`, `SupplierName`, `SupplierSiteNumber`, `PayGroup`.
- **FSB spreadsheet** — `CAS Supplier/Site Number` as `{supplierNumber}/{siteNumber}`, the flattened payee address, and the pay group.
- **Applicant profile** (applicant portal) — `PaymentInfoDataProvider` and `ApplicantPaymentsAppService` expose supplier number/name and site alongside each payment.
