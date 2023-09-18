using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Polly;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Intake;

[Authorize]
public class SubmissionAppService : GrantManagerAppService, ISubmissionAppService
{
    private IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
    //private IApplicationFormRepository _applicationFormRepository;
    private IRepository<ApplicationForm, Guid> _applicationFormRepository;
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

    public SubmissionAppService(
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IRepository<ApplicationForm, Guid> applicationFormRepository,
        RestClient restClient
        )
    {
        _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
        _applicationFormRepository = applicationFormRepository;
        _intakeClient = restClient;
    }


    public async Task<object> GetSubmission(Guid? formSubmissionId)
    {

        if (formSubmissionId == null)
            throw new ApiException(400, "Missing required parameter 'formId' when calling GetSubmission");

        ApplicationForm applicationForm = await getApplicationFormBySubmissionId(formSubmissionId);

        var request = new RestRequest($"/submissions/{formSubmissionId}");
        /** Authenticator as CHEFS form id + api key which is stored in db manually */
        request.Authenticator = new HttpBasicAuthenticator(applicationForm.ChefsApplicationFormGuid, applicationForm.ApiKey ?? "");
        var response = await _intakeClient.GetAsync(request);

        if (((int)response.StatusCode) >= 400)
            throw new ApiException((int)response.StatusCode, "Error calling GetSubmission: " + response.Content, response.ErrorException);
        else if (((int)response.StatusCode) == 0)
            throw new ApiException((int)response.StatusCode, "Error calling GetSubmission: " + response.ErrorMessage, response.ErrorMessage);

        return response.Content;
    }


    public async Task<ApplicationForm> getApplicationFormBySubmissionId(Guid? formSubmissionId)
    {
        ApplicationForm applicationFormData = new ApplicationForm();

        if (formSubmissionId != null)
        {
            var query = from applicationSubmission in await _applicationFormSubmissionRepository.GetQueryableAsync()
                        join applicationForm in await _applicationFormRepository.GetQueryableAsync() on applicationSubmission.ApplicationFormId equals applicationForm.Id
                        where applicationSubmission.ChefsSubmissionGuid == formSubmissionId.ToString()
                        select applicationForm;
            applicationFormData = await AsyncExecuter.FirstOrDefaultAsync(query);
        }
        return applicationFormData;
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
