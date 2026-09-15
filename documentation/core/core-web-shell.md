# Core — Web Shell

`Unity.GrantManager.Web` is 15,000 lines across 226 files, and almost none of it is business logic. It is the shell: module composition, the request pipeline, menus, 36 shared view components, and the tag helpers that make zone-gated markup possible.

## `GrantManagerWebModule`

At 738 lines this is the largest single file in the core, and it is the assembly point for the whole application — every module, every ABP option, and the middleware pipeline.

### Configuration blocks

| Configured | Notable settings |
|---|---|
| `AbpMvcDataAnnotationsLocalizationOptions` | Resource assemblies for every module |
| `TokenCleanupOptions` | OpenIddict token cleanup |
| `DbWarmupOptions` | Bound from the `DbWarmup` section — see [core-persistence.md](core-persistence.md#startup-warm-up) |
| `ForwardedHeadersOptions` | Trusted networks, for running behind the OpenShift router |
| `AbpAntiForgeryOptions` | CSRF |
| `AbpClockOptions` | Clock kind |
| `AbpAuditingOptions`, `AbpAspNetCoreAuditingOptions`, `AbpSecurityLogOptions` | What is audited and what is not |
| `AbpErrorPageOptions` | Error page behaviour |
| `SettingManagementPageOptions` | Configured twice — the core's own groups and the modules' |
| `AbpClaimsPrincipalFactoryOptions` | Unity's additional claims |
| `AbpBundlingOptions` | Script and style bundles |
| `AbpVirtualFileSystemOptions` | Embedded resources |
| `AbpNavigationOptions` | Menu contributors |
| `AbpAspNetCoreMvcOptions` | Conventional controllers — including re-registering module assemblies |
| `AbpLayoutHookOptions` | Layout hooks |
| `AppUrlOptions` | Self and portal URLs |

### The request pipeline

`OnApplicationInitialization` in order:

```text
UseForwardedHeaders
UseAbpRequestLocalization
UseStatusCodePagesWithReExecute("/Error") + UseErrorPage     (or UseDeveloperExceptionPage)
UseCookiePolicy
UseCorrelationId
UseStaticFiles
UseMiddleware<RequestCancellationMiddleware>
UseMiddleware<ExceptionCounterMiddleware>
UseRouting
UseHttpMetrics                       ← Prometheus
UseAuthentication
UseMultiTenancy
UseUnitOfWork
UseDynamicClaims
UseAuthorization
UseMiddleware<TimezoneMiddleware>
UseMiniProfiler                      ← only when profiling is allowed
UseSwagger + UseAbpSwaggerUI
UseAuditing
UseAbpSerilogEnrichers
UseMiddleware<OnboardingRedirectMiddleware>
UseConfiguredEndpoints  → MapMetrics().RequireAuthorization(MetricsAccessPolicy)
UseAbpRequestLocalization(en-CA only)
UseSession                           ← only when Redis AND DataProtection are enabled
```

Four things are worth pulling out:

- **`en-CA` is the only supported culture.** Both supported and UI cultures are that single value.
- **The metrics endpoint is authorized**, not open — `PolicyRegistrant.MetricsAccessPolicy`.
- **Session middleware is conditional** on both `Redis:IsEnabled` and `DataProtection:IsEnabled`; without both, there is no session.
- **`OnboardingRedirectMiddleware` runs last**, after routing and authorization, so it can redirect an authenticated user into the onboarding flow — see [`tenant-management/`](../tenant-management/README.md).

## Custom middleware

`Web/Middleware/` holds eleven files, most of them concerned with exceptions.

| Component | Role |
|---|---|
| `RequestCancellationMiddleware` | Handles client-cancelled requests without logging them as errors |
| `ExceptionCounterMiddleware` (329 lines) | Counts exceptions that **bypass** ABP's handling, for metrics and alerting |
| `AbpExceptionNotificationSubscriber` (374 lines) | An `IExceptionSubscriber` — ABP calls it for every exception it handles, complementing the counter middleware |
| `ExceptionNotificationThrottle` | Per-exception-type cooldowns plus a global cap of **5 notifications per minute**, to prevent alert storms during an outage |
| `ExceptionNotificationHelpers` | Message formatting |
| `GitHubBlameLookupService` / `IBlameLookupService` | Looks up who last changed the failing line, via GitHub GraphQL |
| `ErrorCountingLoggerSink` | A Serilog sink feeding error counts |
| `TimezoneMiddleware` | Establishes the request timezone |
| `OnboardingRedirectMiddleware` | Redirects users into tenant onboarding |
| `AbpUserTenantAccessor` | Static helper to resolve the current tenant name — also used by the Payments module when building CAS invoice descriptions |

The two exception paths are complementary and both are needed: ABP swallows and handles most exceptions before they reach middleware, so the subscriber sees those; anything thrown outside ABP's pipeline is only seen by the counter middleware.

## Menus

`GrantManagerMenuContributor` builds the main menu; `GrantManagerMenus` holds the names. Each module contributes its own — `PaymentsMenuContributor`, `AIMenuContributor`, `NotificationsMenuContributor` — all registered through `AbpNavigationOptions`.

The conventions across contributors: feature-check before adding an item, `requiredPermissionName` on the item, and `.OnlyWhenInRole(...)` for the operations-only areas.

## Pages

`Web/Pages/` — the staff-facing application:

| Area | Pages |
|---|---|
| Applications | `GrantApplications/` (detail and list), `ApplicationLinks/`, `ApplicationTags/`, `ApplicationContact/`, `AssigneeSelection/`, `StatusUpdate/`, `Attachments/` |
| Bulk operations | `BulkActions/`, `BulkApprovals/`, `BulkEmailNotifications/` |
| Applicants | `Applicants/`, `ApplicantContact/`, `ApplicantHistory/` |
| Configuration | `ConfigurationManagement/`, `FormConfiguration/`, `ApplicationForms/`, `GrantPrograms/`, `Intakes/`, `Sites/`, `Tags/`, `SettingManagement/`, `ApplicantPortalSettings/` |
| Operations | `ExceptionLogs/`, `UnityAdmin/`, `Dashboard/` |
| Payments | `PaymentHistory/` |

`GrantManagerPageModel` is the shared base; `PageSection.cs` supports sectioned page composition.

The application **detail** page (`GrantApplications/Details.cshtml`) is the centre of the product — it hosts the zone-driven tabs, the AI analysis tab, and most of the shared widgets.

## Shared view components

`Web/Views/Shared/Components/` — 36 of them. They fall into groups:

| Group | Components |
|---|---|
| Application | `ApplicationActionWidget`, `ApplicationStatusWidget`, `ApplicationTagsWidget`, `ApplicationLinksWidget`, `ApplicationContactsWidget`, `ApplicationAttachments`, `ApplicationFormConfigWidget`, `ApplicationBreadcrumbWidget`, `SummaryWidget` |
| Applicant | `ApplicantInfo`, `ApplicantOrganizationInfo`, `ApplicantAddresses`, `ApplicantContacts`, `ApplicantAttachments`, `ApplicantSubmissions`, `ApplicantHistory`, `ApplicantPayments`, `ApplicantBreadcrumbWidget`, `ApplicantsActionBar` |
| Assessment | `AssessmentResults`, `AssessmentResultAttachments`, `AssessmentScoresWidget`, `ReviewList` |
| Project & funding | `ProjectInfo`, `FundingAgreementInfo` |
| Payments | `PaymentConfiguration` (core-owned, renders Payments concepts) |
| Collaboration | `CommentsWidget`, `HistoryWidget`, `EmailsWidget`, `EmailHistoryWidget`, `Notifications` |
| Other | `ActionBar`, `DetailsActionBar`, `CustomFields`, `ChefsAttachments`, `UserInfoWidget` |

Modules add more into the same screens — `PaymentInfo`, `SupplierInfo`, `PaymentActionBar` from Payments; `AIConfiguration` and `AIPromptsWidget` from AI; Flex's custom-field components.

Most are ABP `[Widget]`s with their own script and style bundle contributors and a `RefreshUrl`, so a widget can reload itself without a page round trip.

## Tag helpers

`Web/TagHelpers/Zone/` is the important one — `<zone>` and `<zone-fieldset>`, gating content on four conditions at once. Fully described in [core-zones-and-ui-config.md](core-zones-and-ui-config.md#the-zone-tag-helper).

`Utilities/HtmlHelperExtensions.cs` holds the remaining view helpers.

## Controllers

`Web/Controllers/` is small — most API surface is ABP's dynamic proxies over app services:

| Controller | Role |
|---|---|
| `FormNotificationsApiController` (526 lines) | Form-level notification configuration |
| `NotificationsController` | Notification actions |
| `Monitoring/` | Health and metrics endpoints |

## Theming and assets

The theme is `Unity.Theme.UX2` (`modules/`), layered on ABP's LeptonX-style theming. `ConfigureBundles` composes the script and style bundles; `ConfigureVirtualFileSystem` maps embedded resources in development so a change in a module's static files is picked up without a rebuild.

Client-side packages are managed with `package.json` plus `abp.resourcemapping.js` and installed with `abp install-libs` — see the JavaScript conventions in `.claude/rules/javascript.md`.

## Swagger

`Web/Swagger/ApplicantProfileDataSchemaFilter.cs` and `Web/Filters/ApiTokenAuthorizationHeaderParameter.cs` shape the generated OpenAPI document — the latter adding the API-token header used by the applicant portal. Swagger is served at `/swagger` in all environments.

## Profiling

MiniProfiler is registered but only enabled when `IsProfilingAllowed(env, configuration)` passes, so it is opt-in per environment rather than development-only.
