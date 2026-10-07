using Microsoft.Extensions.Configuration;
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
    public class DynamicUrlDataSeeder(IDynamicUrlRepository DynamicUrlRepository, ICurrentTenant currentTenant, IConfiguration configuration) : IDataSeedContributor, ITransientDependency
    {
        // Optional per-key URL used only when a missing row is inserted, e.g. "DynamicUrls:Seed:INTAKE_API_BASE".
        // Intended for local development (DbMigrator appsettings.secrets.json); deployed migrators leave it unset.
        public const string SeedConfigSection = "DynamicUrls:Seed";

        public async Task SeedAsync(DataSeedContext context)
        {
            await SeedDynamicUrlAsync();
        }

        public static class DynamicUrls
        {
            public const string PROTOCOL = "https:";
            public const string ORGBOOK_PROD_URL = $"{PROTOCOL}//orgbook.gov.bc.ca/api";
            public const string CSS_API_BASE_URL = $"{PROTOCOL}//api.loginproxy.gov.bc.ca/api/v1";
            public const string CSS_TOKEN_API_BASE_URL = $"{PROTOCOL}//loginproxy.gov.bc.ca/auth/realms/standard/protocol/openid-connect/token";
            public const string GEOCODER_BASE_URL = $"{PROTOCOL}//openmaps.gov.bc.ca/geo/pub/ows?service=WFS&version=1.0.0&request=GetFeature&typeName=";
            public const string GEOCODER_LOCATION_BASE_URL = $"{PROTOCOL}//geocoder.api.gov.bc.ca";
            public const string GITHUB_REPO = $"{PROTOCOL}//github.com/bcgov/Unity";
            public const string GITHUB_GRAPHQL = $"{PROTOCOL}//api.github.com/graphql";
        }

        private async Task SeedDynamicUrlAsync()
        {
            if (currentTenant == null || currentTenant.Id == null)
            {
                int messageIndex = 0;
                int webhookIndex = 0;
                // Only endpoints that are identical in every environment carry a URL. Environment-specific
                // ones are seeded blank (unless SeedConfigSection supplies one) and set per environment
                // through Endpoint Management. For local development, put the seed URLs in the gitignored
                // src/Unity.GrantManager.DbMigrator/appsettings.secrets.json under "DynamicUrls": { "Seed": { ... } }.
                var dynamicUrls = new List<DynamicUrl>
                {
                    new() { KeyName = DynamicUrlKeyNames.GEOCODER_API_BASE, Url = DynamicUrls.GEOCODER_BASE_URL, Description = "Geocoder API Base" },
                    new() { KeyName = DynamicUrlKeyNames.GEOCODER_LOCATION_API_BASE, Url = DynamicUrls.GEOCODER_LOCATION_BASE_URL, Description = "Geocoder Location API Base" },
                    new() { KeyName = DynamicUrlKeyNames.CSS_API_BASE, Url = DynamicUrls.CSS_API_BASE_URL, Description = "Common Single Sign-on Services API" },
                    new() { KeyName = DynamicUrlKeyNames.CSS_TOKEN_API_BASE, Url = DynamicUrls.CSS_TOKEN_API_BASE_URL, Description = "Common Single Sign-on Token API" },
                    new() { KeyName = DynamicUrlKeyNames.PAYMENT_API_BASE, Url = "", Description = "BC Corporate Accounting Services API" },
                    new() { KeyName = DynamicUrlKeyNames.ORGBOOK_API_BASE, Url = DynamicUrls.ORGBOOK_PROD_URL, Description = "OrgBook Services API" },
                    new() { KeyName = DynamicUrlKeyNames.INTAKE_API_BASE, Url = "", Description = "Common Hosted Forms Service API" },
                    new() { KeyName = DynamicUrlKeyNames.NOTIFICATION_API_BASE, Url = "", Description = "Common Hosted Email Service API" },
                    new() { KeyName = DynamicUrlKeyNames.REPORTING_AI, Url = "", Description = "Reporting AI iFrame Source" },
                    new() { KeyName = DynamicUrlKeyNames.NOTIFICATION_AUTH, Url = "", Description = "Common Hosted Email Service OAUTH" },
                    new() { KeyName = DynamicUrlKeyNames.ANALYTICS_MATOMO_BASE, Url = "", Description = "Matomo Analytics" },
                    new() { KeyName = DynamicUrlKeyNames.GITHUB_REPO, Url = DynamicUrls.GITHUB_REPO, Description = "GitHub Repository" },
                    new() { KeyName = DynamicUrlKeyNames.GITHUB_GRAPHQL, Url = DynamicUrls.GITHUB_GRAPHQL, Description = "GitHub GraphQL Endpoint" },
                    new() { KeyName = DynamicUrlKeyNames.METABASE_API_BASE, Url = "", Description = "Metabase Reporting API" },
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
                        var seedUrl = configuration[$"{SeedConfigSection}:{dynamicUrl.KeyName}"];
                        if (!string.IsNullOrWhiteSpace(seedUrl))
                        {
                            dynamicUrl.Url = seedUrl;
                        }

                        await DynamicUrlRepository.InsertAsync(dynamicUrl);
                    }
                }
            }
        }
    }
}
