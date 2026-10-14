using System;
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
using Unity.TenantManagement.Metabase;
using Volo.Abp;
using Volo.Abp.Features;
using Volo.Abp.Security.Encryption;
using Volo.Abp.Settings;
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

    private static (MetabaseTenantRegistrationStep Step, IMetabaseApiClient MetabaseApiClient, ISettingManager SettingManager, Tenant Tenant)
        CreateStep(string? encryptedReadOnlyConnectionString = "encrypted-blob", string? apiKey = "test-api-key", string? dbHostOverride = null, bool? dbSslOverride = null,
            bool enabled = true, bool metabaseUrlConfigured = true, bool reportingEnabled = true)
    {
        var tenant = CreateTenant("AG-MARB");
        if (encryptedReadOnlyConnectionString != null)
        {
            tenant.SetConnectionString(ReadOnlyConnectionStringName, encryptedReadOnlyConnectionString);
        }

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetAsync(tenant.Id, Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>()).Returns(tenant);

        var encryptionService = Substitute.For<IStringEncryptionService>();
        encryptionService.Decrypt(Arg.Any<string>()).Returns(DecryptedConnectionString);

        var settingManager = Substitute.For<ISettingManager>();
        var metabaseApiClient = Substitute.For<IMetabaseApiClient>();
        var metabaseOptions = Options.Create(new MetabaseOptions
        {
            Enabled = enabled,
            ApiKey = apiKey ?? string.Empty,
            DbHostOverride = dbHostOverride ?? string.Empty,
            DbSslOverride = dbSslOverride
        });

        var endpointManagementAppService = Substitute.For<IEndpointManagementAppService>();
        endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.METABASE_API_BASE).Returns(metabaseUrlConfigured
            ? Task.FromResult("https://metabase.example")
            : Task.FromException<string>(new UserFriendlyException("URL for key 'METABASE_API_BASE' not configured.")));

        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync(FeatureConsts.Reporting).Returns(Task.FromResult(reportingEnabled));

        var step = new MetabaseTenantRegistrationStep(
            metabaseApiClient, tenantRepository, encryptionService, settingManager, metabaseOptions,
            endpointManagementAppService, featureChecker,
            Substitute.For<ILogger<MetabaseTenantRegistrationStep>>());

        return (step, metabaseApiClient, settingManager, tenant);
    }

    [Fact]
    public void ContinueOnError_IsTrue_SoAMetabaseOutageDoesNotBlockLaterSteps()
    {
        var (step, _, _, _) = CreateStep();

        step.ContinueOnError.ShouldBeTrue();
    }

    [Fact]
    public async Task CanExecuteAsync_MetabaseDisabled_Skips()
    {
        // Disabled wins even when nothing else is configured: the environment doesn't use Metabase.
        var (step, _, _, tenant) = CreateStep(enabled: false, apiKey: null, metabaseUrlConfigured: false, reportingEnabled: false);

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
        var (step, _, _, tenant) = CreateStep(apiKey: apiKey);

        var check = await step.CanExecuteAsync(tenant.Id);

        check.CanExecute.ShouldBeFalse();
        check.Status.ShouldBe(PostTenantCreationStepStatus.Failure);
        check.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CanExecuteAsync_EnabledButMetabaseUrlNotSet_Fails()
    {
        var (step, _, _, tenant) = CreateStep(metabaseUrlConfigured: false);

        var check = await step.CanExecuteAsync(tenant.Id);

        check.CanExecute.ShouldBeFalse();
        check.Status.ShouldBe(PostTenantCreationStepStatus.Failure);
        check.Reason!.ShouldContain(DynamicUrlKeyNames.METABASE_API_BASE);
    }

    [Fact]
    public async Task CanExecuteAsync_TenantWithoutReportingFeature_Skips()
    {
        var (step, _, _, tenant) = CreateStep(reportingEnabled: false);

        var check = await step.CanExecuteAsync(tenant.Id);

        check.CanExecute.ShouldBeFalse();
        check.Status.ShouldBe(PostTenantCreationStepStatus.Skipped);
        check.Reason!.ShouldContain(FeatureConsts.Reporting);
    }

    [Fact]
    public async Task CanExecuteAsync_TenantWithoutReportingFeature_SkipsEvenWhenConfigMissing()
    {
        var (step, _, _, tenant) = CreateStep(apiKey: null, metabaseUrlConfigured: false, reportingEnabled: false);

        var check = await step.CanExecuteAsync(tenant.Id);

        check.Status.ShouldBe(PostTenantCreationStepStatus.Skipped);
        check.Reason!.ShouldContain(FeatureConsts.Reporting);
    }

    [Fact]
    public async Task CanExecuteAsync_EnabledConfiguredAndReportingOn_Runs()
    {
        var (step, _, _, tenant) = CreateStep();

        (await step.CanExecuteAsync(tenant.Id)).CanExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_NoReadonlyConnectionString_SkipsWithoutCallingMetabase()
    {
        var (step, metabaseApiClient, _, tenant) = CreateStep(encryptedReadOnlyConnectionString: null);

        await step.ExecuteAsync(tenant.Id);

        await metabaseApiClient.DidNotReceiveWithAnyArgs().FindOrCreateDatabaseAsync(default!, default!, default, default!, default!, default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesDatabaseGroupAndCollection_UsingParsedConnectionDetailsAndConfiguredEmails()
    {
        var (step, metabaseApiClient, settingManager, tenant) = CreateStep();
        settingManager.GetOrNullAsync(MetabaseSettings.UserEmails, TenantSettingValueProvider.ProviderName, tenant.Id.ToString())
            .Returns("user1@gov.bc.ca,user2@gov.bc.ca");

        metabaseApiClient.FindOrCreateDatabaseAsync(
                tenant.Name, "dev-crunchy-postgres-primary.ce395f-dev.svc", 5432, "T_ABC123", "t_abc123_readonly", "s3cr3t", true)
            .Returns(11);
        metabaseApiClient.FindOrCreateGroupAsync(tenant.Name).Returns(22);
        metabaseApiClient.FindUserIdByEmailAsync("user1@gov.bc.ca").Returns(101);
        metabaseApiClient.FindUserIdByEmailAsync("user2@gov.bc.ca").Returns((int?)null);
        metabaseApiClient.FindOrCreateCollectionAsync(tenant.Name).Returns(33);

        await step.ExecuteAsync(tenant.Id);

        await metabaseApiClient.Received(1).SyncDatabaseSchemaAsync(11);
        await metabaseApiClient.Received(1).RescanDatabaseValuesAsync(11);
        await metabaseApiClient.Received(1).AddGroupMemberAsync(22, 101);
        await metabaseApiClient.DidNotReceive().AddGroupMemberAsync(22, Arg.Is<int>(id => id != 101));
        await metabaseApiClient.Received(1).GrantGroupDatabaseAccessAsync(22, 11);
        await metabaseApiClient.Received(1).GrantGroupCollectionAccessAsync(22, 33);
    }

    [Fact]
    public async Task ExecuteAsync_DbHostOverrideConfigured_UsesOverrideHostInsteadOfConnectionStringHost()
    {
        var (step, metabaseApiClient, settingManager, tenant) = CreateStep(dbHostOverride: "host.docker.internal");
        settingManager.GetOrNullAsync(MetabaseSettings.UserEmails, TenantSettingValueProvider.ProviderName, tenant.Id.ToString())
            .Returns((string?)null);
        metabaseApiClient.FindOrCreateGroupAsync(tenant.Name).Returns(22);
        metabaseApiClient.FindOrCreateCollectionAsync(tenant.Name).Returns(33);

        await step.ExecuteAsync(tenant.Id);

        await metabaseApiClient.Received(1).FindOrCreateDatabaseAsync(
            tenant.Name, "host.docker.internal", 5432, "T_ABC123", "t_abc123_readonly", "s3cr3t", true);
    }

    [Fact]
    public async Task ExecuteAsync_DbSslOverrideFalse_DisablesSslForDatabaseConnection()
    {
        var (step, metabaseApiClient, settingManager, tenant) = CreateStep(dbSslOverride: false);
        settingManager.GetOrNullAsync(MetabaseSettings.UserEmails, TenantSettingValueProvider.ProviderName, tenant.Id.ToString())
            .Returns((string?)null);
        metabaseApiClient.FindOrCreateGroupAsync(tenant.Name).Returns(22);
        metabaseApiClient.FindOrCreateCollectionAsync(tenant.Name).Returns(33);

        await step.ExecuteAsync(tenant.Id);

        await metabaseApiClient.Received(1).FindOrCreateDatabaseAsync(
            tenant.Name, "dev-crunchy-postgres-primary.ce395f-dev.svc", 5432, "T_ABC123", "t_abc123_readonly", "s3cr3t", false);
    }

    [Fact]
    public async Task ExecuteAsync_UsesTenantScopedUserEmailsSetting_NeverTheGlobalDefault()
    {
        var (step, metabaseApiClient, settingManager, tenant) = CreateStep();
        settingManager.GetOrNullAsync(MetabaseSettings.UserEmails, TenantSettingValueProvider.ProviderName, tenant.Id.ToString())
            .Returns("tenant-scoped@gov.bc.ca");
        metabaseApiClient.FindOrCreateGroupAsync(tenant.Name).Returns(22);
        metabaseApiClient.FindUserIdByEmailAsync(Arg.Any<string>()).Returns((int?)null);

        await step.ExecuteAsync(tenant.Id);

        await metabaseApiClient.Received(1).FindUserIdByEmailAsync("tenant-scoped@gov.bc.ca");
        await settingManager.DidNotReceive().GetOrNullGlobalAsync(MetabaseSettings.UserEmails);
    }

    [Fact]
    public async Task ExecuteAsync_NoTenantScopedUserEmails_AddsNoMembersAndNeverReadsGlobalDefault()
    {
        var (step, metabaseApiClient, settingManager, tenant) = CreateStep();
        settingManager.GetOrNullAsync(MetabaseSettings.UserEmails, TenantSettingValueProvider.ProviderName, tenant.Id.ToString())
            .Returns((string?)null);
        metabaseApiClient.FindOrCreateGroupAsync(tenant.Name).Returns(22);

        await step.ExecuteAsync(tenant.Id);

        await metabaseApiClient.DidNotReceiveWithAnyArgs().FindUserIdByEmailAsync(default!);
        await settingManager.DidNotReceive().GetOrNullGlobalAsync(MetabaseSettings.UserEmails);
    }
}
