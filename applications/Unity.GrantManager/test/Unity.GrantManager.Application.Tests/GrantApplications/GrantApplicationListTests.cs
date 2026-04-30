using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications;

public class GrantApplicationListTests : GrantManagerApplicationTestBase
{
    private readonly IGrantApplicationAppService _appService;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public GrantApplicationListTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _appService = GetRequiredService<IGrantApplicationAppService>();
        _applicationRepository = GetRequiredService<IApplicationRepository>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
    }


    // GetApplicationListRecordsAsync -- requestedFields flag logic
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_NullRequestedFields_ReturnsSeededApplicationsWithBaseFields()
    {
        using var uow = _unitOfWorkManager.Begin();

        var result = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: null);

        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.ShouldAllBe(r => r.Status != null);
        result.ShouldAllBe(r => r.ApplicantName != null);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_WithNonContactNonOwnerFields_OmitsAgentAndOwnerData()
    {
        using var uow = _unitOfWorkManager.Begin();

        var result = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: ["projectName", "referenceNo"]);

        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(1);

        // Agent fields should be null -- not requested
        result.ShouldAllBe(r => r.ContactFullName == null);
        result.ShouldAllBe(r => r.ContactEmail == null);
        result.ShouldAllBe(r => r.ContactTitle == null);

        // Owner fields should be null -- not requested
        result.ShouldAllBe(r => r.OwnerFullName == null);

        // Collections should be empty -- not requested
        result.ShouldAllBe(r => r.Tags.Count == 0);
        result.ShouldAllBe(r => r.Assignments.Count == 0);
        result.ShouldAllBe(r => r.Links.Count == 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_WithContactFields_RunsAgentPath_AssignmentsAndLinksOmitted()
    {
        using var uow = _unitOfWorkManager.Begin();

        var result = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: ["contactFullName", "contactEmail"]);

        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.ShouldAllBe(r => r.ApplicantName != null);
        result.ShouldAllBe(r => r.Tags.Count == 0);
        result.ShouldAllBe(r => r.Assignments.Count == 0);
        result.ShouldAllBe(r => r.Links.Count == 0);
        result.ShouldAllBe(r => r.OwnerFullName == null);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_WithTagField_RunsTagsPath_AgentAndOwnerOmitted()
    {
        using var uow = _unitOfWorkManager.Begin();

        var result = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: ["applicationTag"]);

        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.ShouldAllBe(r => r.Tags != null);
        result.ShouldAllBe(r => r.ContactFullName == null);
        result.ShouldAllBe(r => r.OwnerFullName == null);
        result.ShouldAllBe(r => r.Assignments.Count == 0);
        result.ShouldAllBe(r => r.Links.Count == 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_WithOwnerField_RunsOwnerPath_AgentAndTagsOmitted()
    {
        using var uow = _unitOfWorkManager.Begin();

        var result = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: ["Owner"]);

        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.ShouldAllBe(r => r.ContactFullName == null);
        result.ShouldAllBe(r => r.Tags.Count == 0);
        result.ShouldAllBe(r => r.Assignments.Count == 0);
        result.ShouldAllBe(r => r.Links.Count == 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_WithAssigneesField_RunsAssignmentsPath_TagsOmitted()
    {
        using var uow = _unitOfWorkManager.Begin();

        var result = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: ["assignees"]);

        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.ShouldAllBe(r => r.Assignments != null);
        result.ShouldAllBe(r => r.Tags.Count == 0);
        result.ShouldAllBe(r => r.Links.Count == 0);
        result.ShouldAllBe(r => r.ContactFullName == null);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_WithApplicationLinksField_RunsLinksPath_TagsOmitted()
    {
        using var uow = _unitOfWorkManager.Begin();

        var result = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            requestedFields: ["applicationLinks"]);

        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.ShouldAllBe(r => r.Links != null);
        result.ShouldAllBe(r => r.Tags.Count == 0);
        result.ShouldAllBe(r => r.Assignments.Count == 0);
        result.ShouldAllBe(r => r.ContactFullName == null);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_WithFutureDateFilter_ReturnsEmpty()
    {
        using var uow = _unitOfWorkManager.Begin();

        var result = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            submittedFromDate: DateTime.UtcNow.AddYears(10));

        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationListRecordsAsync_WithDateRangeMatchingSeededData_ReturnsApplications()
    {
        // Seeded applications have SubmissionDate = 2023-01-01
        using var uow = _unitOfWorkManager.Begin();

        var result = await _applicationRepository.GetApplicationListRecordsAsync(
            skipCount: 0,
            maxResultCount: int.MaxValue,
            submittedFromDate: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            submittedToDate: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(1);
    }


    // GetListAsync -- totalCount == items.Count and requestedFields pass-through
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_TotalCount_EqualsItemsCount()
    {
        var result = await _appService.GetListAsync(new GrantApplicationListInputDto());

        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(result.Items.Count);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_ReturnsAtLeastOneItem()
    {
        var result = await _appService.GetListAsync(new GrantApplicationListInputDto());

        result.Items.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_WithFutureDateFilter_ReturnsEmptyPagedResult()
    {
        var result = await _appService.GetListAsync(new GrantApplicationListInputDto
        {
            SubmittedFromDate = DateTime.UtcNow.AddYears(10)
        });

        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(0);
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_WithNullRequestedFields_AllItemsHaveBaseFields()
    {
        var result = await _appService.GetListAsync(new GrantApplicationListInputDto
        {
            RequestedFields = null
        });

        result.ShouldNotBeNull();
        result.Items.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.TotalCount.ShouldBe(result.Items.Count);
        result.Items.ShouldAllBe(dto => !string.IsNullOrEmpty(dto.Status));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_WithContactRequestedFields_TotalCountMatchesItems()
    {
        var result = await _appService.GetListAsync(new GrantApplicationListInputDto
        {
            RequestedFields = new List<string> { "contactFullName", "contactEmail" }
        });

        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(result.Items.Count);
        result.Items.ShouldAllBe(dto => !string.IsNullOrEmpty(dto.Status));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_WithoutContactRequestedFields_ContactFieldsAreNull()
    {
        var result = await _appService.GetListAsync(new GrantApplicationListInputDto
        {
            RequestedFields = new List<string> { "projectName", "referenceNo" }
        });

        result.ShouldNotBeNull();
        result.Items.ShouldAllBe(dto => dto.ContactFullName == null);
        result.Items.ShouldAllBe(dto => dto.ContactEmail == null);
        result.Items.ShouldAllBe(dto => dto.ContactTitle == null);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_TotalCountEqualsItemsCount_IsConsistentAcrossMultipleCalls()
    {
        var result1 = await _appService.GetListAsync(new GrantApplicationListInputDto());
        var result2 = await _appService.GetListAsync(new GrantApplicationListInputDto());

        result1.TotalCount.ShouldBe(result1.Items.Count);
        result2.TotalCount.ShouldBe(result2.Items.Count);
        result1.TotalCount.ShouldBe(result2.TotalCount);
    }
}