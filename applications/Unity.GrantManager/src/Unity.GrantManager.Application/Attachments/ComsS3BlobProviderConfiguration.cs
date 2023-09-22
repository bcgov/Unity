using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public string BucketId
    {
        get => _containerConfiguration
                .GetConfiguration<string>("ComsS3BlobProvider.BucketId");
        set => _containerConfiguration
            .SetConfiguration("ComsS3BlobProvider.BucketId", value);
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
