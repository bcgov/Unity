using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications;

/// <summary>
/// Verifies that <see cref="IApplicationRepository.GetApplicationListRecordsAsync"/> returns
/// field values consistent with the full-entity data returned by
/// <see cref="IApplicationRepository.WithFullDetailsAsync"/> for the same filter inputs.
/// </summary>
public class ApplicationListRecordAlignmentTests : GrantManagerApplicationTestBase
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ApplicationListRecordAlignmentTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _applicationRepository = GetRequiredService<IApplicationRepository>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_WithNullRequestedFields_ReturnsSameCount_As_WithFullDetailsAsync()
    {
        using var uow = _unitOfWorkManager.Begin();

        var fullDetails = await _applicationRepository.WithFullDetailsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue);

        var listRecords = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: null);

        listRecords.Count.ShouldBe(fullDetails.Count);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_ScalarFields_Match_WithFullDetailsAsync()
    {
        using var uow = _unitOfWorkManager.Begin();

        var fullDetails = await _applicationRepository.WithFullDetailsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue);

        var listRecords = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: null);

        fullDetails.ShouldNotBeEmpty();

        foreach (var app in fullDetails)
        {
            var rec = listRecords.FirstOrDefault(r => r.Id == app.Id);
            rec.ShouldNotBeNull($"No ApplicationListRecord found for Application Id={app.Id}");

            rec.ProjectName.ShouldBe(app.ProjectName, $"ProjectName mismatch for Id={app.Id}");
            rec.ReferenceNo.ShouldBe(app.ReferenceNo, $"ReferenceNo mismatch for Id={app.Id}");
            rec.RequestedAmount.ShouldBe(app.RequestedAmount, $"RequestedAmount mismatch for Id={app.Id}");
            rec.TotalProjectBudget.ShouldBe(app.TotalProjectBudget, $"TotalProjectBudget mismatch for Id={app.Id}");
            rec.EconomicRegion.ShouldBe(app.EconomicRegion, $"EconomicRegion mismatch for Id={app.Id}");
            rec.City.ShouldBe(app.City, $"City mismatch for Id={app.Id}");
            rec.SubmissionDate.ShouldBe(app.SubmissionDate, $"SubmissionDate mismatch for Id={app.Id}");
            rec.OwnerId.ShouldBe(app.OwnerId, $"OwnerId mismatch for Id={app.Id}");
            rec.ApplicantId.ShouldBe(app.ApplicantId, $"ApplicantId mismatch for Id={app.Id}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_StatusAndCategory_Match_WithFullDetailsAsync()
    {
        using var uow = _unitOfWorkManager.Begin();

        var fullDetails = await _applicationRepository.WithFullDetailsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue);

        var listRecords = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: null);

        foreach (var app in fullDetails)
        {
            var rec = listRecords.First(r => r.Id == app.Id);

            rec.Status.ShouldBe(app.ApplicationStatus.InternalStatus,
                $"Status (InternalStatus) mismatch for Id={app.Id}");
            rec.Category.ShouldBe(app.ApplicationForm.Category ?? string.Empty,
                $"Category mismatch for Id={app.Id}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_ApplicantFields_Match_WithFullDetailsAsync()
    {
        using var uow = _unitOfWorkManager.Begin();

        var fullDetails = await _applicationRepository.WithFullDetailsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue);

        var listRecords = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: null);

        foreach (var app in fullDetails)
        {
            var rec = listRecords.First(r => r.Id == app.Id);
            var applicant = app.Applicant;

            rec.ApplicantName.ShouldBe(applicant.ApplicantName,
                $"ApplicantName mismatch for Id={app.Id}");
            rec.ApplicantOrgName.ShouldBe(applicant.OrgName,
                $"ApplicantOrgName mismatch for Id={app.Id}");
            rec.ApplicantOrgNumber.ShouldBe(applicant.OrgNumber,
                $"ApplicantOrgNumber mismatch for Id={app.Id}");
            rec.ApplicantOrgStatus.ShouldBe(applicant.OrgStatus,
                $"ApplicantOrgStatus mismatch for Id={app.Id}");
            rec.ApplicantSector.ShouldBe(applicant.Sector,
                $"ApplicantSector mismatch for Id={app.Id}");
            rec.ApplicantSubSector.ShouldBe(applicant.SubSector,
                $"ApplicantSubSector mismatch for Id={app.Id}");
            rec.ApplicantOrganizationType.ShouldBe(applicant.OrganizationType,
                $"ApplicantOrganizationType mismatch for Id={app.Id}");
            rec.ApplicantOrganizationSize.ShouldBe(applicant.OrganizationSize,
                $"ApplicantOrganizationSize mismatch for Id={app.Id}");
            rec.ApplicantIndigenousOrgInd.ShouldBe(applicant.IndigenousOrgInd,
                $"ApplicantIndigenousOrgInd mismatch for Id={app.Id}");
            rec.ApplicantRedStop.ShouldBe(applicant.RedStop,
                $"ApplicantRedStop mismatch for Id={app.Id}");
            rec.ApplicantUnityApplicantId.ShouldBe(applicant.UnityApplicantId,
                $"ApplicantUnityApplicantId mismatch for Id={app.Id}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_CollectionCounts_Match_WithFullDetailsAsync()
    {
        using var uow = _unitOfWorkManager.Begin();

        var fullDetails = await _applicationRepository.WithFullDetailsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue);

        var listRecords = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: null);

        foreach (var app in fullDetails)
        {
            var rec = listRecords.First(r => r.Id == app.Id);

            rec.Tags.Count.ShouldBe(
                app.ApplicationTags?.Count ?? 0,
                $"Tag count mismatch for Id={app.Id}");
            rec.Assignments.Count.ShouldBe(
                app.ApplicationAssignments?.Count ?? 0,
                $"Assignment count mismatch for Id={app.Id}");
            rec.Links.Count.ShouldBe(
                app.ApplicationLinks?.Count ?? 0,
                $"Link count mismatch for Id={app.Id}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_ContactFields_Match_WithFullDetailsAsync()
    {
        using var uow = _unitOfWorkManager.Begin();

        var fullDetails = await _applicationRepository.WithFullDetailsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue);

        var listRecords = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: null);

        foreach (var app in fullDetails)
        {
            var rec = listRecords.First(r => r.Id == app.Id);

            rec.ContactFullName.ShouldBe(app.ApplicantAgent?.Name,
                $"ContactFullName mismatch for Id={app.Id}");
            rec.ContactTitle.ShouldBe(app.ApplicantAgent?.Title,
                $"ContactTitle mismatch for Id={app.Id}");
            rec.ContactEmail.ShouldBe(app.ApplicantAgent?.Email,
                $"ContactEmail mismatch for Id={app.Id}");
            rec.ContactBusinessPhone.ShouldBe(app.ApplicantAgent?.Phone,
                $"ContactBusinessPhone mismatch for Id={app.Id}");
            rec.ContactCellPhone.ShouldBe(app.ApplicantAgent?.Phone2,
                $"ContactCellPhone mismatch for Id={app.Id}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_OwnerFields_Match_WithFullDetailsAsync()
    {
        using var uow = _unitOfWorkManager.Begin();

        var fullDetails = await _applicationRepository.WithFullDetailsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue);

        var listRecords = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: null);

        foreach (var app in fullDetails)
        {
            var rec = listRecords.First(r => r.Id == app.Id);

            if (app.Owner != null)
            {
                rec.OwnerPersonId.ShouldBe(app.Owner.Id,
                    $"OwnerPersonId mismatch for Id={app.Id}");
                rec.OwnerFullName.ShouldBe(app.Owner.FullName,
                    $"OwnerFullName mismatch for Id={app.Id}");
            }
            else
            {
                rec.OwnerPersonId.ShouldBeNull($"Expected null OwnerPersonId for Id={app.Id}");
                rec.OwnerFullName.ShouldBeNull($"Expected null OwnerFullName for Id={app.Id}");
            }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_KnownApplication1_HasExpectedFieldValues()
    {
        using var uow = _unitOfWorkManager.Begin();

        var listRecords = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: null);

        var rec = listRecords.FirstOrDefault(r => r.Id == GrantManagerTestData.Application1_Id);
        rec.ShouldNotBeNull("Application1 seed data should be present in list records");

        rec.ProjectName.ShouldBe("Application For Integration Test Funding");
        rec.ReferenceNo.ShouldBe("TEST12345");
        rec.RequestedAmount.ShouldBe(3456.13m);
        rec.ApplicantId.ShouldBe(GrantManagerTestData.Applicant1_Id);
        rec.ApplicantName.ShouldBe("Integration Tester 1");
        rec.Status.ShouldBe("Submitted");
        rec.SubmissionDate.ShouldBe(new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_KnownApplication2_HasExpectedFieldValues()
    {
        using var uow = _unitOfWorkManager.Begin();

        var listRecords = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: null);

        var rec = listRecords.FirstOrDefault(r => r.Id == GrantManagerTestData.Application2_Id);
        rec.ShouldNotBeNull("Application2 seed data should be present in list records");

        rec.ProjectName.ShouldBe("Application 2 For Integration Test Funding");
        rec.ReferenceNo.ShouldBe("TEST67890");
        rec.RequestedAmount.ShouldBe(5000m);
        rec.ApplicantId.ShouldBe(GrantManagerTestData.Applicant1_Id);
        rec.ApplicantName.ShouldBe("Integration Tester 1");
        rec.Status.ShouldBe("Submitted");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_WithTagsRequestedField_ExcludesContactAndOwnerData()
    {
        using var uow = _unitOfWorkManager.Begin();

        var listRecords = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: new List<string> { "applicationTag" });

        listRecords.ShouldNotBeEmpty();

        foreach (var rec in listRecords)
        {
            rec.ContactFullName.ShouldBeNull(
                $"ContactFullName should not be loaded when only 'applicationTag' is requested for Id={rec.Id}");
            rec.OwnerPersonId.ShouldBeNull(
                $"OwnerPersonId should not be loaded when only 'applicationTag' is requested for Id={rec.Id}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_WithAssigneesRequestedField_ExcludesContactAndOwnerData()
    {
        using var uow = _unitOfWorkManager.Begin();

        var listRecords = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: new List<string> { "assignees" });

        listRecords.ShouldNotBeEmpty();

        foreach (var rec in listRecords)
        {
            rec.ContactFullName.ShouldBeNull(
                $"ContactFullName should not be loaded when only 'assignees' is requested for Id={rec.Id}");
            rec.OwnerPersonId.ShouldBeNull(
                $"OwnerPersonId should not be loaded when only 'assignees' is requested for Id={rec.Id}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_WithContactFieldRequested_ExcludesTagsAssignmentsLinks()
    {
        using var uow = _unitOfWorkManager.Begin();

        var listRecords = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: new List<string> { "contactEmail" });

        listRecords.ShouldNotBeEmpty();

        foreach (var rec in listRecords)
        {
            rec.Tags.ShouldBeEmpty(
                $"Tags should not be loaded when only contact fields are requested for Id={rec.Id}");
            rec.Assignments.ShouldBeEmpty(
                $"Assignments should not be loaded when only contact fields are requested for Id={rec.Id}");
            rec.Links.ShouldBeEmpty(
                $"Links should not be loaded when only contact fields are requested for Id={rec.Id}");
            rec.OwnerPersonId.ShouldBeNull(
                $"OwnerPersonId should not be loaded when only contact fields are requested for Id={rec.Id}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_WithOwnerRequestedField_ExcludesTagsAssignmentsLinks()
    {
        using var uow = _unitOfWorkManager.Begin();

        var listRecords = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: new List<string> { "Owner" });

        listRecords.ShouldNotBeEmpty();

        foreach (var rec in listRecords)
        {
            rec.Tags.ShouldBeEmpty(
                $"Tags should not be loaded when only 'Owner' is requested for Id={rec.Id}");
            rec.Assignments.ShouldBeEmpty(
                $"Assignments should not be loaded when only 'Owner' is requested for Id={rec.Id}");
            rec.Links.ShouldBeEmpty(
                $"Links should not be loaded when only 'Owner' is requested for Id={rec.Id}");
            rec.ContactFullName.ShouldBeNull(
                $"ContactFullName should not be loaded when only 'Owner' is requested for Id={rec.Id}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_WithEmptyRequestedFields_ExcludesAllOptionalData()
    {
        using var uow = _unitOfWorkManager.Begin();

        var listRecords = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: new List<string>());

        listRecords.ShouldNotBeEmpty();

        foreach (var rec in listRecords)
        {
            rec.Tags.ShouldBeEmpty(
                $"Tags should not be loaded when requestedFields is empty for Id={rec.Id}");
            rec.Assignments.ShouldBeEmpty(
                $"Assignments should not be loaded when requestedFields is empty for Id={rec.Id}");
            rec.Links.ShouldBeEmpty(
                $"Links should not be loaded when requestedFields is empty for Id={rec.Id}");
            rec.ContactFullName.ShouldBeNull(
                $"ContactFullName should not be loaded when requestedFields is empty for Id={rec.Id}");
            rec.OwnerPersonId.ShouldBeNull(
                $"OwnerPersonId should not be loaded when requestedFields is empty for Id={rec.Id}");
        }
    }
}
