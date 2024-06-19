using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Unity.Flex.Web.Pages.WorksheetConfiguration;

public class UpsertSectionModalModel : FlexPageModel
{
    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    public string? Name { get; set; }

    [BindProperty]
    public uint Order { get; set; }

    [BindProperty]
    public WorksheetUpsertAction UpsertAction { get; set; }

    public UpsertSectionModalModel(Guid sectionId, WorksheetUpsertAction action)
    {        
    }

    [BindProperty]
    public SectionModalModelModel Section { get; set; } = new();

    public class SectionModalModelModel
    {
        public Guid SectionId { get; set; }
        public Guid ScoresheetId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        [Display(Name = "Worksheet:Configuration:SectionModal.Name")]
        public string Name { get; set; } = string.Empty;
        public uint Order { get; set; }
    }

    public async Task OnGetAsync(Guid sectionId, WorksheetUpsertAction action)
    {
        if (action == WorksheetUpsertAction.Update)
        {
         
        }      
    }

    public async Task<IActionResult> OnPostAsync()
    {
        switch (UpsertAction)
        {

        }

        return NoContent();

        //if (Section.ActionType.Equals("Edit Section On Current Version"))
        //{
        //    await EditSectionOnCurrentVersion();
        //    return NoContent();
        //}
        //else if (Section.ActionType.Equals("Edit Section On New Version"))
        //{
        //    await EditSectionOnNewVersion();
        //    return NoContent();
        //}
        //else if (Section.ActionType.Equals("Add Section On Current Version"))
        //{
        //    await CreateSectionOnCurrentVersion();
        //    return NoContent();
        //}
        //else if (Section.ActionType.Equals("Add Section On New Version"))
        //{
        //    await CreateSectionOnNewVersion();
        //    return NoContent();
        //}
        //else if (Section.ActionType.Equals("Delete Section On Current Version"))
        //{
        //    await DeleteSectionOnCurrentVersion();
        //    return NoContent();
        //}
        //else if (Section.ActionType.Equals("Delete Section On New Version"))
        //{
        //    await DeleteSectionOnNewVersion();
        //    return NoContent();
        //}
        //else
        //{
        //    throw new AbpValidationException("Invalid ActionType!");
        //}
    }

    //private async Task CreateSectionOnCurrentVersion()
    //{
    //    _ = await _scoresheetAppService.CreateSectionAsync(Section.ScoresheetId, new CreateSectionDto() { Name = Section.Name });
    //}

    //private async Task CreateSectionOnNewVersion()
    //{
    //    var clone = await _scoresheetAppService.CloneScoresheetAsync(Section.ScoresheetId, null, null);
    //    _ = await _scoresheetAppService.CreateSectionAsync(clone.ScoresheetId, new CreateSectionDto() { Name = Section.Name });
    //}

    //private async Task EditSectionOnCurrentVersion()
    //{
    //    _ = await _sectionAppService.UpdateAsync(Section.SectionId, new EditSectionDto() { Name = Section.Name });
    //}

    //private async Task EditSectionOnNewVersion()
    //{
    //    var clone = await _scoresheetAppService.CloneScoresheetAsync(Section.ScoresheetId, Section.SectionId, null);
    //    _ = await _sectionAppService.UpdateAsync(clone.SectionId ?? Guid.Empty, new EditSectionDto() { Name = Section.Name });
    //}

    //private async Task DeleteSectionOnCurrentVersion()
    //{
    //    await _sectionAppService.DeleteAsync(Section.SectionId);
    //}

    //private async Task DeleteSectionOnNewVersion()
    //{
    //    var clone = await _scoresheetAppService.CloneScoresheetAsync(Section.ScoresheetId, Section.SectionId, null);
    //    await _sectionAppService.DeleteAsync(clone.SectionId ?? Guid.Empty);
    //}
}
