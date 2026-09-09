# Core — Attachments

Documents reach Unity two ways: attached to a CHEFS submission at intake, or uploaded by staff afterwards. Those two paths store their content in different places and are modelled by different entity hierarchies.

## Two storage paths

```text
CHEFS attachments                       Unity attachments
────────────────────                    ──────────────────
uploaded by the applicant in CHEFS      uploaded by staff in Unity
content stays in CHEFS                  content stored in S3
ApplicationChefsFileAttachment          ApplicationAttachment
  ChefsSubmissionId, ChefsFileId          AbstractS3Attachment
  AISummary                             AssessmentAttachment
                                        ApplicantAttachment
fetched on demand via                   read/written via
ChefsAttachmentDownloadService          S3BlobProvider
```

`AttachmentType` (`Domain.Shared/Attachments/`) names all five kinds:

```csharp
public enum AttachmentType { APPLICATION = 0, ASSESSMENT = 1, CHEFS = 2, EMAIL = 3, APPLICANT = 4 }
```

`EMAIL` belongs to the Notifications module, which stores its own attachments in S3 under a different container — see [`notifications/notifications-attachments.md`](../notifications/notifications-attachments.md).

## The entity hierarchy

Two abstract bases split the model by where the bytes live:

| Base | Subclasses | Content |
|---|---|---|
| `AbstractAttachmentBase` | `ApplicationChefsFileAttachment` | Remote — CHEFS holds the file |
| `AbstractS3Attachment` | `ApplicationAttachment`, `AssessmentAttachment`, `ApplicantAttachment` | S3 |

Each subclass overrides `AttachmentType` and adds its owner key (`ApplicationId`, `AssessmentId`, `ApplicantId`).

`ApplicationChefsFileAttachment` carries two extra fields worth knowing:

- `ChefsSubmissionId` / `ChefsFileId` — the coordinates needed to fetch the content back from CHEFS.
- **`AISummary`** — where the AI module writes the attachment summary. See [`ai/ai-operations.md`](../ai/ai-operations.md#attachment-summary).

## S3

`S3BlobProvider` (330 lines) extends ABP's `BlobProviderBase`, so attachments go through ABP's `IBlobContainer` abstraction rather than a bespoke API. The container is named by attribute:

```csharp
[BlobContainerName("unity-s3-container")]
public class S3Container { }
```

`S3BlobProviderConfiguration` and `S3BlobContainerConfigurationExtensions` supply endpoint, credentials and bucket. As in the Notifications module, this is an **S3-compatible store** rather than AWS proper — path-style addressing, no AWS region.

The provider implements the four `BlobProviderBase` operations — `SaveAsync`, `GetOrNullAsync`, `ExistsAsync`, `DeleteAsync` — and is unusual in that it injects the three attachment repositories directly. It resolves which repository an operation concerns from the blob name, so a save or delete updates the metadata row and the object together.

`UploadToS3(args, bucket, key, mimeType)` is the exposed lower-level entry point.

## Preview and conversion

Staff can preview an attachment without downloading it. `AttachmentPreviewAppService` (158 lines) serves the preview; anything that is not already viewable in a browser is converted to PDF first.

`LibreOfficeConversionService` does the conversion by shelling out to a local binary:

```csharp
private const string LibreOfficeBinary = "/usr/bin/libreoffice";
```

`ConvertToPdfAsync(fileContent, fileName)` writes the bytes to a temp directory, invokes LibreOffice with `pdf` as the conversion target, reads back `{name}.pdf`, and throws if the expected output file is absent. `IsInstalled()` lets callers degrade gracefully when the binary is missing — which it will be in any environment whose container image does not include LibreOffice.

This is the only place in the product that shells out to an external process.

## Services

| Service | Role |
|---|---|
| `AttachmentAppService` (219 lines) | List, upload, rename and delete across all four Unity attachment types |
| `AttachmentPreviewAppService` (158) | Preview, converting to PDF where needed |
| `FileAppService` (46) | Thin file operations |
| `ChefsAttachmentDownloadService` (`Intakes/`) | Fetches CHEFS-held content on demand |
| `S3BlobProvider` (330) | The ABP blob provider |
| `LibreOfficeConversionService` (130) | PDF conversion |

## `AttachmentController`

At **769 lines** this is the largest controller in the product, and the one place attachments cross the HTTP boundary for both staff and the applicant portal. It handles upload, download, preview and deletion across all attachment types, and is where content-type and content-disposition handling lives.

Because it serves the portal as well as the staff UI, its authorization is per-endpoint rather than class-wide — see [`applicant-portal/applicant-portal-integration.md`](../applicant-portal/applicant-portal-integration.md).

## Attachment resync

CHEFS attachment metadata can drift from what CHEFS actually holds. `IntakeFormSubmissionManager.ResyncSubmissionAttachments(applicationId)` re-pulls it, validating carefully at every step and refusing to touch anything unless the whole chain resolves — every failure message ends *"existing attachments are unchanged"*. See [core-intake.md](core-intake.md#attachment-resync).

## Where attachments surface

| Widget | Shows |
|---|---|
| `ChefsAttachments` | The submission's CHEFS files, with AI summaries when generated |
| `ApplicationAttachments` | Staff-uploaded application files |
| `AssessmentResultAttachments` | Files attached to an assessment result |
| `ApplicantAttachments` | Files on the applicant record |

Plus the `Attachments` pages under `Web/Pages/Attachments/`.
