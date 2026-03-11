using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantsPortal.Handlers;
using Unity.GrantManager.GrantsPortal.Messages;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.GrantsPortal;

public class OrganizationEditHandlerTests
{
    private readonly IApplicantRepository _applicantRepository;
    private readonly OrganizationEditHandler _handler;

    public OrganizationEditHandlerTests()
    {
        _applicantRepository = Substitute.For<IApplicantRepository>();

        _applicantRepository.UpdateAsync(Arg.Any<Applicant>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<Applicant>(0));

        _handler = new OrganizationEditHandler(
            _applicantRepository,
            NullLogger<OrganizationEditHandler>.Instance);
    }

    private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
    {
        EntityHelper.TrySetId(entity, () => id);
        return entity;
    }

    private static PluginDataPayload CreatePayload(
        Guid? organizationId = null,
        JObject? data = null)
    {
        organizationId ??= Guid.NewGuid();

        data ??= JObject.FromObject(new
        {
            name = "Updated Org",
            organizationType = "Non-Profit",
            organizationNumber = "ORG-12345",
            status = "Active",
            nonRegOrgName = "Friendly Name",
            fiscalMonth = "April",
            fiscalDay = "15",
            organizationSize = "Medium"
        });

        return new PluginDataPayload
        {
            Action = "ORGANIZATION_EDIT_COMMAND",
            OrganizationId = organizationId.Value.ToString(),
            ProfileId = Guid.NewGuid().ToString(),
            Provider = Guid.NewGuid().ToString(),
            Data = data
        };
    }

    #region Happy path

    [Fact]
    public async Task HandleAsync_ShouldUpdateAllApplicantFields()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var existingApplicant = WithId(new Applicant
        {
            OrgName = "Old Org",
            OrganizationType = "For-Profit"
        }, orgId);

        _applicantRepository.GetAsync(orgId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(existingApplicant);

        Applicant? updatedApplicant = null;
        _applicantRepository.UpdateAsync(Arg.Any<Applicant>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                updatedApplicant = ci.ArgAt<Applicant>(0);
                return updatedApplicant;
            });

        var payload = CreatePayload(organizationId: orgId);

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Organization updated successfully");
        updatedApplicant.ShouldNotBeNull();
        updatedApplicant.OrgName.ShouldBe("Updated Org");
        updatedApplicant.OrganizationType.ShouldBe("Non-Profit");
        updatedApplicant.OrgNumber.ShouldBe("ORG-12345");
        updatedApplicant.OrgStatus.ShouldBe("Active");
        updatedApplicant.NonRegOrgName.ShouldBe("Friendly Name");
        updatedApplicant.FiscalMonth.ShouldBe("April");
        updatedApplicant.FiscalDay.ShouldBe(15);
        updatedApplicant.OrganizationSize.ShouldBe("Medium");
    }

    [Fact]
    public async Task HandleAsync_ShouldCallUpdateOnRepository()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _applicantRepository.GetAsync(orgId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(WithId(new Applicant(), orgId));

        var payload = CreatePayload(organizationId: orgId);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        await _applicantRepository.Received(1).UpdateAsync(Arg.Any<Applicant>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Fiscal day parsing

    [Fact]
    public async Task HandleAsync_WhenFiscalDayIsValidInt_ShouldParseFiscalDay()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _applicantRepository.GetAsync(orgId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(WithId(new Applicant(), orgId));

        Applicant? updatedApplicant = null;
        _applicantRepository.UpdateAsync(Arg.Any<Applicant>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                updatedApplicant = ci.ArgAt<Applicant>(0);
                return updatedApplicant;
            });

        var data = JObject.FromObject(new { name = "Org", fiscalDay = "28" });
        var payload = CreatePayload(organizationId: orgId, data: data);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        updatedApplicant.ShouldNotBeNull();
        updatedApplicant.FiscalDay.ShouldBe(28);
    }

    [Fact]
    public async Task HandleAsync_WhenFiscalDayIsNotNumeric_ShouldNotSetFiscalDay()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var existingApplicant = WithId(new Applicant { FiscalDay = 10 }, orgId);

        _applicantRepository.GetAsync(orgId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(existingApplicant);

        Applicant? updatedApplicant = null;
        _applicantRepository.UpdateAsync(Arg.Any<Applicant>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                updatedApplicant = ci.ArgAt<Applicant>(0);
                return updatedApplicant;
            });

        var data = JObject.FromObject(new { name = "Org", fiscalDay = "not-a-number" });
        var payload = CreatePayload(organizationId: orgId, data: data);

        // Act
        await _handler.HandleAsync(payload);

        // Assert — FiscalDay should remain unchanged (still 10 from initial)
        updatedApplicant.ShouldNotBeNull();
        updatedApplicant.FiscalDay.ShouldBe(10);
    }

    [Fact]
    public async Task HandleAsync_WhenFiscalDayIsNull_ShouldNotSetFiscalDay()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var existingApplicant = WithId(new Applicant { FiscalDay = 5 }, orgId);

        _applicantRepository.GetAsync(orgId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(existingApplicant);

        Applicant? updatedApplicant = null;
        _applicantRepository.UpdateAsync(Arg.Any<Applicant>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                updatedApplicant = ci.ArgAt<Applicant>(0);
                return updatedApplicant;
            });

        var data = JObject.FromObject(new { name = "Org" });
        var payload = CreatePayload(organizationId: orgId, data: data);

        // Act
        await _handler.HandleAsync(payload);

        // Assert — FiscalDay remains unchanged
        updatedApplicant.ShouldNotBeNull();
        updatedApplicant.FiscalDay.ShouldBe(5);
    }

    #endregion

    #region Validation

    [Fact]
    public async Task HandleAsync_WhenOrganizationIdMissing_ShouldThrow()
    {
        // Arrange
        var payload = CreatePayload();
        payload.OrganizationId = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _handler.HandleAsync(payload));
    }

    [Fact]
    public async Task HandleAsync_WhenDataMissing_ShouldThrow()
    {
        // Arrange
        var payload = CreatePayload();
        payload.Data = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _handler.HandleAsync(payload));
    }

    #endregion
}
