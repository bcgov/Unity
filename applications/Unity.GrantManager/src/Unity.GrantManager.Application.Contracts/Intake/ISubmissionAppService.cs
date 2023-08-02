using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Intake;

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
    Task<object> GetSubmission(Guid? formSubmissionId);
}
