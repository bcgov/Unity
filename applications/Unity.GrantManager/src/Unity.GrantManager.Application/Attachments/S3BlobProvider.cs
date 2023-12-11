using Amazon.S3.Model;
using Amazon.S3;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Validation;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;

namespace Unity.GrantManager.Attachments;

public partial class S3BlobProvider : BlobProviderBase, ITransientDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApplicationAttachmentRepository _applicationAttachmentRepository;
    private readonly IAssessmentAttachmentRepository _assessmentAttachmentRepository;    
    private readonly AmazonS3Client _amazonS3Client;

    public S3BlobProvider(IHttpContextAccessor httpContextAccessor, IApplicationAttachmentRepository attachmentRepository, IAssessmentAttachmentRepository assessmentAttachmentRepository, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _applicationAttachmentRepository = attachmentRepository;
        _assessmentAttachmentRepository = assessmentAttachmentRepository;

        AmazonS3Config s3config = new()
        {
            RegionEndpoint = null,
            ServiceURL = configuration["S3:Endpoint"],
            AllowAutoRedirect = true,
            ForcePathStyle = true
        };


        AmazonS3Client s3Client = new(
                configuration["S3:AccessKeyId"],
                configuration["S3:SecretAccessKey"],
                s3config
                );
        _amazonS3Client = s3Client;
    }    

    public override async Task<bool> DeleteAsync(BlobProviderDeleteArgs args)
    {
        string s3ObjectKey = args.BlobName;
        var attachmentType = _httpContextAccessor.HttpContext.Request.Form["AttachmentType"];
        var attachmentTypeId = _httpContextAccessor.HttpContext.Request.Form["AttachmentTypeId"];
        var config = args.Configuration.GetS3BlobProviderConfiguration();
        
        var deleteObjectRequest = new DeleteObjectRequest
        {
            BucketName = config.Bucket,
            Key = EscapeKeyFileName(s3ObjectKey)
        };
        
        await _amazonS3Client.DeleteObjectAsync(deleteObjectRequest);
        if (attachmentType == "Application")
        {
            if (attachmentTypeId.IsNullOrEmpty())
            {
                throw new AbpValidationException("Missing ApplicationId");
            }
            IQueryable<ApplicationAttachment> queryableAttachment = _applicationAttachmentRepository.GetQueryableAsync().Result;
            ApplicationAttachment? attachment = queryableAttachment.FirstOrDefault(a => a.S3ObjectKey.Equals(s3ObjectKey) && a.ApplicationId.Equals(new Guid(attachmentTypeId.ToString())));
            if (attachment != null)
            {
                await _applicationAttachmentRepository.DeleteAsync(attachment);
            }
        }
        else if (attachmentType == "Assessment")
        {
            if (attachmentTypeId.IsNullOrEmpty())
            {
                throw new AbpValidationException("Missing AssessmentId");
            }
            IQueryable<AssessmentAttachment> queryableAttachment = _assessmentAttachmentRepository.GetQueryableAsync().Result;
            AssessmentAttachment? attachment = queryableAttachment.FirstOrDefault(a => a.S3ObjectKey.Equals(s3ObjectKey) && a.AssessmentId.Equals(new Guid(attachmentTypeId.ToString())));
            if (attachment != null)
            {
                await _assessmentAttachmentRepository.DeleteAsync(attachment);
            }
        }
        else
        {
            throw new AbpValidationException("Wrong AttachmentType:"+attachmentType);
        }
        return await Task.FromResult(true);
        
        
    }

    private static string EscapeKeyFileName(string s3ObjectKey)
    {
        Regex regex= S3KeysRegex();
        string[] keys = regex.Split(s3ObjectKey);
        string escapedName = Uri.EscapeDataString(keys[^1]);
        keys[^1] = escapedName;
        return string.Join("", keys);
    }

    [GeneratedRegex("(/)")]
    private static partial Regex S3KeysRegex();

    public override Task<bool> ExistsAsync(BlobProviderExistsArgs args)
    {
        throw new NotImplementedException();
    }

    public override async Task<Stream?> GetOrNullAsync(BlobProviderGetArgs args)
    {       
        var config = args.Configuration.GetS3BlobProviderConfiguration();

        var getObjectRequest = new GetObjectRequest
        {
            BucketName = config.Bucket,
            Key = EscapeKeyFileName(args.BlobName)
        }; 
        using GetObjectResponse response = await _amazonS3Client.GetObjectAsync(getObjectRequest);
        MemoryStream memoryStream = new();
        using Stream responseStream = response.ResponseStream;
        responseStream.CopyTo(memoryStream);
        return memoryStream;              
    }    

    private static string GetMimeType(string fileName)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }

    public override async Task SaveAsync(BlobProviderSaveArgs args)
    {
        var queryParams = _httpContextAccessor.HttpContext.Request.Query;
        var routeData = _httpContextAccessor.HttpContext.GetRouteData();
        var assessmentId = routeData.Values["assessmentId"];
        
        if (assessmentId != null)
        {            
            queryParams.TryGetValue("userId", out StringValues currentUserId);
            queryParams.TryGetValue("userName", out StringValues currentUserName);
#pragma warning disable CS8604 // Possible null reference argument.
            await UploadAssessmentAttachment(args, assessmentId.ToString(), currentUserId.ToString());
#pragma warning restore CS8604 // Possible null reference argument.
        }
        else
        {
            var applicationId = routeData.Values["applicationId"];
            if(applicationId != null)
            {
                queryParams.TryGetValue("userId", out StringValues currentUserId);
                queryParams.TryGetValue("userName", out StringValues currentUserName);
#pragma warning disable CS8604 // Possible null reference argument.
                await UploadApplicationAttachment(args, applicationId.ToString(), currentUserId.ToString());
#pragma warning restore CS8604 // Possible null reference argument.
            }
            else
            {
                throw new AbpValidationException("Missing parameter: applicationId/assessmentId");
            }
        }       
    }    
    
    private async Task UploadAssessmentAttachment(BlobProviderSaveArgs args, string assessmentId, string currentUserId)
    {
        var config = args.Configuration.GetS3BlobProviderConfiguration();
        var bucket = config.Bucket;
        var folder = args.Configuration.GetS3BlobProviderConfiguration().AssessmentS3Folder;
        if (!folder.EndsWith('/'))
        {
            folder += "/";
        }
        folder += assessmentId;
        var key = folder + "/" + args.BlobName; 
        var escapedKey = folder + "/" + Uri.EscapeDataString(args.BlobName);
        var mimeType = GetMimeType(args.BlobName);
        await UploadToS3(args, bucket, escapedKey, mimeType);
        IQueryable<AssessmentAttachment> queryableAttachment = _assessmentAttachmentRepository.GetQueryableAsync().Result;
        AssessmentAttachment? attachment = queryableAttachment.FirstOrDefault(a => a.S3ObjectKey.Equals(key) && a.AssessmentId.Equals(new Guid(assessmentId)));
        if (attachment == null)
        {
            await _assessmentAttachmentRepository.InsertAsync(
               new AssessmentAttachment
               {
                   AssessmentId = new Guid(assessmentId),
                   S3ObjectKey = key,
                   UserId = new Guid(currentUserId),
                   FileName = args.BlobName,
                   Time = DateTime.UtcNow,
               });
        }
        else
        {
            attachment.UserId = new Guid(currentUserId);
            attachment.FileName = args.BlobName;
            attachment.Time = DateTime.UtcNow;
            await _assessmentAttachmentRepository.UpdateAsync(attachment);
        }
    }

    private async Task UploadApplicationAttachment(BlobProviderSaveArgs args, string applicationId, string currentUserId)
    {
        var config = args.Configuration.GetS3BlobProviderConfiguration();
        var bucket = config.Bucket;
        var folder = args.Configuration.GetS3BlobProviderConfiguration().ApplicationS3Folder;
        if (!folder.EndsWith('/'))
        {
            folder += "/";
        }
        folder += applicationId;
        var key = folder + "/" + args.BlobName;
        var escapedKey = folder + "/" + Uri.EscapeDataString(args.BlobName);
        var mimeType = GetMimeType(args.BlobName);
        await UploadToS3(args,bucket, escapedKey, mimeType);
        IQueryable<ApplicationAttachment> queryableAttachment = _applicationAttachmentRepository.GetQueryableAsync().Result;
        ApplicationAttachment? attachment = queryableAttachment.FirstOrDefault(a => a.S3ObjectKey.Equals(key) && a.ApplicationId.Equals(new Guid(applicationId)));
        if (attachment == null)
        {
            await _applicationAttachmentRepository.InsertAsync(
                new ApplicationAttachment
                {
                    ApplicationId = new Guid(applicationId),
                    S3ObjectKey = key,
                    UserId = new Guid(currentUserId),
                    FileName = args.BlobName,                    
                    Time = DateTime.UtcNow,
                });
        }
        else
        {
            attachment.UserId = new Guid(currentUserId);
            attachment.FileName = args.BlobName;            
            attachment.Time = DateTime.UtcNow;
            await _applicationAttachmentRepository.UpdateAsync(attachment);
        }
    }

    public async Task UploadToS3(BlobProviderSaveArgs args, string bucket, string key, string mimeType)
    {
        PutObjectRequest putRequest = new()
        {
            BucketName = bucket,
            Key = key,
            ContentType = mimeType,
            InputStream = args.BlobStream,
        };

        await _amazonS3Client.PutObjectAsync(putRequest);
    }

    
}
