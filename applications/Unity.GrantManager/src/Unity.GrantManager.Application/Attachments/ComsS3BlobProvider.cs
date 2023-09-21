using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Attachments;

public class ComsS3BlobProvider : BlobProviderBase, ITransientDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApplicationAttachmentRepository _applicationAttachmentRepository;

    public ComsS3BlobProvider(IHttpContextAccessor httpContextAccessor, IApplicationAttachmentRepository attachmentRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _applicationAttachmentRepository = attachmentRepository;
    }

    public override Task<bool> DeleteAsync(BlobProviderDeleteArgs args)
    {
        throw new NotImplementedException();
    }

    public override Task<bool> ExistsAsync(BlobProviderExistsArgs args)
    {
        throw new NotImplementedException();
    }

    public override Task<Stream> GetOrNullAsync(BlobProviderGetArgs args)
    {
        throw new NotImplementedException();
    }

    public override async Task SaveAsync(BlobProviderSaveArgs args)
    {
        var config = args.Configuration.GetComsS3BlobProviderConfiguration();
        var bucketId = config.BucketId;
        var baseUri = config.BaseUri;
        var username = config.Username;
        var password = config.Password;
        if (!baseUri.EndsWith("/"))
        {
            baseUri += "/";
        }
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Put, baseUri + "object?bucketId=" + bucketId)
        {
            Content = new StreamContent(args.BlobStream)
        };
        request.Content.Headers.Remove("Content-Disposition");
        var contentDisposition = "attachment; filename=" + Uri.EscapeDataString(args.BlobName) + "; filename*=UTF-8''" + Uri.EscapeDataString(args.BlobName);
        request.Content.Headers.Add("Content-Disposition", contentDisposition);
        var authenticationString = $"{username}:{password}";
        var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));

        request.Headers.Add("Authorization", "Basic " + base64EncodedAuthenticationString);

        var response = await client.SendAsync(request);
        if ((int)response.StatusCode == 409)
        {
            throw new Exception("File Already Existing.");
        }
        else if ((int)response.StatusCode == 200)
        {
            response.EnsureSuccessStatusCode();
            var responseStr = await response.Content.ReadAsStringAsync();
            BlobSaveResult? result = JsonSerializer.Deserialize<BlobSaveResult>(responseStr);
            if (result != null)
            {
                var queryParams = _httpContextAccessor.HttpContext.Request.Query;
                if (queryParams.TryGetValue("ApplicationId", out StringValues applicationId) && queryParams.TryGetValue("CurrentUserId", out StringValues currentUserId))
                {
                    await _applicationAttachmentRepository.InsertAsync(
                        new ApplicationAttachment
                        {
                            ApplicationId = new Guid(applicationId.ToString()),
                            S3Guid = result.Id,
                            UserId = currentUserId.ToString(),
                            Time = DateTime.Now,
                            FileName = args.BlobName,
                        });
                }
            }
        }
        else
        {
            throw new Exception("Error in uploading file. Http Status Code:" + response.StatusCode);
        }

    }
}

class BlobSaveResult
{
    public Guid Id { get; set; }
}
