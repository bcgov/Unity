using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Encryption;

namespace Unity.TenantManagement.Application;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IConnectionStringResolver))]
public class EncryptedTenantConnectionStringResolver : MultiTenantConnectionStringResolver
{
    private readonly IStringEncryptionService _encryptionService;

    public EncryptedTenantConnectionStringResolver(
        IOptionsMonitor<AbpDbConnectionOptions> options,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider,
        IStringEncryptionService encryptionService)
        : base(options, currentTenant, serviceProvider)
    {
        _encryptionService = encryptionService;
    }

    public override async Task<string> ResolveAsync(string connectionStringName = null)
    {
        var value = await base.ResolveAsync(connectionStringName);
        if (string.IsNullOrEmpty(value)) return value;
        try
        {
            var decrypted = _encryptionService.Decrypt(value);
            return (decrypted != null && decrypted.Contains('=')) ? decrypted : value;
        }
        catch
        {
            return value;
        }
    }
}
