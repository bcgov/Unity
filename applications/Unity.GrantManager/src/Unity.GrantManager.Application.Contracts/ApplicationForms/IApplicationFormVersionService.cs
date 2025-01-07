using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Forms;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.ApplicationForms
{
    public interface IApplicationFormVersionAppService : ICrudAppService<
            ApplicationFormVersionDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateApplicationFormVersionDto>
    {
        Task<bool> FormVersionExists(string chefsFormVersionId);
        Task<bool> InitializePublishedFormVersion(dynamic chefsForm, Guid applicationFormId, bool initializePublishedOnly);
        Task<string?> GetFormVersionSubmissionMapping(string chefsFormVersionId);
        Task<ApplicationFormVersionDto> UpdateOrCreateApplicationFormVersion(string chefsFormId, string chefsFormVersionId, Guid applicationFormId, dynamic chefsFormVersion);
        Task<ApplicationFormVersionDto?> TryInitializeApplicationFormVersionWithToken(JToken token, Guid applicationFormId, string formVersionId, bool published);
        Task<ApplicationFormVersionDto?> TryInitializeApplicationFormVersion(string? formId, int version, Guid applicationFormId, string formVersionId, bool published);
        Task<ApplicationFormVersionDto?> GetByChefsFormVersionId(Guid chefsFormVersionId);
        Task<int> GetFormVersionByApplicationIdAsync(Guid applicationId);
        Task DeleteWorkSheetMappingByFormName(string formName, Guid formVersionId);
    }
}
