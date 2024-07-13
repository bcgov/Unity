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
    private readonly ISectionAppService _sectionAppService;

    public SectionModalModel(IScoresheetAppService scoresheetAppService, ISectionAppService sectionAppService)
    {
        _scoresheetAppService = scoresheetAppService;
        _sectionAppService = sectionAppService;
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
            ScoresheetSectionDto section = await _sectionAppService.GetAsync(sectionId);
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
        _ = await _scoresheetAppService.CreateSectionAsync(Section.ScoresheetId, new CreateSectionDto() { Name = Section.Name });
    }

    private async Task EditSection()
    {
        await _scoresheetAppService.ValidateChangeableScoresheet(Section.ScoresheetId);
        _ = await _sectionAppService.UpdateAsync(Section.SectionId, new EditSectionDto() { Name = Section.Name });
    }

    private async Task DeleteSection()
    {
        await _scoresheetAppService.ValidateChangeableScoresheet(Section.ScoresheetId);
        await _sectionAppService.DeleteAsync(Section.SectionId);
    }

    
}
