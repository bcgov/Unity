using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Integrations.Orgbook;
using Unity.Payments.Integrations.Cas;
using Unity.Payments.Suppliers;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Linq;
using Xunit;

namespace Unity.GrantManager.Applicants;

public class ApplicantAddressSaveTests
{
    private readonly Guid _applicantId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly List<Application> _applications = [];
    private readonly List<ApplicantAddress> _addresses = [];
    private readonly IApplicantRepository _applicantRepository = Substitute.For<IApplicantRepository>();
    private readonly IApplicantAddressRepository _addressRepository = Substitute.For<IApplicantAddressRepository>();
    private readonly Applicant _applicant;
    private readonly ApplicantAppService _service;

    public ApplicantAddressSaveTests()
    {
        _applicant = WithId(new Applicant { TenantId = _tenantId }, _applicantId);
        _applicantRepository.GetAsync(_applicantId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(_applicant);

        var applicationRepository = Substitute.For<IApplicationRepository>();
        applicationRepository.GetQueryableAsync().Returns(_applications.AsQueryable());
        applicationRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call => _applications.Find(application => application.Id == call.Arg<Guid>()));
        _applications.Add(NewApplication(new DateTime(2026, 9, 1)));

        _addressRepository.FindByApplicantIdAsync(_applicantId).Returns(_addresses);
        _addressRepository.GetAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call => _addresses.Single(address => address.Id == call.Arg<Guid>()));
        _addressRepository.InsertAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var address = WithId(call.Arg<ApplicantAddress>(), Guid.NewGuid());
                _addresses.Add(address);
                return address;
            });
        _addressRepository.UpdateAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<ApplicantAddress>());

        var asyncExecuter = Substitute.For<IAsyncQueryableExecuter>();
        asyncExecuter.FirstOrDefaultAsync(Arg.Any<IQueryable<Application>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IQueryable<Application>>().FirstOrDefault());
        var lazyServices = Substitute.For<IAbpLazyServiceProvider>();
        lazyServices.LazyGetRequiredService<IAsyncQueryableExecuter>().Returns(asyncExecuter);
        var addressManager = new ApplicantAddressManager(_addressRepository, _applicantRepository, applicationRepository)
        {
            LazyServiceProvider = lazyServices
        };
        _service = new ApplicantAppService(
            _applicantRepository,
            Substitute.For<ISupplierService>(),
            Substitute.For<ISiteAppService>(),
            _addressRepository,
            Substitute.For<IOrgBookService>(),
            Substitute.For<IApplicantAgentRepository>(),
            applicationRepository,
            addressManager);
    }

    [Theory]
    [InlineData(AddressType.PhysicalAddress)]
    [InlineData(AddressType.MailingAddress)]
    public async Task CreatesOnlyTheSubmittedTypeWithApplicationAndTenant(AddressType type)
    {
        var input = NewRequest(type);
        var result = await _service.UpdateApplicantContactAddressesAsync(_applicantId, input);

        var address = _addresses.ShouldHaveSingleItem();
        address.Id.ShouldNotBe(Guid.Empty);
        address.ApplicantId.ShouldBe(_applicantId);
        address.TenantId.ShouldBe(_tenantId);
        address.ApplicationId.ShouldBe(_applications[0].Id);
        address.AddressType.ShouldBe(type);
        address.IsFlaggedPrimary().ShouldBeTrue();
        address.Country.ShouldBeEmpty();
        address.Street.ShouldBe("123 Main St");

        var saved = type == AddressType.PhysicalAddress ? result.PrimaryPhysicalAddress : result.PrimaryMailingAddress;
        saved.ShouldNotBeNull();
        saved.Id.ShouldBe(address.Id);
        saved.ApplicationId.ShouldBe(address.ApplicationId);
        saved.ReferenceNo.ShouldBe(_applications[0].ReferenceNo);
        saved.Street.ShouldBe("123 Main St");
        saved.Postal.ShouldBe("V8W 1A1");
        (type == AddressType.PhysicalAddress ? result.PrimaryMailingAddress : result.PrimaryPhysicalAddress).ShouldBeNull();
        await _addressRepository.Received(2).FindByApplicantIdAsync(_applicantId);
    }

    [Fact]
    public async Task CreatesBothAddressesOnTheSameApplication()
    {
        var input = NewRequest();
        input.PrimaryMailingAddress = new() { Street2 = " PO Box 123 " };

        var result = await _service.UpdateApplicantContactAddressesAsync(_applicantId, input);

        _addresses.Count.ShouldBe(2);
        _addresses.ShouldAllBe(address => address.ApplicationId == _applications[0].Id);
        result.PrimaryMailingAddress!.Street2.ShouldBe("PO Box 123");
        result.PrimaryPhysicalAddress!.Id.ShouldNotBe(result.PrimaryMailingAddress.Id);
        await _addressRepository.Received(4).FindByApplicantIdAsync(_applicantId);
    }

    [Fact]
    public async Task SelectsSubmissionDateBeforeCreationTimeAndFiltersIneligibleApplications()
    {
        var latest = _applications[0];
        latest.CreationTime = new DateTime(2026, 9, 1);
        var importedOlderSubmission = NewApplication(new DateTime(2026, 8, 1));
        importedOlderSubmission.CreationTime = new DateTime(2026, 9, 9);
        var otherApplicant = NewApplication(new DateTime(2026, 9, 9));
        otherApplicant.ApplicantId = Guid.NewGuid();
        var deleted = NewApplication(new DateTime(2026, 9, 9));
        deleted.IsDeleted = true;
        _applications.AddRange([importedOlderSubmission, otherApplicant, deleted]);

        await _service.UpdateApplicantContactAddressesAsync(_applicantId, NewRequest());

        _addresses.Single().ApplicationId.ShouldBe(latest.Id);
    }

    [Fact]
    public async Task BreaksSubmissionDateTiesByCreationTimeThenId()
    {
        var first = _applications[0];
        first.CreationTime = new DateTime(2026, 9, 1);
        var second = NewApplication(first.SubmissionDate, Guid.Parse("00000000-0000-0000-0000-000000000001"));
        second.CreationTime = new DateTime(2026, 9, 2);
        var third = NewApplication(first.SubmissionDate, Guid.Parse("00000000-0000-0000-0000-000000000002"));
        third.CreationTime = second.CreationTime;
        _applications.AddRange([third, second]);
        var input = NewRequest();
        input.ExpectedApplicationId = third.Id;

        await _service.UpdateApplicantContactAddressesAsync(_applicantId, input);

        _addresses.Single().ApplicationId.ShouldBe(third.Id);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("deleted")]
    [InlineData("otherApplicant")]
    [InlineData("emptyId")]
    public async Task RejectsCreationWithoutAnEligibleApplication(string scenario)
    {
        var input = NewRequest();
        var application = _applications[0];
        switch (scenario)
        {
            case "none": _applications.Clear(); break;
            case "deleted": application.IsDeleted = true; break;
            case "otherApplicant": application.ApplicantId = Guid.NewGuid(); break;
            case "emptyId":
                _applications.Clear();
                _applications.Add(NewApplication(application.SubmissionDate, Guid.Empty));
                break;
        }

        var error = await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateApplicantContactAddressesAsync(_applicantId, input));

        error.Code.ShouldBe(GrantManagerDomainErrorCodes.AddressApplicationRequired);
        _addresses.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("empty")]
    [InlineData("changed")]
    public async Task RejectsMissingOrStaleExpectedApplication(string scenario)
    {
        var input = NewRequest();
        if (scenario == "missing")
        {
            input.ExpectedApplicationId = null;
        }
        if (scenario == "empty")
        {
            input.ExpectedApplicationId = Guid.Empty;
        }
        if (scenario == "changed")
        {
            _applications.Add(NewApplication(new DateTime(2026, 9, 9)));
        }

        var error = await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateApplicantContactAddressesAsync(_applicantId, input));

        error.Code.ShouldBe(GrantManagerDomainErrorCodes.AddressApplicationChanged);
        _addresses.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(" ", "\t", "Victoria")]
    public async Task RejectsNewAddressesWithoutEitherStreetLine(string? street, string? street2, string? city)
    {
        var input = NewRequest();
        input.PrimaryPhysicalAddress = new() { Street = street, Street2 = street2, City = city };

        var error = await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateApplicantContactAddressesAsync(_applicantId, input));

        error.Code.ShouldBe(GrantManagerDomainErrorCodes.PhysicalAddressStreetRequired);
        _addresses.ShouldBeEmpty();
    }

    [Fact]
    public async Task RejectsCreationWhenTypeAlreadyExists()
    {
        _addresses.Add(WithId(new ApplicantAddress
        {
            ApplicantId = _applicantId, AddressType = AddressType.PhysicalAddress, Street = "Existing"
        }, Guid.NewGuid()));

        var error = await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateApplicantContactAddressesAsync(_applicantId, NewRequest()));

        error.Code.ShouldBe(GrantManagerDomainErrorCodes.PhysicalAddressAlreadyExists);
        _addresses.ShouldHaveSingleItem().Street.ShouldBe("Existing");
    }

    [Theory]
    [InlineData(AddressType.PhysicalAddress)]
    [InlineData(AddressType.MailingAddress)]
    public async Task Should_RejectCreation_WhenAddressAppearsAfterInitialCheck(AddressType type)
    {
        var competingAddress = WithId(new ApplicantAddress
        {
            ApplicantId = _applicantId,
            ApplicationId = _applications[0].Id,
            TenantId = _tenantId,
            AddressType = type,
            Street = "Saved by another request"
        }, Guid.NewGuid());
        var readCount = 0;
        _addressRepository.FindByApplicantIdAsync(_applicantId).Returns(_ =>
        {
            if (++readCount == 2)
            {
                _addresses.Add(competingAddress);
            }

            return _addresses.ToList();
        });

        var error = await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateApplicantContactAddressesAsync(_applicantId, NewRequest(type)));

        error.Code.ShouldBe(type == AddressType.PhysicalAddress
            ? GrantManagerDomainErrorCodes.PhysicalAddressAlreadyExists
            : GrantManagerDomainErrorCodes.MailingAddressAlreadyExists);
        _addresses.ShouldHaveSingleItem().Id.ShouldBe(competingAddress.Id);
        competingAddress.Street.ShouldBe("Saved by another request");
        await _addressRepository.Received(2).FindByApplicantIdAsync(_applicantId);
        await _addressRepository.DidNotReceive().InsertAsync(
            Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _addressRepository.DidNotReceive().UpdateAsync(
            Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnedIdUpdatesExistingAddressEvenWithoutApplications()
    {
        var created = await _service.UpdateApplicantContactAddressesAsync(_applicantId, NewRequest());
        var existingApplicationId = created.PrimaryPhysicalAddress!.ApplicationId;
        _applications.Clear();
        _addressRepository.ClearReceivedCalls();

        var result = await _service.UpdateApplicantContactAddressesAsync(_applicantId, new()
        {
            PrimaryPhysicalAddress = new() { Id = created.PrimaryPhysicalAddress.Id, City = " Vancouver " }
        });

        _addresses.ShouldHaveSingleItem().ApplicationId.ShouldBe(existingApplicationId);
        result.PrimaryPhysicalAddress!.Id.ShouldBe(created.PrimaryPhysicalAddress.Id);
        result.PrimaryPhysicalAddress.City.ShouldBe("Vancouver");
        result.PrimaryPhysicalAddress.Street.ShouldBeEmpty(); // Existing-address validation is unchanged.
        await _addressRepository.DidNotReceive().FindByApplicantIdAsync(_applicantId);
    }

    [Fact]
    public async Task CanCreateMissingTypeWhileUpdatingExistingAddressWithoutReassigningIt()
    {
        var existingApplicationId = Guid.NewGuid();
        var physical = WithId(new ApplicantAddress
        {
            ApplicantId = _applicantId, ApplicationId = existingApplicationId, AddressType = AddressType.PhysicalAddress
        }, Guid.NewGuid());
        _addresses.Add(physical);
        var input = NewRequest(AddressType.MailingAddress);
        input.PrimaryPhysicalAddress = new() { Id = physical.Id, City = "Victoria" };

        var result = await _service.UpdateApplicantContactAddressesAsync(_applicantId, input);

        result.PrimaryPhysicalAddress!.ApplicationId.ShouldBe(existingApplicationId);
        result.PrimaryMailingAddress!.ApplicationId.ShouldBe(_applications[0].Id);
        _addresses.Count.ShouldBe(2);
    }

    [Fact]
    public async Task InvalidSecondSectionDoesNotWriteFirstSection()
    {
        var input = NewRequest();
        input.PrimaryMailingAddress = new() { City = "Victoria" };

        await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateApplicantContactAddressesAsync(_applicantId, input));

        _addresses.ShouldBeEmpty();
        await _addressRepository.DidNotReceive().InsertAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RejectsMismatchedAddressOwnershipOrType(bool wrongApplicant)
    {
        var address = WithId(new ApplicantAddress
        {
            ApplicantId = wrongApplicant ? Guid.NewGuid() : _applicantId,
            AddressType = wrongApplicant ? AddressType.PhysicalAddress : AddressType.MailingAddress,
            Street = "Existing"
        }, Guid.NewGuid());
        _addresses.Add(address);
        var input = NewRequest();
        input.PrimaryPhysicalAddress!.Id = address.Id;

        await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateApplicantContactAddressesAsync(_applicantId, input));

        address.Street.ShouldBe("Existing");
    }

    [Fact]
    public async Task RejectsDeletedApplicant()
    {
        _applicant.IsDeleted = true;
        var error = await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateApplicantContactAddressesAsync(_applicantId, NewRequest()));
        error.Code.ShouldBe(GrantManagerDomainErrorCodes.AddressApplicantUnavailable);
        _addresses.ShouldBeEmpty();
    }

    [Fact]
    public async Task RejectsNonexistentApplicant()
    {
        _applicantRepository.GetAsync(_applicantId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Applicant>(new EntityNotFoundException(typeof(Applicant), _applicantId)));

        await Should.ThrowAsync<EntityNotFoundException>(() =>
            _service.UpdateApplicantContactAddressesAsync(_applicantId, NewRequest()));
        _addresses.ShouldBeEmpty();
    }

    private UpdateApplicantContactAddressesDto NewRequest(AddressType type = AddressType.PhysicalAddress)
    {
        var address = new UpdatePrimaryApplicantAddressDto { Street = " 123 Main St ", PostalCode = " V8W 1A1 " };
        return new()
        {
            ExpectedApplicationId = _applications[0].Id,
            PrimaryPhysicalAddress = type == AddressType.PhysicalAddress ? address : null,
            PrimaryMailingAddress = type == AddressType.MailingAddress ? address : null
        };
    }

    private Application NewApplication(DateTime submissionDate, Guid? id = null)
    {
        return WithId(new Application
        {
            ApplicantId = _applicantId, TenantId = _tenantId, SubmissionDate = submissionDate, ReferenceNo = "TEST-123"
        }, id ?? Guid.NewGuid());
    }

    private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
    {
        EntityHelper.TrySetId(entity, () => id);
        return entity;
    }
}
