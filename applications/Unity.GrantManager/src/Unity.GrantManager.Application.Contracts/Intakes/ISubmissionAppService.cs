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
    /// <param name="formId">ID of the form</param>
    /// <param name="fields">A list of form fields to search on. Refer to the related &#x60;versions/{formVersionId}/fields&#x60; endpoint for a list of valid values to query for. The list should be comma separated.</param>
    /// <returns>List&lt;FormSubmissionSummary&gt;</returns>
    Task<PagedResultDto<FormSubmissionSummaryDto>> GetSubmissionsList(Guid? formId);

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
