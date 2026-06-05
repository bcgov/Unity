using System;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.Integrations;
using Unity.Modules.Shared.Http;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Security.Encryption;

namespace Unity.GrantManager.Intakes;

public class ChefsAttachmentDownloadService(
    IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
    IRepository<ApplicationForm, Guid> applicationFormRepository,
    IAsyncQueryableExecuter asyncExecuter,
    IEndpointManagementAppService endpointManagementAppService,
    IResilientHttpRequest resilientRestClient,
    IStringEncryptionService stringEncryptionService) : IChefsAttachmentDownloadService, ITransientDependency
{
    public async Task<BlobDto> DownloadAsync(Guid? formSubmissionId, Guid? chefsFileAttachmentId, string name)
    {
        if (formSubmissionId == null)
        {
            throw new ApiException(400, "Missing required parameter 'formId' when calling GetSubmission");
        }

        if (chefsFileAttachmentId == null)
        {
            throw new ApiException(400, "Missing required parameter 'chefsFileAttachmentId' when calling GetFileAttachment");
        }

        ApplicationForm? applicationForm = await GetApplicationFormBySubmissionId(formSubmissionId)
            ?? throw new ApiException(400, "Missing Form configuration");

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
        if (!string.IsNullOrEmpty(name) && name.Contains('%'))
        {
            name = Uri.UnescapeDataString(name);
        }

        return new BlobDto { Name = name, Content = contentBytes, ContentType = contentType };
    }

    private async Task<ApplicationForm?> GetApplicationFormBySubmissionId(Guid? formSubmissionId)
    {
        ApplicationForm? applicationFormData = new();

        if (formSubmissionId != null)
        {
            var query = from applicationSubmission in await applicationFormSubmissionRepository.GetQueryableAsync()
                        join applicationForm in await applicationFormRepository.GetQueryableAsync() on applicationSubmission.ApplicationFormId equals applicationForm.Id
                        where applicationSubmission.ChefsSubmissionGuid == formSubmissionId.ToString()
                        select applicationForm;
            applicationFormData = await asyncExecuter.FirstOrDefaultAsync(query);
        }
        return applicationFormData;
    }
}
