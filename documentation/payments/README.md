# Unity.Payments Module Documentation

Unity.Payments is Unity Portal's **outbound money** module. It turns an approved grant application into a payment request, walks that request through a multi-level expense-approval workflow, and — for tenants that pay through the province's **CAS** (Corporate Accounting System) — creates the AP invoice in CAS and reconciles its status back into Unity. For tenants whose forms are flagged to bypass CAS, it instead hands the batch to the Financial Services Branch as a spreadsheet by email.

It also owns everything the payment needs to be valid: the **supplier** and **site** records mirrored from CAS, the **account coding** (the GL distribution string), the **approval thresholds** that decide how many levels of sign-off a payment needs, and the payment-request **tags** and notes staff use to track work.

This folder documents how the module is built and how it is used. Read in this order:

1. **[payments-overview.md](payments-overview.md)** — what problem it solves, the four big architectural facts, the unusual project layout, feature/permission gating, external dependencies, core concepts glossary.
2. **[payments-domain-model.md](payments-domain-model.md)** — entities, enums, the `Payments` database schema, repositories, permissions, settings, and error codes.
3. **[payments-approval-workflow.md](payments-approval-workflow.md)** — the `PaymentRequestStatus` state machine, `ExpenseApproval` levels, threshold resolution, separation of duties, cancellation, and the FSB branch.
4. **[payments-cas-integration.md](payments-cas-integration.md)** — CAS authentication, the invoice-creation path over RabbitMQ, the nightly reconciliation loop, and how CAS status strings land on the payment request.
5. **[payments-suppliers-and-sites.md](payments-suppliers-and-sites.md)** — how supplier and site records are pulled from CAS, upserted through a local event, and how the default site and pay group are resolved.
6. **[payments-notifications.md](payments-notifications.md)** — the FSB spreadsheet notification, the nightly failed-payment summary, the email-recipient strategies, and the round trip that stamps `FsbApNotified` back on the payment.
7. **[payments-web-ui.md](payments-web-ui.md)** — the Payments list page and its saved views, the create/approve/cancel modals, the application-detail widgets, configuration screens, and permission gating on each surface.
8. **[payments-roadmap.md](payments-roadmap.md)** — known rough edges: dead code, unbounded queries, batch-number races, and other gaps worth knowing before extending this module.

## Source location

```
applications/Unity.GrantManager/modules/Unity.Payments/
├── src/
│   ├── Unity.Payments.Shared/               localization (PaymentsResource, en.json), PaymentsPermissions,
│   │                                          HtmlTable helper
│   ├── Unity.Payments.Application.Contracts/ DTOs, app service interfaces, enums (PaymentRequestStatus,
│   │                                          ExpenseApproval*, PaymentGroup), CAS wire types, setting keys
│   ├── Unity.Payments.Application/           EVERYTHING ELSE — app services, domain entities and managers,
│   │                                          the EF Core context and repositories, CAS clients, RabbitMQ
│   │                                          producer/consumers, Quartz workers, FSB notifier, permissions
│   └── Unity.Payments.Web/                   Razor Pages, view components, menus, page-helper service
└── test/
    ├── Unity.Payments.TestBase/
    └── Unity.Payments.Application.Tests/     domain, repository, rollup, account-coding, view-component tests
```

Note the layout: unlike `Unity.Flex`, `Unity.Notifications`, or `Unity.TenantManagement`, this module has **no separate `.Domain`, `.Domain.Shared`, or `.EntityFrameworkCore` projects**. The domain model lives in `Unity.Payments.Application/Domain/` and the persistence layer in `Unity.Payments.Application/EntityFrameworkCore/`. See [payments-overview.md](payments-overview.md#module-layout-and-dependency-direction).

Host-side pieces that this module cannot own live in `applications/Unity.GrantManager/`:

| Host file | Role |
|---|---|
| `src/Unity.GrantManager.Application/Payments/AccountCodingAppService.cs` | CRUD over the module's `AccountCoding` entity |
| `src/Unity.GrantManager.Application/Payments/PaymentThresholdAppService.cs` | CRUD over the module's `PaymentThreshold` entity |
| `src/Unity.GrantManager.Application/Payments/PaymentSettingsAppService.cs` | Resolves an application's account coding; lists L2 approvers and their thresholds |
| `src/Unity.GrantManager.Application/ApplicantProfile/Payments/ApplicantPaymentsAppService.cs` | Applicant-level payment summary and list |
| `src/Unity.GrantManager.Application/ApplicantProfile/DataProviders/PaymentInfoDataProvider.cs` | Payment section of the applicant-portal profile endpoint |
| `src/Unity.GrantManager.Application/Applicants/ApplicantSupplierAppService.cs` | Links an applicant to a supplier and a default site |
| `src/Unity.GrantManager.Domain.Shared/Payments/PaymentConsts.cs` | `Unity.Payments` feature name and correlation-provider constants |
| `src/Unity.GrantManager.EntityFrameworkCore/EntityFrameworkCore/GrantTenantDbContext.cs` | Calls `modelBuilder.ConfigurePayments()` — payments tables ship in the tenant migrations |
| `src/Unity.GrantManager.Web/Views/Shared/Components/PaymentConfiguration/` | Per-form payment configuration widget (account coding, threshold, `PreventPayment`, hierarchy) |

The module also reaches into `Unity.Notifications` to send email (`EmailNotificationEvent`) and into `Unity.GrantManager` for applications, forms, and application links — see [payments-overview.md](payments-overview.md#the-module-depends-on-the-host).
