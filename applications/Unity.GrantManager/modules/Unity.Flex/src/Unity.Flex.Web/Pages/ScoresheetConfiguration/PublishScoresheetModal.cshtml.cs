using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets;

namespace Unity.Flex.Web.Pages.ScoresheetConfiguration;

public class PublishScoresheetModalModel : FlexPageModel
{
    private readonly IScoresheetAppService _scoresheetAppService;

    public PublishScoresheetModalModel(IScoresheetAppService scoresheetAppService)
    {
        _scoresheetAppService = scoresheetAppService;
    }

    [BindProperty]
    public PublishScoresheetModalModelModel Scoresheet { get; set; } = new();

    public class PublishScoresheetModalModelModel
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
        await PublishScoresheet();
        return NoContent();
    }
    
    private async Task PublishScoresheet()
    {
        await _scoresheetAppService.PublishScoresheetAsync(Scoresheet.Id);
    }

}
