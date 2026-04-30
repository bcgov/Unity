using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.IO;
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
using Unity.Modules.Shared.Http;
using System.Net.Http;
using Unity.GrantManager.Integrations;

namespace Unity.GrantManager.Intakes;

[Authorize]
public class SubmissionAppService(
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IRepository<ApplicationForm, Guid> applicationFormRepository,
        IEndpointManagementAppService endpointManagementAppService,
        IResilientHttpRequest resilientRestClient,
        IStringEncryptionService stringEncryptionService,
        IApplicationRepository applicationRepository,
        ITenantRepository tenantRepository
        ) : GrantManagerAppService, ISubmissionAppService
{

    protected new ILogger Logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

    public async Task<object?> GetSubmission(Guid? formSubmissionId)
    {
        if (formSubmissionId == null)
        {
            throw new ApiException(400, "Missing required parameter 'formId' when calling GetSubmission");
        }

        ApplicationForm? applicationForm = await GetApplicationFormBySubmissionId(formSubmissionId) ?? throw new ApiException(400, "Missing Form configuration");
        if (applicationForm.ChefsApplicationFormGuid == null)
        {
            throw new ApiException(400, "Missing CHEFS form Id");
        }

        if (applicationForm.ApiKey == null)
        {
            throw new ApiException(400, "Missing CHEFS Api Key");
        }

        ApplicationFormSubmission? applicationFormSubmisssion = await GetApplicationFormSubmissionBySubmissionId(formSubmissionId);
        return applicationFormSubmisssion == null
            ? throw new ApiException(400, "Missing Form Submission")
            : (object)applicationFormSubmisssion.Submission;
    }

    [AllowAnonymous]
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

        ApplicationForm? applicationForm = await GetApplicationFormBySubmissionId(formSubmissionId) ?? throw new ApiException(400, "Missing Form configuration");
        if (applicationForm.ChefsApplicationFormGuid == null)
        {
            throw new ApiException(400, "Missing CHEFS form Id");
        }

        if (applicationForm.ApiKey == null)
        {
            throw new ApiException(400, "Missing CHEFS Api Key");
        }

        string chefsApi = await endpointManagementAppService.GetChefsApiBaseUrlAsync();
        string url = $"{chefsApi}/files/{chefsFileAttachmentId}";
        var decryptedApiKey = stringEncryptionService.Decrypt(applicationForm.ApiKey!);

        var response = await resilientRestClient.HttpAsync(
            HttpMethod.Get,
            url,
            null,
            null,
            basicAuth: (applicationForm.ChefsApplicationFormGuid!, decryptedApiKey ?? string.Empty)
        );

        if (((int)response.StatusCode) != 200)
        {
            var errorContent = response.Content != null ? await response.Content.ReadAsStringAsync() : string.Empty;
            throw new ApiException((int)response.StatusCode, "Error calling GetChefsFileAttachment: " + errorContent, response.ReasonPhrase ?? $"{response.StatusCode}");
        }

        var contentBytes = response.Content != null ? await response.Content.ReadAsByteArrayAsync() : [];
        var contentType = response.Content?.Headers?.ContentType?.MediaType ?? "application/octet-stream";
        // Check and decode the file name if it is URL encoded
        if (!string.IsNullOrEmpty(name) && name.Contains('%'))
        {
            name = Uri.UnescapeDataString(name);
        }

        return new BlobDto { Name = name, Content = contentBytes, ContentType = contentType };
    }

    [AllowAnonymous]
    public async Task<ChefsFileAttachmentStream> GetChefsFileAttachmentStream(Guid? formSubmissionId, Guid? chefsFileAttachmentId, string name)
    {
        if (formSubmissionId == null)
        {
            throw new ApiException(400, "Missing required parameter 'formId' when calling GetSubmission");
        }

        if (chefsFileAttachmentId == null)
        {
            throw new ApiException(400, "Missing required parameter 'chefsFileAttachmentId' when calling GetFileAttachment");
        }

        ApplicationForm? applicationForm = await GetApplicationFormBySubmissionId(formSubmissionId) ?? throw new ApiException(400, "Missing Form configuration");
        if (applicationForm.ChefsApplicationFormGuid == null)
        {
            throw new ApiException(400, "Missing CHEFS form Id");
        }

        if (applicationForm.ApiKey == null)
        {
            throw new ApiException(400, "Missing CHEFS Api Key");
        }

        string chefsApi = await endpointManagementAppService.GetChefsApiBaseUrlAsync();
        string url = $"{chefsApi}/files/{chefsFileAttachmentId}";
        var decryptedApiKey = stringEncryptionService.Decrypt(applicationForm.ApiKey!);

        using var response = await resilientRestClient.HttpAsync(
            HttpMethod.Get,
            url,
            null,
            null,
            basicAuth: (applicationForm.ChefsApplicationFormGuid!, decryptedApiKey ?? string.Empty),
            completionOption: HttpCompletionOption.ResponseHeadersRead
        );

        if (((int)response.StatusCode) != 200)
        {
            var errorContent = response.Content != null ? await response.Content.ReadAsStringAsync() : string.Empty;
            throw new ApiException((int)response.StatusCode, "Error calling GetChefsFileAttachment: " + errorContent, response.ReasonPhrase ?? $"{response.StatusCode}");
        }

        var contentType = response.Content?.Headers?.ContentType?.MediaType ?? "application/octet-stream";
        var extension = !string.IsNullOrEmpty(name) ? Path.GetExtension(Uri.UnescapeDataString(name)) : string.Empty;
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");

        try
        {
            await using (var writeStream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var contentStream = await response.Content!.ReadAsStreamAsync())
            {
                await contentStream.CopyToAsync(writeStream);
            }

            var readStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);
            return new ChefsFileAttachmentStream(readStream, contentType);
        }
        catch
        {
            TryDeleteTempFile(tempPath);
            throw;
        }
    }

    private static void TryDeleteTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch
        {
            // Best-effort cleanup; never throw from cleanup path.
        }
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

    public async Task<PagedResultDto<FormSubmissionSummaryDto>> GetSubmissionsListAsync(GetSubmissionsListInput input)
    {
        var chefsSubmissions = new List<FormSubmissionSummaryDto>();
        var serializerOptions = CreateJsonSerializerOptions();
        var tenants = await tenantRepository.GetListAsync();
        var unityRefNos = new HashSet<string>();
        var checkedForms = new HashSet<string>();

        if (!string.IsNullOrWhiteSpace(input.TenantName))
        {
            tenants = tenants
                .Where(t => string.Equals(t.Name, input.TenantName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        foreach (var tenant in tenants)
        {
            using (CurrentTenant.Change(tenant.Id))
            {
                //This could be overloaded to include the input.DateFrom, and input.DateTo when calling applicationRepository,
                //the problem is we need to use all forms returned from app repository to search Chefs via IDs.
                //so date filtering at the moment occurs post-processing
                await ProcessTenantSubmissions(tenant, chefsSubmissions, serializerOptions, checkedForms, unityRefNos);
            }
        }

        foreach (var submission in chefsSubmissions)
        {
            submission.inUnity = unityRefNos.Contains(submission.ConfirmationId.ToString());
        }

        if (input.DateFrom.HasValue || input.DateTo.HasValue)
        {
            DateTime? exclusiveUpperBoundForDateTo = null;
            if (input.DateTo.HasValue && input.DateTo.Value.TimeOfDay == TimeSpan.Zero)
            {
                // Inclusive end-of-day
                exclusiveUpperBoundForDateTo = input.DateTo.Value.Date.AddDays(1);
            }
            chefsSubmissions.RemoveAll(submission =>
                (input.DateFrom.HasValue && submission.CreatedAt < input.DateFrom.Value) ||
                (input.DateTo.HasValue && (
                    exclusiveUpperBoundForDateTo.HasValue
                        ? submission.CreatedAt >= exclusiveUpperBoundForDateTo.Value
                        : submission.CreatedAt > input.DateTo.Value
                )));
        }

        if (!input.ReturnAllSubmissions)
        {
            chefsSubmissions.RemoveAll(submission => submission.inUnity);
        }

        return new PagedResultDto<FormSubmissionSummaryDto>(chefsSubmissions.Count, chefsSubmissions);
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
    private async Task ProcessTenantSubmissions(
        Tenant tenant,
        List<FormSubmissionSummaryDto> chefsSubmissions,
        JsonSerializerOptions serializerOptions,
        HashSet<string> checkedForms,
        HashSet<string> unityRefNos)
    {
        // Replace the invalid method call with `WithFullDetailsAsync`.
        var groupedResult = await applicationRepository.WithFullDetailsAsync(0, int.MaxValue, null, null);
        var appDtos = new List<GrantApplicationDto>();
        var rowCounter = 0;

        foreach (var application in groupedResult)
        {
            var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(application);
            appDto.RowCount = rowCounter++;
            appDtos.Add(appDto);

            await FetchChefsSubmissions(tenant, appDto, chefsSubmissions, serializerOptions, checkedForms);
        }

        unityRefNos.UnionWith(appDtos
                          .Select(a => a.ReferenceNo)
                          .Where(r => !string.IsNullOrWhiteSpace(r))
                          .ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    private async Task FetchChefsSubmissions(
        Tenant tenant,
        GrantApplicationDto appDto,
        List<FormSubmissionSummaryDto> chefsSubmissions,
        JsonSerializerOptions serializerOptions,
        HashSet<string> checkedForms)
    {
        var formGuid = appDto.ApplicationForm.ChefsApplicationFormGuid ?? string.Empty;
        if (!checkedForms.Add(formGuid)) return;

        var apiKey = stringEncryptionService.Decrypt(appDto.ApplicationForm.ApiKey!);

        try
        {
            var chefsApi = await endpointManagementAppService.GetChefsApiBaseUrlAsync();
            var url = $"{chefsApi}/forms/{formGuid}/submissions?fields=applicantAgent.name";
            var response = await resilientRestClient.HttpAsync(
                HttpMethod.Get,
                url,
                null,
                null,
                basicAuth: (formGuid, apiKey ?? string.Empty)
            );

            var contentString = response.Content != null ? await response.Content.ReadAsStringAsync() : "[]";
            var submissions = JsonSerializer.Deserialize<List<FormSubmissionSummaryDto>>(
                                  contentString,
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
            Logger.LogError(ex, "GetSubmissionsList Exception: {Message}", ex.Message);
        }
    }
}
