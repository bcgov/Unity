# Notifications Web UI

The module's own Razor Pages live under `modules/Unity.Notifications/src/Unity.Notifications.Web/Pages/`. Everything an end user does with an *individual* email, however, happens in **host-side** widgets under `src/Unity.GrantManager.Web/` — this module owns the list views and the operations tooling, the host owns the composer.

`NotificationsWebModule` leaves `RazorPagesOptions.Conventions` empty (there is a placeholder `Configure<RazorPagesOptions>` with only a comment), so page authorization here is done with `[Authorize]` attributes on each `PageModel`, not with conventions.

## Module pages

|Page|Purpose|Authorization|
|---|---|---|
|`Notifications/Index.cshtml(.cs)`|**Notification List** — every `EmailLog` in a date window, as a client-side DataTable. Columns: Notification Id (hidden), Submission Id, Applicant Name, Sent Date, Status, From, To, Subject, Recipient, Type. Toolbar: Open (email detail modal), Filter, Export. Date range is persisted to `localStorage` and rejects future dates.|`[Authorize(NotificationsPermissions.NotificationList.View)]`|
|`NotificationLogs/Index.cshtml(.cs)`|**Notification Logs** — the realtime delivery/diagnostics log. Server-side paged. Filters: free text, notification type, severity, channel, tenant, user, date range. Live-updates over SignalR via the `notificationLogCreated` event.|`[Authorize(IdentityConsts.ITOperationsPermissionName)]`|
|`UnityMessaging/Index.cshtml(.cs)`|**Unity Messaging** — the operator console: who is online across tenants, and the send-to-user / send-to-tenant forms.|`[Authorize(IdentityConsts.ITOperationsPermissionName)]` **and** `[RequiresFeature(NotificationsFeatureConsts.DirectMessaging)]`|
|`NotificationsPageModel`|Shared abstract base — sets `LocalizationResourceType = typeof(NotificationsResource)` and `ObjectMapperContext = typeof(NotificationsWebModule)`.|—|

All three `OnGet` methods are empty; the pages are entirely driven by their `Index.js`.

The Notification List runs **client-side** (`serverSideEnabled` off): `NotificationListAppService.GetListAsync` returns every row in the date window and DataTables pages, searches, and sorts in the browser. `TotalCount` is the full match count, not a page count. The source comment explains the choice — it mirrors the Applications list, and the 6-month default date window bounds the payload. Only `EmailLog`-backed columns are sortable server-side; `ResolveSorting` allowlists them and silently falls back to `SentDateTime DESC` for anything else, so client input never reaches the dynamic-LINQ ordering raw.

## Menus

Two contributors, both registered in `NotificationsWebModule`.

### `NotificationsMenuContributor` (main menu)

Runs only when the `Unity.Notifications` feature is enabled and the menu is `StandardMenus.Main`:

|Item|Path|Icon|Order|Required permission|
|---|---|---|---|---|
|`Notifications.NotificationList`|`~/Notifications`|`fl fl-mail`|9|`Notifications.NotificationList.View`|
|`Notifications.NotificationLogs`|`~/NotificationLogs`|`fl fl-table`|10|`ITOperations`|
|`Notifications.UnityMessaging`|`~/UnityMessaging`|`fl fl-users`|11|`ITOperations`|

The third is added only when `DirectMessaging` is also enabled.

### `NotificationLogsUserMenuContributor` (user dropdown)

Adds a second entry point to Notification Logs at order 90 in `StandardMenus.User`, after checking the `Unity.Notifications` feature **and** explicitly resolving `IPermissionChecker` to confirm `ITOperations`. Note that this contributor gates on the `Unity.Notifications` feature, while the main-menu Notification Logs item does too — so a tenant with `DirectMessaging` on but `Unity.Notifications` off sees no logs menu at all, despite the logs being written by the messaging half.

## Settings tab

`NotificationsSettingPageContributor` adds a `GrantManager.Notifications` group to ABP's Setting Management page (order 2), requiring:

- `SettingManagementFeatures.Enable`
- tenant-side feature `Unity.Notifications`
- permission `NotificationsPermissions.Settings` (`SettingManagement.Notifications`)

`NotificationsSettingViewComponent` renders `Views/Settings/NotificationsSettingGroup/Default.cshtml` with a `NotificationsSettingViewModel`:

