using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets;
using Volo.Abp.Validation;

namespace Unity.Flex.Web.Pages.ScoresheetConfiguration;

public class SectionModalModel : FlexPageModel
{
    private readonly IScoresheetAppService _scoresheetAppService;

    public SectionModalModel(IScoresheetAppService scoresheetAppService)
    {
        _scoresheetAppService = scoresheetAppService;
    }

    [BindProperty]
    public SectionModalModelModel Section { get; set; } = new();

    public class SectionModalModelModel
    {
        public Guid SectionId { get; set; }
        public Guid ScoresheetId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        [Display(Name = "Scoresheet:Configuration:SectionModal.Name")]
        public string Name { get; set; } = string.Empty;
        public uint Order { get; set; }
    }
    public async Task OnGetAsync(Guid scoresheetId, Guid sectionId,
       string actionType)
    {
        Section.ScoresheetId = scoresheetId;
        Section.SectionId = sectionId;
        Section.ActionType = actionType;
        if (Section.ActionType.Contains("Edit"))
        {
            ScoresheetSectionDto section = await _scoresheetAppService.GetSectionAsync(sectionId);
            Section.Name = section.Name ?? "";
            Section.Order = section.Order;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Section.ActionType.StartsWith("Edit"))
        {
            await EditSection();
            return NoContent();
        }
        else if (Section.ActionType.StartsWith("Add"))
        {
            await CreateSection();
            return NoContent();
        }
        else if (Section.ActionType.StartsWith("Delete"))
        {
            await DeleteSection();
            return NoContent();
        }
        else
        {
            throw new AbpValidationException("Invalid ActionType!");
        }
    }

    private async Task CreateSection()
    {
        _ = await _scoresheetAppService.CreateSectionAsync(new CreateSectionDto() { Name = Section.Name, ScoresheetId = Section.ScoresheetId });
    }

    private async Task EditSection()
    {
        _ = await _scoresheetAppService.EditSectionAsync(new EditSectionDto() { Name = Section.Name, SectionId = Section.SectionId });
    }

    private async Task DeleteSection()
    {
        await _scoresheetAppService.DeleteSectionAsync(Section.SectionId);
    }
}
