using Microsoft.Extensions.Configuration;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Security.Encryption;
using Volo.Abp.TenantManagement;
using Xunit;

namespace Unity.GrantManager.Domain.Tests;

public class GrantManagerDefaultTenantSeederContributorTests
{
    private readonly ITenantManager _tenantManager = Substitute.For<ITenantManager>();
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IFeatureManager _featureManager = Substitute.For<IFeatureManager>();
    private readonly IStringEncryptionService _encryptionService = Substitute.For<IStringEncryptionService>();

    public GrantManagerDefaultTenantSeederContributorTests()
    {
        // The default tenant already exists, so only the Onboarding tenant path is exercised.
        _tenantRepository
            .FindByNameAsync(GrantManagerConsts.NormalizedDefaultTenantName, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(CreateTenant(GrantManagerConsts.DefaultTenantName));
        _encryptionService.Encrypt(Arg.Any<string>()).Returns("encrypted");
    }

    [Fact]
    public async Task SeedAsync_NewOnboardingTenant_EnablesOnboardingAndFlex()
    {
        var onboarding = CreateTenant(GrantManagerConsts.OnboardingTenantName);
        _tenantManager.CreateAsync(GrantManagerConsts.OnboardingTenantName).Returns(onboarding);

        await CreateSeeder().SeedAsync(new DataSeedContext());

        await _tenantRepository.Received(1).InsertAsync(onboarding, true, Arg.Any<CancellationToken>());
        await _featureManager.Received(1).SetAsync("Unity.Onboarding", "True", "T", onboarding.Id.ToString());
        await _featureManager.Received(1).SetAsync("Unity.Flex", "True", "T", onboarding.Id.ToString());
    }

    [Fact]
    public async Task SeedAsync_ExistingOnboardingTenant_DoesNotChangeFeatures()
    {
        _tenantRepository
            .FindByNameAsync(GrantManagerConsts.NormalizedOnboardingTenantName, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(CreateTenant(GrantManagerConsts.OnboardingTenantName));

        await CreateSeeder().SeedAsync(new DataSeedContext());

        await _tenantManager.DidNotReceiveWithAnyArgs().CreateAsync(default!);
        await _featureManager.DidNotReceiveWithAnyArgs().SetAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task SeedAsync_TenantContext_DoesNothing()
    {
        await CreateSeeder().SeedAsync(new DataSeedContext(Guid.NewGuid()));

        await _tenantRepository.DidNotReceiveWithAnyArgs().FindByNameAsync(default!);
        await _featureManager.DidNotReceiveWithAnyArgs().SetAsync(default!, default, default!, default);
    }

    private GrantManagerDefaultTenantSeederContributor CreateSeeder()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{GrantManagerConsts.OnboardingTenantConnectionStringConfigKey}"] = "Host=localhost;Database=Onboarding"
            })
            .Build();

        return new GrantManagerDefaultTenantSeederContributor(
            _tenantManager, _tenantRepository, configuration, _featureManager, _encryptionService);
    }

    // Tenant's constructors are all non-public (ABP requires going through ITenantManager to
    // create one) - reflection is the standard workaround for exercising it in a plain unit test.
    private static Tenant CreateTenant(string name)
    {
        var constructor = typeof(Tenant).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(Guid), typeof(string), typeof(string)], null)
            ?? throw new InvalidOperationException("Expected ABP Tenant constructor was not found.");
        return (Tenant)constructor.Invoke([Guid.NewGuid(), name, name.ToUpperInvariant()]);
    }
}