|Field|Source|Editable|
|---|---|---|
|`MaximumRetryAttempts`|`EmailMaxRetryAttempts` setting, `TryParse` with fallback `3`, `[MaxValue(10)]`|Yes|
|`EnableEmailDelay`|`EnableEmailDelay` setting|Yes|
|`AllowedFileTypes`|`S3:AllowedFileTypes` configuration|Read-only display|
|`MaxFileSize`|`S3:MaxFileSize`|Read-only display|
|`EmailAttachmentMaxFileSize`|`S3:EmailAttachmentMaxFileSize`|Read-only display|
|`TotalEmailAttachmentMaxFileSize`|`S3:EmailAttachmentsTotalMaxFileSize`, default `"25"`|Read-only display|

Saving goes through `EmailNotificationService.UpdateSettings` (`[Authorize(NotificationsPermissions.Settings)]`), which writes both values with `ISettingManager.SetForCurrentTenantAsync`. The retry value is only written when non-blank.

The tab is much larger than its two settings suggest: `Default.js` (~1500 lines) and `InternalEmailGroups.js` (~945 lines) are the admin UI for **email templates**, **template attachments**, **email address configurations**, and **internal email groups and their members** — all of which are managed from this settings group rather than from dedicated pages.

## Bundles

|Contributor|Adds|
|---|---|
|`NotificationsScriptBundleContributor`|`/libs/select2/dist/js/select2.full.js`, `/libs/signalr/browser/signalr.min.js`|
|`NotificationsStyleBundleContributor`|select2 + select2-bootstrap-5-theme CSS, TinyMCE `oxide` `content.css` and `skin.css`|
|`NotificationsSettingScriptBundleContributor` (nested in the view component)|`Default.js`, `InternalEmailGroups.js`, `select2.full.min.js`|
|`NotificationsSettingStyleBundleContributor`|`Default.css`, select2 CSS|

TinyMCE's skin CSS is bundled here because the template editor on the settings tab is a rich-text field.

Standalone assets not in a bundle: `wwwroot/js/notifications-realtime-client.js` and `wwwroot/css/notifications-realtime-widget.css` (the floating messaging widget), plus each page's own `Index.js`/`Index.css`.

## Host-side surfaces

These are in `src/Unity.GrantManager.Web/`, not in this module, but they are the notification UI most users actually touch.

|Surface|What it does|Gating|
|---|---|---|
|`Views/Shared/Components/EmailsWidget/`|The per-application email composer and history list on the application details page. Pre-fills To from `ApplicantAgent.Email`, From from the `DefaultFromAddress` setting, exposes the template dropdown from `TemplateService.GetTemplatesByTenant`, and shows the send-later control when `EnableEmailDelay` is on.|`Notifications.Email.Send`|
|`Views/Shared/Components/Notifications/Default.cshtml`|The form-level **Scheduled & Event Based Notifications** table, with Create/Edit/Cancel actions.|`Notifications.Form.Email.Schedule.Create` for Create/Edit, `.Cancel` for Cancel|
|`Views/Shared/Components/ActionBar/Default.cshtml`|The bulk **Send Email Notification** button and dropdown on the applications list.|`Unity.Notifications` feature **and** `Notifications.Email.SendBulk` **and** `Notifications.Email.Send`|
|`Controllers/NotificationsController` (`/Notifications/EmailModal`)|Renders the read-only email detail modal opened from the Notification List. Fetches history for the application and picks the single matching email; returns `404` if it is not there.|`Notifications.NotificationList.View`|
|`Controllers/FormNotificationsApiController` (`api/form-notifications`)|The scheduled-notification configuration API — see [notifications-scheduled-notifications.md](notifications-scheduled-notifications.md#configuration-api).|`Notifications.Form.Tab` family|
|`Pages/ConfigurationManagement/Index.cshtml.cs`|Sets `ShowNotifications` from the `Unity.Notifications` feature check, hiding the notification configuration section when off.|feature only|

## Localization

`Unity.Notifications.Domain.Shared/Localization/Notifications/en.json` holds all 119 keys for both this module's pages and the host-side notification surfaces, under the `NotificationsResource` resource. `NotificationsDomainSharedModule` registers it with `AddVirtualJson("/Localization/Notifications")` and base type `AbpValidationResource`, and maps the `Notifications` exception-code namespace to it — which is what turns `BusinessException("Unity.Notifications:EmailGroupInUse")` into a localizable message.

Two keys to know about when editing:

- `Permission:Notifications.Form.Email.Schedule.Delete` is defined but has **no** matching permission constant — a leftover.
- `Setting:Notifications.Mailing.DefaultFromAddress.DisplayName`/`.Description` do not match the key the provider actually requests (`Setting:GrantManager.Notifications.Mailing.DefaultFromAddress.DisplayName`), so the setting renders with its raw key as the label. See [notifications-roadmap.md](notifications-roadmap.md#setting-display-names-dont-resolve).
