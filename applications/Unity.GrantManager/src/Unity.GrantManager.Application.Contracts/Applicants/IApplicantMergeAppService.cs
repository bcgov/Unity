using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Applicants;

public interface IApplicantMergeAppService : IApplicationService
{
    Task<ApplicantMergeDto> MergeAsync(MergeApplicantsDto input);
    Task<ApplicantMergeListDto> GetReversibleMergesAsync(Guid applicantId);
    Task<ApplicantMergePreviewDto> GetUnmergePreviewAsync(Guid mergeOperationId);
    Task<ApplicantMergeDto> UnmergeAsync(Guid mergeOperationId, UnmergeApplicantsDto input);
}
