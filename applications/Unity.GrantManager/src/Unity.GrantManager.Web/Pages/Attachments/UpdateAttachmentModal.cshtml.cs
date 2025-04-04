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
    public UpdateAttachmentViewModel? UpdateModel { get; set; }
    [BindProperty]
    public AttachmentType EditAttachmentType { get; set; }

    public async Task OnGetAsync(AttachmentType attachmentType, Guid attachmentId)
    {
        var attachment = await attachmentService.GetAttachmentMetadataAsync(attachmentType, attachmentId) ?? throw new EntityNotFoundException();
        EditAttachmentType = attachment.AttachmentType;
        UpdateModel = new UpdateAttachmentViewModel
        {
            AttachmentId = attachment.Id,
            FileName = attachment.FileName ?? string.Empty,
            DisplayName = attachment.DisplayName,
            CreatorId = attachment.CreatorId
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UpdateModel == null)
        {
            throw new AbpValidationException("UpdateModel cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(UpdateModel.DisplayName))
        {
            UpdateModel.DisplayName = null;
        }

        try
        {
            await attachmentService.UpdateAttachmentMetadataAsync(
                new UpdateAttachmentMetadataDto
                {
                    Id = UpdateModel.AttachmentId,
                    AttachmentType = EditAttachmentType,
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

    [Display(Name = "Document Name")]
    [ReadOnlyInput(PlainText = true)]
    public string FileName { get; set; } = string.Empty;

    [Display(Name = "Label")]
    [InputInfoText("Max length 256 characters")]
    [FormControlSize(AbpFormControlSize.Large)]
    [Placeholder("Enter file label...")]
    [MaxLength(256)]
    public string? DisplayName { get; set; }

    [HiddenInput]
    public Guid? CreatorId { get; set; }
}
