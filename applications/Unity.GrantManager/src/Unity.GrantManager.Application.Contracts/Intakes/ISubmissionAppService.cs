using System;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Intakes;

/// <summary>
/// Represents a collection of functions to interact with the API endpoints
/// </summary>
public interface ISubmissionAppService : IApplicationService
{
    /// <summary>
    /// List submissions for a form 
    /// </summary>
    /// <param name="input">Filter parameters including tenant, date range, and whether to include all submissions.</param>
    /// <returns>List&lt;FormSubmissionSummary&gt;</returns>
    Task<PagedResultDto<FormSubmissionSummaryDto>> GetSubmissionsListAsync(GetSubmissionsListInput input);

    /// <summary>
    /// Get a form submission 
    /// </summary>
    /// <param name="formSubmissionId">ID of the Submission</param>
    /// <returns>SubmissionFormVersion</returns>
    Task<object?> GetSubmission(Guid? formSubmissionId);

    /// <summary>
    /// Get chefs file attachment
    /// </summary>
    /// <param name="formSubmissionId">ID of the Submission</param>
    /// <param name="chefsFileAttachmentId">ID of the Chefs file attachment</param>
    /// <param name="name">File name of the chefs attachment</param>
    /// <returns>BlobDto</returns>
    Task<BlobDto> GetChefsFileAttachment(Guid? formSubmissionId, Guid? chefsFileAttachmentId, string name);
}
