using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Attachments;

public class ComsS3BlobProvider : BlobProviderBase, ITransientDependency
{
    public override Task<bool> DeleteAsync(BlobProviderDeleteArgs args)
    {
        throw new NotImplementedException();
    }

    public override Task<bool> ExistsAsync(BlobProviderExistsArgs args)
    {
        throw new NotImplementedException();
    }

    public override Task<Stream> GetOrNullAsync(BlobProviderGetArgs args)
    {
        throw new NotImplementedException();
    }

    public override Task SaveAsync(BlobProviderSaveArgs args)
    {
        throw new NotImplementedException();
    }
}
