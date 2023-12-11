using System;
using System.Collections.Generic;
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
        Task<bool> InitializePublishedFormVersion(dynamic chefsForm, Guid applicationFormId);                
        Task<string?> GetFormVersionSubmissionMapping(string chefsFormVersionId);
        Task<ApplicationFormVersionDto> UpdateOrCreateApplicationFormVersion(string chefsFormId, string chefsFormVersionId, Guid applicationFormId, dynamic chefsFormVersion);
    }
}
