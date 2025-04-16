using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Reporting.FieldGenerators
{
    [Authorize(IdentityConsts.ITAdminPolicy)]
    public class ScoresheetReportingFieldsGeneratorAppService(IReportingFieldsGeneratorService<Scoresheet> reportingFieldsGeneratorService,
        IScoresheetRepository scoresheetRepository) : ApplicationService, IScoresheetReportingFieldsGeneratorAppService
    {
        /// <summary>
        /// Generate / Update a scoresheets Reporting Fields Remotely
        /// </summary>
        /// <param name="scoresheetId"></param>
        /// <returns></returns>
        public async Task Generate(Guid scoresheetId)
        {
            var scoresheet = await scoresheetRepository.GetAsync(scoresheetId, true);
            reportingFieldsGeneratorService.GenerateAndSet(scoresheet);
        }
    }
}
