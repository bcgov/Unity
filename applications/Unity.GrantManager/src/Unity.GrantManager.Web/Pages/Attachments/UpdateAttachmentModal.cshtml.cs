using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Web.Pages.Attachments;

public class UpdateAttachmentModalModel(IAttachmentAppService attachmentService) : AbpPageModel
{
    [BindProperty]
    public required UpdateAttachmentViewModel UpdateModel { get; set; }

    public async Task OnGetAsync(AttachmentType attachmentType, Guid attachmentId)
    {
        var attachment = await attachmentService.GetAttachmentMetadataAsync(attachmentType, attachmentId) ?? throw new EntityNotFoundException();
        UpdateModel = new UpdateAttachmentViewModel
        {
            AttachmentId = attachment.Id,
            AttachmentType = attachment.AttachmentType,
            FileName = attachment.FileName ?? string.Empty,
            DisplayName = attachment.DisplayName,
            CreatorId = attachment.CreatorId
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            await attachmentService.UpdateAttachmentMetadataAsync(
                new UpdateAttachmentMetadataDto
                {
                    Id = UpdateModel.AttachmentId,
                    AttachmentType = UpdateModel.AttachmentType,
                    DisplayName = UpdateModel.DisplayName
                });
        }
        catch (Exception ex)
        {
            throw new AbpValidationException(ex.Message, ex);
        }
        return NoContent();
    }
}

public class UpdateAttachmentViewModel
{
    [HiddenInput]
    public required Guid AttachmentId { get; set; }

    [DisabledInput]
    public AttachmentType AttachmentType { get; set; }

    [ReadOnlyInput]
    [Display(Name = "Document File Name")]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Label")]
    [InputInfoText("Set a descriptive label for a file attachment.")]
    [FormControlSize(AbpFormControlSize.Large)]
    [Placeholder("Enter file label...")]
    [StringLength(256)]
    public string? DisplayName { get; set; }

    [HiddenInput]
    public Guid? CreatorId { get; set; }
}