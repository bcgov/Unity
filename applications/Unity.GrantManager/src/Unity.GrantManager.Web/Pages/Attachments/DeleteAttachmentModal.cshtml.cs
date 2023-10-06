using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Unity.GrantManager.Attachments;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Web.Pages.Attachments;

public class DeleteAttachmentModalModel : AbpPageModel
{
        
    [BindProperty]
    public string S3ObjectKey { get; set; } = "";
    [BindProperty]
    public string FileName { get; set; } = "";
    [BindProperty]
    public string AttachmentType { get; set; } = "";
    [BindProperty]
    public string AttachmentTypeId { get; set; } = "";

    private readonly IFileAppService _fileAppService;

    public DeleteAttachmentModalModel(IFileAppService fileAppService)
    {
        _fileAppService = fileAppService;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task OnGetAsync(string s3ObjectKey, string fileName, string attachmentType, string attachmentTypeId)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        S3ObjectKey = s3ObjectKey;
        FileName = fileName;
        AttachmentType = attachmentType;
        AttachmentTypeId = attachmentTypeId;
    }     

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            bool isdeleted = await _fileAppService.DeleteBlobAsync(new DeleteBlobRequestDto { S3ObjectKey = S3ObjectKey, Name = FileName });
            if(!isdeleted)
            {
                throw new AbpValidationException("Failed to delete " + FileName + ".");
            }
        }
        catch (Exception ex)
        {
            throw new AbpValidationException(ex.Message, ex);
        }
        return NoContent();
    }
}
