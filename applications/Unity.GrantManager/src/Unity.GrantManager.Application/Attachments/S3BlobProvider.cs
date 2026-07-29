using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Authorization;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Attachments;

public partial class S3BlobProvider : BlobProviderBase, ITransientDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApplicationAttachmentRepository _applicationAttachmentRepository;
    private readonly IAssessmentAttachmentRepository _assessmentAttachmentRepository;
    private readonly IApplicantAttachmentRepository _applicantAttachmentRepository;
    private readonly IAmazonS3 _amazonS3Client;
    private readonly ICurrentUser _currentUser;

    public S3BlobProvider(IHttpContextAccessor httpContextAccessor, IApplicationAttachmentRepository attachmentRepository, IAssessmentAttachmentRepository assessmentAttachmentRepository, IApplicantAttachmentRepository applicantAttachmentRepository, IAmazonS3 amazonS3Client, ICurrentUser currentUser)
    {
        _httpContextAccessor = httpContextAccessor;
        _applicationAttachmentRepository = attachmentRepository;
        _assessmentAttachmentRepository = assessmentAttachmentRepository;
        _applicantAttachmentRepository = applicantAttachmentRepository;
        _amazonS3Client = amazonS3Client;
        _currentUser = currentUser;
    }

    public override async Task<bool> DeleteAsync(BlobProviderDeleteArgs args)
    {
        string s3ObjectKey = args.BlobName;
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HttpContext.");
        var form = httpContext.Request?.Form ?? throw new InvalidOperationException("No form data in the current request.");
        string attachmentType = form.TryGetValue("AttachmentType", out var typeValue) ? typeValue.ToString() : string.Empty;
        string attachmentTypeId = form.TryGetValue("AttachmentTypeId", out var idValue) ? idValue.ToString() : string.Empty;
        var config = args.Configuration.GetS3BlobProviderConfiguration();
        
        var deleteObjectRequest = new DeleteObjectRequest
        {
            BucketName = config.Bucket,
            Key = EscapeKeyFileName(s3ObjectKey)
        };
        
        await _amazonS3Client.DeleteObjectAsync(deleteObjectRequest);

        // Also delete the cached preview PDF if one was generated (S3 DeleteObject is idempotent)
        var lastSlash = s3ObjectKey.LastIndexOf('/');
        if (lastSlash >= 0)
        {
            var previewKey = s3ObjectKey[..lastSlash] + "/preview/" + s3ObjectKey[(lastSlash + 1)..] + ".pdf";
            await _amazonS3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = config.Bucket,
                Key = EscapeKeyFileName(previewKey)
            });
        }

        if (attachmentType == "Application")
        {
            if (attachmentTypeId.IsNullOrEmpty())
            {
                throw new AbpValidationException("Missing ApplicationId");
            }
            IQueryable<ApplicationAttachment> queryableAttachment = await _applicationAttachmentRepository.GetQueryableAsync();
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
            IQueryable<AssessmentAttachment> queryableAttachment = await _assessmentAttachmentRepository.GetQueryableAsync();
            AssessmentAttachment? attachment = queryableAttachment.FirstOrDefault(a => a.S3ObjectKey.Equals(s3ObjectKey) && a.AssessmentId.Equals(new Guid(attachmentTypeId.ToString())));
            if (attachment != null)
            {
                await _assessmentAttachmentRepository.DeleteAsync(attachment);
            }
        }
        else if (attachmentType == "Applicant")
        {
            if (attachmentTypeId.IsNullOrEmpty())
            {
                throw new AbpValidationException("Missing ApplicantId");
            }
            IQueryable<ApplicantAttachment> queryableAttachment = await _applicantAttachmentRepository.GetQueryableAsync();
            ApplicantAttachment? attachment = queryableAttachment.FirstOrDefault(a => a.S3ObjectKey.Equals(s3ObjectKey) && a.ApplicantId.Equals(new Guid(attachmentTypeId.ToString())));
            if (attachment != null)
            {
                await _applicantAttachmentRepository.DeleteAsync(attachment);
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
        await responseStream.CopyToAsync(memoryStream);
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
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HttpContext.");
        var routeData = httpContext.GetRouteData();

        var assessmentId = routeData.Values["assessmentId"];
        var applicationId = routeData.Values["applicationId"];
        var applicantId = routeData.Values["applicantId"];

        // The uploader must be the authenticated caller, not a client-supplied value - a userId
        // taken from the request (query string, form field, etc.) can be set to any GUID by the
        // caller, letting one user attribute an upload to another user's identity. A missing
        // ICurrentUser.Id means this was reached without authentication (AttachmentController
        // requires [Authorize], so this should never happen) - fail loudly rather than silently
        // attributing the upload to Guid.Empty, which would look like a valid, specific user.
        var currentUserId = _currentUser.Id ?? throw new AbpAuthorizationException("Cannot save an attachment without an authenticated user.");

        if (assessmentId != null)
        {
            await UploadAssessmentAttachment(args, assessmentId.ToString()!, currentUserId);
        }
        else if(applicationId != null)
        {
            await UploadApplicationAttachment(args, applicationId.ToString()!, currentUserId);
        }
        else if (applicantId != null)
        {
            await UploadApplicantAttachment(args, applicantId.ToString()!, currentUserId);
        }
        else
        {
            throw new AbpValidationException("Missing parameter: applicationId/assessmentId/applicantId");
        }
    }

    private async Task UploadAssessmentAttachment(BlobProviderSaveArgs args, string assessmentId, Guid currentUserId)
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
                   UserId = currentUserId,
                   FileName = args.BlobName,
                   Time = DateTime.UtcNow,
               });
        }
        else
        {
            attachment.UserId = currentUserId;
            attachment.FileName = args.BlobName;
            attachment.Time = DateTime.UtcNow;
            await _assessmentAttachmentRepository.UpdateAsync(attachment);
        }
    }

    private async Task UploadApplicationAttachment(BlobProviderSaveArgs args, string applicationId, Guid currentUserId)
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
                    UserId = currentUserId,
                    FileName = args.BlobName,
                    Time = DateTime.UtcNow,
                });
        }
        else
        {
            attachment.UserId = currentUserId;
            attachment.FileName = args.BlobName;
            attachment.Time = DateTime.UtcNow;
            await _applicationAttachmentRepository.UpdateAsync(attachment);
        }
    }

    private async Task UploadApplicantAttachment(BlobProviderSaveArgs args, string applicantId, Guid currentUserId)
    {
        var config = args.Configuration.GetS3BlobProviderConfiguration();
        var bucket = config.Bucket;
        var folder = args.Configuration.GetS3BlobProviderConfiguration().ApplicantS3Folder;
        if (!folder.EndsWith('/'))
        {
            folder += "/";
        }
        folder += applicantId;
        var key = folder + "/" + args.BlobName;
        var escapedKey = folder + "/" + Uri.EscapeDataString(args.BlobName);
        var mimeType = GetMimeType(args.BlobName);
        await UploadToS3(args, bucket, escapedKey, mimeType);
        IQueryable<ApplicantAttachment> queryableAttachment = _applicantAttachmentRepository.GetQueryableAsync().Result;
        ApplicantAttachment? attachment = queryableAttachment.FirstOrDefault(a => a.S3ObjectKey.Equals(key) && a.ApplicantId.Equals(new Guid(applicantId)));
        if (attachment == null)
        {
            await _applicantAttachmentRepository.InsertAsync(
                new ApplicantAttachment
                {
                    ApplicantId = new Guid(applicantId),
                    S3ObjectKey = key,
                    UserId = currentUserId,
                    FileName = args.BlobName,
                    Time = DateTime.UtcNow,
                });
        }
        else
        {
            attachment.UserId = currentUserId;
            attachment.FileName = args.BlobName;
            attachment.Time = DateTime.UtcNow;
            await _applicantAttachmentRepository.UpdateAsync(attachment);
        }
    }

    public async Task UploadToS3(BlobProviderSaveArgs args, string bucket, string key, string mimeType)
    {        
        byte[] fileBytes;
        if (args.BlobStream.CanSeek)
        {
            args.BlobStream.Position = 0;
        }        
        using (var memoryStream = new MemoryStream())
        {
            await args.BlobStream.CopyToAsync(memoryStream);
            fileBytes = memoryStream.ToArray();
        }        
        using var uploadStream = new MemoryStream(fileBytes);
        var putRequest = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            ContentType = mimeType,
            InputStream = uploadStream,
            UseChunkEncoding = false,
            DisablePayloadSigning = false
        };
        await _amazonS3Client.PutObjectAsync(putRequest);        
    }
    
}
