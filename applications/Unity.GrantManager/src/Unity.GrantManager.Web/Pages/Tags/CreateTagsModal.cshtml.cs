using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.GrantManager.GlobalTag;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Web.Pages.Tags 
{
    public class NewTag 
    {
        public string? ApplicationId { get; set; }
        public List<TagDto> CommonTags { get; set; } = new();
        public List<TagDto> UncommonTags { get; set; } = new();
    }

    public class CreateTagsModalModel : AbpPageModel
    {
    
        [BindProperty]
        [Required(ErrorMessage = "Tag Name is required")]
        [RegularExpression(@"^[^\s,]+$", ErrorMessage = "Tag cannot contain spaces or commas.")]
        public string Tag { get; set; } = string.Empty;

        private readonly ITagsService _tagsService;


        public CreateTagsModalModel(ITagsService tagsService)
        {
            _tagsService = tagsService ?? throw new ArgumentNullException(nameof(tagsService));
        }

       

        public async Task<IActionResult> OnPostAsync()
        {
         
            try
            {
                await _tagsService.CreateTagsAsync(new TagDto { Name = Tag });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error creating application tags");
                throw new AbpValidationException(ex.Message);

            }

            return NoContent();
        }
    }
}
