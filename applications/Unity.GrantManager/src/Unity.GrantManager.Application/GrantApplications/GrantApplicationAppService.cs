using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Comments;
using Unity.GrantManager.Events;
using Unity.GrantManager.Exceptions;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Permissions;
using Unity.Payments.Suppliers;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Microsoft.EntityFrameworkCore;
using Unity.Modules.Shared.Correlation;
using Unity.GrantManager.Payments;
using Unity.Flex.WorksheetInstances;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Flex;
using Unity.Payments.Integrations.Cas;
using Microsoft.Extensions.Logging;
using Unity.Flex.Worksheets;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(GrantApplicationAppService), typeof(IGrantApplicationAppService))]
public class GrantApplicationAppService : GrantManagerAppService, IGrantApplicationAppService
{

    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationManager _applicationManager;
    private readonly IApplicationStatusRepository _applicationStatusRepository;
    private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
    private readonly IApplicationAssignmentRepository _applicationAssignmentRepository;
    private readonly IApplicantRepository _applicantRepository;
    private readonly ICommentsManager _commentsManager;
    private readonly IApplicationFormRepository _applicationFormRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IApplicantAgentRepository _applicantAgentRepository;
    private readonly IApplicantAddressRepository _applicantAddressRepository;
    private readonly ILocalEventBus _localEventBus;
    private readonly ISupplierService _iSupplierService;

    public GrantApplicationAppService(
        IApplicationManager applicationManager,
        IApplicationRepository applicationRepository,
        IApplicationStatusRepository applicationStatusRepository,
        IApplicationAssignmentRepository applicationAssignmentRepository,
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IApplicantRepository applicantRepository,
        ICommentsManager commentsManager,
        IApplicationFormRepository applicationFormRepository,
        IPersonRepository personRepository,
        IApplicantAgentRepository applicantAgentRepository,
        IApplicantAddressRepository applicantAddressRepository,
        ILocalEventBus localEventBus,
        ISupplierService iSupplierService)
    {
        _applicationRepository = applicationRepository;
        _applicationManager = applicationManager;
        _applicationStatusRepository = applicationStatusRepository;
        _applicationAssignmentRepository = applicationAssignmentRepository;
        _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
        _applicantRepository = applicantRepository;
        _commentsManager = commentsManager;
        _applicationFormRepository = applicationFormRepository;
        _personRepository = personRepository;
        _applicantAgentRepository = applicantAgentRepository;
        _applicantAddressRepository = applicantAddressRepository;
        _iSupplierService = iSupplierService;
        _localEventBus = localEventBus;
    }

    public async Task<PagedResultDto<GrantApplicationDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var groupedResult = await _applicationRepository.WithFullDetailsGroupedAsync(input.SkipCount, input.MaxResultCount);
        var appDtos = new List<GrantApplicationDto>();
        var rowCounter = 0;

        foreach (var grouping in groupedResult)
        {
            var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(grouping.First());
            appDto.Status = grouping.First().ApplicationStatus.InternalStatus;

            appDto.Applicant = ObjectMapper.Map<Applicant, GrantApplicationApplicantDto>(grouping.First().Applicant);
            appDto.Category = grouping.First().ApplicationForm.Category ?? string.Empty;
            appDto.AssessmentCount = grouping.First().Assessments?.Count ?? 0;
            appDto.AssessmentReviewCount = grouping.First().Assessments?.Count(a => a.Status == AssessmentState.IN_REVIEW) ?? 0;
            appDto.ApplicationTag = grouping.First().ApplicationTags?.FirstOrDefault()?.Text ?? string.Empty;
            appDto.Owner = BuildApplicationOwner(grouping.First().Owner);
            appDto.OrganizationName = grouping.First().Applicant?.OrgName ?? string.Empty;
            appDto.OrganizationType = grouping.First().Applicant?.OrganizationType ?? string.Empty;
            appDto.Assignees = BuildApplicationAssignees(grouping.First().ApplicationAssignments);
            appDto.SubStatusDisplayValue = MapSubstatusDisplayValue(appDto.SubStatus);
            appDto.DeclineRational = MapDeclineRationalDisplayValue(appDto.DeclineRational);
            appDto.ContactFullName = grouping.First().ApplicantAgent?.Name;
            appDto.ContactEmail = grouping.First().ApplicantAgent?.Email;
            appDto.ContactTitle = grouping.First().ApplicantAgent?.Title;
            appDto.ContactBusinessPhone = grouping.First().ApplicantAgent?.Phone;
            appDto.ContactCellPhone = grouping.First().ApplicantAgent?.Phone2;
            appDto.RowCount = rowCounter;
            appDtos.Add(appDto);
            rowCounter++;
        }

