# Core — Comments and Tags

Two lightweight collaboration features that attach to several different subjects. Both are small, and both are shared with modules — which is the part worth knowing.

## Comments

Staff discussion threads on an application, an assessment or an applicant.

### One base, three subjects

```csharp
public abstract class CommentBase : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public string Comment { get; set; } = string.Empty;
    public Guid CommenterId { get; set; }
    public Guid? TenantId { get; set; }
    public DateTime? PinDateTime { get; set; }
}
```

| Subclass | Adds |
|---|---|
| `ApplicationComment` | `ApplicationId` |
| `AssessmentComment` | `AssessmentId` |
| `ApplicantComment` | `ApplicantId` |

Each is a separate table with its own repository (`ICommentsRepository<T>`), rather than one polymorphic table with a discriminator.

`PinDateTime` is how a comment is pinned to the top of a thread — non-null means pinned, and the timestamp orders multiple pins.

### `CommentsManager`

The domain service takes a `CommentType` discriminator and dispatches to the right repository, so callers work with one API across all three subjects:

| Method | Notes |
|---|---|
| `CreateCommentAsync(ownerId, comment, type)` | |
| `GetCommentsAsync(ownerId, type)` | Raw entities |
| `GetCommentsDisplayListAsync(ownerId, type)` | Projects to `CommentListItem`, resolving commenter names |
| `GetCommentAsync(ownerId, commentId, type)` | |
| `UpdateCommentAsync(ownerId, commentId, comment, type)` | |
| `DeleteCommentAsync(ownerId, commentId, type)` | |
| `PinCommentAsync(ownerId, commentId, type)` | |

Every method takes `ownerId` alongside `commentId` — the owner is part of the lookup, not just context, so a comment id from one application cannot be used to reach a comment on another.

`CommentListItem` is the display projection: the text, `CommenterDisplayName` and `CommenterBadge`, timestamps, and the pin state. Name resolution happens here rather than in the UI.

### @-mention notifications

Mentioning a colleague emails them. That path goes through the Notifications module's `EmailNotificationService.SendCommentNotification`, which is one of only two paths in the product that **bypass the queued email pipeline** and call CHES directly — the other being exception alerts.

It renders an embedded `CommentNotification` template by string replacement rather than through the `EmailTemplate` mechanism, and includes a deep link back to the comment. See [`notifications/notifications-overview.md`](../notifications/notifications-overview.md#the-one-path-that-skips-all-of-it).

### Where comments surface

The `CommentsWidget` view component, on the application, assessment and applicant screens.

## Tags

Free-form labels applied to applications and payments.

### One tag table, two join tables

```text
Tag  (GrantTenant.Tags)
 │      Name, TenantId
 ├── ApplicationTags   → ApplicationId + TagId     (core)
 └── PaymentTags       → PaymentRequestId + TagId  (Unity.Payments)
```

The `Tag` table is **shared**. This is why `PaymentsDbContextModelCreatingExtensions.ConfigurePayments()` re-declares the core's `Tag` entity mapping — so the `PaymentTag → Tag` foreign key resolves from the Payments context as well as the tenant context. See [`payments/payments-domain-model.md`](../payments/payments-domain-model.md#schema).

`Tags.cs` also declares a second, near-identical `Tags` aggregate alongside `Tag`. Only `Tag` is referenced by the join tables.

`TagUsageSummary` is the cross-cutting projection:

```csharp
public int ApplicationTagCount { get; set; }
public int PaymentTagCount { get; set; }
public int TotalUsageCount => ApplicationTagCount + PaymentTagCount;
```

That is the shape the tag-management screen renders — a tag's usage counted across both modules.

### Deleting a tag is an event, not a cascade

Because a tag is referenced from two modules, deleting one cannot be a foreign-key cascade. `TagsAppService` publishes instead:

```text
TagsAppService.DeleteTagAsync / delete-with-usage
        ↓
publish DeleteTagEto { TagId }        (local event bus)
        ↓
Payments  DeleteTagHandler  → PaymentTagAppService.DeleteTagWithTagIdAsync
                            → publish TagDeletedEto
        ↓
core      TagDeletedHandler → clean up application tags
```

Two event types, deliberately distinct: `DeleteTagEto` is the **request** to remove a tag's usages, `TagDeletedEto` the **confirmation** that a module has done so. Both are declared in `Unity.Payments.Application/Events/`, with handlers on both sides.

### Permissions

Tag administration is gated by the `UnitySelector.SettingManagement.Tags` subtree:

| Operation | Permission |
|---|---|
| `CreateTagsAsync` | `SettingManagement.Tags.Create` |
| `RenameTagAsync` | `SettingManagement.Tags.Update` |
| `DeleteTagAsync` | `SettingManagement.Tags.Delete` |
| `GetTagSummaryAsync` | `SettingManagement.Tags.Default` |

*Applying* a tag is gated separately, per subject:

| Subject | Permission |
|---|---|
| Applications | `UnitySelector.Application.Tags.Create` / `.Delete` — note the distinct namespace `Unity.Applications.Tags.*` |
| Payments | `UnitySelector.Payment.Tags.Create` / `.Delete` — `Unity.Payments.Tags.*`, and `.RequireFeatures("Unity.Payments")` |

So a user can be allowed to tag applications without being allowed to create new tags, and vice versa.

`RenameTagAsync(id, originalTag, replacementTag)` returns the list of affected ids, since a rename touches every usage across both modules.

### Where tags surface

- **Tag management** (`Web/Pages/Tags/`, `Views/Settings/TagManagement/`) — create, rename, delete, and the usage summary.
- **Application tags** — the `ApplicationTagsWidget` and the `ApplicationTags` pages.
- **Payment tags** — the Payments list column and its selection modal, documented in [`payments/payments-web-ui.md`](../payments/payments-web-ui.md).
