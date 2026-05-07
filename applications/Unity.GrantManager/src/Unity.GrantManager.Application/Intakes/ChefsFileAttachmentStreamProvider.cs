using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Integrations;
using Unity.Modules.Shared.Http;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Encryption;

namespace Unity.GrantManager.Intakes;

public class ChefsFileAttachmentStreamProvider(
    IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
    IRepository<ApplicationForm, Guid> applicationFormRepository,
    IEndpointManagementAppService endpointManagementAppService,
    IResilientHttpRequest resilientRestClient,
    IStringEncryptionService stringEncryptionService)
    : IChefsFileAttachmentStreamProvider, ITransientDependency
{
    public async Task<ChefsFileAttachmentStream> OpenAsync(Guid formSubmissionId, Guid chefsFileAttachmentId, string name)
    {
        var applicationForm = await GetApplicationFormBySubmissionId(formSubmissionId)
            ?? throw new ApiException(400, "Missing Form configuration");

        if (applicationForm.ChefsApplicationFormGuid == null)
        {
            throw new ApiException(400, "Missing CHEFS form Id");
        }

        if (applicationForm.ApiKey == null)
        {
            throw new ApiException(400, "Missing CHEFS Api Key");
        }

        var chefsApi = await endpointManagementAppService.GetChefsApiBaseUrlAsync();
        var url = $"{chefsApi}/files/{chefsFileAttachmentId}";
        var decryptedApiKey = stringEncryptionService.Decrypt(applicationForm.ApiKey);

        using var response = await resilientRestClient.HttpAsync(
            HttpMethod.Get,
            url,
            null,
            null,
            basicAuth: (applicationForm.ChefsApplicationFormGuid, decryptedApiKey ?? string.Empty),
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

    private async Task<ApplicationForm?> GetApplicationFormBySubmissionId(Guid formSubmissionId)
    {
        var submission = await applicationFormSubmissionRepository.FirstOrDefaultAsync(
            x => x.ChefsSubmissionGuid == formSubmissionId.ToString());

        return submission == null
            ? null
            : await applicationFormRepository.FirstOrDefaultAsync(x => x.Id == submission.ApplicationFormId);
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
}
