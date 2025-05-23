using Microsoft.AspNetCore.Authorization;
using Polly;
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
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Encryption;
using Volo.Abp.TenantManagement;
using static Unity.Modules.Shared.UnitySelector.Notification;

namespace Unity.GrantManager.Intakes;

[Authorize]
public class SubmissionAppService : GrantManagerAppService, ISubmissionAppService
{
    private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
    private readonly IRepository<ApplicationForm, Guid> _applicationFormRepository;
    private readonly RestClient _intakeClient;
    private readonly IStringEncryptionService _stringEncryptionService;
    private readonly IApplicationRepository _applicationRepository;
    private readonly ITenantRepository _tenantRepository;
    private static List<string> SummaryFieldsFilter
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
                "totalRequest"
            });
        }
    }

    public SubmissionAppService(
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IRepository<ApplicationForm, Guid> applicationFormRepository,
        RestClient restClient,
        IStringEncryptionService stringEncryptionService,
        IApplicationRepository applicationRepository,
        ITenantRepository tenantRepository
        )
    {
        _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
        _applicationFormRepository = applicationFormRepository;
        _intakeClient = restClient;
        _stringEncryptionService = stringEncryptionService;
        _applicationRepository = applicationRepository;
        _tenantRepository = tenantRepository;
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
        //if (formId == null)
        //{
        //    throw new ApiException(400, "Missing required parameter 'formId' when calling ListFormSubmissions");
        //} 
        string ? apiKey = Environment.GetEnvironmentVariable("API_KEY");
        var id = "166bc157-9a68-4ef2-8559-7d680b6870b4";

        var request = new RestRequest($"/forms/{id}/submissions", Method.Get)
            //.AddParameter("draft", true)
            .AddParameter("fields", "applicantAgent.name");
        request.Authenticator = new HttpBasicAuthenticator(id, apiKey ?? "no api key given");

        var response = await _intakeClient.GetAsync(request);

        if (((int)response.StatusCode) >= 400)
        {
            throw new ApiException((int)response.StatusCode, "Error calling ListFormSubmissions: " + response.Content, response.ErrorMessage ?? $"{response.StatusCode}");
        }
        else if (((int)response.StatusCode) == 0)
        {
            throw new ApiException((int)response.StatusCode, "Error calling ListFormSubmissions: " + response.ErrorMessage, response.ErrorMessage ?? $"{response.StatusCode}");
        }

        var submissionOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        List<FormSubmissionSummaryDto>? jsonResponse = JsonSerializer.Deserialize<List<FormSubmissionSummaryDto>>(response.Content ?? string.Empty, submissionOptions);
        
        if (null == jsonResponse)
        {
            return new PagedResultDto<FormSubmissionSummaryDto>(0, new List<FormSubmissionSummaryDto>());
        }
        else
        {

            var tenants = await _tenantRepository.GetListAsync();
            foreach (var tenant in tenants)
            {
                System.Diagnostics.Debug.WriteLine($"{tenant.Id} - {tenant.Name}");
                using (CurrentTenant.Change(tenant.Id))
                {
                    var groupedResult = await _applicationRepository.WithFullDetailsGroupedAsync(0, 100);
                    var appDtos = new List<GrantApplicationDto>();
                    var rowCounter = 0;
                    foreach (var grouping in groupedResult)
                    {
                        var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(grouping.First());
                        appDto.RowCount = rowCounter;
                        appDtos.Add(appDto);
                        rowCounter++;
                        System.Diagnostics.Debug.WriteLine(appDto.ReferenceNo);
                        System.Diagnostics.Debug.WriteLine(appDto.CreationTime); 
                    }

                    jsonResponse.RemoveAll(r => appDtos.Any(appDto => r.ConfirmationId.ToString() == appDto.ReferenceNo));
                }
            }


            // Remove all deleted and draft submissions
            //System.Diagnostics.Debug.WriteLine(JsonSerializer.Serialize(jsonResponse, submissionOptions));
            //jsonResponse.RemoveAll(r => r.Deleted || r.FormSubmissionStatusCode != "SUBMITTED");
            jsonResponse.RemoveAll(r => r.Deleted);
            return new PagedResultDto<FormSubmissionSummaryDto>(jsonResponse.Count, jsonResponse);
        }
    }
}