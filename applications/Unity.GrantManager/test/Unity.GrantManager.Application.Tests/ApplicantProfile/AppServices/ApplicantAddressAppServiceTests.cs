using NSubstitute;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.Addresses;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.ApplicantProfile;

public class ApplicantAddressAppServiceTests
{
    private readonly Guid _applicantId = Guid.NewGuid();
    private readonly Guid _addressId = Guid.NewGuid();
    private readonly IApplicantAddressRepository _addressRepository =
        Substitute.For<IApplicantAddressRepository>();
    private readonly ApplicantAddressManager _manager;
    private readonly ApplicantAddressAppService _service;

    public ApplicantAddressAppServiceTests()
    {
        _manager = new ApplicantAddressManager(
            _addressRepository,
            Substitute.For<IApplicantRepository>(),
            Substitute.For<IApplicationRepository>());

        _service = new ApplicantAddressAppService(_manager);
    }

    private void GivenAddress(Guid ownerId, Guid? applicationId)
    {
        var address = new ApplicantAddress
        {
            ApplicantId = ownerId,
            ApplicationId = applicationId,
            AddressType = AddressType.PhysicalAddress,
            Street = "1 Existing St"
        };
        EntityHelper.TrySetId(address, () => _addressId);
        _addressRepository
            .GetAsync(_addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(address);
    }

    private static UpdateApplicantProfileAddressDto ValidInput() =>
        new()
        {
            AddressType = AddressType.PhysicalAddress,
            Street = "2 New St",
            Street2 = null,
            Unit = "5",
            City = "Victoria",
            Province = "British Columbia",
            PostalCode = "V8V 1V1",
            IsPrimary = false
        };

    [Fact]
    public async Task Should_Reject_WhenAddressBelongsToAnotherApplicant()
    {
        GivenAddress(Guid.NewGuid(), applicationId: null);

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateAsync(_applicantId, _addressId, ValidInput()));

        exception.Code.ShouldBe("Unity:Applicant:AddressNotFound");
    }

    [Fact]
    public async Task Should_Reject_WhenAddressIsLinkedToASubmission()
    {
        GivenAddress(_applicantId, applicationId: Guid.NewGuid());

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateAsync(_applicantId, _addressId, ValidInput()));

        exception.Code.ShouldBe(GrantManagerDomainErrorCodes.AddressNotEditable);
    }

    [Fact]
    public async Task Should_ReportResolvedPrimary_WhenNoAddressOfTheTypeIsFlagged()
    {
        var olderId = Guid.NewGuid();
        var newerId = Guid.NewGuid();
        var older = CreateUnflaggedPhysicalAddress(olderId, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = CreateUnflaggedPhysicalAddress(newerId, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        _addressRepository
            .GetAsync(olderId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(older);
        _addressRepository
            .GetAsync(newerId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(newer);
        _addressRepository.FindByApplicantIdAsync(_applicantId).Returns([older, newer]);

        var newerResult = await _service.GetAsync(_applicantId, newerId);
        var olderResult = await _service.GetAsync(_applicantId, olderId);

        newerResult.IsPrimary.ShouldBeTrue();
        olderResult.IsPrimary.ShouldBeFalse();
    }

    private ApplicantAddress CreateUnflaggedPhysicalAddress(Guid id, DateTime creationTime)
    {
        var address = new ApplicantAddress
        {
            ApplicantId = _applicantId,
            AddressType = AddressType.PhysicalAddress,
            Street = "1 Existing St",
            CreationTime = creationTime
        };
        EntityHelper.TrySetId(address, () => id);
        return address;
    }
}
