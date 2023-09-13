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
    public string ApplicationBucketId
    {
        get => _containerConfiguration
                .GetConfiguration<string>("ComsS3BlobProvider.ApplicationBucketId");
        set => _containerConfiguration
            .SetConfiguration("ComsS3BlobProvider.ApplicationBucketId", value);
    }
    public string AdjudicationBucketId
    {
        get => _containerConfiguration
                .GetConfiguration<string>("ComsS3BlobProvider.AdjudicationBucketId");
        set => _containerConfiguration
            .SetConfiguration("ComsS3BlobProvider.AdjudicationBucketId", value);
    }

    public string BaseUri
    {
        get => _containerConfiguration
                .GetConfiguration<string>("ComsS3BlobProvider.BaseUri");
        set => _containerConfiguration
            .SetConfiguration("ComsS3BlobProvider.BaseUri", value);
    }

    public string Username
    {
        get => _containerConfiguration
                .GetConfiguration<string>("ComsS3BlobProvider.Username");
        set => _containerConfiguration
            .SetConfiguration("ComsS3BlobProvider.Username", value);
    }
    public string Password
    {
        get => _containerConfiguration
                .GetConfiguration<string>("ComsS3BlobProvider.Password");
        set => _containerConfiguration
            .SetConfiguration("ComsS3BlobProvider.Password", value);
    }
}
