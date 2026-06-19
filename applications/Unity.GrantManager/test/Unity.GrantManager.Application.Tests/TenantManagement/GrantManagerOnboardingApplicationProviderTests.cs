using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.TenantManagement;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.TenantManagement;

public class GrantManagerOnboardingApplicationProviderTests : GrantManagerApplicationTestBase
{
    private readonly IOnboardingApplicationProvider _provider;
    private readonly IRepository<Application, Guid> _applicationRepository;
    private readonly IRepository<ApplicationForm, Guid> _applicationFormRepository;
    private readonly IRepository<ApplicationStatus, Guid> _applicationStatusRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public GrantManagerOnboardingApplicationProviderTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _provider = GetRequiredService<IOnboardingApplicationProvider>();
        _applicationRepository = GetRequiredService<IRepository<Application, Guid>>();
        _applicationFormRepository = GetRequiredService<IRepository<ApplicationForm, Guid>>();
        _applicationStatusRepository = GetRequiredService<IRepository<ApplicationStatus, Guid>>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
    }

    private async Task<Guid> CreateFormAsync(string category)
    {
        var form = await _applicationFormRepository.InsertAsync(new ApplicationForm
        {
            IntakeId = GrantManagerTestData.Intake1_Id,
            ApplicationFormName = "Onboarding Provider Test Form",
            Category = category
        }, autoSave: true);
        return form.Id;
    }

    private async Task<Guid> CreateStatusAsync(string internalStatus, GrantApplicationState statusCode) =>
        (await _applicationStatusRepository.InsertAsync(new ApplicationStatus
        {
            InternalStatus = internalStatus,
            ExternalStatus = internalStatus,
            StatusCode = statusCode
        }, autoSave: true)).Id;

    private async Task<Application> CreateApplicationAsync(
        Guid applicationFormId, Guid applicationStatusId, string referenceNo, DateTime submissionDate) =>
        await _applicationRepository.InsertAsync(new Application
        {
            ApplicationFormId = applicationFormId,
            ApplicationStatusId = applicationStatusId,
            ApplicantId = GrantManagerTestData.Applicant1_Id,
            ReferenceNo = referenceNo,
            SubmissionDate = submissionDate
        }, autoSave: true);

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedListAsync_NoSorting_DefaultsToSubmissionDateDescending()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        var statusId = await CreateStatusAsync("Submitted", GrantApplicationState.ASSIGNED);

        var older = await CreateApplicationAsync(formId, statusId, "REF-OLD", new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = await CreateApplicationAsync(formId, statusId, "REF-NEW", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetPagedListAsync(0, 10, sorting: null, category: category);

        result.Items[0].Id.ShouldBe(newer.Id);
        result.Items[1].Id.ShouldBe(older.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedListAsync_UnknownSortField_FallsBackToSubmissionDateDescending()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        var statusId = await CreateStatusAsync("Submitted", GrantApplicationState.WITHDRAWN);

        var older = await CreateApplicationAsync(formId, statusId, "REF-OLD", new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = await CreateApplicationAsync(formId, statusId, "REF-NEW", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetPagedListAsync(0, 10, sorting: "notAField asc", category: category);

        result.Items[0].Id.ShouldBe(newer.Id);
        result.Items[1].Id.ShouldBe(older.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedListAsync_StaticColumnFilters_AreCaseInsensitive()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        var approvedStatusId = await CreateStatusAsync("Approved", GrantApplicationState.ON_HOLD);
        var rejectedStatusId = await CreateStatusAsync("Rejected", GrantApplicationState.DEFER);

        var approved = await CreateApplicationAsync(formId, approvedStatusId, "REF-A", DateTime.UtcNow);
        await CreateApplicationAsync(formId, rejectedStatusId, "REF-B", DateTime.UtcNow);

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetPagedListAsync(0, 10, sorting: null, category: category,
            staticColumnFilters: [new ColumnFilterDto { Name = "status", Value = "APPROVED" }]);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(approved.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedListAsync_EmptyDynamicColumnMatchIds_ShortCircuitsToZeroResults()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        var statusId = await CreateStatusAsync("Submitted", GrantApplicationState.GRANT_APPROVED);
        await CreateApplicationAsync(formId, statusId, "REF-A", DateTime.UtcNow);

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetPagedListAsync(0, 10, sorting: null, category: category,
            dynamicColumnMatchIds: []);

        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedListAsync_GlobalFilter_OrsStaticFieldMatchWithPrecomputedWorksheetMatchIds()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        var statusId = await CreateStatusAsync("Submitted", GrantApplicationState.GRANT_NOT_APPROVED);

        // Matches via static ReferenceNo field
        var staticMatch = await CreateApplicationAsync(formId, statusId, "ZEBRA-MATCH", DateTime.UtcNow);
        // Doesn't match any static field, but is in the pre-computed worksheet match set
        var worksheetMatch = await CreateApplicationAsync(formId, statusId, "REF-OTHER", DateTime.UtcNow);
        // Matches neither
        await CreateApplicationAsync(formId, statusId, "REF-EXCLUDED", DateTime.UtcNow);

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetPagedListAsync(0, 10, sorting: null, category: category,
            filter: "ZEBRA",
            globalDynamicMatchIds: [worksheetMatch.Id]);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldContain(i => i.Id == staticMatch.Id);
        result.Items.ShouldContain(i => i.Id == worksheetMatch.Id);
    }
}
