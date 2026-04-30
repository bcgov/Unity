using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IApplicationRepository))]
public class ApplicationRepository
    : EfCoreRepository<GrantTenantDbContext, Application, Guid>,
      IApplicationRepository
{
    private static readonly TimeZoneInfo VancouverTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

    public ApplicationRepository(
        IDbContextProvider<GrantTenantDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    /// <summary>
    /// Converts Vancouver local date range to UTC range (inclusive)
    /// </summary>
    private static (DateTime? FromUtc, DateTime? ToUtc) ConvertToUtcRange(
        DateTime? fromLocal,
        DateTime? toLocal)
    {
        DateTime? fromUtc = null;
        DateTime? toUtc = null;

        if (fromLocal.HasValue)
        {
            var localFrom = DateTime.SpecifyKind(
                fromLocal.Value,
                DateTimeKind.Unspecified);

            fromUtc = TimeZoneInfo.ConvertTimeToUtc(
                localFrom,
                VancouverTimeZone);
        }

        if (toLocal.HasValue)
        {
            // End of local day (23:59:59.9999999)
            var localToEndOfDay = DateTime.SpecifyKind(
                toLocal.Value.Date.AddDays(1).AddTicks(-1),
                DateTimeKind.Unspecified);

            toUtc = TimeZoneInfo.ConvertTimeToUtc(
                localToEndOfDay,
                VancouverTimeZone);
        }

        return (fromUtc, toUtc);
    }

    private static readonly HashSet<string> ApplicantAgentFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "contactFullName", "contactTitle", "contactEmail",
        "contactBusinessPhone", "contactCellPhone"
    };

    private static readonly HashSet<string> TagFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "applicationTag"
    };

    private static readonly HashSet<string> OwnerFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "Owner"
    };

    private static readonly HashSet<string> ApplicationLinkFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "applicationLinks"
    };

    private static readonly HashSet<string> AssignmentFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "assignees"
    };

    /// <summary>
    /// Base query with conditional includes driven by <paramref name="requestedFields"/>.
    /// Applicant, ApplicationForm, and ApplicationStatus are always joined because
    /// their getters throw <see cref="InvalidOperationException"/> when the backing
    /// field is null. The remaining five navigations are only joined when a visible
    /// column requires them.
    /// When <paramref name="requestedFields"/> is <c>null</c> or empty every navigation
    /// is included (backward-compatible behaviour).
    /// </summary>
    private async Task<IQueryable<Application>> BuildBaseQueryAsync()
    {
        return (await GetQueryableAsync())
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Applicant)
            .Include(a => a.ApplicationForm)
            .Include(a => a.ApplicationStatus)
            .Include(a => a.ApplicantAgent)
            .Include(a => a.ApplicationTags!).ThenInclude(x => x.Tag)
            .Include(a => a.Owner)
            .Include(a => a.ApplicationLinks)
            .Include(a => a.ApplicationAssignments!).ThenInclude(aa => aa.Assignee);
    }

    public async Task<Application> WithBasicDetailsAsync(Guid id)
    {
        var application = await (await GetQueryableAsync())
            .AsNoTracking()
            .Include(a => a.Applicant)
                .ThenInclude(a => a.ApplicantAddresses)
            .Include(a => a.ApplicantAgent)
            .Include(a => a.ApplicationStatus)
            .FirstAsync(a => a.Id == id);

        if (application.Applicant?.ApplicantAddresses != null)
        {
            application.Applicant.ApplicantAddresses =
                new Collection<ApplicantAddress>(
                    application.Applicant.ApplicantAddresses
                        .Where(addr => addr.ApplicationId == id)
                        .ToList());
        }

        return application;
    }

    public async Task<Application?> GetWithFullDetailsByIdAsync(Guid id)
    {
        return await (await GetQueryableAsync())
            .AsNoTracking()
            .Include(a => a.ApplicationStatus)
            .Include(a => a.ApplicationForm)
            .Include(a => a.ApplicationTags)
            .Include(a => a.Owner)
            .Include(a => a.ApplicationAssignments!)
                .ThenInclude(aa => aa.Assignee)
            .Include(a => a.Applicant)
            .Include(a => a.ApplicantAgent)
            .Include(a => a.ApplicationLinks)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Application>> GetListByIdsAsync(Guid[] ids)
    {
        return await (await GetQueryableAsync())
            .AsNoTracking()
            .Include(a => a.ApplicationStatus)
            .Include(a => a.Applicant)
            .Include(a => a.ApplicationForm)
            .Where(a => ids.Contains(a.Id))
            .ToListAsync();
    }

    public override async Task<IQueryable<Application>> WithDetailsAsync()
    {
        return (await GetQueryableAsync()).IncludeDetails();
    }

    public async Task<long> GetCountAsync(
        DateTime? submittedFromDate,
        DateTime? submittedToDate)
    {
        // Dont use full query, run basic to just get count.
        var query = (await GetQueryableAsync()).AsNoTracking();
        var (fromUtc, toUtc) = ConvertToUtcRange(
            submittedFromDate,
            submittedToDate);

        if (fromUtc.HasValue)
            query = query.Where(a => a.SubmissionDate >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(a => a.SubmissionDate <= toUtc.Value);

        return await query.LongCountAsync();
    }

    public async Task<List<Application>> WithFullDetailsAsync(
        int skipCount,
        int maxResultCount,
        string? sorting = null,
        DateTime? submittedFromDate = null,
        DateTime? submittedToDate = null,
        string? searchTerm = null)
    {
        var query = await BuildBaseQueryAsync();
        var (fromUtc, toUtc) = ConvertToUtcRange(
            submittedFromDate,
            submittedToDate);

        if (fromUtc.HasValue)
            query = query.Where(a => a.SubmissionDate >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(a => a.SubmissionDate <= toUtc.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(a =>
                a.ProjectName.Contains(searchTerm) ||
                a.ReferenceNo.Contains(searchTerm));

        query = ApplySorting(query, sorting);

        return await query
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync();
    }

    public async Task<List<Application>> GetByApplicantIdAsync(Guid applicantId)
    {
        var query = await BuildBaseQueryAsync();

        return await query
            .Where(a => a.ApplicantId == applicantId)
            .OrderByDescending(a => a.SubmissionDate)
            .ToListAsync();
    }

    public async Task<List<ApplicationListRecord>> GetApplicationListRecordsAsync(
        int skipCount,
        int maxResultCount,
        string? sorting = null,
        DateTime? submittedFromDate = null,
        DateTime? submittedToDate = null,
        string? searchTerm = null,
        IReadOnlyList<string>? requestedFields = null)
    {
        var fields = requestedFields != null
            ? new HashSet<string>(requestedFields, StringComparer.OrdinalIgnoreCase)
            : null; // null = all fields included

        var query = (await GetQueryableAsync()).AsNoTracking();

        // Check for required links
        bool includeTags = fields == null || fields.Overlaps(TagFields);
        bool includeAssignees = fields == null || fields.Overlaps(AssignmentFields);
        bool includeLinks = fields == null || fields.Overlaps(ApplicationLinkFields);
        bool includeApplicantAgent = fields == null || fields.Overlaps(ApplicantAgentFields);
        bool includeOwner = fields == null || fields.Overlaps(OwnerFields);

        var (fromUtc, toUtc) = ConvertToUtcRange(submittedFromDate, submittedToDate);
        if (fromUtc.HasValue)
        {
            query = query.Where(a => a.SubmissionDate >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(a => a.SubmissionDate <= toUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(a =>
                a.ProjectName.Contains(searchTerm) ||
                a.ReferenceNo.Contains(searchTerm));
        }

        query = ApplySorting(query, sorting);

        return await query
            .Skip(skipCount)
            .Take(maxResultCount)
            .Select(a => new ApplicationListRecord
            {
                Id = a.Id,
                ProjectName = a.ProjectName,
                ReferenceNo = a.ReferenceNo,
                RequestedAmount = a.RequestedAmount,
                TotalProjectBudget = a.TotalProjectBudget,
                EconomicRegion = a.EconomicRegion,
                City = a.City,
                ProposalDate = a.ProposalDate,
                SubmissionDate = a.SubmissionDate,
                FinalDecisionDate = a.FinalDecisionDate,
                DueDate = a.DueDate,
                NotificationDate = a.NotificationDate,
                ProjectSummary = a.ProjectSummary,
                TotalScore = a.TotalScore,
                RecommendedAmount = a.RecommendedAmount,
                ApprovedAmount = a.ApprovedAmount,
                LikelihoodOfFunding = a.LikelihoodOfFunding,
                DueDiligenceStatus = a.DueDiligenceStatus,
                SubStatus = a.SubStatus,
                DeclineRational = a.DeclineRational,
                Notes = a.Notes,
                AssessmentResultStatus = a.AssessmentResultStatus,
                AssessmentResultDate = a.AssessmentResultDate,
                ProjectStartDate = a.ProjectStartDate,
                ProjectEndDate = a.ProjectEndDate,
                PercentageTotalProjectBudget = a.PercentageTotalProjectBudget,
                ProjectFundingTotal = a.ProjectFundingTotal,
                Community = a.Community,
                CommunityPopulation = a.CommunityPopulation,
                Acquisition = a.Acquisition,
                Forestry = a.Forestry,
                ForestryFocus = a.ForestryFocus,
                ElectoralDistrict = a.ElectoralDistrict,
                ApplicantElectoralDistrict = a.ApplicantElectoralDistrict,
                Place = a.Place,
                RegionalDistrict = a.RegionalDistrict,
                OwnerId = a.OwnerId,
                DefaultSiteId = a.DefaultSiteId,
                SigningAuthorityFullName = a.SigningAuthorityFullName,
                SigningAuthorityTitle = a.SigningAuthorityTitle,
                SigningAuthorityEmail = a.SigningAuthorityEmail,
                SigningAuthorityBusinessPhone = a.SigningAuthorityBusinessPhone,
                SigningAuthorityCellPhone = a.SigningAuthorityCellPhone,
                ContractNumber = a.ContractNumber,
                ContractExecutionDate = a.ContractExecutionDate,
                RiskRanking = a.RiskRanking,
                UnityApplicationId = a.UnityApplicationId,

                // ApplicationStatus
                Status = a.ApplicationStatus.InternalStatus,

                // ApplicationForm
                Category = a.ApplicationForm.Category ?? string.Empty,

                // Applicant
                ApplicantId = a.ApplicantId,
                ApplicantName = a.Applicant.ApplicantName,
                ApplicantSupplierId = a.Applicant.SupplierId,
                ApplicantSector = a.Applicant.Sector,
                ApplicantSubSector = a.Applicant.SubSector,
                ApplicantOrgName = a.Applicant.OrgName,
                ApplicantNonRegOrgName = a.Applicant.NonRegOrgName,
                ApplicantOrganizationType = a.Applicant.OrganizationType,
                ApplicantOrgNumber = a.Applicant.OrgNumber,
                ApplicantOrgStatus = a.Applicant.OrgStatus,
                ApplicantBusinessNumber = a.Applicant.BusinessNumber,
                ApplicantOrganizationSize = a.Applicant.OrganizationSize,
                ApplicantSectorSubSectorIndustryDesc = a.Applicant.SectorSubSectorIndustryDesc,
                ApplicantRedStop = a.Applicant.RedStop,
                ApplicantIndigenousOrgInd = a.Applicant.IndigenousOrgInd,
                ApplicantFiscalDay = a.Applicant.FiscalDay,
                ApplicantFiscalMonth = a.Applicant.FiscalMonth,
                ApplicantUnityApplicantId = a.Applicant.UnityApplicantId,

                // ApplicantAgent (left-joined)
                ContactFullName = includeApplicantAgent && a.ApplicantAgent != null ? a.ApplicantAgent.Name : null,
                ContactTitle = includeApplicantAgent && a.ApplicantAgent != null ? a.ApplicantAgent.Title : null,
                ContactEmail = includeApplicantAgent && a.ApplicantAgent != null ? a.ApplicantAgent.Email : null,
                ContactBusinessPhone = includeApplicantAgent && a.ApplicantAgent != null ? a.ApplicantAgent.Phone : null,
                ContactCellPhone = includeApplicantAgent && a.ApplicantAgent != null ? a.ApplicantAgent.Phone2 : null,

                // Owner (left-joined)
                OwnerPersonId = includeOwner && a.Owner != null ? a.Owner.Id : (Guid?)null,
                OwnerFullName = includeOwner && a.Owner != null ? a.Owner.FullName : null,

                // Collections projected as correlated subqueries — skipped when column not visible
                Tags = includeTags && a.ApplicationTags != null
                    ? a.ApplicationTags.Select(t => new ApplicationTagListItem
                    {
                        Id = t.Id,
                        ApplicationId = t.ApplicationId,
                        TagName = t.Tag != null ? t.Tag.Name : null
                    }).ToList()
                    : new List<ApplicationTagListItem>(),

                Assignments = includeAssignees && a.ApplicationAssignments != null
                    ? a.ApplicationAssignments.Select(aa => new ApplicationAssignmentListItem
                    {
                        Id = aa.Id,
                        ApplicationId = aa.ApplicationId,
                        AssigneeId = aa.AssigneeId,
                        AssigneeName = aa.Assignee != null ? aa.Assignee.FullName : string.Empty,
                        Duty = aa.Duty
                    }).ToList()
                    : new List<ApplicationAssignmentListItem>(),

                Links = includeLinks && a.ApplicationLinks != null
                    ? a.ApplicationLinks.Select(l => new ApplicationLinkListItem
                    {
                        Id = l.Id,
                        ApplicationId = l.ApplicationId,
                        LinkedApplicationId = l.LinkedApplicationId,
                        LinkType = l.LinkType
                    }).ToList()
                    : new List<ApplicationLinkListItem>()
            }).ToListAsync();
    }

    public async Task<List<Application>> GetApplicationsBySiteIdAsync(Guid siteId)
    {
        return await (await GetQueryableAsync())
            .AsNoTracking()
            .Where(a => a.DefaultSiteId == siteId)
            .ToListAsync();
    }

    private static IQueryable<Application> ApplySorting(
        IQueryable<Application> query,
        string? sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting))
            return query.OrderBy(a => a.SubmissionDate);

        var sortingFields = sorting
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !f.StartsWith("rowCount", StringComparison.OrdinalIgnoreCase))
            .Select(MapSortingField)
            .Where(f => f != null)
            .ToArray();

        if (sortingFields.Length > 0)
        {
            var sortingExpression = string.Join(",", sortingFields);
            try
            {
                return query.OrderBy(sortingExpression);
            }
            catch
            {
                return query.OrderBy(a => a.SubmissionDate);
            }
        }

        return query.OrderBy(a => a.SubmissionDate);
    }

    private static string? MapSortingField(string field)
    {
        if (field.StartsWith("status", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "status",
                "ApplicationStatus.InternalStatus",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("category", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "category",
                "ApplicationForm.Category",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("assignees", StringComparison.OrdinalIgnoreCase))
        {
            var parts = field.Split(' ', 2);
            return parts.Length == 2
                ? $"ApplicationAssignments.Count() {parts[1]}"
                : "ApplicationAssignments.Count()";
        }

        if (field.StartsWith("subStatusDisplayValue", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "subStatusDisplayValue",
                "SubStatus",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("applicationTag", StringComparison.OrdinalIgnoreCase))
        {
            var parts = field.Split(' ', 2);
            return parts.Length == 2
                ? $"ApplicationTags.FirstOrDefault().Text {parts[1]}"
                : "ApplicationTags.FirstOrDefault().Text";
        }

        if (field.StartsWith("organizationType", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "organizationType",
                "Applicant.OrganizationType",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("organizationName", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "organizationName",
                "Applicant.OrgName",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("businessNumber", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "businessNumber",
                "Applicant.BusinessNumber",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("contactFullName", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "contactFullName",
                "ApplicantAgent.Name",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("contactTitle", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "contactTitle",
                "ApplicantAgent.Title",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("contactEmail", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "contactEmail",
                "ApplicantAgent.Email",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("contactBusinessPhone", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "contactBusinessPhone",
                "ApplicantAgent.Phone",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("contactCellPhone", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "contactCellPhone",
                "ApplicantAgent.Phone2",
                StringComparison.OrdinalIgnoreCase);

        return field;
    }
}