        var totalCount = await _applicationRepository.GetCountAsync();

        return new PagedResultDto<GrantApplicationDto>(totalCount, appDtos);
    }


    private static string MapSubstatusDisplayValue(string subStatus)
    {
        if (subStatus == null) { return string.Empty; }
        var hasKey = AssessmentResultsOptionsList.SubStatusActionList.TryGetValue(subStatus, out string? subStatusValue);
        if (hasKey)
            return subStatusValue ?? string.Empty;
        else
            return string.Empty;
    }
    private static string MapDeclineRationalDisplayValue(string value)
    {
        if (value == null) { return string.Empty; }
        var hasKey = AssessmentResultsOptionsList.DeclineRationalActionList.TryGetValue(value, out string? subStatusValue);
        if (hasKey)
            return subStatusValue ?? string.Empty;
        else
            return string.Empty;
    }

    private static List<GrantApplicationAssigneeDto> BuildApplicationAssignees(IEnumerable<ApplicationAssignment>? applicationAssignments)
    {
        var appAssignmentDtos = new List<GrantApplicationAssigneeDto>();
        if (applicationAssignments != null)
        {
            foreach (var assignment in applicationAssignments)
            {
                appAssignmentDtos.Add(new GrantApplicationAssigneeDto()
                {
                    ApplicationId = assignment.ApplicationId,
                    AssigneeId = assignment.AssigneeId,
                    FullName = assignment.Assignee?.FullName ?? string.Empty,
                    Id = assignment.Id,
                    Duty = assignment.Duty
                });
            }
        }
        return appAssignmentDtos;
    }

    private static GrantApplicationAssigneeDto BuildApplicationOwner(Person? applicationOwner)
    {
        if (applicationOwner != null)
        {
            return new GrantApplicationAssigneeDto()
            {
                Id = applicationOwner.Id,
                FullName = applicationOwner.FullName
            };
        }
        return new GrantApplicationAssigneeDto();
    }

    public async Task<GrantApplicationDto> GetAsync(Guid id)
    {
        var application = await _applicationRepository.GetAsync(id, true);

        if (application == null) return new GrantApplicationDto();

        var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(application);

        appDto.StatusCode = application.ApplicationStatus.StatusCode;
        appDto.Status = application.ApplicationStatus.InternalStatus;

        if (application.ApplicantAgent != null)
        {
            appDto.ContactFullName = application.ApplicantAgent.Name;
            appDto.ContactEmail = application.ApplicantAgent.Email;
            appDto.ContactTitle = application.ApplicantAgent.Title;
            appDto.ContactBusinessPhone = application.ApplicantAgent.Phone;
            appDto.ContactCellPhone = application.ApplicantAgent.Phone2;
        }

        if (application.Applicant != null)
        {
            appDto.OrganizationName = application.Applicant.OrgName;
            appDto.OrgNumber = application.Applicant.OrgNumber;
            appDto.OrganizationSize = application.Applicant.OrganizationSize;
            appDto.OrgStatus = application.Applicant.OrgStatus;
            appDto.OrganizationName = application.Applicant.OrgName;
            appDto.Sector = application.Applicant.Sector;
            appDto.OrganizationType = application.Applicant.OrganizationType;
            appDto.SubSector = application.Applicant.SubSector;
            appDto.SectorSubSectorIndustryDesc = application.Applicant.SectorSubSectorIndustryDesc;
        }

        return appDto;
    }

    public async Task<GetSummaryDto> GetSummaryAsync(Guid applicationId)
    {
        var query = from application in await _applicationRepository.GetQueryableAsync()
                    join applicationForm in await _applicationFormRepository.GetQueryableAsync() on application.ApplicationFormId equals applicationForm.Id
                    join applicant in await _applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                    where application.Id == applicationId
                    select new GetSummaryDto
                    {
                        Category = applicationForm == null ? string.Empty : applicationForm.Category,
                        SubmissionDate = application.SubmissionDate,
                        OrganizationName = applicant.OrgName,
                        OrganizationNumber = applicant.OrgNumber,
                        EconomicRegion = application.EconomicRegion,
                        City = application.City,
                        RequestedAmount = application.RequestedAmount,
                        ProjectBudget = application.TotalProjectBudget,
                        Sector = applicant.Sector,
                        Community = application.Community,
                        Status = application.ApplicationStatus.InternalStatus,
                        LikelihoodOfFunding = application.LikelihoodOfFunding != null && application.LikelihoodOfFunding != "" ? AssessmentResultsOptionsList.FundingList[application.LikelihoodOfFunding] : "",
                        AssessmentStartDate = string.Format("{0:yyyy/MM/dd}", application.AssessmentStartDate),
                        FinalDecisionDate = string.Format("{0:yyyy/MM/dd}", application.FinalDecisionDate),
                        TotalScore = application.TotalScore.ToString(),
                        AssessmentResult = application.AssessmentResultStatus != null && application.AssessmentResultStatus != "" ? AssessmentResultsOptionsList.AssessmentResultStatusList[application.AssessmentResultStatus] : "",
                        RecommendedAmount = application.RecommendedAmount,
                        ApprovedAmount = application.ApprovedAmount,
                        Batch = "", // to-do: ask BA for the implementation of Batch field,                        
                        RegionalDistrict = application.RegionalDistrict,
                        OwnerId = application.OwnerId,

                    };

        var queryResult = await AsyncExecuter.FirstOrDefaultAsync(query);
        if (queryResult != null)
        {
            var ownerId = queryResult.OwnerId ?? Guid.Empty;
            queryResult.Owner = await GetOwnerAsync(ownerId);
            queryResult.Assignees = await GetAssigneesAsync(applicationId);

            return queryResult;
        }
        else
        {
            return await Task.FromResult(new GetSummaryDto());
        }

    }

    public async Task<GrantApplicationDto> UpdateAssessmentResultsAsync(Guid id, CreateUpdateAssessmentResultsDto input)
    {
        var application = await _applicationRepository.GetAsync(id);

        SanitizeAssessmentResultsDisabledInputs(input, application);

        application.ValidateAndChangeDueDate(input.DueDate);
        application.UpdateAlwaysChangeableFields(input.Notes, input.SubStatus, input.LikelihoodOfFunding, input.TotalProjectBudget, input.NotificationDate, input.RiskRanking);

        if (application.IsInFinalDecisionState())
        {
            if (await CurrentUserCanUpdateFieldsPostFinalDecisionAsync()) // User allowed to edit specific fields past approval
            {
                application.UpdateFieldsRequiringPostEditPermission(input.ApprovedAmount, input.RequestedAmount, input.TotalScore);
            }
        }
        else
        {
            if (await CurrentUsCanUpdateAssessmentFieldsAsync())
            {
                application.ValidateAndChangeFinalDecisionDate(input.FinalDecisionDate);
                application.UpdateFieldsRequiringPostEditPermission(input.ApprovedAmount, input.RequestedAmount, input.TotalScore);
                application.UpdateFieldsOnlyForPreFinalDecision(input.DueDiligenceStatus,
                    input.RecommendedAmount,
                    input.DeclineRational);

                application.UpdateAssessmentResultStatus(input.AssessmentResultStatus);
            }
        }

        await PublishCustomFieldUpdatesAsync(application.Id, FlexConsts.AssessmentInfoUiAnchor, input);

        await _applicationRepository.UpdateAsync(application);

        return ObjectMapper.Map<Application, GrantApplicationDto>(application);
    }

    private static void SanitizeAssessmentResultsDisabledInputs(CreateUpdateAssessmentResultsDto input, Application application)
    {
        // Cater for disabled fields that are not serialized with post - fall back to the previous value, these should be 0 from the API call
        input.TotalProjectBudget ??= application.TotalProjectBudget;
        input.RecommendedAmount ??= application.RecommendedAmount;
        input.ApprovedAmount ??= application.ApprovedAmount;
        input.TotalScore ??= application.TotalScore;
        input.RequestedAmount ??= application.RequestedAmount;
    }

    private async Task<bool> CurrentUsCanUpdateAssessmentFieldsAsync()
    {
        return await AuthorizationService.IsGrantedAsync(GrantApplicationPermissions.AssessmentResults.Edit);
    }

    private async Task<bool> CurrentUserCanUpdateFieldsPostFinalDecisionAsync()
    {
        return await AuthorizationService.IsGrantedAsync(GrantApplicationPermissions.AssessmentResults.EditFinalStateFields);
    }

    public async Task<GrantApplicationDto> UpdateProjectInfoAsync(Guid id, CreateUpdateProjectInfoDto input)
    {
        var application = await _applicationRepository.GetAsync(id);

        SanitizeProjectInfoDisabledInputs(input, application);

        var percentageTotalProjectBudget = (input.TotalProjectBudget == 0 || input.TotalProjectBudget == null) ? 0 : decimal.Multiply(decimal.Divide(input.RequestedAmount ?? 0, input.TotalProjectBudget ?? 0), 100).To<double>();

        if (application != null)
        {
            application.ProjectSummary = input.ProjectSummary;
            application.ProjectName = input.ProjectName ?? string.Empty;
            application.RequestedAmount = input.RequestedAmount ?? 0;
            application.TotalProjectBudget = input.TotalProjectBudget ?? 0;
            application.ProjectStartDate = input.ProjectStartDate;
            application.ProjectEndDate = input.ProjectEndDate;
            application.PercentageTotalProjectBudget = Math.Round(percentageTotalProjectBudget, 2);
            application.ProjectFundingTotal = input.ProjectFundingTotal;
            application.Community = input.Community;
            application.CommunityPopulation = input.CommunityPopulation;
            application.Acquisition = input.Acquisition;
            application.Forestry = input.Forestry;
            application.ForestryFocus = input.ForestryFocus;
            application.EconomicRegion = input.EconomicRegion;
            application.ElectoralDistrict = input.ElectoralDistrict;
            application.RegionalDistrict = input.RegionalDistrict;
            application.ContractNumber = input.ContractNumber;
            application.ContractExecutionDate = input.ContractExecutionDate;
            application.Place = input.Place;

            await PublishCustomFieldUpdatesAsync(application.Id, FlexConsts.ProjectInfoUiAnchor, input);

            await _applicationRepository.UpdateAsync(application);

            return ObjectMapper.Map<Application, GrantApplicationDto>(application);
        }
        else
        {
            throw new EntityNotFoundException();
        }
    }

    private static void SanitizeProjectInfoDisabledInputs(CreateUpdateProjectInfoDto input, Application application)
    {
        // Cater for disabled fields that are not serialized with post - fall back to the previous value, these should be 0 from the API call
        input.TotalProjectBudget ??= application.TotalProjectBudget;
        input.RequestedAmount ??= application.RequestedAmount;
        input.ProjectFundingTotal ??= application.ProjectFundingTotal;
    }

    public async Task<GrantApplicationDto> UpdateProjectApplicantInfoAsync(Guid id, CreateUpdateApplicantInfoDto input)
    {
        var application = await _applicationRepository.GetAsync(id);

        if (application != null)
        {
            var applicant = await _applicantRepository.FirstOrDefaultAsync(a => a.Id == application.ApplicantId) ?? throw new EntityNotFoundException();
            // This applicant should never be null!

            applicant.OrganizationType = input.OrganizationType ?? "";
            applicant.OrgName = input.OrgName ?? "";
            applicant.OrgNumber = input.OrgNumber ?? "";
            applicant.OrgStatus = input.OrgStatus ?? "";
            applicant.OrganizationSize = input.OrganizationSize ?? "";
            applicant.Sector = input.Sector ?? "";
            applicant.SubSector = input.SubSector ?? "";
            applicant.SectorSubSectorIndustryDesc = input.SectorSubSectorIndustryDesc ?? "";

            _ = await _applicantRepository.UpdateAsync(applicant);

            // Integrate with payments module to update / insert supplier
            // Check that the original supplier number has changed
            if (await FeatureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature)
                && !string.IsNullOrEmpty(input.SupplierNumber)
                && input.OriginalSupplierNumber != input.SupplierNumber)
            {
                dynamic casSupplierResponse = await _iSupplierService.GetCasSupplierInformationAsync(input.SupplierNumber);
                UpsertSupplierEto supplierEto = GetEventDtoFromCasResponse(casSupplierResponse);
                supplierEto.CorrelationId = applicant.Id;
                supplierEto.CorrelationProvider = CorrelationConsts.Applicant;
                await _localEventBus.PublishAsync(supplierEto);
            }

            var applicantAgent = await _applicantAgentRepository.FirstOrDefaultAsync(agent => agent.ApplicantId == application.ApplicantId && agent.ApplicationId == application.Id);
            if (applicantAgent == null)
            {
                applicantAgent = await _applicantAgentRepository.InsertAsync(new ApplicantAgent
                {
                    ApplicantId = application.ApplicantId,
                    ApplicationId = application.Id,
                    Name = input.ContactFullName ?? "",
                    Phone = input.ContactBusinessPhone ?? "",
                    Phone2 = input.ContactCellPhone ?? "",
                    Email = input.ContactEmail ?? "",
                    Title = input.ContactTitle ?? ""
                });
            }
            else
            {
                applicantAgent.Name = input.ContactFullName ?? "";
                applicantAgent.Phone = input.ContactBusinessPhone ?? "";
                applicantAgent.Phone2 = input.ContactCellPhone ?? "";
                applicantAgent.Email = input.ContactEmail ?? "";
                applicantAgent.Title = input.ContactTitle ?? "";
                applicantAgent = await _applicantAgentRepository.UpdateAsync(applicantAgent);
            }

            await UpdateApplicantAddresses(input);

            application.SigningAuthorityFullName = input.SigningAuthorityFullName ?? "";
            application.SigningAuthorityTitle = input.SigningAuthorityTitle ?? "";
            application.SigningAuthorityEmail = input.SigningAuthorityEmail ?? "";
            application.SigningAuthorityBusinessPhone = input.SigningAuthorityBusinessPhone ?? "";
            application.SigningAuthorityCellPhone = input.SigningAuthorityCellPhone ?? "";

            await PublishCustomFieldUpdatesAsync(application.Id, FlexConsts.ApplicantInfoUiAnchor, input);

            await _applicationRepository.UpdateAsync(application);

            var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(application);

            appDto.ContactFullName = applicantAgent.Name;
            appDto.ContactEmail = applicantAgent.Email;
            appDto.ContactTitle = applicantAgent.Title;
            appDto.ContactBusinessPhone = applicantAgent.Phone;
            appDto.ContactCellPhone = applicantAgent.Phone2;

            return appDto;
        }
        else
        {
            throw new EntityNotFoundException();
        }
    }

    protected virtual async Task PublishCustomFieldUpdatesAsync(Guid applicationId,
        string uiAnchor,
        CustomDataFieldDto input)
    {
        if (await FeatureChecker.IsEnabledAsync("Unity.Flex"))
        {
            if (input.CorrelationId != Guid.Empty)
            {
                await _localEventBus.PublishAsync(new PersistWorksheetIntanceValuesEto()
                {
                    InstanceCorrelationId = applicationId,
                    InstanceCorrelationProvider = CorrelationConsts.Application,
                    SheetCorrelationId = input.CorrelationId,
                    SheetCorrelationProvider = CorrelationConsts.FormVersion,
                    UiAnchor = uiAnchor,
                    CustomFields = input.CustomFields,
                    WorksheetId = input.WorksheetId                    
                });
            }
            else
            {
                Logger.LogError("Unable to resolve for version");
            }
        }
    }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
    protected virtual UpsertSupplierEto GetEventDtoFromCasResponse(dynamic casSupplierResponse)
    {
        string lastUpdated = casSupplierResponse.GetProperty("lastupdated").ToString();
        string suppliernumber = casSupplierResponse.GetProperty("suppliernumber").ToString();
        string suppliername = casSupplierResponse.GetProperty("suppliername").ToString();
        string subcategory = casSupplierResponse.GetProperty("subcategory").ToString();
        string providerid = casSupplierResponse.GetProperty("providerid").ToString();
        string businessnumber = casSupplierResponse.GetProperty("businessnumber").ToString();
        string status = casSupplierResponse.GetProperty("status").ToString();
        string supplierprotected = casSupplierResponse.GetProperty("supplierprotected").ToString();
        string standardindustryclassification = casSupplierResponse.GetProperty("standardindustryclassification").ToString();

        _ = DateTime.TryParse(lastUpdated, out DateTime lastUpdatedDate);
        List<SiteEto> siteEtos = new List<SiteEto>();
        JArray siteArray = JsonConvert.DeserializeObject<dynamic>(casSupplierResponse.GetProperty("supplieraddress").ToString());
        foreach (dynamic site in siteArray)
        {
            siteEtos.Add(GetSiteEto(site));
        }

        return new UpsertSupplierEto
        {
            Number = suppliernumber,
            Name = suppliername,
            Subcategory = subcategory,
            ProviderId = providerid,
            BusinessNumber = businessnumber,
            Status = status,
            SupplierProtected = supplierprotected,
            StandardIndustryClassification = standardindustryclassification,
            LastUpdatedInCAS = lastUpdatedDate,
            SiteEtos = siteEtos
        };
    }

    protected static SiteEto GetSiteEto(dynamic site)
    {
        string supplierSiteCode = site["suppliersitecode"].ToString();
        string addressLine1 = site["addressline1"].ToString();
        string addressLine2 = site["addressline2"].ToString();
        string city = site["city"].ToString();
        string province = site["province"].ToString();
        string country = site["country"].ToString();
        string postalCode = site["postalcode"].ToString();
        string emailAddress = site["emailaddress"].ToString();
        string eftAdvicePref = site["eftadvicepref"].ToString();
        string providerId = site["providerid"].ToString();
        string siteStatus = site["status"].ToString();
        string siteProtected = site["siteprotected"].ToString();
        string siteLastUpdated = site["lastupdated"].ToString();

        _ = DateTime.TryParse(siteLastUpdated, out DateTime siteLastUpdatedDate);
        return new SiteEto
        {
            SupplierSiteCode = supplierSiteCode,
            AddressLine1 = addressLine1,
            AddressLine2 = addressLine2,
            AddressLine3 = addressLine2,
            City = city,
            Province = province,
            Country = country,
            PostalCode = postalCode,
            EmailAddress = emailAddress,
            EFTAdvicePref = eftAdvicePref,
            ProviderId = providerId,
            Status = siteStatus,
            SiteProtected = siteProtected,
            LastUpdated = siteLastUpdatedDate
        };
    }
