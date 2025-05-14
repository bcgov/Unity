using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Views.Settings.TagManagement;

public class RenameTagModal(IApplicationTagsService appTagService) : AbpPageModel
{

    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public TagType SelectedTagType { get; set; }

    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public string SelectedTagText { get; set; } = string.Empty;

    [BindProperty]
    public RenameTagViewModel? ViewModel { get; set; }

    public List<TagSummaryCountDto> SummaryList = [];

    public void OnGetAsync()
    {
        ViewModel = new RenameTagViewModel(SelectedTagText);
    }

    public class RenameTagViewModel(string inputTagName)
    {
        [Required]
        public string OriginalTag { get; set; } = inputTagName;

        [DisplayName("New Tag Name")]
        [Required(ErrorMessage = "Replacement tag is required")]
        [RegularExpression(@"^[^\s,]+$", ErrorMessage = "Tag cannot contain spaces or commas")]
        [CustomValidation(typeof(RenameTagViewModel), nameof(ValidateTagNotEqual))]
        public string ReplacementTag { get; set; } = inputTagName;

        public static ValidationResult? ValidateTagNotEqual(string replacementTag, ValidationContext context)
        {
            var instance = (RenameTagViewModel)context.ObjectInstance;
            if (replacementTag == instance.OriginalTag)
            {
                return new ValidationResult("New tag cannot be the same as the original tag");
            }
            return ValidationResult.Success;
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TagType
    {
        Application,
        Payment
    }
}
