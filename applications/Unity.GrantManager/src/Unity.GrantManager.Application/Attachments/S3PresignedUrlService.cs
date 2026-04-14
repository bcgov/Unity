using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.RegularExpressions;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Attachments;

public partial class S3PresignedUrlService : IS3PresignedUrlService, ITransientDependency
{
    private readonly AmazonS3Client _amazonS3Client;
    private readonly string _bucket;

    public S3PresignedUrlService(IConfiguration configuration)
    {
        _bucket = configuration["S3:Bucket"] ?? throw new InvalidOperationException("Missing configuration: S3:Bucket");

        AmazonS3Config s3config = new()
        {
            RegionEndpoint = null,
            ServiceURL = configuration["S3:Endpoint"],
            AllowAutoRedirect = true,
            ForcePathStyle = true
        };

        _amazonS3Client = new AmazonS3Client(
            configuration["S3:AccessKeyId"],
            configuration["S3:SecretAccessKey"],
            s3config);
    }

    public string GetPresignedUrl(string s3ObjectKey, int expiryMinutes = 10)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = EscapeKeyFileName(s3ObjectKey),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Verb = HttpVerb.GET
        };

        return _amazonS3Client.GetPreSignedURL(request);
    }

    private static string EscapeKeyFileName(string s3ObjectKey)
    {
        Regex regex = S3KeysRegex();
        string[] keys = regex.Split(s3ObjectKey);
        string escapedName = Uri.EscapeDataString(keys[^1]);
        keys[^1] = escapedName;
        return string.Join("", keys);
    }

    [GeneratedRegex("(/)")]
    private static partial Regex S3KeysRegex();
}
