using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Integrations;
using Unity.GrantManager.Integrations.Metabase;
using Unity.Modules.Shared.Features;
using Unity.Modules.Shared.PostTenantCreation;
using Volo.Abp;
using Volo.Abp.Features;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Encryption;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Xunit;

namespace Unity.GrantManager.Tenants.PostCreation.Steps;

public class MetabaseTenantRegistrationStepTests
{
    private const string ReadOnlyConnectionStringName = "Tenant_Readonly";
    private const string DecryptedConnectionString =
        "Host=dev-crunchy-postgres-primary.ce395f-dev.svc;Port=5432;Database=T_ABC123;Username=t_abc123_readonly;Password=s3cr3t;";

    // Tenant's constructors are all non-public (ABP requires going through ITenantManager to
    // create one) - reflection is the standard workaround for exercising its instance state
    // (SetConnectionString/FindConnectionString) in a plain, DB-less unit test.
    private static Tenant CreateTenant(string name)
    {
        var ctor = typeof(Tenant).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(Guid), typeof(string), typeof(string)], null)!;
        return (Tenant)ctor.Invoke([Guid.NewGuid(), name, name.ToUpperInvariant()]);
    }

    private static (MetabaseTenantRegistrationStep Step, IMetabaseApiClient MetabaseApiClient, Tenant Tenant)
        CreateStep(string? apiKey = "test-api-key", bool enabled = true, bool metabaseUrlConfigured = true, bool reportingEnabled = true)
    {
        var tenant = CreateTenant("AG-MARB");
        tenant.SetConnectionString(ReadOnlyConnectionStringName, "encrypted-blob");

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetAsync(tenant.Id, Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>()).Returns(tenant);

        var encryptionService = Substitute.For<IStringEncryptionService>();
        encryptionService.Decrypt(Arg.Any<string>()).Returns(DecryptedConnectionString);

        var metabaseApiClient = Substitute.For<IMetabaseApiClient>();
        var metabaseOptions = Options.Create(new MetabaseOptions
        {
            Enabled = enabled,
            ApiKey = apiKey ?? string.Empty
        });

        var endpointManagementAppService = Substitute.For<IEndpointManagementAppService>();
        endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.METABASE_API_BASE).Returns(metabaseUrlConfigured
            ? Task.FromResult("https://metabase.example")
            : Task.FromException<string>(new UserFriendlyException("URL for key 'METABASE_API_BASE' not configured.")));

        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync(FeatureConsts.Reporting).Returns(Task.FromResult(reportingEnabled));

        var identityUserRepository = Substitute.For<IIdentityUserRepository>();
        identityUserRepository.GetListByNormalizedRoleNameAsync(default!, default, default).ReturnsForAnyArgs(new List<IdentityUser>());

        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(Arg.Any<Guid?>(), Arg.Any<string?>()).Returns(Substitute.For<IDisposable>());

        var provisioner = new MetabaseTenantProvisioner(
            metabaseApiClient, tenantRepository, identityUserRepository, encryptionService,
            Substitute.For<ISettingManager>(), currentTenant, metabaseOptions,
            Substitute.For<ILogger<MetabaseTenantProvisioner>>());

        var step = new MetabaseTenantRegistrationStep(provisioner, metabaseOptions, endpointManagementAppService, featureChecker);

        return (step, metabaseApiClient, tenant);
    }

    [Fact]
    public void ContinueOnError_IsTrue_SoAMetabaseOutageDoesNotBlockLaterSteps()
    {
        var (step, _, _) = CreateStep();

        step.ContinueOnError.ShouldBeTrue();
    }

    [Fact]
    public async Task CanExecuteAsync_MetabaseDisabled_Skips()
    {
        // Disabled wins even when nothing else is configured: the environment doesn't use Metabase.
        var (step, _, tenant) = CreateStep(enabled: false, apiKey: null, metabaseUrlConfigured: false, reportingEnabled: false);

        var check = await step.CanExecuteAsync(tenant.Id);

        check.CanExecute.ShouldBeFalse();
        check.Status.ShouldBe(PostTenantCreationStepStatus.Skipped);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CanExecuteAsync_EnabledButNoApiKeyConfigured_Fails(string? apiKey)
    {
        var (step, _, tenant) = CreateStep(apiKey: apiKey);

        var check = await step.CanExecuteAsync(tenant.Id);

        check.CanExecute.ShouldBeFalse();
        check.Status.ShouldBe(PostTenantCreationStepStatus.Failure);
        check.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CanExecuteAsync_EnabledButMetabaseUrlNotSet_Fails()
    {
        var (step, _, tenant) = CreateStep(metabaseUrlConfigured: false);

        var check = await step.CanExecuteAsync(tenant.Id);

        check.CanExecute.ShouldBeFalse();
        check.Status.ShouldBe(PostTenantCreationStepStatus.Failure);
        check.Reason!.ShouldContain(DynamicUrlKeyNames.METABASE_API_BASE);
    }

    [Fact]
    public async Task CanExecuteAsync_TenantWithoutReportingFeature_Skips()
    {
        var (step, _, tenant) = CreateStep(reportingEnabled: false);

        var check = await step.CanExecuteAsync(tenant.Id);

        check.CanExecute.ShouldBeFalse();
        check.Status.ShouldBe(PostTenantCreationStepStatus.Skipped);
        check.Reason!.ShouldContain(FeatureConsts.Reporting);
    }

    [Fact]
    public async Task CanExecuteAsync_TenantWithoutReportingFeature_SkipsEvenWhenConfigMissing()
    {
        var (step, _, tenant) = CreateStep(apiKey: null, metabaseUrlConfigured: false, reportingEnabled: false);

        var check = await step.CanExecuteAsync(tenant.Id);

        check.Status.ShouldBe(PostTenantCreationStepStatus.Skipped);
        check.Reason!.ShouldContain(FeatureConsts.Reporting);
    }

    [Fact]
    public async Task CanExecuteAsync_EnabledConfiguredAndReportingOn_Runs()
    {
        var (step, _, tenant) = CreateStep();

        (await step.CanExecuteAsync(tenant.Id)).CanExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_RunsProvisionerSetup()
    {
        var (step, metabaseApiClient, tenant) = CreateStep();

        await step.ExecuteAsync(tenant.Id);

        await metabaseApiClient.Received(1).FindOrCreateDatabaseAsync(
            tenant.Name, Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
    }
}
