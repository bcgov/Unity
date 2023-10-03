using Volo.Abp.BlobStoring;

namespace Unity.GrantManager.Attachments;

public class S3BlobProviderConfiguration
{
    private readonly BlobContainerConfiguration _containerConfiguration;
    public S3BlobProviderConfiguration(
            BlobContainerConfiguration containerConfiguration)
    {
        _containerConfiguration = containerConfiguration;
    }
    public string AccessKeyId
    {
        get => _containerConfiguration
                .GetConfiguration<string>("S3BlobProvider.AccessKeyId");
        set => _containerConfiguration
            .SetConfiguration("S3BlobProvider.AccessKeyId", value);
    }
    public string Bucket
    {
        get => _containerConfiguration
                .GetConfiguration<string>("S3BlobProvider.Bucket");
        set => _containerConfiguration
            .SetConfiguration("S3BlobProvider.Bucket", value);
    }
    public string Endpoint
    {
        get => _containerConfiguration
                .GetConfiguration<string>("S3BlobProvider.Endpoint");
        set => _containerConfiguration
            .SetConfiguration("S3BlobProvider.Endpoint", value);
    }
    public string SecretAccessKey
    {
        get => _containerConfiguration
                .GetConfiguration<string>("S3BlobProvider.SecretAccessKey");
        set => _containerConfiguration
            .SetConfiguration("S3BlobProvider.SecretAccessKey", value);
    }
    public string ApplicationS3Folder
    {
        get => _containerConfiguration
                .GetConfiguration<string>("S3BlobProvider.ApplicationS3Folder");
        set => _containerConfiguration
            .SetConfiguration("S3BlobProvider.ApplicationS3Folder", value);
    }
    public string AssessmentS3Folder
    {
        get => _containerConfiguration
                .GetConfiguration<string>("S3BlobProvider.AssessmentS3Folder");
        set => _containerConfiguration
            .SetConfiguration("S3BlobProvider.AssessmentS3Folder", value);
    }    
}
