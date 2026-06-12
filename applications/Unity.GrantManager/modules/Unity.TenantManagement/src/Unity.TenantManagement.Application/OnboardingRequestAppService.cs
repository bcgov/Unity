#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.Modules.Shared.Permissions;
using Unity.TenantManagement.Onboarding;
using Unity.TenantManagement.Validation;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.TenantManagement;

[Authorize(IdentityConsts.ITOperationsPolicyName)]
public class OnboardingRequestAppService(
    IEnumerable<IOnboardingValidationStep> validationSteps
) : ApplicationService, IOnboardingRequestAppService
{
    private readonly IEnumerable<IOnboardingValidationStep> _validationSteps = validationSteps;

    private ITenantAppService TenantAppService =>
        LazyServiceProvider.LazyGetRequiredService<ITenantAppService>();

    private IOnboardingUserLookup? UserLookup =>
        LazyServiceProvider.LazyGetService<IOnboardingUserLookup>();

    private static readonly List<OnboardingRequestDto> MockOnboardingRequests =
    [
        new()
        {
            Id = Guid.NewGuid(),
            TenantName = "Community Housing Programs",
            TenantDescription = "Grant programs supporting community housing initiatives",
            ProgramAreaName = "Housing Supply",
            ProgramAreaDescription = "Programs that increase the supply of affordable housing",
            Contacts = "Jane Smith, Tom Lee",
            Features = "Application Intake, Reporting",
            SuperUsers = "jane.smith@gov.bc.ca",
            ExecutiveDirector = "Pat Wilson",
            Branch = "Housing Policy Branch",
            Ministry = "Ministry of Housing",
            Status = "In Review",
            Category = "Grant",
            SubmissionDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.NewGuid(),
            TenantName = "Clean Energy Rebates",
            TenantDescription = "Rebate programs for clean energy adoption",
            ProgramAreaName = "Energy Efficiency",
            ProgramAreaDescription = "Programs that promote energy efficient retrofits",
            Contacts = "Alex Chen",
            Features = "Application Intake, Payments, Reporting",
            SuperUsers = "andre.goncalves@gov.bc.ca",
            ExecutiveDirector = "Morgan Lee",
            Branch = "Energy Programs Branch",
            Ministry = "Ministry of Energy and Climate Solutions",
            Status = "Approved",
            Category = "Rebate",
            SubmissionDate = new DateTime(2025, 11, 3, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.NewGuid(),
            TenantName = "Agricultural Innovation Fund",
            TenantDescription = "Funding to support innovation in the agriculture sector",
            ProgramAreaName = "Agri-Innovation",
            ProgramAreaDescription = "Programs that fund innovative agricultural projects",
            Contacts = "Priya Patel, Sam O'Brien",
            Features = "Application Intake, Scoring, Reporting",
            SuperUsers = "priya.patel@gov.bc.ca",
            ExecutiveDirector = "Casey Brown",
            Branch = "Industry Competitiveness Branch",
            Ministry = "Ministry of Agriculture and Food",
            Status = "Pending",
            Category = "Grant",
            SubmissionDate = new DateTime(2026, 3, 22, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.NewGuid(),
            TenantName = "Small Business Recovery Grants",
            TenantDescription = "Grants supporting small business recovery and growth",
            ProgramAreaName = "Business Supports",
            ProgramAreaDescription = "Programs that provide direct supports to small businesses",
            Contacts = "Jordan Taylor",
            Features = "Application Intake, Payments",
            SuperUsers = "jordan.taylor@gov.bc.ca",
            ExecutiveDirector = "Riley Anderson",
            Branch = "Small Business Branch",
            Ministry = "Ministry of Jobs, Economic Development and Innovation",
            Status = "In Review",
            Category = "Grant",
            SubmissionDate = new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.NewGuid(),
            TenantName = "Watershed Restoration Grants",
            TenantDescription = "Grants funding the restoration of watershed ecosystems",
            ProgramAreaName = "Environmental Stewardship",
            ProgramAreaDescription = "Programs that support environmental restoration projects",
            Contacts = "Taylor Morgan, Casey Nguyen",
            Features = "Application Intake, Reporting",
            SuperUsers = "taylor.morgan@gov.bc.ca",
            ExecutiveDirector = "Drew Campbell",
            Branch = "Ecosystems Branch",
            Ministry = "Ministry of Water, Land and Resource Stewardship",
            Status = "Approved",
            Category = "Contribution Agreement",
            SubmissionDate = new DateTime(2025, 9, 17, 0, 0, 0, DateTimeKind.Utc)
        }
    ];

    public virtual Task<OnboardingRequestDto?> GetAsync(Guid id)
    {
        return Task.FromResult(MockOnboardingRequests.FirstOrDefault(r => r.Id == id));
    }

    public virtual async Task<OnboardingValidationResultDto> ValidateAsync(Guid id)
    {
        var request = MockOnboardingRequests.FirstOrDefault(r => r.Id == id);
        if (request == null)
            return new OnboardingValidationResultDto
            {
                IsValid = false,
                Issues = ["Onboarding request not found."]
            };

        var issues = new List<string>();

        foreach (var step in _validationSteps.OrderBy(s => s.Order))
        {
            var stepResult = await step.ValidateAsync(request);
            if (!stepResult.IsValid && stepResult.Issue is not null)
                issues.Add($"[{step.StepName}] {stepResult.Issue}");
        }

        return new OnboardingValidationResultDto
        {
            IsValid = issues.Count == 0,
            Issues = issues
        };
    }

    public virtual async Task CreateTenantAsync(Guid id)
    {
        var request = MockOnboardingRequests.FirstOrDefault(r => r.Id == id)
            ?? throw new UserFriendlyException("Onboarding request not found.");

        var emails = SuperUsersValidationStep.ParseEmails(request.SuperUsers);

        // Resolve all valid GUIDs in host context before tenant creation
        var userGuids = new List<string>();
        if (UserLookup is not null)
        {
            foreach (var email in emails)
            {
                var guid = await UserLookup.FindUserGuidByEmailAsync(email);
                if (!string.IsNullOrWhiteSpace(guid))
                    userGuids.Add(guid);
            }
        }

        if (userGuids.Count == 0)
            throw new UserFriendlyException("No valid super users could be resolved. Cannot create tenant without at least one valid program manager.");

        var featureKeys = OnboardingFeatureMap.ResolveFeatureKeys(request.Features);

        // TenantCreatedEto handler migrates/seeds the DB, imports the first user, and enables features
        var tenantDto = await TenantAppService.CreateAsync(new TenantCreateDto
        {
            Name = request.TenantName,
            Branch = request.Branch,
            Description = request.TenantDescription,
            UserIdentifier = userGuids[0],
            FeatureKeys = featureKeys.Count > 0 ? string.Join(',', featureKeys) : null
        });

        // Assign any additional super users as program managers
        foreach (var userGuid in userGuids.Skip(1))
        {
            await TenantAppService.AssignManagerAsync(new TenantAssignManagerDto
            {
                TenantId = tenantDto.Id,
                UserIdentifier = userGuid
            });
        }
    }

    public virtual Task<PagedResultDto<OnboardingRequestDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var sorted = ApplySorting(MockOnboardingRequests, input.Sorting ?? string.Empty);

        var items = sorted
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        return Task.FromResult(new PagedResultDto<OnboardingRequestDto>(MockOnboardingRequests.Count, items));
    }

    private static IEnumerable<OnboardingRequestDto> ApplySorting(IEnumerable<OnboardingRequestDto> query, string sorting)
    {
        if (sorting.IsNullOrWhiteSpace())
        {
            return query.OrderBy(o => o.TenantName);
        }

        var sortParts = sorting.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sortField = sortParts[0];
        var descending = sortParts.Length > 1 && sortParts[1].Equals("DESC", StringComparison.OrdinalIgnoreCase);

        Func<OnboardingRequestDto, string> keySelector = sortField switch
        {
            nameof(OnboardingRequestDto.TenantDescription) => o => o.TenantDescription,
            nameof(OnboardingRequestDto.ProgramAreaName) => o => o.ProgramAreaName,
            nameof(OnboardingRequestDto.ProgramAreaDescription) => o => o.ProgramAreaDescription,
            nameof(OnboardingRequestDto.Contacts) => o => o.Contacts,
            nameof(OnboardingRequestDto.Features) => o => o.Features,
            nameof(OnboardingRequestDto.SuperUsers) => o => o.SuperUsers,
            nameof(OnboardingRequestDto.ExecutiveDirector) => o => o.ExecutiveDirector,
            nameof(OnboardingRequestDto.Branch) => o => o.Branch,
            nameof(OnboardingRequestDto.Ministry) => o => o.Ministry,
            nameof(OnboardingRequestDto.Status) => o => o.Status,
            nameof(OnboardingRequestDto.Category) => o => o.Category,
            nameof(OnboardingRequestDto.SubmissionDate) => o => o.SubmissionDate?.ToString("O") ?? "",
            _ => o => o.TenantName
        };

        return descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }
}
