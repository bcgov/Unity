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
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Encryption;
using Volo.Abp.TenantManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Intakes;

[Authorize]
public class SubmissionAppService(
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IRepository<ApplicationForm, Guid> applicationFormRepository,
        RestClient restClient,
        IStringEncryptionService stringEncryptionService,
        IApplicationRepository applicationRepository,
        ITenantRepository tenantRepository
        ) : GrantManagerAppService, ISubmissionAppService
{

    protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

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
            Authenticator = new HttpBasicAuthenticator(applicationForm.ChefsApplicationFormGuid!, stringEncryptionService.Decrypt(applicationForm.ApiKey!) ?? string.Empty)
        };

        var response = await restClient.GetAsync(request);


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
            var query = from applicationSubmission in await applicationFormSubmissionRepository.GetQueryableAsync()
                        join applicationForm in await applicationFormRepository.GetQueryableAsync() on applicationSubmission.ApplicationFormId equals applicationForm.Id
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
            var query = from applicationFormSubmission in await applicationFormSubmissionRepository.GetQueryableAsync()
                        where applicationFormSubmission.ChefsSubmissionGuid == formSubmissionId.ToString()
                        select applicationFormSubmission;
            applicationFormSubmissionData = await AsyncExecuter.FirstOrDefaultAsync(query);
        }
        return applicationFormSubmissionData;
    }

    public async Task<PagedResultDto<FormSubmissionSummaryDto>> GetSubmissionsList(Guid? formId)
    {
        List<FormSubmissionSummaryDto> chefsSubmissions = new List<FormSubmissionSummaryDto>();

        var tenants = await tenantRepository.GetListAsync();
        foreach (var tenant in tenants)
        {
            using (CurrentTenant.Change(tenant.Id))
            {
                var groupedResult = await applicationRepository.WithFullDetailsGroupedAsync(0, int.MaxValue);
                var appDtos = new List<GrantApplicationDto>();
                var rowCounter = 0;

                List<string> checkedForms = new List<string>();

                foreach (var grouping in groupedResult)
                {
                    var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(grouping.First());
                    appDto.RowCount = rowCounter;
                    appDtos.Add(appDto);
                    rowCounter++;

                    // Chef's API call to get submissions
                    if (!checkedForms.Contains(appDto.ApplicationForm.ChefsApplicationFormGuid ?? string.Empty))
                    {

                        var id = appDto.ApplicationForm.ChefsApplicationFormGuid;
                        var apiKey = stringEncryptionService.Decrypt(appDto.ApplicationForm.ApiKey! ?? string.Empty);
                        var request = new RestRequest($"/forms/{id}/submissions", Method.Get)
                            .AddParameter("fields", "applicantAgent.name");
                        request.Authenticator = new HttpBasicAuthenticator(id ?? "ID", apiKey ?? "no api key given");

                        RestResponse? response = null;
                        try
                        {
                            response = await restClient.GetAsync(request);
                            var submissionOptions = new JsonSerializerOptions
                            {
                                WriteIndented = true,
                                PropertyNameCaseInsensitive = true,
                                ReadCommentHandling = JsonCommentHandling.Skip,
                                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                            };

                            var submissions = JsonSerializer.Deserialize<List<FormSubmissionSummaryDto>>(response.Content ?? string.Empty, submissionOptions);
                            if (submissions != null)
                            {
                                foreach (var submission in submissions)
                                {
                                    submission.tenant = tenant.Name;
                                    submission.form = appDto.ApplicationForm.ApplicationFormName ?? "";
                                }
                                chefsSubmissions.AddRange(submissions);
                            }
                        }
                        catch (Exception ex)
                        {
                            var ExceptionMessage = ex.Message;
                            logger.LogError(ex, "GetSubmissionsList Exception: {ExceptionMessage}", ExceptionMessage);
                        }

                        checkedForms.Add(id ?? string.Empty);
                    }
                }

                // Remove chef's submissions if Unity has an application with the same reference number
                chefsSubmissions.RemoveAll(r => appDtos.Any(appDto => r.ConfirmationId.ToString() == appDto.ReferenceNo));
            }
        }

        // Remove all deleted submissions
        chefsSubmissions.RemoveAll(r => r.Deleted);
        return new PagedResultDto<FormSubmissionSummaryDto>(chefsSubmissions.Count, chefsSubmissions);
    }
}