using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Values;
using Unity.Flex.WorksheetInstances;
using Unity.TenantManagement.Onboarding;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.SettingManagement;
using Xunit;

namespace Unity.TenantManagement;

public class OnboardingRequestAppServiceTests : AbpTenantManagementApplicationTestBase
{
    private const string ApplicationCorrelationProvider = "Application";

    private IOnboardingApplicationProvider _applicationProvider = null!;
    private IWorksheetInstanceAppService _worksheetInstanceAppService = null!;
    private IWorksheetAppService _worksheetAppService = null!;
    private IOnboardingUserLookup _userLookup = null!;
    private ITenantAppService _tenantAppService = null!;
    private ISettingManager _settingManager = null!;

    private readonly IOnboardingRequestAppService _appService;

    protected override void AfterAddApplication(IServiceCollection services)
    {
        _applicationProvider = Substitute.For<IOnboardingApplicationProvider>();
        _applicationProvider.GetAllIdsAsync(Arg.Any<string>()).Returns(new List<Guid>());
        _applicationProvider.GetFormVersionIdsAsync(Arg.Any<string>()).Returns(new List<Guid>());
        _applicationProvider.GetMappedCoreFieldColumnsAsync(Arg.Any<string>()).Returns(new List<OnboardingColumnDto>());
        _applicationProvider.GetPagedListAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<IReadOnlyList<ColumnFilterDto>>(), Arg.Any<IReadOnlyList<Guid>>())
            .Returns(new PagedResultDto<OnboardingApplicationRecord>(0, []));

