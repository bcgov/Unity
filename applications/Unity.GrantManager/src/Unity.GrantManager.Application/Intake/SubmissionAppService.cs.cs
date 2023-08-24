using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Intake;

[Authorize]
public class SubmissionAppService : GrantManagerAppService, ISubmissionAppService
{
    private readonly RestClient _intakeClient;
    private static List<string> _summaryFieldsFilter
    {
        // NOTE: This will be replaced by a customizable filter.
        get
        {
            return new List<string>(new string[] {
                "projectTitle",
                "projectLocation",
                "contactName",
                "organizationLegalName",
                "eligibleCost",
                "totalRequestToMjf"
            });
        }
    }

    public SubmissionAppService(RestClient restClient)
    {
        _intakeClient = restClient;
    }

    public async Task<object> GetSubmission(Guid? formSubmissionId)
    {
        if (formSubmissionId == null)
            throw new ApiException(400, "Missing required parameter 'formId' when calling GetSubmission");

        var request = new RestRequest($"/submissions/{formSubmissionId}");
        var response = await _intakeClient.GetAsync(request);

        if (((int)response.StatusCode) >= 400)
            throw new ApiException((int)response.StatusCode, "Error calling GetSubmission: " + response.Content, response.ErrorException);
        else if (((int)response.StatusCode) == 0)
            throw new ApiException((int)response.StatusCode, "Error calling GetSubmission: " + response.ErrorMessage, response.ErrorMessage);

        return response.Content;
    }

    public async Task<PagedResultDto<FormSubmissionSummaryDto>> GetSubmissionsList(Guid? formId)
    {
        if (formId == null) 
            throw new ApiException(400, "Missing required parameter 'formId' when calling ListFormSubmissions");
        
        var request = new RestRequest($"/forms/{formId}/submissions", Method.Get)
            .AddParameter("fields", _summaryFieldsFilter.JoinAsString(","));

        var response = await _intakeClient.GetAsync(request);

        if (((int)response.StatusCode) >= 400)
            throw new ApiException((int)response.StatusCode, "Error calling ListFormSubmissions: " + response.Content, response.ErrorException);
        else if (((int)response.StatusCode) == 0)
            throw new ApiException((int)response.StatusCode, "Error calling ListFormSubmissions: " + response.ErrorMessage, response.ErrorMessage);

        var submissionOptions = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        List<FormSubmissionSummaryDto> jsonResponse
            = JsonSerializer.Deserialize<List<FormSubmissionSummaryDto>>(response.Content, submissionOptions);

        // Remove all deleted and draft submissions
        jsonResponse.RemoveAll(r => r.Deleted || r.FormSubmissionStatusCode != "SUBMITTED");

        return new PagedResultDto<FormSubmissionSummaryDto>(
            jsonResponse.Count,
            jsonResponse);
    }
}
