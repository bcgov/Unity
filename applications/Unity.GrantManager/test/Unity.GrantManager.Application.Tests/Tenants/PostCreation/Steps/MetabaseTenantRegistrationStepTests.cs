using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Integrations.Metabase;
using Unity.TenantManagement.Metabase;
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
        CreateStep(string? encryptedReadOnlyConnectionString = "encrypted-blob", string? apiKey = "test-api-key", string? dbHostOverride = null, bool? dbSslOverride = null)
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
            ApiKey = apiKey ?? string.Empty,
            DbHostOverride = dbHostOverride ?? string.Empty,
            DbSslOverride = dbSslOverride
        });

        var step = new MetabaseTenantRegistrationStep(
            metabaseApiClient, tenantRepository, encryptionService, settingManager, metabaseOptions,
            Substitute.For<ILogger<MetabaseTenantRegistrationStep>>());

        return (step, metabaseApiClient, settingManager, tenant);
    }

    [Fact]
    public void ContinueOnError_IsTrue_SoAMetabaseOutageDoesNotBlockLaterSteps()
    {
        var (step, _, _, _) = CreateStep();

        step.ContinueOnError.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CanExecuteAsync_NoApiKeyConfigured_ReturnsFalse(string? apiKey)
    {
        var (step, _, _, tenant) = CreateStep(apiKey: apiKey);

        (await step.CanExecuteAsync(tenant.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task CanExecuteAsync_ApiKeyConfigured_ReturnsTrue()
    {
        var (step, _, _, tenant) = CreateStep();

        (await step.CanExecuteAsync(tenant.Id)).ShouldBeTrue();
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
            .Returns((string?)null);
        settingManager.GetOrNullGlobalAsync(MetabaseSettings.UserEmails).Returns("user1@gov.bc.ca,user2@gov.bc.ca");

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
        settingManager.GetOrNullGlobalAsync(MetabaseSettings.UserEmails).Returns((string?)null);
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
        settingManager.GetOrNullGlobalAsync(MetabaseSettings.UserEmails).Returns((string?)null);
        metabaseApiClient.FindOrCreateGroupAsync(tenant.Name).Returns(22);
        metabaseApiClient.FindOrCreateCollectionAsync(tenant.Name).Returns(33);

        await step.ExecuteAsync(tenant.Id);

        await metabaseApiClient.Received(1).FindOrCreateDatabaseAsync(
            tenant.Name, "dev-crunchy-postgres-primary.ce395f-dev.svc", 5432, "T_ABC123", "t_abc123_readonly", "s3cr3t", false);
    }

    [Fact]
    public async Task ExecuteAsync_TenantScopedUserEmailsSetting_TakesPrecedenceOverGlobalDefault()
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
}
