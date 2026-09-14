# Unity.Notifications Module Documentation

Unity.Notifications is Unity Portal's **outbound communication** module. It covers two largely independent concerns that share a project and a database schema:

- **Email** — composing, templating, scheduling, queueing, sending (through the BC Government **CHES** API), retrying, and auditing every message the system sends to applicants and internal staff, including S3-backed attachments.
- **In-app realtime messaging and notification logging** — a SignalR hub that tracks who is online, delivers operator-to-user and operator-to-tenant messages, and persists every delivery as a `NotificationLog` row for IT Operations to review.

This folder documents how the module is built and how it is used. Read in this order:

1. **[notifications-overview.md](notifications-overview.md)** — what problem it solves, the two-halves architecture, module layout and dependency direction, feature/permission gating, core concepts glossary.
2. **[notifications-domain-model.md](notifications-domain-model.md)** — entities, enums, the `Notifications` database schema, repositories, permissions, features, and settings.
3. **[notifications-email-pipeline.md](notifications-email-pipeline.md)** — the central flow: `EmailNotificationEvent` → handler → `EmailLog` → RabbitMQ → `EmailConsumer` → CHES, with status transitions, retry policy, and delayed/scheduled sends.
4. **[notifications-attachments.md](notifications-attachments.md)** — the S3 attachment lifecycle: upload, template copy vs. replace, existence validation, shared-object-safe deletion.
5. **[notifications-scheduled-notifications.md](notifications-scheduled-notifications.md)** — the host-owned `ScheduledNotification` configuration, its event-driven and date-driven triggers, template token substitution, and recipient resolution.
6. **[notifications-realtime.md](notifications-realtime.md)** — `NotificationHub`, presence tracking, direct/tenant messaging, the notification log, read state, the Redis backplane, and the API-key broadcast endpoints.
7. **[notifications-web-ui.md](notifications-web-ui.md)** — Razor Pages inventory, menus, the settings tab, the host-side email widget, and permission gating on each surface.
8. **[notifications-roadmap.md](notifications-roadmap.md)** — known rough edges: dead code, an in-process delay used as a retry backoff, log channels declared but never written, and other gaps worth knowing before extending this module.

## Source location

```
applications/Unity.GrantManager/modules/Unity.Notifications/
├── src/
│   ├── Unity.Notifications.Domain.Shared/       enums (EmailAction/Type, RecipientType, NotificationLog*),
│   │                                             feature consts, localization
│   ├── Unity.Notifications.Domain/              entities, repository interfaces, settings definitions,
│   │                                             NotificationReadStateManager, data seeding
│   ├── Unity.Notifications.Application.Contracts/  DTOs, app service interfaces, permission definitions
│   ├── Unity.Notifications.Application/         EmailNotificationManager/Service, EmailAttachmentService,
│   │                                             CHES client, RabbitMQ producer/consumer, the local event
│   │                                             handler, templates, notification-log app services
│   ├── Unity.Notifications.EntityFrameworkCore/ model-creating extension + custom repositories
│   ├── Unity.Notifications.Web/                 Razor Pages, SignalR hub + presence, controllers, menus,
│   │                                             settings view component, realtime client JS
│   ├── Unity.Notifications.HttpApi/             abstract base controller only
│   ├── Unity.Notifications.HttpApi.Client/      generated client proxy
│   └── Unity.Notifications.Installer/
└── test/
    ├── Unity.Notifications.TestBase/
    ├── Unity.Notifications.Application.Tests/   (EmailAttachmentServiceTests)
    ├── Unity.Notifications.Domain.Tests/
    └── Unity.Notifications.EntityFrameworkCore.Tests/
```

The module's entities are **not** persisted through `NotificationsDbContext` at runtime. `GrantTenantDbContext.OnModelCreating` calls `modelBuilder.ConfigureNotifications()`, so notification tables live in the per-tenant database alongside the rest of the tenant schema — see [notifications-domain-model.md](notifications-domain-model.md#persistence-two-dbcontexts-one-of-which-is-the-real-one).

Host-side pieces that this module cannot own live in `applications/Unity.GrantManager/src/Unity.GrantManager.Application/`:
`Notifications/EmailAppService.cs` and `NotificationListAppService.cs`, `Events/ScheduledNotificationEventHandler.cs`, `Events/ScheduledNotificationHelper.cs`, `Events/DateBasedScheduledNotificationJob.cs`, `GrantApplications/BulkEmailNotificationAppService.cs`, and the `ScheduledNotification` entity in `Unity.GrantManager.Domain/Notifications/`. `Unity.Payments` publishes into the same pipeline from `modules/Unity.Payments/src/Unity.Payments.Application/PaymentRequests/Notifications/FsbPaymentNotifier.cs`.

A related one-page visual summary is in `documentation/handover/notifications-module-handover.html`.
