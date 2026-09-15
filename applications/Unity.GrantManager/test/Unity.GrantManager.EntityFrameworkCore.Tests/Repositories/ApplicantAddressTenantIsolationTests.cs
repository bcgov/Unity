using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.Repositories;

public class ApplicantAddressTenantIsolationTests : GrantManagerEntityFrameworkCoreTestBase
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IApplicantRepository _applicantRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicantAddressRepository _addressRepository;
    private readonly IApplicantAddressManager _manager;

    public ApplicantAddressTenantIsolationTests()
    {
        _currentTenant = GetRequiredService<ICurrentTenant>();
        _guidGenerator = GetRequiredService<IGuidGenerator>();
        _applicantRepository = GetRequiredService<IApplicantRepository>();
        _applicationRepository = GetRequiredService<IApplicationRepository>();
        _addressRepository = GetRequiredService<IApplicantAddressRepository>();
        _manager = GetRequiredService<IApplicantAddressManager>();
    }

    [Fact]
    public async Task Should_IsolateLatestApplicationLookup_InTenantAndHostContexts()
    {
        var tenantA = _guidGenerator.Create();
        var tenantB = _guidGenerator.Create();
        var applications = new[]
        {
            await CreateApplicationAsync(tenantA),
            await CreateApplicationAsync(tenantB),
            await CreateApplicationAsync(null)
        };

        foreach (var contextApplication in applications)
        {
            using (_currentTenant.Change(contextApplication.TenantId))
            {
                await WithUnitOfWorkAsync(async () =>
                {
                    foreach (var application in applications)
                    {
                        var selected = await _manager.FindLatestApplicationAsync(application.ApplicantId);
                        if (application.Id == contextApplication.Id)
                        {
                            selected.ShouldNotBeNull();
                            selected.Id.ShouldBe(application.Id);
                        }
                        else
                        {
                            selected.ShouldBeNull();
                        }
                    }
                });
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_RejectAccessToAnotherTenantsApplicantAndAddress(bool useHostContext)
    {
        var tenantA = _guidGenerator.Create();
        var ownerApplication = await CreateApplicationAsync(tenantA);
        var otherApplication = await CreateApplicationAsync(useHostContext ? null : _guidGenerator.Create());
        Guid addressId;

        using (_currentTenant.Change(tenantA))
        {
            addressId = await WithUnitOfWorkAsync(async () =>
            {
                var saved = await _manager.SavePrimaryAddressesAsync(ownerApplication.ApplicantId,
                    ownerApplication.Id, AddressInput(Guid.Empty), null);
                saved.PhysicalAddress.ShouldNotBeNull();
                saved.PhysicalAddress.TenantId.ShouldBe(tenantA);
                saved.PhysicalAddress.ApplicationId.ShouldBe(ownerApplication.Id);
                saved.PhysicalAddress.IsFlaggedPrimary().ShouldBeTrue();
                return saved.PhysicalAddress.Id;
            });
        }

        using (_currentTenant.Change(otherApplication.TenantId))
        {
            await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await WithUnitOfWorkAsync(async () =>
                {
                    await _manager.SavePrimaryAddressesAsync(ownerApplication.ApplicantId,
                        ownerApplication.Id, AddressInput(Guid.Empty), null);
                });
            });

            await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await WithUnitOfWorkAsync(async () =>
                {
                    await _manager.SavePrimaryAddressesAsync(otherApplication.ApplicantId,
                        null, AddressInput(addressId), null);
                });
            });

            await WithUnitOfWorkAsync(async () =>
            {
                (await _addressRepository.FindByApplicantIdAsync(ownerApplication.ApplicantId)).ShouldBeEmpty();
            });
        }

        using (_currentTenant.Change(tenantA))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var addresses = await _addressRepository.FindByApplicantIdAndApplicationIdAsync(
                    ownerApplication.ApplicantId, ownerApplication.Id);
                addresses.ShouldHaveSingleItem().Id.ShouldBe(addressId);
            });
        }
    }

    private async Task<Application> CreateApplicationAsync(Guid? tenantId)
    {
        Application template;
        using (_currentTenant.Change(null))
        {
            template = await WithUnitOfWorkAsync(() =>
                _applicationRepository.GetAsync(GrantManagerTestData.Application1_Id, includeDetails: false));
        }

        using (_currentTenant.Change(tenantId))
        {
            return await WithUnitOfWorkAsync(async () =>
            {
                var applicant = await _applicantRepository.InsertAsync(new Applicant
                {
                    ApplicantName = "Address isolation test"
                }, autoSave: true);

                // Reuse the fixture's required form/status; address selection does not load those navigations.
                return await _applicationRepository.InsertAsync(new Application
                {
                    ApplicantId = applicant.Id,
                    ApplicationFormId = template.ApplicationFormId,
                    ApplicationStatusId = template.ApplicationStatusId,
                    ProjectName = "Address isolation test",
                    ReferenceNo = $"ADDRESS-{_guidGenerator.Create():N}",
                    SubmissionDate = new DateTime(2026, 9, 1)
                }, autoSave: true);
            });
        }
    }

    private static ApplicantAddressInput AddressInput(Guid id)
    {
        return new(id, "123 Main St", null, null, "Victoria", "BC", "V8W 1A1");
    }
}
