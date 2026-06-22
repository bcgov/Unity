using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly IRepository<ApplicationFormVersion, Guid> _applicationFormVersionRepository;
    private readonly IRepository<ApplicationStatus, Guid> _applicationStatusRepository;
    private readonly IRepository<Applicant, Guid> _applicantRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public GrantManagerOnboardingApplicationProviderTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _provider = GetRequiredService<IOnboardingApplicationProvider>();
        _applicationRepository = GetRequiredService<IRepository<Application, Guid>>();
        _applicationFormRepository = GetRequiredService<IRepository<ApplicationForm, Guid>>();
        _applicationFormVersionRepository = GetRequiredService<IRepository<ApplicationFormVersion, Guid>>();
        _applicationStatusRepository = GetRequiredService<IRepository<ApplicationStatus, Guid>>();
        _applicantRepository = GetRequiredService<IRepository<Applicant, Guid>>();
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

    private async Task<Guid> CreateFormVersionAsync(
        Guid applicationFormId, int? version, bool published, string? submissionHeaderMapping = null) =>
        (await _applicationFormVersionRepository.InsertAsync(new ApplicationFormVersion
        {
            ApplicationFormId = applicationFormId,
            Version = version,
            Published = published,
            SubmissionHeaderMapping = submissionHeaderMapping
        }, autoSave: true)).Id;

    private async Task<Guid> CreateStatusAsync(string internalStatus, GrantApplicationState statusCode) =>
        (await _applicationStatusRepository.InsertAsync(new ApplicationStatus
        {
            InternalStatus = internalStatus,
            ExternalStatus = internalStatus,
            StatusCode = statusCode
        }, autoSave: true)).Id;

    private async Task<Guid> CreateApplicantAsync(string applicantName) =>
        (await _applicantRepository.InsertAsync(new Applicant
        {
            ApplicantName = applicantName
        }, autoSave: true)).Id;

    private async Task<Application> CreateApplicationAsync(
        Guid applicationFormId, Guid applicationStatusId, string referenceNo, DateTime submissionDate,
        Guid? applicantId = null, string? projectName = null, decimal? requestedAmount = null) =>
        await _applicationRepository.InsertAsync(new Application
        {
            ApplicationFormId = applicationFormId,
            ApplicationStatusId = applicationStatusId,
            ApplicantId = applicantId ?? GrantManagerTestData.Applicant1_Id,
            ReferenceNo = referenceNo,
            SubmissionDate = submissionDate,
            ProjectName = projectName ?? string.Empty,
            RequestedAmount = requestedAmount ?? 0
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

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetFormVersionIdsAsync_IncludesEveryVersion_PerForm_RegardlessOfPublishedFlag()
    {
        // CHEFS sync auto-unpublishes a form's previous version the moment a new one is
        // published, so in practice only one version is ever Published==true at a time. Column
        // discovery must not filter on that flag or it would silently drop every prior version's
        // mappings — see GetFormVersionsAsync.
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        var oldUnpublished = await CreateFormVersionAsync(formId, version: 1, published: false);
        var currentPublished = await CreateFormVersionAsync(formId, version: 2, published: true);

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetFormVersionIdsAsync(category);

        result.ShouldBe([oldUnpublished, currentPublished], ignoreOrder: true);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetFormVersionIdsAsync_FormWithNoVersions_IsExcluded()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        await CreateFormAsync(category);

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetFormVersionIdsAsync(category);

        result.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetFormVersionIdsAsync_UnknownCategory_ReturnsEmpty()
    {
        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetFormVersionIdsAsync($"OnboardingTest-{Guid.NewGuid()}");

        result.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetMappedCoreFieldColumnsAsync_ReturnsOnlyFieldsWithASubmissionHeaderMapping()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        await CreateFormVersionAsync(formId, version: 1, published: true,
            submissionHeaderMapping: """{"ProjectName": "projectName", "RequestedAmount": "requestedAmount"}""");

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetMappedCoreFieldColumnsAsync(category);

        result.Select(c => c.Key).ShouldBe(["ProjectName", "RequestedAmount"], ignoreOrder: true);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetMappedCoreFieldColumnsAsync_CombinesMappingsAcrossAllVersions()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        // The older version got auto-unpublished when v2 went live (CHEFS sync behavior), and
        // it maps a different core field than the current published version. Both should still
        // surface — columns are combined by key across every version, published or not.
        await CreateFormVersionAsync(formId, version: 1, published: false,
            submissionHeaderMapping: """{"ProjectName": "projectName"}""");
        await CreateFormVersionAsync(formId, version: 2, published: true,
            submissionHeaderMapping: """{"RequestedAmount": "requestedAmount"}""");

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetMappedCoreFieldColumnsAsync(category);

        result.Select(c => c.Key).ShouldBe(["ProjectName", "RequestedAmount"], ignoreOrder: true);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetMappedCoreFieldColumnsAsync_FieldNotInRegistry_IsExcluded()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        // TotalScore is grant-assessment workflow state, intentionally never added to the
        // onboarding core-field registry — see OnboardingCoreFieldRegistry header comment.
        await CreateFormVersionAsync(formId, version: 1, published: true,
            submissionHeaderMapping: """{"TotalScore": "totalScore"}""");

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetMappedCoreFieldColumnsAsync(category);

        result.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetMappedCoreFieldColumnsAsync_IncludesApplicantAgentBackedContactEmail()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        await CreateFormVersionAsync(formId, version: 1, published: true,
            submissionHeaderMapping: """{"ContactEmail": "contactEmail"}""");

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetMappedCoreFieldColumnsAsync(category);

        result.Select(c => c.Key).ShouldBe(["ContactEmail"]);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedListAndById_PopulateContactEmail_FromApplicantAgent()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        await CreateFormVersionAsync(formId, version: 1, published: true,
            submissionHeaderMapping: """{"ContactEmail": "contactEmail"}""");
        var statusId = await CreateStatusAsync("Submitted", GrantApplicationState.ASSIGNED);

        var withAgent = await CreateApplicationAsync(formId, statusId, "REF-AGENT", DateTime.UtcNow);
        var applicantAgentRepository = GetRequiredService<IRepository<ApplicantAgent, Guid>>();
        await applicantAgentRepository.InsertAsync(new ApplicantAgent
        {
            ApplicantId = withAgent.ApplicantId,
            ApplicationId = withAgent.Id,
            Name = "Jane Contact",
            Email = "jane.contact@example.com"
        }, autoSave: true);

        var withoutAgent = await CreateApplicationAsync(formId, statusId, "REF-NO-AGENT", DateTime.UtcNow);

        using var uow = _unitOfWorkManager.Begin();

        var paged = await _provider.GetPagedListAsync(0, 10, sorting: null, category: category);
        paged.Items.First(i => i.Id == withAgent.Id).CoreFieldValues["ContactEmail"].ShouldBe("jane.contact@example.com");
        paged.Items.First(i => i.Id == withoutAgent.Id).CoreFieldValues["ContactEmail"].ShouldBeNull();

        var byId = await _provider.GetByIdAsync(withAgent.Id);
        byId!.CoreFieldValues["ContactEmail"].ShouldBe("jane.contact@example.com");

        var byIdNoAgent = await _provider.GetByIdAsync(withoutAgent.Id);
        byIdNoAgent!.CoreFieldValues["ContactEmail"].ShouldBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedListAndById_PopulateCoreFieldValues_ForMappedFields()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        await CreateFormVersionAsync(formId, version: 1, published: true,
            submissionHeaderMapping: """{"ProjectName": "projectName"}""");
        var statusId = await CreateStatusAsync("Submitted", GrantApplicationState.ASSIGNED);
        var applicantId = await CreateApplicantAsync("Test Applicant");

        var application = await CreateApplicationAsync(formId, statusId, "REF-CORE", DateTime.UtcNow,
            applicantId: applicantId, projectName: "Bridge Repair", requestedAmount: 5000m);

        using var uow = _unitOfWorkManager.Begin();

        var paged = await _provider.GetPagedListAsync(0, 10, sorting: null, category: category);
        paged.Items[0].CoreFieldValues.ShouldContainKey("ProjectName");
        paged.Items[0].CoreFieldValues["ProjectName"].ShouldBe("Bridge Repair");
        paged.Items[0].CoreFieldValues.ShouldNotContainKey("RequestedAmount"); // not mapped for this form

        var byId = await _provider.GetByIdAsync(application.Id);
        byId!.CoreFieldValues["ProjectName"].ShouldBe("Bridge Repair");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedListAsync_SortsByCoreField()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        var statusId = await CreateStatusAsync("Submitted", GrantApplicationState.ASSIGNED);

        var bravo = await CreateApplicationAsync(formId, statusId, "REF-A", DateTime.UtcNow, projectName: "Bravo Project");
        var alpha = await CreateApplicationAsync(formId, statusId, "REF-B", DateTime.UtcNow, projectName: "Alpha Project");

        using var uow = _unitOfWorkManager.Begin();

        var ascending = await _provider.GetPagedListAsync(0, 10, sorting: "ProjectName ASC", category: category);
        ascending.Items[0].Id.ShouldBe(alpha.Id);
        ascending.Items[1].Id.ShouldBe(bravo.Id);

        var descending = await _provider.GetPagedListAsync(0, 10, sorting: "ProjectName DESC", category: category);
        descending.Items[0].Id.ShouldBe(bravo.Id);
        descending.Items[1].Id.ShouldBe(alpha.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedListAsync_SortsByApplicantBackedCoreField()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        var statusId = await CreateStatusAsync("Submitted", GrantApplicationState.ASSIGNED);

        var zooApplicantId = await CreateApplicantAsync("Zoo Org");
        var acmeApplicantId = await CreateApplicantAsync("Acme Org");

        var zoo = await CreateApplicationAsync(formId, statusId, "REF-A", DateTime.UtcNow, applicantId: zooApplicantId);
        var acme = await CreateApplicationAsync(formId, statusId, "REF-B", DateTime.UtcNow, applicantId: acmeApplicantId);

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetPagedListAsync(0, 10, sorting: "ApplicantName ASC", category: category);

        result.Items[0].Id.ShouldBe(acme.Id);
        result.Items[1].Id.ShouldBe(zoo.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedListAsync_FiltersByCoreFieldContains()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        var statusId = await CreateStatusAsync("Submitted", GrantApplicationState.ASSIGNED);

        var matching = await CreateApplicationAsync(formId, statusId, "REF-A", DateTime.UtcNow, projectName: "Bridge Repair");
        await CreateApplicationAsync(formId, statusId, "REF-B", DateTime.UtcNow, projectName: "Road Resurfacing");

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetPagedListAsync(0, 10, sorting: null, category: category,
            staticColumnFilters: [new ColumnFilterDto { Name = "ProjectName", Value = "bridge" }]);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(matching.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedListAsync_FiltersByNonTextCoreField_HasNoEffect()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        var statusId = await CreateStatusAsync("Submitted", GrantApplicationState.ASSIGNED);
        await CreateApplicationAsync(formId, statusId, "REF-A", DateTime.UtcNow, requestedAmount: 5000m);

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetPagedListAsync(0, 10, sorting: null, category: category,
            staticColumnFilters: [new ColumnFilterDto { Name = "RequestedAmount", Value = "5000" }]);

        // Currency/Number fields aren't text-filterable yet — the filter is a no-op rather than an error.
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedListAsync_GlobalFilter_MatchesMappedCoreFieldValue()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        await CreateFormVersionAsync(formId, version: 1, published: true,
            submissionHeaderMapping: """{"ProjectName": "projectName"}""");
        var statusId = await CreateStatusAsync("Submitted", GrantApplicationState.ASSIGNED);

        var matching = await CreateApplicationAsync(formId, statusId, "REF-A", DateTime.UtcNow, projectName: "Bridge Repair");
        await CreateApplicationAsync(formId, statusId, "REF-B", DateTime.UtcNow, projectName: "Road Resurfacing");

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetPagedListAsync(0, 10, sorting: null, category: category, filter: "bridge");

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(matching.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedListAsync_GlobalFilter_UnmappedCoreField_DoesNotMatch()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        await CreateFormVersionAsync(formId, version: 1, published: true); // no SubmissionHeaderMapping
        var statusId = await CreateStatusAsync("Submitted", GrantApplicationState.ASSIGNED);
        await CreateApplicationAsync(formId, statusId, "REF-A", DateTime.UtcNow, projectName: "Bridge Repair");

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetPagedListAsync(0, 10, sorting: null, category: category, filter: "bridge");

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedListAsync_GlobalFilter_CombinesCoreFieldMatchesWithPrecomputedWorksheetMatchIds()
    {
        var category = $"OnboardingTest-{Guid.NewGuid()}";
        var formId = await CreateFormAsync(category);
        await CreateFormVersionAsync(formId, version: 1, published: true,
            submissionHeaderMapping: """{"ProjectName": "projectName"}""");
        var statusId = await CreateStatusAsync("Submitted", GrantApplicationState.ASSIGNED);

        var coreMatch = await CreateApplicationAsync(formId, statusId, "REF-A", DateTime.UtcNow, projectName: "Bridge Repair");
        var worksheetMatch = await CreateApplicationAsync(formId, statusId, "REF-B", DateTime.UtcNow);
        await CreateApplicationAsync(formId, statusId, "REF-C", DateTime.UtcNow); // matches neither

        using var uow = _unitOfWorkManager.Begin();
        var result = await _provider.GetPagedListAsync(0, 10, sorting: null, category: category,
            filter: "bridge",
            globalDynamicMatchIds: [worksheetMatch.Id]);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldContain(i => i.Id == coreMatch.Id);
        result.Items.ShouldContain(i => i.Id == worksheetMatch.Id);
    }
}
