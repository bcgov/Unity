using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GrantManager.GrantApplications;
using Unity.Notifications.EmailGroups;
using System.Threading.Tasks;
using Unity.Notifications.Emails;
using Volo.Abp.Users;
using Unity.GrantManager.Events;
using Volo.Abp.Identity.Integration;

namespace Unity.GrantManager.Web.Controllers
{
    [Route("api/form-notifications")]
    [ApiController]
    public class FormNotificationsApiController : ControllerBase
    {
        private readonly IApplicationStatusService _statusService;
        private readonly IEmailGroupsAppService _emailGroupsAppService;
        private readonly Unity.Notifications.Templates.ITemplateService _templateService;
        private readonly Notifications.IAutomatedNotificationAppService _automatedNotificationAppService;
        private readonly IEmailLogAttachmentRepository _emailLogAttachmentRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IEmailGroupUsersAppService _emailGroupUsersAppService;
        private readonly IIdentityUserIntegrationService _identityUserIntegrationService;
        private readonly IGrantApplicationAppService _grantApplicationAppService;
        private readonly ScheduledNotificationHelper _scheduledNotificationHelper;

        public FormNotificationsApiController(IApplicationStatusService statusService, IEmailGroupsAppService emailGroupsAppService, Unity.Notifications.Templates.ITemplateService templateService, Unity.GrantManager.Notifications.IAutomatedNotificationAppService automatedNotificationAppService, IEmailLogAttachmentRepository emailLogAttachmentRepository, ICurrentUser currentUser, IEmailGroupUsersAppService emailGroupUsersAppService, IIdentityUserIntegrationService identityUserIntegrationService, IGrantApplicationAppService grantApplicationAppService, ScheduledNotificationHelper scheduledNotificationHelper)
        {
            _statusService = statusService;
            _emailGroupsAppService = emailGroupsAppService;
            _templateService = templateService;
            _automatedNotificationAppService = automatedNotificationAppService;
            _emailLogAttachmentRepository = emailLogAttachmentRepository;
            _currentUser = currentUser;
            _emailGroupUsersAppService = emailGroupUsersAppService;
            _identityUserIntegrationService = identityUserIntegrationService;
            _grantApplicationAppService = grantApplicationAppService;
            _scheduledNotificationHelper = scheduledNotificationHelper;
        }
        // In-memory storage removed; persisting to ScheduledNotifications table via IAutomatedNotificationAppService


        [HttpGet("templates")]
        public async Task<ActionResult<List<EmailTemplateDto>>> GetTemplates()
        {
            var templates = await _templateService.GetTemplatesByTenant();
            var list = templates.Select(t => new EmailTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Subject = t.Subject,
                Body = t.BodyHTML,
                SendFrom = t.SendFrom,
                RecipientCategory = t.RecipientCategory,
                RecipientIdentifier = t.RecipientIdentifier
            }).ToList();

            return Ok(list);
        }

