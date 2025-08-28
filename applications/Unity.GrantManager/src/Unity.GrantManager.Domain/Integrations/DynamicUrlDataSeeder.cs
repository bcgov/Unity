using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Integrations
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(DynamicUrlDataSeeder), typeof(IDataSeedContributor))]
    public class DynamicUrlDataSeeder(IDynamicUrlRepository DynamicUrlRepository, ICurrentTenant currentTenant) : IDataSeedContributor, ITransientDependency
    {

        public async Task SeedAsync(DataSeedContext context)
        {
            await SeedDynamicUrlAsync();
        }

        public static class DynamicUrls
        {
            public const string PROTOCOL = "https:";
            public const string CHEFS_PROD_URL = $"{PROTOCOL}//submit.digital.gov.bc.ca/app/api/v1";
            public const string CAS_PROD_URL = $"{PROTOCOL}//cfs-systws.cas.gov.bc.ca:7026/ords/cas"; // Not entered for security reasons
            public const string CHES_PROD_URL = $"{PROTOCOL}//ches.api.gov.bc.ca/api/v1";
            public const string CHES_PROD_AUTH = $"{PROTOCOL}//loginproxy.gov.bc.ca/auth/realms/comsvcauth/protocol/openid-connect/token";
            public const string ORGBOOK_PROD_URL = $"{PROTOCOL}//orgbook.gov.bc.ca/api";
            public const string CSS_API_BASE_URL = $"{PROTOCOL}//api.loginproxy.gov.bc.ca/api/v1";
            public const string CSS_TOKEN_API_BASE_URL = $"{PROTOCOL}//loginproxy.gov.bc.ca/auth/realms/standard/protocol/openid-connect/token";
            public const string GEOCODER_BASE_URL = $"{PROTOCOL}//openmaps.gov.bc.ca/geo/pub/ows?service=WFS&version=1.0.0&request=GetFeature&typeName=";
            public const string GEOCODER_LOCATION_BASE_URL = $"{PROTOCOL}//geocoder.api.gov.bc.ca";
        }

        private async Task SeedDynamicUrlAsync()
        {
            if (currentTenant == null || currentTenant.Id == null)
            {
                int messageIndex = 0;
                int webhookIndex = 0;
                var dynamicUrls = new List<DynamicUrl>
                {
                    new() { KeyName = DynamicUrlKeyNames.GEOCODER_API_BASE, Url = DynamicUrls.GEOCODER_BASE_URL, Description = "Geocoder API Base" },
                    new() { KeyName = DynamicUrlKeyNames.GEOCODER_LOCATION_API_BASE, Url = DynamicUrls.GEOCODER_LOCATION_BASE_URL, Description = "Geocoder Location API Base" },
                    new() { KeyName = DynamicUrlKeyNames.CSS_API_BASE, Url = DynamicUrls.CSS_API_BASE_URL, Description = "Common Single Sign-on Services API" },
                    new() { KeyName = DynamicUrlKeyNames.CSS_TOKEN_API_BASE, Url = DynamicUrls.CSS_TOKEN_API_BASE_URL, Description = "Common Single Sign-on Token API" },
                    new() { KeyName = DynamicUrlKeyNames.PAYMENT_API_BASE, Url = DynamicUrls.CAS_PROD_URL, Description = "BC Corporate Accounting Services API" },
                    new() { KeyName = DynamicUrlKeyNames.ORGBOOK_API_BASE, Url = DynamicUrls.ORGBOOK_PROD_URL, Description = "OrgBook Services API" },
                    new() { KeyName = DynamicUrlKeyNames.INTAKE_API_BASE, Url = DynamicUrls.CHEFS_PROD_URL, Description = "Common Hosted Forms Service API" },
                    new() { KeyName = DynamicUrlKeyNames.NOTIFICATION_API_BASE, Url = DynamicUrls.CHES_PROD_URL, Description = "Common Hosted Email Service API" },
                    new() { KeyName = DynamicUrlKeyNames.NOTIFICATION_AUTH, Url = DynamicUrls.CHES_PROD_AUTH, Description = "Common Hosted Email Service OAUTH" },
                    (() => { var currentMessageIndex = messageIndex++; return new DynamicUrl { KeyName = $"{DynamicUrlKeyNames.DIRECT_MESSAGE_KEY_PREFIX}{currentMessageIndex}", Url = "", Description = $"Direct message webhook {currentMessageIndex}" }; })(),
                    (() => { var currentMessageIndex = messageIndex++; return new DynamicUrl { KeyName = $"{DynamicUrlKeyNames.DIRECT_MESSAGE_KEY_PREFIX}{currentMessageIndex}", Url = "", Description = $"Direct message webhook {currentMessageIndex}" }; })(),
                    (() => { var currentMessageIndex = messageIndex++; return new DynamicUrl { KeyName = $"{DynamicUrlKeyNames.DIRECT_MESSAGE_KEY_PREFIX}{currentMessageIndex}", Url = "", Description = $"Direct message webhook {currentMessageIndex}" }; })(),
                    (() => { var currentWebhookIndex = webhookIndex++; return new DynamicUrl { KeyName = $"{DynamicUrlKeyNames.WEBHOOK_KEY_PREFIX}{currentWebhookIndex}", Url = "", Description = $"Webhook {currentWebhookIndex}" }; })(),
                    (() => { var currentWebhookIndex = webhookIndex++; return new DynamicUrl { KeyName = $"{DynamicUrlKeyNames.WEBHOOK_KEY_PREFIX}{currentWebhookIndex}", Url = "", Description = $"Webhook {currentWebhookIndex}" }; })(),
                    (() => { var currentWebhookIndex = webhookIndex++; return new DynamicUrl { KeyName = $"{DynamicUrlKeyNames.WEBHOOK_KEY_PREFIX}{currentWebhookIndex}", Url = "", Description = $"Webhook {currentWebhookIndex}" }; })(),
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
}
