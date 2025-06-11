using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Forms;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.ApplicationForms
{
    public interface IApplicationFormSycnronizationService : ICrudAppService<
            ApplicationFormDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateApplicationFormDto>
    {
        Task<IList<ApplicationFormDto>> GetConnectedApplicationFormsAsync();
        Task<(HashSet<string> MissingSubmissions, string MissingSubmissionsReport)> GetMissingSubmissions(int numberOfDaysToCheck);
        Task<HashSet<string>> GetChefsSubmissions(ApplicationFormDto applicationFormDto, int numberOfDaysToCheck);
        HashSet<string> GetSubmissionsByForm(Guid applicationFormId);

    }
}
