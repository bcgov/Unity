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
using Volo.Abp.Users;

namespace Unity.GrantManager.Attachments;

public class ComsS3BlobProvider : BlobProviderBase, ITransientDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApplicationAttachmentRepository _applicationAttachmentRepository;
    private readonly IAdjudicationAttachmentRepository _adjudicationAttachmentRepository;

    public ComsS3BlobProvider(IHttpContextAccessor httpContextAccessor, IApplicationAttachmentRepository attachmentRepository, IAdjudicationAttachmentRepository adjudicationAttachmentRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _applicationAttachmentRepository = attachmentRepository;
        _adjudicationAttachmentRepository = adjudicationAttachmentRepository;   
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
        var queryParams = _httpContextAccessor.HttpContext.Request.Query;
        if (queryParams.TryGetValue("AttachmentType", out StringValues attachmentType))
        {
            if(attachmentType.ToString() == "Application")
            {
                StringValues applicationId;
                StringValues currentUserId;
                queryParams.TryGetValue("ApplicationId", out applicationId);
                queryParams.TryGetValue("CurrentUserId", out currentUserId);
                await UploadApplicationAttachment(args, applicationId.ToString(), currentUserId.ToString());
            } else if (attachmentType.ToString() == "Adjudication")
            {
                StringValues assessmentId;
                StringValues currentUserId;
                queryParams.TryGetValue("AssessmentId", out assessmentId);
                queryParams.TryGetValue("CurrentUserId", out currentUserId);
                await UploadAdjudicationAttachment(args, assessmentId.ToString(), currentUserId.ToString());
            } else
            {
                throw new Exception("Invalid AttachmentType:" + attachmentType.ToString());
            }
        } else
        {
            throw new Exception("Missing parameter:AttachmentType");
        }
    }

    private async Task UploadAdjudicationAttachment(BlobProviderSaveArgs args, string assessmentId, string currentUserId)
    {
        var response = await UploadToComsS3(args, args.Configuration.GetComsS3BlobProviderConfiguration().AdjudicationBucketId);
        if ((int)response.StatusCode == 409)
        {
            throw new Exception("File Already Existing.");
        }
        else if ((int)response.StatusCode == 200)
        {
            response.EnsureSuccessStatusCode();
            var responseStr = await response.Content.ReadAsStringAsync();
            BlobSaveResult result = JsonSerializer.Deserialize<BlobSaveResult>(responseStr);


            await _adjudicationAttachmentRepository.InsertAsync(
                new AdjudicationAttachment
                {
                    AdjudicationId = new Guid(assessmentId),
                    S3Guid = result.id,
                    UserId = new Guid(currentUserId),
                    FileName = args.BlobName,
                    Time = DateTime.Now,
                });
        }
        else
        {
            throw new Exception("Error in uploading file. Http Status Code:" + response.StatusCode);
        }
    }

    private async Task UploadApplicationAttachment(BlobProviderSaveArgs args, string applicationId, string currentUserId)
    {
        var response = await UploadToComsS3(args, args.Configuration.GetComsS3BlobProviderConfiguration().ApplicationBucketId);
        if ((int)response.StatusCode == 409)
        {
            throw new Exception("File Already Existing.");
        }
        else if ((int)response.StatusCode == 200)
        {
            response.EnsureSuccessStatusCode();
            var responseStr = await response.Content.ReadAsStringAsync();
            BlobSaveResult result = JsonSerializer.Deserialize<BlobSaveResult>(responseStr);


            await _applicationAttachmentRepository.InsertAsync(
                new ApplicationAttachment
                {
                    ApplicationId = new Guid(applicationId),
                    S3Guid = result.id,
                    UserId = currentUserId,
                    FileName = args.BlobName,
                    Time = DateTime.Now,                    
                });
        }
        else
        {
            throw new Exception("Error in uploading file. Http Status Code:" + response.StatusCode);
        }
    }

    private async Task<HttpResponseMessage> UploadToComsS3(BlobProviderSaveArgs args, string bucketId)
    {
        var config = args.Configuration.GetComsS3BlobProviderConfiguration();        
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
        return response;
    }
}

class BlobSaveResult
{
    public Guid id { get; set; }
}
