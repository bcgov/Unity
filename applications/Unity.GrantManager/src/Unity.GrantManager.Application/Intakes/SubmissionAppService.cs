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

    public async Task<PagedResultDto<FormSubmissionSummaryDto>> GetSubmissionsList(bool allSubmissions)
    {
        var chefsSubmissions = new List<FormSubmissionSummaryDto>();
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var tenants = await tenantRepository.GetListAsync();
        foreach (var tenant in tenants)
        {
            using (CurrentTenant.Change(tenant.Id))
            {
                var groupedResult = await applicationRepository.WithFullDetailsGroupedAsync(0, int.MaxValue);
                var appDtos = new List<GrantApplicationDto>();
                var rowCounter = 0;

                var checkedForms = new HashSet<string>();

                foreach (var grouping in groupedResult)
                {
                    var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(grouping.First());
                    appDto.RowCount = rowCounter++;
                    appDtos.Add(appDto);

                    // Chef's API call to get submissions
                    var formGuid = appDto.ApplicationForm.ChefsApplicationFormGuid ?? string.Empty;
                    if (!checkedForms.Add(formGuid)) continue;   // already queried this form


                    var apiKey = stringEncryptionService.Decrypt(appDto.ApplicationForm.ApiKey!);
                    var request = new RestRequest($"/forms/{formGuid}/submissions", Method.Get)
                                    .AddParameter("fields", "applicantAgent.name");
                    request.Authenticator = new HttpBasicAuthenticator(formGuid, apiKey ?? string.Empty);

                    try
                    {
                        var response = await restClient.GetAsync(request);
                        var submissions = JsonSerializer.Deserialize<List<FormSubmissionSummaryDto>>(
                                              response.Content ?? "[]",
                                              serializerOptions) ?? [];

                        foreach (var s in submissions)
                        {
                            s.tenant = tenant.Name;
                            s.form = appDto.ApplicationForm.ApplicationFormName ?? string.Empty;
                            s.category = appDto.ApplicationForm.Category ?? string.Empty;
                        }

                        chefsSubmissions.AddRange(submissions);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "GetSubmissionsList Exception: {Message}", ex.Message);
                    }
                }

                var unityRefNos = appDtos
                                  .Select(a => a.ReferenceNo)
                                  .Where(r => !string.IsNullOrWhiteSpace(r))
                                  .ToHashSet(StringComparer.OrdinalIgnoreCase);

                logger.LogInformation("Total CHEFS: {Chefs}  | Total Unity: {Unity}",
                                      chefsSubmissions.Count, unityRefNos.Count);

                // Set inUnity flag
                foreach (var submission in chefsSubmissions)
                {
                    submission.inUnity = unityRefNos.Contains(submission.ConfirmationId.ToString());
                }

                // Remove duplicates unless caller asked for *all* submissions
                if (!allSubmissions)
                {
                    chefsSubmissions.RemoveAll(s => unityRefNos.Contains(s.ConfirmationId.ToString()));
                }
            }
        }

        chefsSubmissions.RemoveAll(r => r.Deleted);
        return new PagedResultDto<FormSubmissionSummaryDto>(chefsSubmissions.Count, chefsSubmissions);
    }

}