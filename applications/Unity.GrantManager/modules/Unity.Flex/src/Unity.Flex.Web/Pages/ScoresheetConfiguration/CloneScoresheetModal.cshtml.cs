using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets;
using Volo.Abp.Validation;

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
        public Guid GroupId { get; set; }
        public string Name { get; set; } = string.Empty;        
        public string Version {  get; set; } = string.Empty;
    }
    public async Task OnGetAsync(Guid scoresheetId, Guid groupId)
    {
        Scoresheet.Id = scoresheetId;
        Scoresheet.GroupId = groupId;
        PreCloneScoresheetDto scoresheet = await _scoresheetAppService.GetPreCloneInformationAsync(scoresheetId);
        Scoresheet.Name = scoresheet.Name;
        Scoresheet.Version = "V"+ (scoresheet.HighestVersion + 1) + ".0";
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
