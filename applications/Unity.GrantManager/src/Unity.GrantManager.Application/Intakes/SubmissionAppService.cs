using Microsoft.AspNetCore.Authorization;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Attachments;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Encryption;

namespace Unity.GrantManager.Intakes;

[Authorize]
public class SubmissionAppService : GrantManagerAppService, ISubmissionAppService
{
    private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
    private readonly IRepository<ApplicationForm, Guid> _applicationFormRepository;
    private readonly RestClient _intakeClient;
    private readonly IStringEncryptionService _stringEncryptionService;

    private static readonly List<string> _summaryFieldsFilter =
    [
        "projectTitle",
        "projectLocation",
        "contactName",
        "organizationLegalName",
        "eligibleCost",
        "totalRequest"
    ];

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SubmissionAppService(
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IRepository<ApplicationForm, Guid> applicationFormRepository,
        RestClient restClient,
        IStringEncryptionService stringEncryptionService
        )
    {
        _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
        _applicationFormRepository = applicationFormRepository;
        _intakeClient = restClient;
        _stringEncryptionService = stringEncryptionService;
    }


    public async Task<object?> GetSubmission(Guid? formSubmissionId)
    {
        if (formSubmissionId == null)
        {
            throw new ApiException(400, "Missing required parameter 'formId' when calling GetSubmission");
        }

        ApplicationForm? applicationForm = await GetApplicationFormBySubmissionId(formSubmissionId);

        if (applicationForm == null)
        {
            throw new ApiException(400, "Missing Form configuration");
        }

        if (applicationForm.ChefsApplicationFormGuid == null)
        {
            throw new ApiException(400, "Missing CHEFS form Id");
        }

        if (applicationForm.ApiKey == null)
        {
            throw new ApiException(400, "Missing CHEFS Api Key");
        }

        ApplicationFormSubmission? applicationFormSubmisssion = await GetApplicationFormSubmissionBySubmissionId(formSubmissionId);
        if (applicationFormSubmisssion == null)
        {
            throw new ApiException(400, "Missing Form Submission");
        }

        return applicationFormSubmisssion.Submission;
    }

    public async Task<BlobDto> GetChefsFileAttachment(Guid? formSubmissionId, Guid? chefsFileAttachmentId, string name)
    {
        if (formSubmissionId == null)
        {
            throw new ApiException(400, "Missing required parameter 'formId' when calling GetSubmission");
        }

        if (chefsFileAttachmentId == null)
        {
            throw new ApiException(400, "Missing required parameter 'chefsFileAttachmentId' when calling GetFileAttachment");
        }

        ApplicationForm? applicationForm = await GetApplicationFormBySubmissionId(formSubmissionId);

        if (applicationForm == null)
        {
            throw new ApiException(400, "Missing Form configuration");
        }

        if (applicationForm.ChefsApplicationFormGuid == null)
        {
            throw new ApiException(400, "Missing CHEFS form Id");
        }

        if (applicationForm.ApiKey == null)
        {
            throw new ApiException(400, "Missing CHEFS Api Key");
        }

        var request = new RestRequest($"/files/{chefsFileAttachmentId}", Method.Get)
        {
            Authenticator = new HttpBasicAuthenticator(applicationForm.ChefsApplicationFormGuid!, _stringEncryptionService.Decrypt(applicationForm.ApiKey!) ?? string.Empty)
        };

        var response = await _intakeClient.GetAsync(request);


        if (((int)response.StatusCode) != 200)
        {
            throw new ApiException((int)response.StatusCode, "Error calling GetChefsFileAttachment: " + response.Content, response.ErrorMessage ?? $"{response.StatusCode}");
        }        

        return new BlobDto { Name = name, Content = response.RawBytes ?? Array.Empty<byte>(), ContentType = response.ContentType ?? "application/octet-stream" };
    }


    public async Task<ApplicationForm?> GetApplicationFormBySubmissionId(Guid? formSubmissionId)
    {
        ApplicationForm? applicationFormData = new();

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

    public async Task<ApplicationFormSubmission?> GetApplicationFormSubmissionBySubmissionId(Guid? formSubmissionId)
    {
        ApplicationFormSubmission? applicationFormSubmissionData = new();

        if (formSubmissionId != null)
        {
            var query = from applicationFormSubmission in await _applicationFormSubmissionRepository.GetQueryableAsync()
                        where applicationFormSubmission.ChefsSubmissionGuid == formSubmissionId.ToString()
                        select applicationFormSubmission;
            applicationFormSubmissionData = await AsyncExecuter.FirstOrDefaultAsync(query);
        }
        return applicationFormSubmissionData;
    }

    public async Task<PagedResultDto<FormSubmissionSummaryDto>> GetSubmissionsList(Guid? formId)
    {
        if (formId == null)
        {
            throw new ApiException(400, "Missing required parameter 'formId' when calling ListFormSubmissions");
        }

        var request = new RestRequest($"/forms/{formId}/submissions", Method.Get)
            .AddParameter("fields", _summaryFieldsFilter.JoinAsString(","));

        var response = await _intakeClient.GetAsync(request);

        if (((int)response.StatusCode) >= 400)
        {
            throw new ApiException((int)response.StatusCode, "Error calling ListFormSubmissions: " + response.Content, response.ErrorMessage ?? $"{response.StatusCode}");
        }
        else if (((int)response.StatusCode) == 0)
        {
            throw new ApiException((int)response.StatusCode, "Error calling ListFormSubmissions: " + response.ErrorMessage, response.ErrorMessage ?? $"{response.StatusCode}");
        }

        List<FormSubmissionSummaryDto>? jsonResponse = JsonSerializer.Deserialize<List<FormSubmissionSummaryDto>>(response.Content ?? string.Empty, _jsonSerializerOptions);

        if (null == jsonResponse)
        {
            return new PagedResultDto<FormSubmissionSummaryDto>(0, new List<FormSubmissionSummaryDto>());
        }
        else
        {
            // Remove all deleted and draft submissions
            jsonResponse.RemoveAll(r => r.Deleted || r.FormSubmissionStatusCode != "SUBMITTED");
            return new PagedResultDto<FormSubmissionSummaryDto>(jsonResponse.Count, jsonResponse);
        }
    }
}