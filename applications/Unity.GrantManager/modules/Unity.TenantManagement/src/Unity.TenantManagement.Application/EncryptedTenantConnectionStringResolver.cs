using System;
using System.Security.Cryptography;
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
        if (PlainConnectionStringDetector.LooksLikePlainConnectionString(value)) return value;

        try
        {
            return _encryptionService.Decrypt(value);
        }
        catch (FormatException)
        {
            // Not valid base64, so it can't be ciphertext - it's plain text.
            return value;
        }
        catch (CryptographicException)
        {
            // Valid base64 but failed to decrypt (wrong passphrase/corrupted ciphertext) -
            // fall back to the raw value rather than breaking connection resolution.
            return value;
        }
    }
}
