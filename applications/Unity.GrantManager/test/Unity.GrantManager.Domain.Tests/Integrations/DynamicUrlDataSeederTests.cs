using Microsoft.Extensions.Configuration;
using Shouldly;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Integrations;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.Domain.Tests.Integrations;

public class DynamicUrlDataSeederTests : GrantManagerDomainTestBase
{
    private readonly IDynamicUrlRepository _dynamicUrlRepository;
    private readonly DynamicUrlDataSeeder _seeder;

    public DynamicUrlDataSeederTests()
    {
        _dynamicUrlRepository = GetRequiredService<IDynamicUrlRepository>();
        _seeder = GetRequiredService<DynamicUrlDataSeeder>();
    }

    // Environment-specific endpoints are seeded as blank placeholders - their URL is set per
    // environment through the Endpoint Management admin page, never by the DbMigrator.
    [Theory]
    [InlineData(DynamicUrlKeyNames.ANALYTICS_MATOMO_BASE, "Matomo Analytics")]
    [InlineData(DynamicUrlKeyNames.INTAKE_API_BASE, "Common Hosted Forms Service API")]
    [InlineData(DynamicUrlKeyNames.METABASE_API_BASE, "Metabase Reporting API")]
    [InlineData(DynamicUrlKeyNames.REPORTING_AI, "Reporting AI iFrame Source")]
    [InlineData(DynamicUrlKeyNames.PAYMENT_API_BASE, "BC Corporate Accounting Services API")]
    [InlineData(DynamicUrlKeyNames.NOTIFICATION_API_BASE, "Common Hosted Email Service API")]
    [InlineData(DynamicUrlKeyNames.NOTIFICATION_AUTH, "Common Hosted Email Service OAUTH")]
    public async Task Should_Seed_Blank_Url_For_Environment_Specific_Key(string keyName, string description)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var row = await _dynamicUrlRepository.FirstOrDefaultAsync(s => s.KeyName == keyName);

            row.ShouldNotBeNull();
            row.Url.ShouldBeNullOrEmpty();
            row.Description.ShouldBe(description);
        });
    }

    [Theory]
    [InlineData(DynamicUrlKeyNames.ANALYTICS_MATOMO_BASE, "https://test-analytics-matomo.apps.silver.devops.gov.bc.ca")]
    [InlineData(DynamicUrlKeyNames.INTAKE_API_BASE, "https://chefs-test.apps.silver.devops.gov.bc.ca/app/api/v1")]
    [InlineData(DynamicUrlKeyNames.METABASE_API_BASE, "https://test-unity-reporting.apps.gold.devops.gov.bc.ca")]
    [InlineData(DynamicUrlKeyNames.REPORTING_AI, "https://test-unity-ai-reporting-ce395f-test.apps.gold.devops.gov.bc.ca")]
    [InlineData(DynamicUrlKeyNames.PAYMENT_API_BASE, "https://oci-cfs-systws.cas.gov.bc.ca:7026/ords/cas")]
    [InlineData(DynamicUrlKeyNames.NOTIFICATION_API_BASE, "https://ches-dev.api.gov.bc.ca/api/v1")]
    [InlineData(DynamicUrlKeyNames.NOTIFICATION_AUTH, "https://dev.loginproxy.gov.bc.ca/auth/realms/comsvcauth/protocol/openid-connect/token")]
    public async Task Should_Not_Overwrite_Url_Set_Through_Endpoint_Management(string keyName, string url)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var row = await _dynamicUrlRepository.FirstAsync(s => s.KeyName == keyName);
            row.Url = url;
            await _dynamicUrlRepository.UpdateAsync(row);
        });

        await WithUnitOfWorkAsync(() => _seeder.SeedAsync(new DataSeedContext()));

        await WithUnitOfWorkAsync(async () =>
        {
            var row = await _dynamicUrlRepository.FirstAsync(s => s.KeyName == keyName);
            row.Url.ShouldBe(url);
        });
    }

    // Endpoints that are identical in every environment keep a seeded URL.
    [Theory]
    [InlineData(DynamicUrlKeyNames.CSS_API_BASE, "https://api.loginproxy.gov.bc.ca/api/v1")]
    [InlineData(DynamicUrlKeyNames.ORGBOOK_API_BASE, "https://orgbook.gov.bc.ca/api")]
    [InlineData(DynamicUrlKeyNames.GEOCODER_LOCATION_API_BASE, "https://geocoder.api.gov.bc.ca")]
    public async Task Should_Seed_Url_For_Shared_Key(string keyName, string url)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var row = await _dynamicUrlRepository.FirstAsync(s => s.KeyName == keyName);
            row.Url.ShouldBe(url);
        });
    }

    [Fact]
    public async Task Should_Use_Configured_Seed_Url_When_Inserting_Missing_Row()
    {
        const string chefsDev = "https://chefs-dev.apps.silver.devops.gov.bc.ca/app/api/v1";
        await WithUnitOfWorkAsync(async () =>
        {
            var row = await _dynamicUrlRepository.FirstAsync(s => s.KeyName == DynamicUrlKeyNames.INTAKE_API_BASE);
            await _dynamicUrlRepository.HardDeleteAsync(row);
        });

        await WithUnitOfWorkAsync(() => CreateSeederWithSeedUrl(DynamicUrlKeyNames.INTAKE_API_BASE, chefsDev)
            .SeedAsync(new DataSeedContext()));

        await WithUnitOfWorkAsync(async () =>
        {
            var row = await _dynamicUrlRepository.FirstAsync(s => s.KeyName == DynamicUrlKeyNames.INTAKE_API_BASE);
            row.Url.ShouldBe(chefsDev);
        });
    }

    [Fact]
    public async Task Should_Not_Apply_Configured_Seed_Url_To_Existing_Row()
    {
        await WithUnitOfWorkAsync(() => CreateSeederWithSeedUrl(DynamicUrlKeyNames.ANALYTICS_MATOMO_BASE, "https://dev-analytics-matomo.apps.silver.devops.gov.bc.ca")
            .SeedAsync(new DataSeedContext()));

        await WithUnitOfWorkAsync(async () =>
        {
            var row = await _dynamicUrlRepository.FirstAsync(s => s.KeyName == DynamicUrlKeyNames.ANALYTICS_MATOMO_BASE);
            row.Url.ShouldBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task Should_Not_Duplicate_Rows_When_Seeded_Again()
    {
        var before = await WithUnitOfWorkAsync(() => _dynamicUrlRepository.GetCountAsync());

        await WithUnitOfWorkAsync(() => _seeder.SeedAsync(new DataSeedContext()));

        var after = await WithUnitOfWorkAsync(() => _dynamicUrlRepository.GetCountAsync());
        after.ShouldBe(before);
    }

    private DynamicUrlDataSeeder CreateSeederWithSeedUrl(string keyName, string url)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DynamicUrlDataSeeder.SeedConfigSection}:{keyName}"] = url
            })
            .Build();

        return new DynamicUrlDataSeeder(_dynamicUrlRepository, GetRequiredService<ICurrentTenant>(), configuration);
    }
}
