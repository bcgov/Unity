# Payments Web UI

All paths are relative to `applications/Unity.GrantManager/modules/Unity.Payments/src/Unity.Payments.Web/` unless noted.

`PaymentsWebModule` registers the menu contributor, the embedded file set, the Mapperly object mapper, and `PaymentRequestPageHelperService` as scoped. It also declares two Razor Pages conventions:

```csharp
options.Conventions.AllowAnonymousToPage("/Payments/Index");
options.Conventions.AllowAnonymousToPage("/PaymentConfigurations/Index");
```

See [payments-roadmap.md](payments-roadmap.md#anonymous-page-conventions).

## Menu

`PaymentsMenuContributor` adds a single main-menu item — **Payments** → `~/PaymentRequests` — and only when `IFeatureChecker.IsEnabledAsync("Unity.Payments")` is true. The display name is a hard-coded string rather than a localized resource.

## Pages

| Route | Page model | Purpose |
|---|---|---|
| `/PaymentRequests` | `Pages/PaymentRequests/Index.cshtml(.cs)` → `PaymentsPageModel` | The Payments list — the module's main screen |
| `/PaymentRequests/CreatePaymentRequests` | `CreatePaymentRequestsModel` | Modal: create payment requests for the selected applications |
| `/PaymentRequests/CreateHistoricalPayments` | `CreateHistoricalPaymentsModel` | Modal: record historical (already-paid) payments |
| `/PaymentRequests/CasPaymentRequestResponse` | `DisplayCasPaymentRequestResponseModel` | Modal: the full CAS response text for one payment |
| `/PaymentApprovals/UpdatePaymentRequestStatus` | `UpdatePaymentRequestStatus` | Modal: approve or decline the selected payments |
| `/PaymentTags/PaymentTagsSelectionModal` | `PaymentTagsSelectionModalModel` | Modal: assign tags to the selected payments |
| `/PaymentConfigurations` | `PaymentConfigurationModel` | Tenant payment configuration (prefix + default account coding) |
| `/AccountCoding/CreateModal`, `/AccountCoding/UpdateModal` | `CreateModalModel`, `UpdateModalModel` | Account coding CRUD modals |
| `/PaymentThresholds/UpdateModal` | `UpdateModalModel` (namespace `Unity.GrantManager.Web.Pages.PaymentThresholds`) | Set an L2 approver's threshold |

The payment **history** page itself lives in the host: `src/Unity.GrantManager.Web/Pages/PaymentHistory/Details.cshtml`.

## The Payments list

`Pages/PaymentRequests/Index.js` (~1,400 lines) is the largest surface in the module. It is a DataTables 2.x grid over `PaymentRequestAppService.GetListAsync(PaymentRequestListInputDto)`.

### Requested fields

The client sends `requestedFields` — the list of columns actually visible — alongside the page request. `PaymentRequestQueryManager` uses it to decide what to eager-load and what to hydrate:

| Requested field(s) | Effect |
|---|---|
| `siteNumber`, `payGroup` | `Include(pr => pr.Site)` |
| `accountCodingDisplay` | `Include(pr => pr.AccountCoding)` and format the distribution code |
| `paymentTags` | `Include(pr => pr.PaymentTags).ThenInclude(pt => pt.Tag)` |
| `l1ApproverName`/`l1ApprovalDate`/…/`l3ApprovalDate` | `Include(pr => pr.ExpenseApprovals)` and resolve decision-user names |
| `paymentRequesterName` | resolve `CreatorId` to a `PaymentUserDto` |
| `applicantName`, `category` | load the applications and stamp `ApplicantId` / `Category` |

An empty or absent `requestedFields` list means "include everything" (`IncludesAny` returns `true` when the list is empty). User names come from `IExternalUserLookupServiceProvider`, one lookup per distinct user id.

### Date filtering

The list is date-bounded by design. The action bar carries a quick-range dropdown plus custom from/to inputs; the selection is persisted in `localStorage` under `PaymentRequests_QuickRange` / `_FromDate` / `_ToDate`, defaulting to **last 6 months**. The dates are sent as `requestedFromDate` / `requestedToDate` and converted in `PaymentRequestQueryManager.ConvertToUtcRange` from Vancouver local dates to a UTC range, with the "to" date extended to the end of the local day.

### Columns

`getColumns()` defines the full set; `defaultVisibleColumns` is the subset shown before the user customises anything:

```text
select · referenceNumber (Payment ID) · batchName · applicantName · supplierNumber · siteNumber
contactNumber · invoiceNumber · payGroup · amount · status · requestedOn · updatedOn · paidOn
l1ApprovalDate · l2ApprovalDate · l3ApprovalDate · CASResponse · accountCodingDisplay
```

Also available but hidden by default: supplier name, submission confirmation code, description, invoice status, payment status, L1/L2/L3 approver names, payment requester name, note, tags, `fsbApNotified`, category, and the three cancellation columns (Cancelled, Cancelled By, Cancelled On).

`ErrorSummary` (set by `ApplyErrorSummary` whenever `CasResponse` is present and not `"SUCCEEDED"`) drives the clickable CAS Response cell, which opens the `CasPaymentRequestResponse` modal.

### Saved views

The grid uses DataTables' `stateRestore` (`savedStates` button) with a creation modal, so users can save named views. `restoreCustomFilters` re-applies the saved search value and date range when a view is loaded, and **Reset to Default View** restores `defaultVisibleColumns`, the default sort, cleared filters, and the default quick date range.

### Toolbar actions

| Button | Behaviour |
|---|---|
| **Check Status** | `POST /api/app/payment-request/manually-add-payment-requests-to-reconciliation-queue` with the selected ids — enqueues a CAS re-check |
| **Approve** / **Decline** | Store the selected ids via `paymentBulkActions.storePaymentIds`, then open the approval modal with `{ cacheKey, isApprove }` |
| **Cancel** | Only rendered when `abp.auth.isGranted('PaymentsPermissions.Payments.CancelPayment')`. Requires exactly one selected row, confirms with `abp.message.confirm`, then calls `paymentRequest.cancel(id)` |
| **History** | Navigates to `/PaymentHistory/Details?PaymentId={id}` |
| **Filter** | Toggles the column filter row |
| **Save As View** / **Update** / **Rename** / **Delete** / **Delete All Views** / **Reset to Default View** | DataTables saved-state management |
| **Export** | CSV of visible, non-`.notexport` columns |

### Why the cache key

Selections can be large, and payment ids in a query string would blow the URL length limit. `PaymentBulkActionsAppService.StorePaymentIdsAsync` writes the id list into the distributed cache under `BulkAction:PaymentRequestIds:{guid}` with a **5-minute TTL** (`PaymentIdsCacheService`) and returns the key. The modal is opened with the key, reads the ids back, and shows "The session has expired. Please select payment requests and try again." if the entry has gone. The same mechanism is used for the tag-selection modal (`PaymentTagAppService.GetListWithCacheKeyAsync`), and the host's equivalent `ApplicationIdsCacheService` carries application ids into the create-payment modals.

## Create payment requests modal

`CreatePaymentRequestsModel` reads the selected application ids from the cache, then per application builds a `PaymentsModel` row with the applicant name, reference number, contract number, the **remaining amount** as the default payment amount, the resolved site (`{siteNumber} ({supplierNumber}, {city})`), the supplier, and the resolved account coding.

`PaymentRequestPageHelperService.GetErrorListAsync` decides whether the row is payable; rows with errors are rendered with their fields disabled and the errors shown inline. `SortByHierarchy` groups child rows under their parent, and `PopulateParentChildValidationDataAsync` stamps the shared parent/child ceiling onto every row of a group so the client can validate live. See [payments-approval-workflow.md](payments-approval-workflow.md#amount-validation-before-creation).

The page also shows the next batch name (`GetNextBatchInfoAsync`) and, for users with `PaymentsPermissions.Payments.AccountCodingOverride`, a batch-wide account coding override dropdown listing every `AccountCoding` with the default marked `(Default)`.

`OnPostAsync` re-validates everything server-side — standalone amounts, parent/child totals, supplier number present, site present, account coding present (unless overridden) — throwing `UserFriendlyException` with the joined messages, then calls `PaymentRequestAppService.CreateAsync`.

`CreateHistoricalPaymentsModel` is the same shape with `isHistorical: true` (supplier/site/account-coding checks relaxed) plus a required **Paid Date**: it must parse as `yyyy-MM-dd` and must not be in the future.

## Approve / decline modal

`UpdatePaymentRequestStatus` reads the ids from the cache and builds a `PaymentsApprovalModel` per payment:

- resolves the applicable threshold per row (`min(form threshold, user threshold)`),
- adds or removes the Level 3 `ExpenseApproval` row when the amount crosses that threshold,
- computes the destination status for display (`GetNextStatus` — including the `FSB` branch when the form prevents payment),
- runs data-annotation validation into `ModelState`,
- drops rows the current user lacks the level permission for.

Surviving rows are grouped by destination status into `PaymentGroupings` so the modal can show "these will go to L2 Pending, these to Submitted". `GetStatusText` / `GetStatusTextColor` provide the display strings and colours (`Submitted` renders as "Submitted to CAS", `FSB` as "Sent to Accounts Payable").

A single **Note** applies to the whole submission. `OnPostAsync` flattens the groupings into `UpdatePaymentStatusRequestDto`s and calls `PaymentRequestAppService.UpdateStatusAsync`.

## View components

All three live under `Views/Shared/Components/` and are ABP `[Widget]`s with their own script/style bundle contributors.

| Widget | Refresh URL | Shows |
|---|---|---|
| `PaymentInfo` | `Widget/PaymentInfo/Refresh` | On the application detail page: requested/recommended/approved amounts, the payment rollup (`TotalPaid`, `TotalPending`), and the remaining amount. Returns an empty model when the feature is off. Also hosts the Flex custom fields anchored at `FlexConsts.PaymentInfoUiAnchor` |
| `SupplierInfo` | `Widget/SupplierInfo/Refresh` | Supplier number/name/status and site management for an applicant. `HasEditSupplierInfo` is `UnitySelector.Payment.Supplier.Update`. Returns an empty model when the feature is off |
| `PaymentActionBar` | `Widget/PaymentActionBar/Refresh` | The Payments list toolbar; takes `showDateRangeFilter`. Its script bundle also pulls in `Pages/PaymentTags/PaymentTags.js` |

`PaymentInfoController` and `SupplierInfoController` back the refresh endpoints. `PaymentInfoAppService.UpdateAsync` is the write side of the PaymentInfo widget's Flex fields: it splits the posted custom fields per worksheet id and publishes `PersistWorksheetIntanceValuesEto` for each, but only when the `Unity.Flex` feature is enabled — see [`flex/flex-integration.md`](../flex/flex-integration.md).

## Configuration surfaces

| Surface | Where | Gated by |
|---|---|---|
| Payment configuration (prefix, default account coding) | `Pages/PaymentConfigurations/Index` + `Index.js`, reached from the host's Configuration Management page | `UnitySettingManagementPermissions.ConfigurePayments` (checked in the host's `ConfigurationManagement/Index.cshtml.cs`) |
| Account coding CRUD | `Pages/AccountCoding/CreateModal`, `UpdateModal` → host `AccountCodingAppService` | `ConfigurePayments` is the delete policy; create/update follow the CRUD service defaults |
| L2 approver thresholds | `Pages/PaymentThresholds/UpdateModal` → host `PaymentThresholdAppService`, list from `PaymentSettingsAppService.GetL2ApproversThresholds()` | `ConfigurePayments` for create/delete |
| Per-form payment configuration | Host `Views/Shared/Components/PaymentConfiguration/` — account coding, approval threshold, `Payable`, `PreventPayment`, form hierarchy, parent form, default pay group | `PaymentsPermissions.Payments.EditFormPaymentConfiguration` |
| Background job cron expressions | Host `Views/Settings/BackgroundJobsSettingGroup/` | Settings management |

## Permission gating summary

| Surface | Gate |
|---|---|
| Payments menu item, all widgets, all payment app services | Feature `Unity.Payments` |
| Create payment requests | `PaymentsPermissions.Payments.RequestPayment` |
| Create historical payments | `PaymentsPermissions.Payments.AddHistoricalPayment` |
| Approve/Decline buttons and modal rows | `L1/L2/L3ApproveOrDecline`, matched to the payment's current status |
| Cancel button and endpoint | `PaymentsPermissions.Payments.CancelPayment` |
| Account coding override on the create modal | `PaymentsPermissions.Payments.AccountCodingOverride` |
| Supplier editing in the Supplier Info widget | `UnitySelector.Payment.Supplier.Update` |
| Assigning / removing payment tags | `UnitySelector.Payment.Tags.Create` / `.Delete` |
| Tag summary and global tag deletion | `UnitySelector.SettingManagement.Tags.Default` / `.Delete` |
| Configuration screens | `UnitySettingManagementPermissions.ConfigurePayments` |
