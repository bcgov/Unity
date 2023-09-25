using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Attachments;

public class ComsS3BlobProvider : BlobProviderBase, ITransientDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApplicationAttachmentRepository _applicationAttachmentRepository;
    private readonly IAdjudicationAttachmentRepository _adjudicationAttachmentRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IAssessmentRepository _assessmentsRepository;

    public ComsS3BlobProvider(IHttpContextAccessor httpContextAccessor, IApplicationAttachmentRepository attachmentRepository, IAdjudicationAttachmentRepository adjudicationAttachmentRepository, IApplicationRepository applicationRepository, IAssessmentRepository assessmentsRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _applicationAttachmentRepository = attachmentRepository;
        _adjudicationAttachmentRepository = adjudicationAttachmentRepository;   
        _applicationRepository = applicationRepository;
        _assessmentsRepository = assessmentsRepository;
    }

    public override async Task<bool> DeleteAsync(BlobProviderDeleteArgs args)
    {
        string s3guid = args.BlobName;
        var attachmentType = _httpContextAccessor.HttpContext.Request.Form["AttachmentType"];        
        var config = args.Configuration.GetComsS3BlobProviderConfiguration();
        var baseUri = config.BaseUri;
        var username = config.Username;
        var password = config.Password;
        if (!baseUri.EndsWith("/"))
        {
            baseUri += "/";
        }
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Delete, baseUri + "object/" + s3guid);
        var authenticationString = $"{username}:{password}";
        var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));
        request.Headers.Add("Authorization", "Basic " + base64EncodedAuthenticationString);
        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode || (int) response.StatusCode==502) // 502 response means file is already deleted
        {
            if(attachmentType == "Application")
            {
                IQueryable<ApplicationAttachment> queryableAttachment = _applicationAttachmentRepository.GetQueryableAsync().Result;
                ApplicationAttachment attachment = queryableAttachment.FirstOrDefault(a => a.S3Guid.Equals(new Guid(s3guid)));
                if(attachment != null)
                {
                    await _applicationAttachmentRepository.DeleteAsync(attachment);
                }
            }
            else if (attachmentType == "Adjudication")
            {
                IQueryable<AdjudicationAttachment> queryableAttachment = _adjudicationAttachmentRepository.GetQueryableAsync().Result;
                AdjudicationAttachment attachment = queryableAttachment.FirstOrDefault(a => a.S3Guid.Equals(new Guid(s3guid)));
                if (attachment != null)
                {
                    await _adjudicationAttachmentRepository.DeleteAsync(attachment);
                }
            }
            else
            {
                throw new AbpValidationException("Wrong AttachmentType:"+attachmentType);
            }
            return await Task.FromResult(true);
        }
        else
        {
            return await Task.FromResult(false);
        }
        
    }

    public override Task<bool> ExistsAsync(BlobProviderExistsArgs args)
    {
        throw new NotImplementedException();
    }

    public override async Task<Stream> GetOrNullAsync(BlobProviderGetArgs args)
    {
        var queryParams = _httpContextAccessor.HttpContext.Request.Query;
        if (queryParams.TryGetValue("S3Guid", out StringValues s3guid))
        {              
            return await DownloadAttachment(args, s3guid.ToString());            
        }
        else
        {
            throw new Exception("Missing parameter:AttachmentType");
        }      
    }    

    private async Task<Stream> DownloadAttachment(BlobProviderGetArgs args, string s3guid)
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
        var request = new HttpRequestMessage(HttpMethod.Get, baseUri + "object/" + s3guid);
        var authenticationString = $"{username}:{password}";
        var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));
        request.Headers.Add("Authorization", "Basic " + base64EncodedAuthenticationString);
        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            System.Net.Http.HttpContent content = response.Content;
            var contentStream = await content.ReadAsStreamAsync(); // get the actual content stream
            return contentStream;
        }
        else
        {
            throw new FileNotFoundException("File not found. S3guid " + s3guid + ".");
        }
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
                StringValues currentUserName;
                queryParams.TryGetValue("ApplicationId", out applicationId);
                queryParams.TryGetValue("CurrentUserId", out currentUserId);
                queryParams.TryGetValue("CurrentUserName", out currentUserName);
                var bucketId = await GetApplicationBucketId(args, applicationId.ToString());
                await UploadApplicationAttachment(args, bucketId, applicationId.ToString(), currentUserId.ToString(), currentUserName.ToString());
            } else if (attachmentType.ToString() == "Adjudication")
            {
                StringValues assessmentId;
                StringValues currentUserId;
                StringValues currentUserName;
                queryParams.TryGetValue("AssessmentId", out assessmentId);
                queryParams.TryGetValue("CurrentUserId", out currentUserId);
                queryParams.TryGetValue("CurrentUserName", out currentUserName);
                var bucketId = await GetAdjudicationBucketId(args, assessmentId.ToString());
                await UploadAdjudicationAttachment(args, bucketId, assessmentId.ToString(), currentUserId.ToString(), currentUserName.ToString());
            } else
            {
                throw new Exception("Invalid AttachmentType:" + attachmentType.ToString());
            }
        } else
        {
            throw new Exception("Missing parameter:AttachmentType");
        }
    }

    private async Task<string> GetAdjudicationBucketId(BlobProviderSaveArgs args, string assessmentId)
    {
        var assessment = await _assessmentsRepository.GetAsync(new Guid(assessmentId));
        if (assessment == null)
        {
            throw new Exception($"Assessment {assessmentId} does not exist");
        }
        if (assessment.S3BucketId == null)
        {
            //create S3 bucketId
            var key = args.Configuration.GetComsS3BlobProviderConfiguration().AdjudicationS3Folder + "/" + assessmentId;
            var bucketId = await CreateS3BucketId(args, key);
            assessment.S3BucketId = bucketId;
            await _assessmentsRepository.UpdateAsync(assessment);
            return bucketId.ToString();
        }
        else
        {
            return assessment.S3BucketId.ToString();
        }
    }

    private async Task<string> GetApplicationBucketId(BlobProviderSaveArgs args, string applicationId)
    {
        var application = await _applicationRepository.GetAsync(new Guid(applicationId));
        if (application == null)
        {
            throw new Exception($"Application {applicationId} does not exist");
        }
        if(application.S3BucketId == null)
        {
            //create S3 bucketId
            var key = args.Configuration.GetComsS3BlobProviderConfiguration().ApplicationS3Folder + "/" + applicationId;
            var bucketId = await CreateS3BucketId(args, key);
            application.S3BucketId = bucketId;
            await _applicationRepository.UpdateAsync(application);
            return bucketId.ToString();
        } else
        {
            return application.S3BucketId.ToString();
        }
    }

    private async Task<Guid?> CreateS3BucketId(BlobProviderSaveArgs args, string key)
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
        var request = new HttpRequestMessage(HttpMethod.Put, baseUri+"bucket");
        var authenticationString = $"{username}:{password}";
        var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));
        request.Headers.Add("Authorization", "Basic " + base64EncodedAuthenticationString);        
        var payload = new CreateBucketPayload
        {
            accessKeyId = config.AccessKeyId,
            active = true,
            bucket = config.Bucket,
            bucketName = key,
            endpoint = config.Endpoint,
            secretAccessKey = config.SecretAccessKey,
            key = key,
        };        
        string jsonData = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonData, null, "application/json");
        request.Content = content;
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        if((int) response.StatusCode != 201)
        {
            throw new NotImplementedException("COMS setup is not yet implemented.");
        }
        var responseStr = await response.Content.ReadAsStringAsync();
        CreateBucketIdResult result = JsonSerializer.Deserialize<CreateBucketIdResult>(responseStr);
        return new Guid(result.bucketId);
    }

    private async Task UploadAdjudicationAttachment(BlobProviderSaveArgs args, string bucketId, string assessmentId, string currentUserId, string currentUserName)
    {
        var response = await UploadToComsS3(args, bucketId);
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
                    AttachedBy = currentUserName,
                    Time = DateTime.Now,
                });
        }
        else
        {
            throw new Exception("Error in uploading file. Http Status Code:" + response.StatusCode);
        }
    }

    private async Task UploadApplicationAttachment(BlobProviderSaveArgs args, string bucketId, string applicationId, string currentUserId, string currentUserName)
    {
        var response = await UploadToComsS3(args, bucketId);
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
                    AttachedBy = currentUserName,
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

class CreateBucketPayload
{
    public string accessKeyId { get; set; }
    public bool active { get; set; }
    public string bucket { get; set; }
    public string bucketName { get; set; }
    public string endpoint { get; set; }
    public string secretAccessKey { get; set; }
    public string key { get; set; }
}

class CreateBucketIdResult
{
    public string bucketId { get; set; }
}
