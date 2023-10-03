using Volo.Abp.BlobStoring;

namespace Unity.GrantManager.Attachments;

public class ComsS3BlobProviderConfiguration
{
    private readonly BlobContainerConfiguration _containerConfiguration;
    public ComsS3BlobProviderConfiguration(
            BlobContainerConfiguration containerConfiguration)
    {
        _containerConfiguration = containerConfiguration;
    }
    public string AccessKeyId
    {
        get => _containerConfiguration
                .GetConfiguration<string>("ComsS3BlobProvider.AccessKeyId");
        set => _containerConfiguration
            .SetConfiguration("ComsS3BlobProvider.AccessKeyId", value);
    }
    public string Bucket
    {
        get => _containerConfiguration
                .GetConfiguration<string>("ComsS3BlobProvider.Bucket");
        set => _containerConfiguration
            .SetConfiguration("ComsS3BlobProvider.Bucket", value);
    }
    public string Endpoint
    {
        get => _containerConfiguration
                .GetConfiguration<string>("ComsS3BlobProvider.Endpoint");
        set => _containerConfiguration
            .SetConfiguration("ComsS3BlobProvider.Endpoint", value);
    }
    public string SecretAccessKey
    {
        get => _containerConfiguration
                .GetConfiguration<string>("ComsS3BlobProvider.SecretAccessKey");
        set => _containerConfiguration
            .SetConfiguration("ComsS3BlobProvider.SecretAccessKey", value);
    }
    public string ApplicationS3Folder
    {
        get => _containerConfiguration
                .GetConfiguration<string>("ComsS3BlobProvider.ApplicationS3Folder");
        set => _containerConfiguration
            .SetConfiguration("ComsS3BlobProvider.ApplicationS3Folder", value);
    }
    public string AssessmentS3Folder
    {
        get => _containerConfiguration
                .GetConfiguration<string>("ComsS3BlobProvider.AssessmentS3Folder");
        set => _containerConfiguration
            .SetConfiguration("ComsS3BlobProvider.AssessmentS3Folder", value);
    }    
}
