using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Security.Encryption;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Data;

public class TenantConnectionStringEncryptionMigrator(
    ITenantRepository tenantRepository,
    IStringEncryptionService encryptionService) : ITransientDependency
{
    public async Task MigrateAsync()
    {
        var tenants = await tenantRepository.GetListAsync(includeDetails: true);

        foreach (var tenant in tenants)
        {
            // IsPlainText returns true when the value is not valid ciphertext (not yet encrypted)
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

    private bool IsPlainText(string value)
    {
        try
        {
            encryptionService.Decrypt(value);
            return false;
        }
        catch
        {
            return true;
        }
    }
}
