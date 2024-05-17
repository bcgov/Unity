using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets;
using Volo.Abp.Validation;

namespace Unity.Flex.Web.Pages.ScoresheetConfiguration;

public class ScoresheetModalModel : FlexPageModel
{
    private readonly IScoresheetAppService _scoresheetAppService;

    public ScoresheetModalModel(IScoresheetAppService scoresheetAppService)
    {
        _scoresheetAppService = scoresheetAppService;
    }

    [BindProperty]
    public ScoresheetModalModelModel Scoresheet { get; set; } = new();

    public class ScoresheetModalModelModel
    {
        public Guid Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        [Display(Name = "Scoresheet:Configuration:ScoresheetModal.Name")]
        public string Name { get; set; } = string.Empty;
    }
    public async Task OnGetAsync(Guid scoresheetId,
       string actionType)
    {
        Scoresheet.Id = scoresheetId;
        Scoresheet.ActionType = actionType;
        if (Scoresheet.ActionType.Contains("Edit"))
        {
            ScoresheetDto scoresheet = await _scoresheetAppService.GetAsync(scoresheetId);
            Scoresheet.Name = scoresheet.Name ?? "";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Scoresheet.ActionType.StartsWith("Edit"))
        {
            await EditScoresheet();
            return NoContent();
        }
        else if (Scoresheet.ActionType.StartsWith("Add"))
        {
            await CreateScoresheet();
            return NoContent();
        }
        else if (Scoresheet.ActionType.StartsWith("Delete"))
        {
            await DeleteScoresheet();
            return NoContent();
        }
        else
        {
            throw new AbpValidationException("Invalid ActionType!");
        }
    }

    private async Task CreateScoresheet()
    {
        _ = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto() { Name = Scoresheet.Name });
    }

    private async Task EditScoresheet()
    {
        _ = await _scoresheetAppService.EditAsync(new EditScoresheetDto() { Name = Scoresheet.Name, ScoresheetId = Scoresheet.Id});
    }

    private async Task DeleteScoresheet()
    {
        await _scoresheetAppService.DeleteAsync(Scoresheet.Id);
    }
}
