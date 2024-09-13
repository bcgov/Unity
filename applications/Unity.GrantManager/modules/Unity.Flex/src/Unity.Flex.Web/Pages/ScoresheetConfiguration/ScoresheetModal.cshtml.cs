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
        [Display(Name = "Scoresheet:Configuration:ScoresheetModal.Title")]
        [MinLength(3)]
        [MaxLength(25)]
        public string Title { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Published {  get; set; } 
    }
    public async Task OnGetAsync(Guid scoresheetId,
       string actionType)
    {
        Scoresheet.Id = scoresheetId;
        Scoresheet.ActionType = actionType;
        if (Scoresheet.ActionType.Contains("Edit"))
        {
            ScoresheetDto scoresheet = await _scoresheetAppService.GetAsync(scoresheetId);
            Scoresheet.Title = scoresheet.Title ?? "";
            Scoresheet.Name = scoresheet.Name ?? "";
            Scoresheet.Published = scoresheet.Published;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Scoresheet.ActionType.StartsWith("Edit"))
        {
            await EditScoresheets();
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
        _ = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto() { Title = Scoresheet.Title, Name = Scoresheet.Name });
    }

    private async Task EditScoresheets()
    {
        await _scoresheetAppService.UpdateAsync(Scoresheet.Id, new EditScoresheetDto() { Title = Scoresheet.Title, ActionType = Scoresheet.ActionType });
    }    

    private async Task DeleteScoresheet()
    {
        await _scoresheetAppService.DeleteAsync(Scoresheet.Id);
    }
}
