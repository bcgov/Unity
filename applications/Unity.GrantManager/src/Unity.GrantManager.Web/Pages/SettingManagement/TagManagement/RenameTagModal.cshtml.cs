using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.GrantManager.GlobalTag;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Web.Pages.SettingManagement.TagManagement;

public class RenameTagModal(ITagsService tagService) : AbpPageModel
{
    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public Guid SelectedTagId  { get; set; }
    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public string SelectedTagText { get; set; } = string.Empty;

    [BindProperty]
    public RenameTagViewModel? ViewModel { get; set; }

    public void OnGet()
    {
        ViewModel = new RenameTagViewModel
        {
            TagId = SelectedTagId,
            OriginalTag = SelectedTagText,
            ReplacementTag = SelectedTagText
        };
    }

    public class RenameTagViewModel
    {
        [Required]
        [HiddenInput]
        public required Guid TagId { get; set; }
        [Required]
        [HiddenInput]
        public required string OriginalTag { get; set; }

        [DisplayName("New Tag Name")]
        [Required(ErrorMessage = "Replacement tag is required")]
        [RegularExpression(@"^[^\s,]+$", ErrorMessage = "Tag cannot contain spaces or commas.")]
        [MaxLength(250)]
        public required string ReplacementTag { get; set; }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ViewModel == null) return NoContent();

        if (ViewModel.OriginalTag == ViewModel.ReplacementTag)
        {
            throw new AbpValidationException("New tag cannot be the same as the original tag.");
        }

        try
        {
            await tagService.RenameTagGlobalAsync(ViewModel.TagId,ViewModel.OriginalTag, ViewModel.ReplacementTag);
        }
        catch (Exception ex)
        {
            throw new AbpValidationException(ex.Message);
        }

        return NoContent();
    }
}
