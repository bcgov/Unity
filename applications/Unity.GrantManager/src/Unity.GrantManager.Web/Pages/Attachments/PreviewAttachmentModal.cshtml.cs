using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.Attachments;

public class PreviewAttachmentModalModel : AbpPageModel
{
    public string FileName { get; private set; } = "";
    public string DisplayName { get; private set; } = "";
    public string DownloadUrl { get; private set; } = "";
    public string AttachmentType { get; private set; } = "";
    public string PreviewPdfUrl { get; private set; } = "";

#pragma warning disable CS1998
    public async Task OnGetAsync(
        string attachmentType,
        string ownerId,
        string fileName,
        string? displayName = null,
        string? chefsFileId = null)
#pragma warning restore CS1998
    {
        FileName = fileName;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? fileName : displayName;
        AttachmentType = attachmentType.ToLower();

        if (string.Equals(attachmentType, "chefs", StringComparison.OrdinalIgnoreCase))
        {
            DownloadUrl = $"/api/app/attachment/chefs/{Uri.EscapeDataString(ownerId)}/download/{Uri.EscapeDataString(chefsFileId ?? string.Empty)}/{Uri.EscapeDataString(fileName)}";
            PreviewPdfUrl = $"/api/app/attachment/chefs/{Uri.EscapeDataString(ownerId)}/preview-pdf/{Uri.EscapeDataString(chefsFileId ?? string.Empty)}/{Uri.EscapeDataString(fileName)}";
        }
        else
        {
            DownloadUrl = $"/api/app/attachment/{AttachmentType}/{Uri.EscapeDataString(ownerId)}/download/{Uri.EscapeDataString(fileName)}";
            PreviewPdfUrl = $"/api/app/attachment/{AttachmentType}/{Uri.EscapeDataString(ownerId)}/preview-pdf/{Uri.EscapeDataString(fileName)}";
        }
    }
}
