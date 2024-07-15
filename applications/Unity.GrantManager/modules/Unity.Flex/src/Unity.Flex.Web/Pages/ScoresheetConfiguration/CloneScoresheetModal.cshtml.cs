using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets;

namespace Unity.Flex.Web.Pages.ScoresheetConfiguration;

public class CloneScoresheetModalModel : FlexPageModel
{
    private readonly IScoresheetAppService _scoresheetAppService;

    public CloneScoresheetModalModel(IScoresheetAppService scoresheetAppService)
    {
        _scoresheetAppService = scoresheetAppService;
    }

    [BindProperty]
    public CloneScoresheetModalModelModel Scoresheet { get; set; } = new();

    public class CloneScoresheetModalModelModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;        
        public string Name {  get; set; } = string.Empty;
    }
    public async Task OnGetAsync(Guid scoresheetId)
    {
        Scoresheet.Id = scoresheetId;
        var scoresheet = await _scoresheetAppService.GetAsync(scoresheetId);
        Scoresheet.Title = scoresheet.Title;
        Scoresheet.Name = scoresheet.Name;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await CloneScoresheet();
        return NoContent();
    }
    
    private async Task CloneScoresheet()
    {
        await _scoresheetAppService.CloneScoresheetAsync(Scoresheet.Id);
    }

}
