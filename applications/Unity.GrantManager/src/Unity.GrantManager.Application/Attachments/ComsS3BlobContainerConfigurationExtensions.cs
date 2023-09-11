using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.BlobStoring;

namespace Unity.GrantManager.Attachments;

public static class ComsS3BlobContainerConfigurationExtensions
{
    public static BlobContainerConfiguration UseComsS3CustomBlobProvider(this BlobContainerConfiguration containerConfiguration, Action<ComsS3BlobProviderConfiguration> configureAction)
    {
        containerConfiguration.ProviderType = typeof(ComsS3BlobProvider);
        configureAction.Invoke(new ComsS3BlobProviderConfiguration(containerConfiguration));
        return containerConfiguration;
    }

    public static ComsS3BlobProviderConfiguration GetComsS3BlobProviderConfiguration(this BlobContainerConfiguration containerConfiguration) 
    {
        return new ComsS3BlobProviderConfiguration(containerConfiguration);
    }
}
