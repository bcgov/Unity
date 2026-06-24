using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Security.Encryption;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Data;

public partial class TenantConnectionStringEncryptionMigrator(
    ITenantRepository tenantRepository,
    IStringEncryptionService encryptionService) : ITransientDependency
{
    public async Task MigrateAsync()
    {
        var tenants = await tenantRepository.GetListAsync(includeDetails: true);

        foreach (var tenant in tenants)
        {
            // IsPlainText returns true only when the value isn't valid base64 (i.e. not yet encrypted)
            var plainTextStrings = tenant.ConnectionStrings
                .Where(cs => IsPlainText(cs.Value))
                .ToList();

            foreach (var cs in plainTextStrings)
            {
                tenant.SetConnectionString(cs.Name, encryptionService.Encrypt(cs.Value));
            }

            if (plainTextStrings.Count > 0)
            {
                await tenantRepository.UpdateAsync(tenant);
            }
        }
    }

    // Require at least 2 keyword hits - a single hit (e.g. "Pwd=" or "Uid=") could
    // coincidentally occur at the tail of valid base64 ciphertext, right before the
    // padding '='. Real connection strings always carry several key=value pairs.
    private const int MinKeywordMatches = 2;

    private bool IsPlainText(string value)
    {
        if (ConnectionStringKeywordPattern().Matches(value).Count >= MinKeywordMatches) return true;

        try
        {
            encryptionService.Decrypt(value);
            return false;
        }
        catch (FormatException)
        {
            // Not valid base64, so it can't be ciphertext - it's plain text.
            return true;
        }
        // Valid base64 that failed to decrypt with a CryptographicException (e.g. wrong
        // passphrase, corrupted ciphertext) is left to throw, so we don't silently overwrite
        // undecryptable ciphertext as if it were plain text.
    }

    [GeneratedRegex(@"(Host|Server|Port|Database|Initial Catalog|Username|User Id|Uid|Pwd|Password|Data Source)\s*=",
        RegexOptions.IgnoreCase)]
    private static partial Regex ConnectionStringKeywordPattern();
}
