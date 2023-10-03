using System;
using Volo.Abp.BlobStoring;

namespace Unity.GrantManager.Attachments;

public static class S3BlobContainerConfigurationExtensions
{
    public static BlobContainerConfiguration UseS3CustomBlobProvider(this BlobContainerConfiguration containerConfiguration, Action<S3BlobProviderConfiguration> configureAction)
    {
        containerConfiguration.ProviderType = typeof(S3BlobProvider);
        configureAction.Invoke(new S3BlobProviderConfiguration(containerConfiguration));
        return containerConfiguration;
    }

    public static S3BlobProviderConfiguration GetS3BlobProviderConfiguration(this BlobContainerConfiguration containerConfiguration) 
    {
        return new S3BlobProviderConfiguration(containerConfiguration);
    }
}
