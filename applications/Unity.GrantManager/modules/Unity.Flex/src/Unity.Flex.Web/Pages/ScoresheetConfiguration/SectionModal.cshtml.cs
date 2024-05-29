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
        if (Section.ActionType.Equals("Edit Section On Current Version"))
        {
            await EditSectionOnCurrentVersion();
            return NoContent();
        }
        else if (Section.ActionType.Equals("Edit Section On New Version"))
        {
            await EditSectionOnNewVersion();
            return NoContent();
        }
        else if (Section.ActionType.Equals("Add Section On Current Version"))
        {
            await CreateSectionOnCurrentVersion();
            return NoContent();
        }
        else if (Section.ActionType.Equals("Add Section On New Version"))
        {
            await CreateSectionOnNewVersion();
            return NoContent();
        }
        else if (Section.ActionType.Equals("Delete Section On Current Version"))
        {
            await DeleteSectionOnCurrentVersion();
            return NoContent();
        }
        else if (Section.ActionType.Equals("Delete Section On New Version"))
        {
            await DeleteSectionOnNewVersion();
            return NoContent();
        }
        else
        {
            throw new AbpValidationException("Invalid ActionType!");
        }
    }

    private async Task CreateSectionOnCurrentVersion()
    {
        _ = await _scoresheetAppService.CreateSectionAsync(new CreateSectionDto() { Name = Section.Name, ScoresheetId = Section.ScoresheetId });
    }

    private async Task CreateSectionOnNewVersion()
    {
        var clone = await _scoresheetAppService.CloneScoresheetAsync(Section.ScoresheetId, null, null);
        _ = await _scoresheetAppService.CreateSectionAsync(new CreateSectionDto() { Name = Section.Name, ScoresheetId = clone.ScoresheetId });
    }

    private async Task EditSectionOnCurrentVersion()
    {
        _ = await _sectionAppService.UpdateAsync(new EditSectionDto() { Name = Section.Name, SectionId = Section.SectionId });
    }

    private async Task EditSectionOnNewVersion()
    {
        var clone = await _scoresheetAppService.CloneScoresheetAsync(Section.ScoresheetId, Section.SectionId, null);
        _ = await _sectionAppService.UpdateAsync(new EditSectionDto() { Name = Section.Name, SectionId = clone.SectionId ?? Guid.Empty });
    }

    private async Task DeleteSectionOnCurrentVersion()
    {
        await _sectionAppService.DeleteAsync(Section.SectionId);
    }

    private async Task DeleteSectionOnNewVersion()
    {
        var clone = await _scoresheetAppService.CloneScoresheetAsync(Section.ScoresheetId, Section.SectionId, null);
        await _sectionAppService.DeleteAsync(clone.SectionId ?? Guid.Empty);
    }
}