        [HttpGet("templates/{templateId:guid}/resolved-recipients")]
        public async Task<ActionResult<ResolvedRecipientsDto>> GetResolvedTemplateRecipients(Guid templateId, [FromQuery] Guid? applicationId = null)
        {
            var template = await _templateService.GetTemplateById(templateId);
            if (template == null)
            {
                return NotFound();
            }

            var recipientCategory = template.RecipientCategory?.Trim();
            var recipientIdentifier = template.RecipientIdentifier?.Trim();
            if (string.IsNullOrWhiteSpace(recipientCategory) || string.IsNullOrWhiteSpace(recipientIdentifier))
            {
                return Ok(new ResolvedRecipientsDto { EmailTo = string.Empty });
            }

            if (string.Equals(recipientCategory, "Internal", StringComparison.OrdinalIgnoreCase))
            {
                var notification = new Notifications.ScheduledNotification
                {
                    RecipientCategory = recipientCategory,
                    RecipientIdentifier = recipientIdentifier
                };

                var emailTo = await _scheduledNotificationHelper.GetInternalRecipientEmailAddressesAsync(
                    notification,
                    _emailGroupsAppService,
                    _emailGroupUsersAppService,
                    _identityUserIntegrationService);

                return Ok(new ResolvedRecipientsDto { EmailTo = emailTo });
            }

            if (string.Equals(recipientCategory, "External", StringComparison.OrdinalIgnoreCase))
            {
                if (!applicationId.HasValue || applicationId.Value == Guid.Empty)
                {
                    return Ok(new ResolvedRecipientsDto { EmailTo = string.Empty });
                }

                var application = await _grantApplicationAppService.GetAsync(applicationId.Value);
                var identifiers = recipientIdentifier
                    .Split(',')
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var emailAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var identifier in identifiers)
                {
                    if (identifier.Contains('@'))
                    {
                        emailAddresses.Add(identifier);
                        continue;
                    }

                    if (string.Equals(identifier, "ApplicationContact", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(application.ContactEmail))
                    {
                        emailAddresses.Add(application.ContactEmail);
                        continue;
                    }

                    if (string.Equals(identifier, "SigningAuthority", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(application.SigningAuthorityEmail))
                    {
                        emailAddresses.Add(application.SigningAuthorityEmail);
                    }
                }

                return Ok(new ResolvedRecipientsDto { EmailTo = string.Join("; ", emailAddresses) });
            }

            return Ok(new ResolvedRecipientsDto { EmailTo = string.Empty });
        }

        [HttpPost("email-template/{templateId}/copy-attachments")]
        public async Task<ActionResult<CopyAttachmentsResponseDto>> CopyTemplateAttachments(Guid templateId, [FromBody] CopyAttachmentsInput input)
        {
            if (input.EmailLogId == Guid.Empty)
                return BadRequest("EmailLogId is required");

            // Get all attachments for the template
            var templateAttachments = await _emailLogAttachmentRepository.GetByTemplateIdAsync(templateId);

            if (templateAttachments.Count == 0)
            {
                return Ok(new CopyAttachmentsResponseDto { AttachmentCount = 0 });
            }

            // Copy attachments to the email log
            int copiedCount = 0;
            foreach (var templateAttachment in templateAttachments)
            {
                var newAttachment = new EmailLogAttachment
                {
                    EmailLogId = input.EmailLogId,
                    TemplateId = null,
                    OriginTemplateId = templateId,
                    S3ObjectKey = templateAttachment.S3ObjectKey,
                    FileName = templateAttachment.FileName,
                    DisplayName = templateAttachment.DisplayName,
                    ContentType = templateAttachment.ContentType,
                    FileSize = templateAttachment.FileSize,
                    Time = DateTime.UtcNow,
                    UserId = _currentUser.Id ?? Guid.Empty,
                    TenantId = _currentUser.TenantId ?? Guid.Empty
                };

                await _emailLogAttachmentRepository.InsertAsync(newAttachment);
                copiedCount++;
            }

            return Ok(new CopyAttachmentsResponseDto { AttachmentCount = copiedCount });
        }

        [HttpDelete("email-log/{emailLogId}/origin-attachments")]
        public async Task<ActionResult<CopyAttachmentsResponseDto>> DeleteOriginAttachments(Guid emailLogId)
        {
            var attachments = await _emailLogAttachmentRepository.GetOriginAttachmentsByEmailLogIdAsync(emailLogId);
            foreach (var attachment in attachments)
            {
                await _emailLogAttachmentRepository.DeleteAsync(attachment.Id);
            }
            return Ok(new CopyAttachmentsResponseDto { AttachmentCount = attachments.Count });
        }

        [HttpGet("statuses")]
        public async Task<ActionResult<List<object>>> GetApplicationStatuses()
        {
            var statuses = await _statusService.GetListAsync();
            var list = statuses.Select(s => new { id = s.Id, internalStatus = s.InternalStatus }).ToList<object>();
            return Ok(list);
        }

        [HttpGet("recipients")]
        public async Task<ActionResult<List<RecipientDto>>> GetRecipients([FromQuery] string category)
        {
            // For internal recipients, load EmailGroups from the notifications module and expose their Name
            if (string.Equals(category, "Internal", StringComparison.OrdinalIgnoreCase))
            {
                var groups = await _emailGroupsAppService.GetListAsync();
                var list = groups.Select(g => new RecipientDto { Id = g.Name, DisplayName = g.Name }).ToList();
                return Ok(list);
            }

            // For external recipients we expose the two choices required by the UI
            var externalContacts = new List<RecipientDto>
            {
                new() { Id = "ApplicationContact", DisplayName = "Application Contact" },
                new() { Id = "SigningAuthority", DisplayName = "Signing Authority" }
            };

            if (string.Equals(category, "External", StringComparison.OrdinalIgnoreCase)) return Ok(externalContacts);

            return Ok(new List<RecipientDto>());
        }

        [HttpGet("{formId}")]
        public async Task<ActionResult<List<ScheduledNotificationDto>>> GetForForm(string formId)
        {
            if (!Guid.TryParse(formId, out var parsedFormId)) return BadRequest("Invalid form id");

            var listResult = await _automatedNotificationAppService.GetListAsync(new Unity.GrantManager.Notifications.GetNotificationsInput { FormId = parsedFormId, MaxResultCount = 1000 });

            // Resolve template names and status labels
            var templateIds = listResult.Items.Select(x => x.EmailTemplateId).Where(id => id != Guid.Empty).Distinct().ToList();
            var templateMap = new Dictionary<Guid, Unity.Notifications.Templates.EmailTemplate?>();
            foreach (var id in templateIds)
            {
                templateMap[id] = await _templateService.GetTemplateById(id);
            }

            var statuses = await _statusService.GetListAsync();
            var statusMap = statuses.ToDictionary(s => s.Id, s => s.InternalStatus);

            var items = listResult.Items.Select(e => new ScheduledNotificationDto
            {
                Id = e.Id,
                TemplateId = e.EmailTemplateId,
                TemplateName = templateMap.TryGetValue(e.EmailTemplateId, out var t) && t != null ? t.Name : string.Empty,
                TriggerType = e.TriggerType,
                DateType = e.DateField,
                EventStatus = e.ApplicationStatus,
                ApplicationStatusId = e.ApplicationStatusId,
                RecipientCategory = e.RecipientCategory,
                RecipientIdentifier = e.RecipientIdentifier,
                CreatedAt = DateTime.UtcNow,
                IsActive = e.IsActive
            }).ToList();

            return Ok(items);
        }

        [HttpPost("{formId}")]
        public async Task<ActionResult<ScheduledNotificationDto>> CreateForForm(string formId, [FromBody] CreateScheduledNotificationInput input)
        {
            if (input.TemplateId == Guid.Empty) return BadRequest("TemplateId required");

            if (string.Equals(input.TriggerType, "Event", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(input.RecipientIdentifier))
            {
                return BadRequest("RecipientIdentifier required for Event trigger");
            }

            var template = await _templateService.GetTemplateById(input.TemplateId);
            if (template == null) return BadRequest("Template not found");
            if (!Guid.TryParse(formId, out var parsedFormId)) return BadRequest("Invalid form id");

            // Resolve status label if an ApplicationStatusId was provided
            string? statusLabel = null;
            if (input.ApplicationStatusId.HasValue)
            {
                var statuses = await _statusService.GetListAsync();
                var match = statuses.FirstOrDefault(s => s.Id == input.ApplicationStatusId.Value);
                if (match != null) statusLabel = match.InternalStatus;
            }

            var createDto = new Notifications.CreateUpdateNotificationDto
            {
                FormId = parsedFormId,
                EmailTemplateId = template.Id,
                TriggerType = input.TriggerType,
                TriggerDetail = input.TriggerType == "Date" ? input.DateType : statusLabel,
                IsActive = true,
                EventType = null,
                ApplicationStatusId = input.ApplicationStatusId,
                ApplicationStatus = statusLabel,
                DateField = input.DateType,
                RecipientCategory = input.RecipientCategory,
                RecipientIdentifier = input.RecipientIdentifier
            };

            var created = await _automatedNotificationAppService.CreateAsync(createDto);

            var dto = new ScheduledNotificationDto
            {
                Id = created.Id,
                TemplateId = input.TemplateId,
                TemplateName = template.Name,
                TriggerType = created.TriggerType,
                DateType = created.DateField,
                EventStatus = created.ApplicationStatus,
                ApplicationStatusId = created.ApplicationStatusId,
                RecipientCategory = created.RecipientCategory,
                RecipientIdentifier = created.RecipientIdentifier,
                CreatedAt = DateTime.UtcNow
            };

            return CreatedAtAction(nameof(GetForForm), new { formId }, dto);
        }

        [HttpDelete("{formId}/{id:guid}")]
        public async Task<IActionResult> Delete(string formId, Guid id)
        {
            await _automatedNotificationAppService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPatch("{formId}/{id:guid}/cancel")]
        public async Task<IActionResult> CancelNotification(string formId, Guid id)
        {
            if (!Guid.TryParse(formId, out _)) return BadRequest("Invalid form id");
            await _automatedNotificationAppService.CancelAsync(id);
            return NoContent();
        }

        [HttpGet("can-delete-template/{templateId:guid}")]
        public async Task<ActionResult<object>> CanDeleteTemplate(Guid templateId)
        {
            var result = await _automatedNotificationAppService.GetListAsync(
                new Notifications.GetNotificationsInput { MaxResultCount = 1000 });

            var inUse = result.Items.Any(n => n.EmailTemplateId == templateId);

            if (inUse)
            {
                return Ok(new
                {
                    canDelete = false,
                    errorMessage = "This template cannot be deleted because it is assigned to one or more Scheduled Notifications. Please remove the template from all Scheduled Notifications before deleting."
                });
            }

            return Ok(new { canDelete = true, errorMessage = (string?)null });
        }

        [HttpPut("{formId}/{id:guid}")]
        public async Task<ActionResult<ScheduledNotificationDto>> UpdateForForm(string formId, Guid id, [FromBody] CreateScheduledNotificationInput input)
        {
            if (!Guid.TryParse(formId, out var parsedFormId)) return BadRequest("Invalid form id");
            if (input.TemplateId == Guid.Empty) return BadRequest("TemplateId required");

            var template = await _templateService.GetTemplateById(input.TemplateId);
            if (template == null) return BadRequest("Template not found");

            string? statusLabel = null;
            if (input.ApplicationStatusId.HasValue)
            {
                var statuses = await _statusService.GetListAsync();
                var match = statuses.FirstOrDefault(s => s.Id == input.ApplicationStatusId.Value);
                if (match != null) statusLabel = match.InternalStatus;
            }

            var updateDto = new Notifications.CreateUpdateNotificationDto
            {
                FormId = parsedFormId,
                EmailTemplateId = template.Id,
                TriggerType = input.TriggerType,
                TriggerDetail = input.TriggerType == "Date" ? input.DateType : statusLabel,
                IsActive = true,
                EventType = null,
                ApplicationStatusId = input.ApplicationStatusId,
                ApplicationStatus = statusLabel,
                DateField = input.DateType,
                RecipientCategory = input.RecipientCategory,
                RecipientIdentifier = input.RecipientIdentifier
            };

            var updated = await _automatedNotificationAppService.UpdateAsync(id, updateDto);

            var dto = new ScheduledNotificationDto
            {
                Id = updated.Id,
                TemplateId = input.TemplateId,
                TemplateName = template.Name,
                TriggerType = updated.TriggerType,
                DateType = updated.DateField,
                EventStatus = updated.ApplicationStatus,
                ApplicationStatusId = updated.ApplicationStatusId,
                RecipientCategory = updated.RecipientCategory,
                RecipientIdentifier = updated.RecipientIdentifier,
                CreatedAt = DateTime.UtcNow
            };

            return Ok(dto);
        }
    }

    public record EmailTemplateDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Subject { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public string SendFrom { get; init; } = string.Empty;
        public string? RecipientCategory { get; init; }
        public string? RecipientIdentifier { get; init; }
    }

    public record ScheduledNotificationDto
    {
        public Guid Id { get; init; }
        public Guid TemplateId { get; init; }
        public string TemplateName { get; init; } = string.Empty;
        public string TriggerType { get; init; } = string.Empty;
        public string? DateType { get; init; }
        public string? EventStatus { get; init; }
        public Guid? ApplicationStatusId { get; init; }
        public string? RecipientCategory { get; init; }
        public string? RecipientIdentifier { get; init; }
        public DateTime CreatedAt { get; init; }
        public bool IsActive { get; init; }
    }

    public record CreateScheduledNotificationInput
    {
        public Guid TemplateId { get; init; }
        public string TriggerType { get; init; } = "Date";
        public string? DateType { get; init; }
        public Guid? ApplicationStatusId { get; init; }
        public string? EventStatus { get; init; }
        public string? RecipientCategory { get; init; }
        public string? RecipientIdentifier { get; init; }
    }

    public record RecipientDto
    {
        public string Id { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
    }

    public record CopyAttachmentsInput
    {
        public Guid EmailLogId { get; init; }
    }

    public record CopyAttachmentsResponseDto
    {
        public int AttachmentCount { get; init; }
    }

    public record ResolvedRecipientsDto
    {
        public string EmailTo { get; init; } = string.Empty;
    }
}
