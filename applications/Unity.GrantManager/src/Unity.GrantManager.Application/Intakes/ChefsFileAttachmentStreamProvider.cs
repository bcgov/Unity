using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Intakes;

public class ChefsFileAttachmentStreamProvider(
    IChefsAttachmentDownloadService chefsAttachmentDownloadService,
    ILogger<ChefsFileAttachmentStreamProvider> logger)
    : IChefsFileAttachmentStreamProvider, ITransientDependency
{
    public async Task<ChefsFileAttachmentStream> OpenAsync(Guid formSubmissionId, Guid chefsFileAttachmentId, string name)
    {
        try
        {
            var file = await chefsAttachmentDownloadService.DownloadAsync(formSubmissionId, chefsFileAttachmentId, name);
            var content = file.Content ?? [];
            var stream = new MemoryStream(content, writable: false);

            logger.LogInformation(
                "Opened CHEFS attachment {ChefsFileAttachmentId} for submission {FormSubmissionId}. ContentType: {ContentType}; DownloadedLength: {DownloadedLength}.",
                chefsFileAttachmentId,
                formSubmissionId,
                file.ContentType,
                content.Length);

            return new ChefsFileAttachmentStream(stream, file.ContentType);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to open CHEFS attachment {ChefsFileAttachmentId} for submission {FormSubmissionId}.",
                chefsFileAttachmentId,
                formSubmissionId);
            throw;
        }
    }
}
