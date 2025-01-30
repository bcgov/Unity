using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Reporting.FieldGenerators
{
    [Authorize]
    public class FormVersionReportingFieldsGeneratorAppService(IReportingFieldsGeneratorService reportingFieldsGeneratorService,
        IApplicationFormVersionRepository applicationFormVersionRepository) : ApplicationService, IFormVersionReportingFieldsGeneratorAppService
    {
        public async Task Generate(Guid formVersionId)
        {
            var applicationFormVersion = await applicationFormVersionRepository.GetAsync(formVersionId);
            await reportingFieldsGeneratorService.GenerateAndSetAsync(applicationFormVersion);
        }
    }
}