        _worksheetInstanceAppService = Substitute.For<IWorksheetInstanceAppService>();
        _worksheetInstanceAppService.GetListByCorrelationIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<string>())
            .Returns(new List<WorksheetInstanceDataDto>());

        _worksheetAppService = Substitute.For<IWorksheetAppService>();
        _userLookup = Substitute.For<IOnboardingUserLookup>();
        _tenantAppService = Substitute.For<ITenantAppService>();

        _settingManager = Substitute.For<ISettingManager>();
        _settingManager.GetOrNullAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns((string)null);

        services.AddSingleton(_applicationProvider);
        services.AddSingleton(_worksheetInstanceAppService);
        services.AddSingleton(_worksheetAppService);
        services.AddSingleton(_userLookup);
        services.AddSingleton(_tenantAppService);
        services.AddSingleton(_settingManager);
    }

    public OnboardingRequestAppServiceTests()
    {
        _appService = GetRequiredService<IOnboardingRequestAppService>();
    }

    private static WorksheetInstanceDataDto WorksheetInstanceFor(Guid correlationId, params (string Key, string Value)[] fields) =>
        new()
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            WorksheetId = Guid.NewGuid(),
            CurrentValue = System.Text.Json.JsonSerializer.Serialize(new WorksheetInstanceValue
            {
                Values = fields.Select(f => new FieldInstanceValue(f.Key, f.Value)).ToList()
            })
        };

    [Fact]
    public async Task GetListAsync_DynamicColumnFilters_AreCaseInsensitiveAndAndCombined()
    {
        var matching = Guid.NewGuid();
        var wrongMinistry = Guid.NewGuid();
        var wrongBranch = Guid.NewGuid();
        var allIds = new List<Guid> { matching, wrongMinistry, wrongBranch };

        _applicationProvider.GetAllIdsAsync("Onboarding").Returns(allIds);
        _worksheetInstanceAppService.GetListByCorrelationIdsAsync(Arg.Any<List<Guid>>(), ApplicationCorrelationProvider).Returns(new List<WorksheetInstanceDataDto>
        {
            WorksheetInstanceFor(matching, ("ministry", "Health"), ("branch", "North")),
            WorksheetInstanceFor(wrongMinistry, ("ministry", "Finance"), ("branch", "North")),
            WorksheetInstanceFor(wrongBranch, ("ministry", "Health"), ("branch", "South"))
        });

        IReadOnlyList<Guid> capturedMatchIds = null;
        _applicationProvider
            .When(p => p.GetPagedListAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<IReadOnlyList<ColumnFilterDto>>(), Arg.Any<IReadOnlyList<Guid>>()))
            .Do(call => capturedMatchIds = call.ArgAt<IReadOnlyList<Guid>>(7));

        await _appService.GetListAsync(new OnboardingListRequestDto
        {
            ColumnFilters = [
                new ColumnFilterDto { Name = "ministry", Value = "HEALTH" },
                new ColumnFilterDto { Name = "branch", Value = "nor" }
            ]
        });

        capturedMatchIds.ShouldNotBeNull();
        capturedMatchIds.ShouldBe([matching]);
    }

    [Fact]
    public async Task GetListAsync_DynamicColumnSort_StripsFieldsPrefixAndSortsInMemoryWithPaging()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var idC = Guid.NewGuid();
        var allIds = new List<Guid> { idA, idB, idC };

        _applicationProvider.GetAllIdsAsync("Onboarding").Returns(allIds);
        _applicationProvider.GetPagedListAsync(0, int.MaxValue, null, "Onboarding", null, null, null, null)
            .Returns(new PagedResultDto<OnboardingApplicationRecord>(3, [
                new OnboardingApplicationRecord { Id = idA, Category = "Onboarding" },
                new OnboardingApplicationRecord { Id = idB, Category = "Onboarding" },
                new OnboardingApplicationRecord { Id = idC, Category = "Onboarding" }
            ]));

        _worksheetInstanceAppService.GetListByCorrelationIdsAsync(Arg.Any<List<Guid>>(), ApplicationCorrelationProvider).Returns(new List<WorksheetInstanceDataDto>
        {
            WorksheetInstanceFor(idA, ("score", "Charlie")),
            WorksheetInstanceFor(idB, ("score", "Apple")),
            WorksheetInstanceFor(idC, ("score", "Bravo"))
        });

        var result = await _appService.GetListAsync(new OnboardingListRequestDto
        {
            Sorting = "fields.score asc",
            SkipCount = 0,
            MaxResultCount = 2
        });

        result.TotalCount.ShouldBe(3);
        result.Items.Select(i => i.Id).ShouldBe([idB, idC]);
    }

    [Fact]
    public async Task GetColumnSchemaAsync_DedupesFieldsAcrossWorksheets_PreservesOrder()
    {
        var formVersionId = Guid.NewGuid();
        var ws1 = Guid.NewGuid();
        var ws2 = Guid.NewGuid();

        _applicationProvider.GetFormVersionIdsAsync("Onboarding").Returns(new List<Guid> { formVersionId });

        _worksheetAppService.GetListByCorrelationAsync(formVersionId, "FormVersion").Returns(new List<WorksheetDto>
        {
            new() {
                Id = ws1,
                Sections = [
                    new WorksheetSectionDto { Order = 0, Fields = [
                        new CustomFieldDto { Key = "branch", Label = "Branch", Order = 0, Enabled = true },
                        new CustomFieldDto { Key = "ministry", Label = "Ministry", Order = 1, Enabled = true }
                    ]}
                ]
            },
            new() {
                Id = ws2,
                Sections = [
                    new WorksheetSectionDto { Order = 0, Fields = [
                        new CustomFieldDto { Key = "branch", Label = "Branch", Order = 0, Enabled = true }, // duplicate
                        new CustomFieldDto { Key = "hidden", Label = "Hidden", Order = 1, Enabled = false }, // disabled
                        new CustomFieldDto { Key = "email", Label = "Email", Order = 2, Enabled = true }
                    ]}
                ]
            }
        });

        var result = await _appService.GetColumnSchemaAsync();

        result.Columns!.Select(c => c.Key).ShouldBe(["branch", "ministry", "email"]);
    }

    [Fact]
    public async Task GetColumnSchemaAsync_ReturnsColumns_WithNoApplicationsSubmittedYet()
    {
        var formVersionId = Guid.NewGuid();

        _applicationProvider.GetFormVersionIdsAsync("Onboarding").Returns(new List<Guid> { formVersionId });

        _worksheetAppService.GetListByCorrelationAsync(formVersionId, "FormVersion").Returns(new List<WorksheetDto>
        {
            new() {
                Id = Guid.NewGuid(),
                Sections = [
                    new WorksheetSectionDto { Order = 0, Fields = [
                        new CustomFieldDto { Key = "branch", Label = "Branch", Order = 0, Enabled = true }
                    ]}
                ]
            }
        });

        var result = await _appService.GetColumnSchemaAsync();

        result.Columns!.Select(c => c.Key).ShouldBe(["branch"]);
    }

    [Fact]
    public async Task GetColumnSchemaAsync_CombinesFieldsAcrossFormVersions_ByKeyEvenWithDifferentLabel()
    {
        var oldFormVersionId = Guid.NewGuid();
        var newFormVersionId = Guid.NewGuid();

        // Both versions are published and mapped — GetFormVersionIdsAsync now returns every
        // published version, not just the latest, so both contribute columns here.
        _applicationProvider.GetFormVersionIdsAsync("Onboarding")
            .Returns(new List<Guid> { oldFormVersionId, newFormVersionId });

        _worksheetAppService.GetListByCorrelationAsync(oldFormVersionId, "FormVersion").Returns(new List<WorksheetDto>
        {
            new() {
                Id = Guid.NewGuid(),
                Sections = [
                    new WorksheetSectionDto { Order = 0, Fields = [
                        new CustomFieldDto { Key = "branch", Label = "Branch (old wording)", Order = 0, Enabled = true }
                    ]}
                ]
            }
        });
        _worksheetAppService.GetListByCorrelationAsync(newFormVersionId, "FormVersion").Returns(new List<WorksheetDto>
        {
            new() {
                Id = Guid.NewGuid(),
                Sections = [
                    new WorksheetSectionDto { Order = 0, Fields = [
                        new CustomFieldDto { Key = "branch", Label = "Branch (new wording)", Order = 0, Enabled = true },
                        new CustomFieldDto { Key = "ministry", Label = "Ministry", Order = 1, Enabled = true }
                    ]}
                ]
            }
        });

        var result = await _appService.GetColumnSchemaAsync();

        // Same key across versions collapses to one column — the first version encountered wins the label.
        result.Columns!.Select(c => c.Key).ShouldBe(["branch", "ministry"]);
        result.Columns!.First(c => c.Key == "branch").Label.ShouldBe("Branch (old wording)");
    }

    [Fact]
    public async Task GetColumnSchemaAsync_AppendsMappedCoreFieldColumns_AfterWorksheetColumns()
    {
        var formVersionId = Guid.NewGuid();

        _applicationProvider.GetFormVersionIdsAsync("Onboarding").Returns(new List<Guid> { formVersionId });
        _worksheetAppService.GetListByCorrelationAsync(formVersionId, "FormVersion").Returns(new List<WorksheetDto>
        {
            new() {
                Id = Guid.NewGuid(),
                Sections = [
                    new WorksheetSectionDto { Order = 0, Fields = [
                        new CustomFieldDto { Key = "branch", Label = "Branch", Order = 0, Enabled = true }
                    ]}
                ]
            }
        });
        _applicationProvider.GetMappedCoreFieldColumnsAsync("Onboarding").Returns(new List<OnboardingColumnDto>
        {
            new() { Key = "ProjectName", Label = "Project Name", Type = "String", Selected = true },
            new() { Key = "branch", Label = "Branch", Type = "String", Selected = true } // duplicate key — should be deduped
        });

        var result = await _appService.GetColumnSchemaAsync();

        result.Columns!.Select(c => c.Key).ShouldBe(["branch", "ProjectName"]);
    }

    [Fact]
    public async Task GetListAsync_MergesCoreFieldValues_IntoFields()
    {
        var appId = Guid.NewGuid();

        _applicationProvider.GetPagedListAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), "Onboarding", Arg.Any<string>(),
                Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<IReadOnlyList<ColumnFilterDto>>(), Arg.Any<IReadOnlyList<Guid>>())
            .Returns(new PagedResultDto<OnboardingApplicationRecord>(1, [
                new OnboardingApplicationRecord
                {
                    Id = appId,
                    Category = "Onboarding",
                    CoreFieldValues = new Dictionary<string, object> { ["ProjectName"] = "Bridge Repair" }
                }
            ]));

        var result = await _appService.GetListAsync(new OnboardingListRequestDto());

        result.Items[0].Fields["ProjectName"].ShouldBe("Bridge Repair");
    }

    [Fact]
    public async Task GetListAsync_SortByMappedCoreField_PassesThroughToProvider_NoInMemoryFetchAll()
    {
        _applicationProvider.GetMappedCoreFieldColumnsAsync("Onboarding").Returns(new List<OnboardingColumnDto>
        {
            new() { Key = "ProjectName", Label = "Project Name", Type = "String", Selected = true }
        });

        (int Skip, int Take, string Sorting)? captured = null;
        _applicationProvider
            .When(p => p.GetPagedListAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<IReadOnlyList<ColumnFilterDto>>(), Arg.Any<IReadOnlyList<Guid>>()))
            .Do(call => captured = (call.ArgAt<int>(0), call.ArgAt<int>(1), call.ArgAt<string>(2)));

        await _appService.GetListAsync(new OnboardingListRequestDto
        {
            Sorting = "fields.ProjectName asc",
            SkipCount = 5,
            MaxResultCount = 10
        });

        // A core field is provider-handled: paging/sort go straight to SQL — no "fetch everything,
        // sort in memory" fallback (which would show up as Skip=0, Take=int.MaxValue, Sorting=null).
        captured.ShouldNotBeNull();
        captured!.Value.Skip.ShouldBe(5);
        captured.Value.Take.ShouldBe(10);
        captured.Value.Sorting.ShouldBe("ProjectName ASC");
    }

    [Fact]
    public async Task GetListAsync_ColumnFilterOnMappedCoreField_RoutedAsStaticFilter_NotWorksheetScan()
    {
        _applicationProvider.GetMappedCoreFieldColumnsAsync("Onboarding").Returns(new List<OnboardingColumnDto>
        {
            new() { Key = "ProjectName", Label = "Project Name", Type = "String", Selected = true }
        });

        IReadOnlyList<ColumnFilterDto> capturedStaticFilters = null;
        IReadOnlyList<Guid> capturedDynamicMatchIds = null;
        _applicationProvider
            .When(p => p.GetPagedListAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<IReadOnlyList<ColumnFilterDto>>(), Arg.Any<IReadOnlyList<Guid>>()))
            .Do(call =>
            {
                capturedStaticFilters = call.ArgAt<IReadOnlyList<ColumnFilterDto>>(6);
                capturedDynamicMatchIds = call.ArgAt<IReadOnlyList<Guid>>(7);
            });

        await _appService.GetListAsync(new OnboardingListRequestDto
        {
            ColumnFilters = [new ColumnFilterDto { Name = "ProjectName", Value = "bridge" }]
        });

        capturedStaticFilters.ShouldNotBeNull();
        capturedStaticFilters!.ShouldContain(f => f.Name == "ProjectName" && f.Value == "bridge");
        // Worksheet match precomputation never ran for this filter.
        capturedDynamicMatchIds.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_ExplicitTenantNameFieldKey_OverridesSavedMapping()
    {
        var id = Guid.NewGuid();
        _applicationProvider.GetByIdAsync(id).Returns(new OnboardingApplicationRecord { Id = id, Category = "Onboarding" });
        _worksheetInstanceAppService.GetListByCorrelationIdsAsync(Arg.Any<List<Guid>>(), ApplicationCorrelationProvider).Returns(new List<WorksheetInstanceDataDto>
        {
            WorksheetInstanceFor(id, ("explicitKey", "Brand New Co"), ("savedKey", "acme"))
        });

        // Simulates a previously saved column mapping pointing at a different (colliding) field.
        _settingManager.GetOrNullAsync(OnboardingColumnConfigSettings.TenantNameFieldKey, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns("savedKey");

        var result = await _appService.ValidateAsync(id, tenantNameFieldKey: "explicitKey", superUsersFieldKey: null);

        result.Issues.ShouldNotContain(i => i.StartsWith("[Tenant Name]"));
    }

    [Fact]
    public async Task ValidateAsync_AggregatesFailuresFromAllStepsInOrder()
    {
        var id = Guid.NewGuid();
        _applicationProvider.GetByIdAsync(id).Returns(new OnboardingApplicationRecord { Id = id, Category = "Onboarding" });
        _worksheetInstanceAppService.GetListByCorrelationIdsAsync(Arg.Any<List<Guid>>(), ApplicationCorrelationProvider).Returns(new List<WorksheetInstanceDataDto>
        {
            WorksheetInstanceFor(id, ("tn", "acme"), ("su", "not-an-email"))
        });

        var result = await _appService.ValidateAsync(id, tenantNameFieldKey: "tn", superUsersFieldKey: "su");

        result.IsValid.ShouldBeFalse();
        result.Issues.Count.ShouldBe(2);
        result.Issues[0].ShouldStartWith("[Tenant Name]");
        result.Issues[1].ShouldStartWith("[Super Users]");
    }

    [Fact]
    public async Task CreateTenantAsync_NoValidSuperUsers_ThrowsAndDoesNotCreateTenant()
    {
        var id = Guid.NewGuid();
        _applicationProvider.GetByIdAsync(id).Returns(new OnboardingApplicationRecord { Id = id, Category = "Onboarding" });
        _worksheetInstanceAppService.GetListByCorrelationIdsAsync(Arg.Any<List<Guid>>(), ApplicationCorrelationProvider).Returns(new List<WorksheetInstanceDataDto>
        {
            WorksheetInstanceFor(id, ("tn", "Brand New Co"), ("su", "not-an-email"))
        });

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateTenantAsync(id, new CreateTenantInputDto
        {
            TenantNameFieldKey = "tn",
            SuperUsersFieldKey = "su"
        }));

        await _tenantAppService.DidNotReceive().CreateAsync(Arg.Any<TenantCreateDto>());
    }

    [Fact]
    public async Task CreateTenantAsync_DuplicateTenantName_ThrowsAndDoesNotCreateTenant()
    {
        var id = Guid.NewGuid();
        _applicationProvider.GetByIdAsync(id).Returns(new OnboardingApplicationRecord { Id = id, Category = "Onboarding" });
        _worksheetInstanceAppService.GetListByCorrelationIdsAsync(Arg.Any<List<Guid>>(), ApplicationCorrelationProvider).Returns(new List<WorksheetInstanceDataDto>
        {
            // "acme" is seeded by the test host, so this collides even though super users resolve fine.
            WorksheetInstanceFor(id, ("tn", "acme"), ("su", "first@example.com"))
        });
        _userLookup.FindUserGuidByEmailAsync("first@example.com").Returns("guid-1");

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateTenantAsync(id, new CreateTenantInputDto
        {
            TenantNameFieldKey = "tn",
            SuperUsersFieldKey = "su"
        }));

        await _tenantAppService.DidNotReceive().CreateAsync(Arg.Any<TenantCreateDto>());
        await _applicationProvider.DidNotReceive().CloseApplicationAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task CreateTenantAsync_ValidSuperUsers_CreatesTenantAndAssignsRemainingAsManagers()
    {
        var id = Guid.NewGuid();
        var newTenantId = Guid.NewGuid();

        _applicationProvider.GetByIdAsync(id).Returns(new OnboardingApplicationRecord { Id = id, Category = "Onboarding" });
        _worksheetInstanceAppService.GetListByCorrelationIdsAsync(Arg.Any<List<Guid>>(), ApplicationCorrelationProvider).Returns(new List<WorksheetInstanceDataDto>
        {
            WorksheetInstanceFor(id, ("tn", "New Co"), ("su", "first@example.com,second@example.com"), ("branch", "North"))
        });

        _userLookup.FindUserGuidByEmailAsync("first@example.com").Returns("guid-1");
        _userLookup.FindUserGuidByEmailAsync("second@example.com").Returns("guid-2");

        _tenantAppService.CreateAsync(Arg.Any<TenantCreateDto>())
            .Returns(new TenantDto { Id = newTenantId, Name = "New Co" });

        await _appService.CreateTenantAsync(id, new CreateTenantInputDto
        {
            TenantNameFieldKey = "tn",
            SuperUsersFieldKey = "su",
            BranchFieldKey = "branch"
        });

        await _tenantAppService.Received(1).CreateAsync(Arg.Is<TenantCreateDto>(d =>
            d.Name == "New Co" && d.Branch == "North" && d.UserIdentifier == "guid-1"));
        await _tenantAppService.Received(1).AssignManagerAsync(Arg.Is<TenantAssignManagerDto>(d =>
            d.TenantId == newTenantId && d.UserIdentifier == "guid-2"));
        await _applicationProvider.Received(1).CloseApplicationAsync(id);
    }
}
