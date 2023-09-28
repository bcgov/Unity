using Amazon.S3.Model;
using Amazon.S3;
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
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Data.SqlTypes;

namespace Unity.GrantManager.Attachments;

public class ComsS3BlobProvider : BlobProviderBase, ITransientDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApplicationAttachmentRepository _applicationAttachmentRepository;
    private readonly IAdjudicationAttachmentRepository _adjudicationAttachmentRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IAssessmentRepository _assessmentsRepository;    
    private readonly IConfiguration _configuration;
    private readonly AmazonS3Client _amazonS3Client;

    public ComsS3BlobProvider(IHttpContextAccessor httpContextAccessor, IApplicationAttachmentRepository attachmentRepository, IAdjudicationAttachmentRepository adjudicationAttachmentRepository, IApplicationRepository applicationRepository, IAssessmentRepository assessmentsRepository, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _applicationAttachmentRepository = attachmentRepository;
        _adjudicationAttachmentRepository = adjudicationAttachmentRepository;   
        _applicationRepository = applicationRepository;
        _assessmentsRepository = assessmentsRepository;        
        _configuration = configuration;
        AmazonS3Config s3config = new AmazonS3Config();
        s3config.RegionEndpoint = null;
        s3config.ServiceURL = _configuration["S3:Endpoint"];
        s3config.AllowAutoRedirect = true;
        s3config.ForcePathStyle = true;


        AmazonS3Client s3Client = new AmazonS3Client(
                _configuration["S3:AccessKeyId"],
                configuration["S3:SecretAccessKey"],
                s3config
                );
        _amazonS3Client = s3Client;
    }    

    /*public override async Task<bool> DeleteAsync(BlobProviderDeleteArgs args)
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
        
    }*/

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

    

    private string GetMimeType(string fileName)
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
                await UploadApplicationAttachment(args, applicationId.ToString(), currentUserId.ToString(), currentUserName.ToString());
            } else if (attachmentType.ToString() == "Adjudication")
            {
                StringValues assessmentId;
                StringValues currentUserId;
                StringValues currentUserName;
                queryParams.TryGetValue("AssessmentId", out assessmentId);
                queryParams.TryGetValue("CurrentUserId", out currentUserId);
                queryParams.TryGetValue("CurrentUserName", out currentUserName);               
                await UploadAdjudicationAttachment(args, assessmentId.ToString(), currentUserId.ToString(), currentUserName.ToString());
            } else
            {
                throw new AbpValidationException("Invalid AttachmentType:" + attachmentType.ToString());
            }
        } else
        {
            throw new AbpValidationException("Missing parameter:AttachmentType");
        }
    }    
    
    private async Task UploadAdjudicationAttachment(BlobProviderSaveArgs args, string assessmentId, string currentUserId, string currentUserName)
    {
        var config = args.Configuration.GetComsS3BlobProviderConfiguration();
        var bucket = config.Bucket;
        var folder = args.Configuration.GetComsS3BlobProviderConfiguration().AdjudicationS3Folder;
        if (!folder.EndsWith('/'))
        {
            folder += "/";
        }
        folder += assessmentId;
        var key = folder + "/" + args.BlobName;
        var mimeType = GetMimeType(args.BlobName);
        await UploadToS3(args, bucket, key, mimeType);
        IQueryable<AdjudicationAttachment> queryableAttachment = _adjudicationAttachmentRepository.GetQueryableAsync().Result;
        AdjudicationAttachment attachment = queryableAttachment.FirstOrDefault(a => a.S3ObjectKey.Equals(key) && a.AdjudicationId.Equals(new Guid(assessmentId)));
        if (attachment == null)
        {
            await _adjudicationAttachmentRepository.InsertAsync(
               new AdjudicationAttachment
               {
                   AdjudicationId = new Guid(assessmentId),
                   S3ObjectKey = key,
                   UserId = new Guid(currentUserId),
                   FileName = args.BlobName,
                   AttachedBy = currentUserName,
                   Time = DateTime.Now,
               });
        }
        else
        {
            attachment.UserId = new Guid(currentUserId);
            attachment.FileName = args.BlobName;
            attachment.AttachedBy = currentUserName;
            attachment.Time = DateTime.Now;
            await _adjudicationAttachmentRepository.UpdateAsync(attachment);
        }

       
       
    }

    private async Task UploadApplicationAttachment(BlobProviderSaveArgs args, string applicationId, string currentUserId, string currentUserName)
    {
        var config = args.Configuration.GetComsS3BlobProviderConfiguration();
        var bucket = config.Bucket;
        var folder = args.Configuration.GetComsS3BlobProviderConfiguration().ApplicationS3Folder;
        if (!folder.EndsWith('/'))
        {
            folder += "/";
        }
        folder += applicationId;
        var key = folder + "/" + args.BlobName;
        var mimeType = GetMimeType(args.BlobName);
        await UploadToS3(args,bucket,key,mimeType);
        IQueryable<ApplicationAttachment> queryableAttachment = _applicationAttachmentRepository.GetQueryableAsync().Result;
        ApplicationAttachment attachment = queryableAttachment.FirstOrDefault(a => a.S3ObjectKey.Equals(key) && a.ApplicationId.Equals(new Guid(applicationId)));
        if (attachment == null)
        {
            await _applicationAttachmentRepository.InsertAsync(
                new ApplicationAttachment
                {
                    ApplicationId = new Guid(applicationId),
                    S3ObjectKey = key,
                    UserId = currentUserId,
                    FileName = args.BlobName,
                    AttachedBy = currentUserName,
                    Time = DateTime.Now,
                });
        }
        else
        {
            attachment.UserId = currentUserId;
            attachment.FileName = args.BlobName;
            attachment.AttachedBy = currentUserName;
            attachment.Time = DateTime.Now;
            await _applicationAttachmentRepository.UpdateAsync(attachment);
        }
        
        
    }

    public async Task UploadToS3(BlobProviderSaveArgs args, string bucket, string key, string mimeType)
    {
        PutObjectRequest putRequest = new PutObjectRequest
        {
            BucketName = bucket,
            Key = Uri.EscapeDataString(key),
            ContentType = mimeType,
            InputStream = args.BlobStream,
        };

        await _amazonS3Client.PutObjectAsync(putRequest);
    }

    public override Task<bool> DeleteAsync(BlobProviderDeleteArgs args)
    {
        throw new NotImplementedException();
    }
}
