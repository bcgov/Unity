using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Reporting.DataGenerators
{
    [Authorize(IdentityConsts.ITAdminPolicy)]
    public class ScoresheetsReportingDataGeneratorAppService(IReportingDataGeneratorService<Scoresheet, ScoresheetInstance> reportingDataGeneratorService,
        IScoresheetInstanceRepository scoresheetInstanceRepository,
        IScoresheetRepository scoresheetRepository)
        : ApplicationService, IScoresheetReportingDataGeneratorAppService
    {
        public async Task Generate(Guid scoresheetInstanceId)
        {
            var scoresheetInstance = await scoresheetInstanceRepository.GetAsync(scoresheetInstanceId, true);
            var scoresheet = await scoresheetRepository.GetAsync(scoresheetInstance.ScoresheetId);
            reportingDataGeneratorService.GenerateAndSet(scoresheet, scoresheetInstance);
        }
    }
}