#pragma warning restore CS8600

    protected virtual async Task UpdateApplicantAddresses(CreateUpdateApplicantInfoDto input)
    {
        var applicantAddresses = await _applicantAddressRepository.FindByApplicantIdAsync(input.ApplicantId);
        await UpsertAddress(input, applicantAddresses, AddressType.MailingAddress, input.ApplicantId);
        await UpsertAddress(input, applicantAddresses, AddressType.PhysicalAddress, input.ApplicantId);
    }

    protected virtual async Task UpsertAddress(CreateUpdateApplicantInfoDto input, List<ApplicantAddress> applicantAddresses, AddressType applicantAddressType, Guid applicantId)
    {
        ApplicantAddress? dbAddress = applicantAddresses.Find(address => address.AddressType == applicantAddressType);

        if (dbAddress != null)
        {
            MapApplicantAddress(input, applicantAddressType, dbAddress);
            await _applicantAddressRepository.UpdateAsync(dbAddress);
        }
        else
        {
            var newAddress = new ApplicantAddress() { AddressType = applicantAddressType, ApplicantId = applicantId };
            MapApplicantAddress(input, applicantAddressType, newAddress);
            await _applicantAddressRepository.InsertAsync(newAddress);
        }
    }

    private static void MapApplicantAddress(CreateUpdateApplicantInfoDto input, AddressType applicantAddressType, ApplicantAddress address)
    {
        switch (applicantAddressType)
        {
            case AddressType.MailingAddress:
                address.AddressType = AddressType.MailingAddress;
                address.Street = input.MailingAddressStreet ?? "";
                address.Street2 = input.MailingAddressStreet2 ?? "";
                address.Unit = input.MailingAddressUnit ?? "";
                address.City = input.MailingAddressCity ?? "";
                address.Province = input.MailingAddressProvince ?? "";
                address.Postal = input.MailingAddressPostalCode ?? "";
                break;
            case AddressType.PhysicalAddress:
                address.AddressType = AddressType.PhysicalAddress;
                address.Street = input.PhysicalAddressStreet ?? "";
                address.Street2 = input.PhysicalAddressStreet2 ?? "";
                address.Unit = input.PhysicalAddressUnit ?? "";
                address.City = input.PhysicalAddressCity ?? "";
                address.Province = input.PhysicalAddressProvince ?? "";
                address.Postal = input.PhysicalAddressPostalCode ?? "";
                break;
        }
    }

    public async Task<List<GrantApplicationAssigneeDto>> GetAssigneesAsync(Guid applicationId)
    {
        var query = from userAssignment in await _applicationAssignmentRepository.GetQueryableAsync()
                    join user in await _personRepository.GetQueryableAsync() on userAssignment.AssigneeId equals user.Id
                    where userAssignment.ApplicationId == applicationId
                    select new GrantApplicationAssigneeDto
                    {
                        Id = userAssignment.Id,
                        AssigneeId = userAssignment.AssigneeId,
                        FullName = user.FullName,
                        Duty = userAssignment.Duty,
                        ApplicationId = applicationId
                    };

        return await query.ToListAsync();
    }

    public async Task<GrantApplicationAssigneeDto> GetOwnerAsync(Guid ownerId)
    {
        var owner = await _personRepository.FindAsync(ownerId);

        if (owner != null)
        {
            return new GrantApplicationAssigneeDto
            {
                Id = owner.Id,
                FullName = owner.FullName
            };
        }
        else
            return new GrantApplicationAssigneeDto();
    }

    public async Task<ApplicationFormSubmission> GetFormSubmissionByApplicationId(Guid applicationId)
    {
        ApplicationFormSubmission applicationFormSubmission = new();
        var application = await _applicationRepository.GetAsync(applicationId, false);
        if (application != null)
        {
            IQueryable<ApplicationFormSubmission> queryableFormSubmissions = await _applicationFormSubmissionRepository.GetQueryableAsync();
            if (queryableFormSubmissions != null)
            {
                var dbResult = queryableFormSubmissions
                    .FirstOrDefault(a => a.ApplicationId.Equals(applicationId));

                if (dbResult != null)
                {
                    applicationFormSubmission = dbResult;
                }
            }
        }
        return applicationFormSubmission;
    }

    public async Task UpdateApplicationStatus(Guid[] applicationIds, Guid statusId)
    {
        foreach (Guid applicationId in applicationIds)
        {
            try
            {
                var application = await _applicationRepository.GetAsync(applicationId, false);
                if (application != null)
                {
                    application.ApplicationStatusId = statusId;
                    await _applicationRepository.UpdateAsync(application);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }

    public async Task InsertAssigneeAsync(Guid applicationId, Guid assigneeId, string? duty)
    {
        try
        {
            var assignees = await GetAssigneesAsync(applicationId);
            if (assignees == null || assignees.FindIndex(a => a.AssigneeId == assigneeId) == -1)
            {
                await _applicationManager.AssignUserAsync(applicationId, assigneeId, duty);
            }
            else
            {
                await _applicationManager.UpdateAssigneeAsync(applicationId, assigneeId, duty);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    public async Task DeleteAssigneeAsync(Guid applicationId, Guid assigneeId)
    {
        try
        {
            await _applicationManager.RemoveAssigneeAsync(applicationId, assigneeId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    public async Task<IList<GrantApplicationDto>> GetApplicationListAsync(List<Guid> applicationIds)
    {
        var applications = await
            (await _applicationRepository.WithDetailsAsync())
            .OrderBy(s => s.Id)
            .Where(s => applicationIds.Contains(s.Id))
            .ToListAsync();

        return ObjectMapper.Map<List<Application>, List<GrantApplicationDto>>(applications);
    }

    public async Task<IList<GrantApplicationDto>> GetApplicationDetailsListAsync(List<Guid> applicationIds)
    {
        var query = from application in await _applicationRepository.GetQueryableAsync()
                    join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                    join applicant in await _applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                    join applicationForm in await _applicationFormRepository.GetQueryableAsync() on application.ApplicationFormId equals applicationForm.Id
                    where applicationIds.Contains(application.Id)
                    select new
                    {
                        application,
                        appStatus,
                        applicant,
                        applicationForm
                    };

        var result = query

                .OrderBy(s => s.application.Id)
                .GroupBy(s => s.application.Id)
                .AsEnumerable()
                .ToList();

        var appDtos = new List<GrantApplicationDto>();

        foreach (var grouping in result)
        {
            var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(grouping.First().application);
            appDto.Status = grouping.First().appStatus.InternalStatus;
            appDto.StatusCode = grouping.First().appStatus.StatusCode;
            appDto.Applicant = ObjectMapper.Map<Applicant, GrantApplicationApplicantDto>(grouping.First().applicant);
            appDto.ApplicationForm = ObjectMapper.Map<ApplicationForm, ApplicationFormDto>(grouping.First().applicationForm);
            appDtos.Add(appDto);
        }

        return new List<GrantApplicationDto>(appDtos);
    }

    public async Task InsertOwnerAsync(Guid applicationId, Guid? assigneeId)
    {
        try
        {
            var application = await _applicationRepository.GetAsync(applicationId, false);
            if (application != null)
            {
                application.OwnerId = assigneeId;
                await _applicationRepository.UpdateAsync(application);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    public async Task DeleteOwnerAsync(Guid applicationId)
    {
        try
        {
            var application = await _applicationRepository.GetAsync(applicationId, false);
            if (application != null)
            {
                application.OwnerId = null;
                await _applicationRepository.UpdateAsync(application);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    [HttpPut]
    public async Task UpdateAssigneesAsync(dynamic modifiedAssignees)
    {
        var dynamicObject = JsonConvert.DeserializeObject<dynamic>(modifiedAssignees);
        if (dynamicObject is IEnumerable)
        {
            Guid previousApplicationId = Guid.Empty;
            foreach (JProperty item in dynamicObject)
            {
                Guid currentApplicationId = Guid.Parse(item.Name);
                if (currentApplicationId != previousApplicationId)
                {
                    var assignees = new List<(Guid? assigneeId, string? fullName)>();

                    foreach (JToken assigneeToken in item.Value.Children())
                    {
                        string? assigneeId = assigneeToken.Value<string?>("assigneeId") ?? null;
                        string? fullName = assigneeToken.Value<string?>("fullName") ?? null;
                        assignees.Add(new(assigneeId != null ? Guid.Parse(assigneeId) : null, fullName));
                    }

                    await _applicationManager.SetAssigneesAsync(currentApplicationId, assignees);
                }

                previousApplicationId = currentApplicationId;
            }
        }
    }

    public async Task<CommentDto> CreateCommentAsync(Guid id, CreateCommentDto dto)
    {
        return ObjectMapper.Map<ApplicationComment, CommentDto>((ApplicationComment)
         await _commentsManager.CreateCommentAsync(id, dto.Comment, CommentType.ApplicationComment));
    }

    public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid id)
    {
        return ObjectMapper.Map<IReadOnlyList<ApplicationComment>, IReadOnlyList<CommentDto>>((IReadOnlyList<ApplicationComment>)
            await _commentsManager.GetCommentsAsync(id, CommentType.ApplicationComment));
    }

    public async Task<CommentDto> UpdateCommentAsync(Guid id, UpdateCommentDto dto)
    {
        try
        {
            return ObjectMapper.Map<ApplicationComment, CommentDto>((ApplicationComment)
                await _commentsManager.UpdateCommentAsync(id, dto.CommentId, dto.Comment, CommentType.ApplicationComment));

        }
        catch (EntityNotFoundException)
        {
            throw new InvalidCommentParametersException();
        }
    }

    public async Task<CommentDto> GetCommentAsync(Guid id, Guid commentId)
    {
        var comment = await _commentsManager.GetCommentAsync(id, commentId, CommentType.ApplicationComment);

        return comment == null
            ? throw new InvalidCommentParametersException()
            : ObjectMapper.Map<ApplicationComment, CommentDto>((ApplicationComment)comment);
    }

    public async Task<ApplicationStatusDto> GetApplicationStatusAsync(Guid id)
    {
        var application = await _applicationRepository.GetAsync(id, true);
        return ObjectMapper.Map<ApplicationStatus, ApplicationStatusDto>(await _applicationStatusRepository.GetAsync(application.ApplicationStatusId));
    }

    #region APPLICATION WORKFLOW
    /// <summary>
    /// Fetches the list of actions and their status context for a given application.
    /// </summary>
    /// <param name="applicationId">The application</param>
    /// <returns>A list of application actions with their state machine permitted and authorization status.</returns>
    public async Task<ListResultDto<ApplicationActionDto>> GetActions(Guid applicationId, bool includeInternal = false)
    {
        var actionList = await _applicationManager.GetActions(applicationId);
        var application = await _applicationRepository.GetAsync(applicationId, true);

        // Note: Remove internal state change actions that are side-effects of domain events
        var externalActionsList = actionList.Where(a => includeInternal || !a.IsInternal).ToList();
        var actionDtos = ObjectMapper.Map<
            List<ApplicationActionResultItem>,
            List<ApplicationActionDto>>(externalActionsList);

        // NOTE: Authorization is applied on the AppService layer and is false by default
        // TODO: Replace placeholder loop with authorization handler mapped to permissions
        // AUTHORIZATION HANDLING
        actionDtos.ForEach(async item =>
        {
            item.IsPermitted = item.IsPermitted && (await AuthorizationService.IsGrantedAsync(application, GetActionAuthorizationRequirement(item.ApplicationAction)));
            item.IsAuthorized = true;
        });

        return new ListResultDto<ApplicationActionDto>(actionDtos);
    }

    private static OperationAuthorizationRequirement GetActionAuthorizationRequirement(GrantApplicationAction triggerAction)
    {
        return new OperationAuthorizationRequirement { Name = triggerAction.ToString() };
    }

    /// <summary>
    /// Transitions the Application workflow state machine given an action.
    /// </summary>
    /// <param name="applicationId">The application</param>
    /// <param name="triggerAction">The action to be invoked on an Application</param>
    public async Task<GrantApplicationDto> TriggerAction(Guid applicationId, GrantApplicationAction triggerAction)
    {
        // AUTHORIZATION HANDLING
        var application = await _applicationRepository.GetAsync(applicationId, true);
        if (!await AuthorizationService.IsGrantedAsync(application, GetActionAuthorizationRequirement(triggerAction)))
        {
            throw new UnauthorizedAccessException();
        }

        application = await _applicationManager.TriggerAction(applicationId, triggerAction);

        await _localEventBus.PublishAsync(
            new ApplicationChangedEvent
            {
                Action = triggerAction,
                ApplicationId = applicationId
            }
        );

        return ObjectMapper.Map<Application, GrantApplicationDto>(application);
    }
    #endregion APPLICATION WORKFLOW    


    public async Task<List<GrantApplicationLiteDto>> GetAllApplicationsAsync()
    {
        
        var query = from applications in await _applicationRepository.GetQueryableAsync()
                    select new GrantApplicationLiteDto
                    {
                        Id = applications.Id,
                        ProjectName = applications.ProjectName,
                        ReferenceNo = applications.ReferenceNo
                    };

        return await query.ToListAsync();
    }
}
