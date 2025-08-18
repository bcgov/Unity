using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Integrations
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(DynamicUrlDataSeeder), typeof(IDataSeedContributor))]
    public class DynamicUrlDataSeeder(IDynamicUrlRepository DynamicUrlRepository) : IDataSeedContributor, ITransientDependency
    {

        public async Task SeedAsync(DataSeedContext context)
        {
            await SeedDynamicUrlAsync();
        }

        public static class DynamicUrls
        {
            public const string PROTOCOL = "https://";
            public const string CHEFS_PROD_URL = $"{PROTOCOL}submit.digital.gov.bc.ca/app/api/v1";
            public const string CAS_PROD_URL = ""; // Not entered for security reasons
            public const string CHES_PROD_URL = $"{PROTOCOL}ches.api.gov.bc.ca/api/v1";
            public const string CHES_PROD_AUTH = $"{PROTOCOL}loginproxy.gov.bc.ca/auth/realms/comsvcauth/protocol/openid-connect/token";
        }

        private async Task SeedDynamicUrlAsync()
        {
            int messageIndex = 0;
            int webhookIndex = 0;
            var dynamicUrls = new List<DynamicUrl>
        {
            new() { KeyName = DynamicUrlKeyNames.PAYMENT_API_BASE, Url = DynamicUrls.CAS_PROD_URL, Description = "BC Corporate Accounting Services API" },
            new() { KeyName = DynamicUrlKeyNames.INTAKE_API_BASE, Url = DynamicUrls.CHEFS_PROD_URL, Description = "Common Hosted Forms Service API" },
            new() { KeyName = DynamicUrlKeyNames.NOTFICATION_API_BASE, Url = DynamicUrls.CHES_PROD_URL, Description = "Common Hosted Email Service API" },
            new() { KeyName = DynamicUrlKeyNames.NOTFICATION_AUTH, Url = DynamicUrls.CHES_PROD_AUTH, Description = "Common Hosted Email Service OAUTH" },
            new() { KeyName = $"{DynamicUrlKeyNames.DIRECT_MESSAGE_KEY_PREFIX}{messageIndex++}", Url = "", Description = $"Direct message webhook {messageIndex}" },
            new() { KeyName = $"{DynamicUrlKeyNames.DIRECT_MESSAGE_KEY_PREFIX}{messageIndex++}", Url = "", Description = $"Direct message webhook {messageIndex}" },
            new() { KeyName = $"{DynamicUrlKeyNames.DIRECT_MESSAGE_KEY_PREFIX}{messageIndex++}", Url = "", Description = $"Direct message webhook {messageIndex}" },
            new() { KeyName = $"{DynamicUrlKeyNames.WEBHOOK_KEY_PREFIX}{webhookIndex++}", Url = "", Description = $"Webhook {webhookIndex}" },
            new() { KeyName = $"{DynamicUrlKeyNames.WEBHOOK_KEY_PREFIX}{webhookIndex++}", Url = "", Description = $"Webhook {webhookIndex}" },
            new() { KeyName = $"{DynamicUrlKeyNames.WEBHOOK_KEY_PREFIX}{webhookIndex++}", Url = "", Description = $"Webhook {webhookIndex}" },
            };

            foreach (var dynamicUrl in dynamicUrls)
            {
                var existing = await DynamicUrlRepository.FirstOrDefaultAsync(s => s.KeyName == dynamicUrl.KeyName);
                if (existing == null)
                {
                    await DynamicUrlRepository.InsertAsync(dynamicUrl);
                }
            }
        }


    }
}
