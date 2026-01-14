using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets;
using Unity.GrantManager.Applicants;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Events;
using Unity.GrantManager.Flex;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Payments;
using Unity.Modules.Shared;
using Unity.Modules.Shared.Correlation;
using Unity.Payments.Enums;
using Unity.Payments.PaymentRequests;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(GrantApplicationAppService), typeof(IGrantApplicationAppService))]
public class GrantApplicationAppService(
    IApplicationManager applicationManager,
    IApplicationRepository applicationRepository,
    IApplicationStatusRepository applicationStatusRepository,    
    IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
    IApplicantRepository applicantRepository,
    IApplicationFormRepository applicationFormRepository,    
    IApplicantAgentRepository applicantAgentRepository,
    IApplicantAddressRepository applicantAddressRepository,
    IApplicantSupplierAppService applicantSupplierService,
    IPaymentRequestAppService paymentRequestService)
    : GrantManagerAppService, IGrantApplicationAppService
{
    public async Task<PagedResultDto<GrantApplicationDto>> GetListAsync(GrantApplicationListInputDto input)
    {
        // 1️⃣ Fetch applications with filters + paging in DB
        var applications = await applicationRepository.WithFullDetailsAsync(
            input.SkipCount,
            input.MaxResultCount,
            input.Sorting,
            input.SubmittedFromDate,
            input.SubmittedToDate
        );

        var applicationIds = applications.Select(a => a.Id).ToList();

        bool paymentsFeatureEnabled = await FeatureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature);

        List<PaymentDetailsDto> paymentRequests = [];

        if (paymentsFeatureEnabled && applicationIds.Count > 0)
        {
            paymentRequests = await paymentRequestService.GetListByApplicationIdsAsync(applicationIds);
        }

        // 2️⃣ Pre-aggregate payment amounts for O(1) lookup
        var paymentRequestsByApplication = paymentRequests
            .Where(pr => pr.Status == PaymentRequestStatus.Submitted)
            .GroupBy(pr => pr.CorrelationId)
            .ToDictionary(g => g.Key, g => g.Sum(pr => pr.Amount));

        // 3️⃣ Map applications to DTOs
        var appDtos = applications.Select(app =>
        {
            var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(app);

            appDto.Status = app.ApplicationStatus.InternalStatus;
            appDto.Applicant = ObjectMapper.Map<Applicant, GrantApplicationApplicantDto>(app.Applicant);
            appDto.Category = app.ApplicationForm.Category ?? string.Empty;
            appDto.Owner = BuildApplicationOwner(app.Owner);
            appDto.OrganizationName = app.Applicant?.OrgName ?? string.Empty;
            appDto.ApplicationTag = ObjectMapper.Map<List<ApplicationTags>, List<ApplicationTagsDto>>(app.ApplicationTags?.ToList() ?? []);
            appDto.NonRegOrgName = app.Applicant?.NonRegOrgName ?? string.Empty;
            appDto.OrganizationType = app.Applicant?.OrganizationType ?? string.Empty;
            appDto.Assignees = BuildApplicationAssignees(app.ApplicationAssignments);
            appDto.SubStatusDisplayValue = MapSubstatusDisplayValue(appDto.SubStatus);
            appDto.DeclineRational = MapDeclineRationalDisplayValue(appDto.DeclineRational);
            appDto.ContactFullName = app.ApplicantAgent?.Name;
            appDto.ContactEmail = app.ApplicantAgent?.Email;
            appDto.ContactTitle = app.ApplicantAgent?.Title;
            appDto.ContactBusinessPhone = app.ApplicantAgent?.Phone;
            appDto.ContactCellPhone = app.ApplicantAgent?.Phone2;

            if (paymentsFeatureEnabled && paymentRequestsByApplication.Count > 0)
            {
                appDto.PaymentInfo = new PaymentInfoDto
                {
                    ApprovedAmount = app.ApprovedAmount,
                    TotalPaid = paymentRequestsByApplication.GetValueOrDefault(app.Id)
                };
            }

            return appDto;

        }).ToList();

        // 4️⃣ Get total count using same filters
        var totalCount = await applicationRepository.GetCountAsync(
            input.SubmittedFromDate,
            input.SubmittedToDate
        );

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
        // NOTE: Changes to this method can impact Email Notification Templates
        var application = await applicationRepository.GetWithFullDetailsByIdAsync(id);

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
            appDto.BusinessNumber = application.Applicant.BusinessNumber;
            appDto.NonRegOrgName = application.Applicant.NonRegOrgName;
            appDto.Sector = application.Applicant.Sector;
            appDto.OrganizationType = application.Applicant.OrganizationType;
            appDto.SubSector = application.Applicant.SubSector;
            appDto.SectorSubSectorIndustryDesc = application.Applicant.SectorSubSectorIndustryDesc;
        }

        return appDto;
    }

    public async Task<ApplicationForm?> GetApplicationFormAsync(Guid applicationFormId)
    {
        return await (await applicationFormRepository.GetQueryableAsync()).FirstOrDefaultAsync(s => s.Id == applicationFormId);
    }  

    [Authorize(UnitySelector.Review.AssessmentResults.Update.Default)]
    public async Task<GrantApplicationDto> UpdateAssessmentResultsAsync(Guid id, CreateUpdateAssessmentResultsDto input)
    {
        var application = await applicationRepository.GetAsync(id);

        await SanitizeApprovalZoneInputs(input, application);
        await SanitizeAssessmentResultsZoneInputs(input, application);

        application.ValidateAndSetDueDate(input.DueDate);
        application.UpdateAlwaysChangeableFields(
            input.Notes,
            input.SubStatus,
            input.LikelihoodOfFunding,
            input.TotalProjectBudget,
            input.NotificationDate,
            input.RiskRanking
        );

        if (application.IsInFinalDecisionState())
        {
            if (await AuthorizationService.IsGrantedAsync(UnitySelector.Review.Approval.Update.UpdateFinalStateFields))
            {
                application.UpdateApprovalFieldsRequiringPostEditPermission(input.ApprovedAmount);
            }

            if (await AuthorizationService.IsGrantedAsync(UnitySelector.Review.AssessmentResults.Update.UpdateFinalStateFields))
            {
                application.UpdateAssessmentResultFieldsRequiringPostEditPermission(input.RequestedAmount, input.TotalScore);
            }
        }
        else
        {
            if (await CurrentUserCanUpdateAssessmentFieldsAsync())
            {
                application.ValidateAndSetFinalDecisionDate(input.FinalDecisionDate);
                application.UpdateApprovalFieldsRequiringPostEditPermission(input.ApprovedAmount);
                application.UpdateAssessmentResultFieldsRequiringPostEditPermission(input.RequestedAmount, input.TotalScore);
                application.UpdateFieldsOnlyForPreFinalDecision(
                    input.DueDiligenceStatus,
                    input.RecommendedAmount,
                    input.DeclineRational
                );

                application.UpdateAssessmentResultStatus(input.AssessmentResultStatus);
            }
        }

        // Handle custom fields for assessment info
        if (HasCustomFields(input))
        {
            await PublishCustomFieldsAsync(application.Id, input);
        }

        await applicationRepository.UpdateAsync(application);
        return ObjectMapper.Map<Application, GrantApplicationDto>(application);
    }

    private async Task PublishCustomFieldsAsync(Guid applicationId, UpdateProjectInfoDto dto)
    {
        if (dto.WorksheetIds?.Count > 0)
        {
            foreach (var worksheetId in dto.WorksheetIds)
            {
                var worksheetFields = ExtractCustomFieldsForWorksheet(dto.CustomFields, worksheetId);
                if (worksheetFields.Count > 0)
                {
                    await PublishCustomFieldUpdatesAsync(applicationId, FlexConsts.ProjectInfoUiAnchor, new CustomDataFieldDto
                    {
                        WorksheetId = worksheetId,
                        CustomFields = worksheetFields,
                        CorrelationId = dto.CorrelationId
                    });
                }
            }
        }
        else if (dto.WorksheetId != Guid.Empty)
        {
            await PublishCustomFieldUpdatesAsync(applicationId, FlexConsts.ProjectInfoUiAnchor, dto);
        }
    }

    private async Task PublishCustomFieldsAsync(Guid applicationId, CreateUpdateAssessmentResultsDto dto)
    {
        if (dto.WorksheetIds?.Count > 0)
        {
            foreach (var worksheetId in dto.WorksheetIds)
            {
                var worksheetFields = ExtractCustomFieldsForWorksheet(dto.CustomFields, worksheetId);
                if (worksheetFields.Count > 0)
                {
                    await PublishCustomFieldUpdatesAsync(applicationId, FlexConsts.AssessmentInfoUiAnchor, new CustomDataFieldDto
                    {
                        WorksheetId = worksheetId,
                        CustomFields = worksheetFields,
                        CorrelationId = dto.CorrelationId
                    });
                }
            }
        }
        else if (dto.WorksheetId != Guid.Empty)
        {
            await PublishCustomFieldUpdatesAsync(applicationId, FlexConsts.AssessmentInfoUiAnchor, dto);
        }
    }

    private async Task SanitizeApprovalZoneInputs(CreateUpdateAssessmentResultsDto input, Application application)
    {
        // Approval Zone Fields - Disabled Inputs
        input.ApprovedAmount ??= application.ApprovedAmount;

        // Sanitize if zone is disabled
        if (!await ZoneChecker.IsEnabledAsync(UnitySelector.Review.Approval.Default, application.ApplicationFormId))
        {
            input.SubStatus ??= application.SubStatus;
            input.FinalDecisionDate ??= application.FinalDecisionDate;
            input.Notes ??= application.Notes;
        }
        else
        {
            // Sanitize if zone is enabled but fields are disabled
            if (application.IsInFinalDecisionState())
            {
                input.FinalDecisionDate ??= application.FinalDecisionDate;
            }
        }
    }

    private async Task SanitizeAssessmentResultsZoneInputs(CreateUpdateAssessmentResultsDto input, Application application)
    {
        // Approval Zone Fields - Disabled Inputs
        input.RequestedAmount ??= application.RequestedAmount;
        input.TotalProjectBudget ??= application.TotalProjectBudget;
        input.RecommendedAmount ??= application.RecommendedAmount;
        input.TotalScore ??= application.TotalScore;

        // Sanitize if zone is disabled
        if (!await ZoneChecker.IsEnabledAsync(UnitySelector.Review.AssessmentResults.Default, application.ApplicationFormId))
        {
            input.LikelihoodOfFunding ??= application.LikelihoodOfFunding;
            input.RiskRanking ??= application.RiskRanking;
            input.DueDiligenceStatus ??= application.DueDiligenceStatus;
            input.AssessmentResultStatus ??= application.AssessmentResultStatus;
            input.DeclineRational ??= application.DeclineRational;

            input.NotificationDate ??= application.NotificationDate;
            input.DueDate ??= application.DueDate;
        }
        else
        {
            // Sanitize if zone is enabled but fields are disabled
            if (application.IsInFinalDecisionState())
            {
                input.LikelihoodOfFunding ??= application.LikelihoodOfFunding;
                input.RiskRanking ??= application.RiskRanking;
                input.DueDiligenceStatus ??= application.DueDiligenceStatus;
                input.AssessmentResultStatus ??= application.AssessmentResultStatus;
                input.DeclineRational ??= application.DeclineRational;
            }
        }
    }

    private async Task<bool> CurrentUserCanUpdateAssessmentFieldsAsync()
    {
        return await AuthorizationService.IsGrantedAsync(UnitySelector.Review.AssessmentResults.Update.Default);
    }

    [Authorize(UnitySelector.Project.UpdatePolicy)]
    public async Task<GrantApplicationDto> UpdateProjectInfoAsync(Guid id, CreateUpdateProjectInfoDto input)
    {
        // Check if the user has the required permissions to update Project Info for either fieldset zone
        var hasSummaryPermission = await AuthorizationService.IsGrantedAsync(UnitySelector.Project.Summary.Update.Default);
        var hasLocationPermission = await AuthorizationService.IsGrantedAsync(UnitySelector.Project.Location.Update.Default);

        if (!hasSummaryPermission || !hasLocationPermission)
        {
            throw new AbpAuthorizationException("The user doesn't have the required permissions to update Project Info.");
        }

        var application = await applicationRepository.GetAsync(id);

        var hasSummaryZone = await ZoneChecker.IsEnabledAsync(UnitySelector.Project.Summary.Default, application.ApplicationFormId);
        var hasLocationZone = await ZoneChecker.IsEnabledAsync(UnitySelector.Project.Location.Default, application.ApplicationFormId);

        if (!hasSummaryZone || !hasLocationZone)
        {
            throw new BusinessException("The Project Info zones are not enabled for this application form.");
        }

        SanitizeProjectInfoDisabledInputs(input, application);

        var percentageTotalProjectBudget = (input.TotalProjectBudget == 0 || input.TotalProjectBudget == null) ? 0 : decimal.Multiply(decimal.Divide(input.RequestedAmount ?? 0, input.TotalProjectBudget ?? 0), 100).To<double>();

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
        application.Place = input.Place;

        // Handle custom fields for project info
        if (input.CustomFields != null && HasValue(input.CustomFields) && input.CorrelationId != Guid.Empty)
        {
            // Handle multiple worksheets
            if (input.WorksheetIds?.Count > 0)
            {
                foreach (var worksheetId in input.WorksheetIds)
                {
                    var worksheetCustomFields = ExtractCustomFieldsForWorksheet(input.CustomFields, worksheetId);
                    if (worksheetCustomFields.Count > 0)
                    {
                        var worksheetData = new CustomDataFieldDto
                        {
                            WorksheetId = worksheetId,
                            CustomFields = worksheetCustomFields,
                            CorrelationId = input.CorrelationId
                        };
                        await PublishCustomFieldUpdatesAsync(application.Id, FlexConsts.ProjectInfoUiAnchor, worksheetData);
                    }
                }
            }
            // Fallback for single worksheet (backward compatibility)
            else if (input.WorksheetId != Guid.Empty)
            {
                await PublishCustomFieldUpdatesAsync(application.Id, FlexConsts.ProjectInfoUiAnchor, input);
            }
        }

        await applicationRepository.UpdateAsync(application);

        return ObjectMapper.Map<Application, GrantApplicationDto>(application);
    }

    private static void SanitizeProjectInfoDisabledInputs(CreateUpdateProjectInfoDto input, Application application)
    {
        // Cater for disabled fields that are not serialized with post - fall back to the previous value, these should be 0 from the API call
        input.TotalProjectBudget ??= application.TotalProjectBudget;
        input.RequestedAmount ??= application.RequestedAmount;
        input.ProjectFundingTotal ??= application.ProjectFundingTotal;
    }

    [Authorize(UnitySelector.Project.UpdatePolicy)]
    public async Task<GrantApplicationDto> UpdatePartialProjectInfoAsync(Guid id, PartialUpdateDto<UpdateProjectInfoDto> input)
    {
        var application = await applicationRepository.GetAsync(id)
            ?? throw new EntityNotFoundException($"Application with ID {id} not found.");

        // Map incoming values
        ObjectMapper.Map<UpdateProjectInfoDto, Application>(input.Data, application);

        // Explicitly clear properties that were set to null in the update
        ApplyExplicitNulls(input, application);

        // Update derived values
        application.UpdatePercentageTotalProjectBudget();

        // Handle custom fields
        if (HasCustomFields(input.Data))
        {
            await PublishCustomFieldsAsync(application.Id, input.Data);
        }

        await applicationRepository.UpdateAsync(application);
        return ObjectMapper.Map<Application, GrantApplicationDto>(application);
    }

    private static void ApplyExplicitNulls(PartialUpdateDto<UpdateProjectInfoDto> input, Application application)
    {
        var dtoProps = typeof(UpdateProjectInfoDto)
            .GetProperties()
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        var appProps = typeof(Application)
            .GetProperties()
            .ToDictionary(p => p.Name, p => p);

        foreach (var field in input.ModifiedFields)
        {
            if (dtoProps.TryGetValue(field, out var dtoProp) &&
                dtoProp.GetValue(input.Data) == null &&
                appProps.TryGetValue(dtoProp.Name, out var appProp) &&
                appProp.CanWrite)
            {
                appProp.SetValue(application, GetDefaultValue(appProp.PropertyType));
            }
        }
    }

    private static bool HasCustomFields<T>(T dto)
        where T : class
    {
        var correlationIdProp = typeof(T).GetProperty("CorrelationId");
        var customFieldsProp = typeof(T).GetProperty("CustomFields");

        if (correlationIdProp == null || customFieldsProp == null)
            return false;

        var correlationId = (Guid)(correlationIdProp.GetValue(dto) ?? Guid.Empty);
        if (correlationId == Guid.Empty)
            return false;

        if (customFieldsProp.GetValue(dto) is JsonElement el)
            return el.ValueKind != JsonValueKind.Undefined && HasValue(el);

        return false;
    }


    private static object? GetDefaultValue(Type type) =>
        type.IsValueType && Nullable.GetUnderlyingType(type) == null
            ? Activator.CreateInstance(type)
            : null;

    private static bool HasValue(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().Any(),
            JsonValueKind.Array => element.EnumerateArray().Any(),
            JsonValueKind.String => !string.IsNullOrWhiteSpace(element.GetString()),
            JsonValueKind.Number => true,
            JsonValueKind.True => true,
            JsonValueKind.False => true,
            _ => false
        };

    // Overload for object/dynamic input (safer)
    private static bool HasValue(object? element) => element switch
    {
        null => false,
        JsonElement el => HasValue(el),
        string s => !string.IsNullOrWhiteSpace(s),
        IEnumerable<object> e => e.Any(),
        _ => true
    };


    public async Task<GrantApplicationDto> UpdateFundingAgreementInfoAsync(Guid id, CreateUpdateFundingAgreementInfoDto input)
    {
        var application = await applicationRepository.GetAsync(id);

        // Update simple fields
        if (application.ContractNumber != input.ContractNumber ||
            application.ContractExecutionDate != input.ContractExecutionDate)
        {
            application.ContractNumber = input.ContractNumber;
            application.ContractExecutionDate = input.ContractExecutionDate;
        }

        // Handle custom fields
        if (HasValue(input.CustomFields) && input.CorrelationId != Guid.Empty)
        {
            if (input.WorksheetIds?.Count > 0)
            {
                foreach (var worksheetId in input.WorksheetIds)
                {
                    var worksheetCustomFields = ExtractCustomFieldsForWorksheet(input.CustomFields, worksheetId);
                    if (worksheetCustomFields.Count > 0)
                    {
                        var worksheetData = new CustomDataFieldDto
                        {
                            WorksheetId = worksheetId,
                            CustomFields = worksheetCustomFields,
                            CorrelationId = input.CorrelationId
                        };

                        await PublishCustomFieldUpdatesAsync(
                            application.Id,
                            FlexConsts.FundingAgreementInfoUiAnchor,
                            worksheetData
                        );
                    }
                }
            }
            else if (input.WorksheetId != Guid.Empty) // backward compatibility
            {
                await PublishCustomFieldUpdatesAsync(
                    application.Id,
                    FlexConsts.FundingAgreementInfoUiAnchor,
                    input
                );
            }
        }

        await applicationRepository.UpdateAsync(application);
        return ObjectMapper.Map<Application, GrantApplicationDto>(application);
    }

    /// <summary>
    /// Update the supplier number for the applicant associated with the application.
    /// </summary>
    [Authorize(UnitySelector.Payment.Supplier.Update)]
    public async Task UpdateSupplierNumberAsync(Guid applicationId, string? supplierNumber)
    {
        // Could be moved to payments module but dependency on ApplicationId
        // Integrate with payments module to update / insert supplier
        var application = await applicationRepository.GetAsync(applicationId);
        if (await FeatureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature) && application != null)
        {
            var pendingPayments = await paymentRequestService.GetPaymentPendingListByCorrelationIdAsync(applicationId);
            if (pendingPayments != null && pendingPayments.Count > 0)
            {
                throw new UserFriendlyException("There are outstanding payment requests with the current Supplier. Please decline or approve the outstanding payments before changing the Supplier Number");
            }
            // Handle both clearing (null/empty) and updating supplier number
            if (string.IsNullOrWhiteSpace(supplierNumber))
            {
                await applicantSupplierService.ClearApplicantSupplierAsync(application.ApplicantId);
            }
            else
            {
                // Update supplier number
                await applicantSupplierService.UpdateApplicantSupplierNumberAsync(application.ApplicantId, supplierNumber, applicationId);
            }
        }
    }

    protected internal async Task<ApplicantAgent?> CreateOrUpdateApplicantAgentAsync(Application application, ContactInfoDto? input)
    {
        if (input == null
            || !await AuthorizationService.IsGrantedAnyAsync(UnitySelector.Applicant.Contact.Create, UnitySelector.Applicant.Contact.Update))
        {
            return null;
        }

        var applicantAgent = await applicantAgentRepository
            .FirstOrDefaultAsync(a => a.ApplicantId == application.ApplicantId)
            ?? new ApplicantAgent
            {
                ApplicantId = application.ApplicantId,
                ApplicationId = application.Id
            };

        applicantAgent.Name = input.Name ?? string.Empty;
        applicantAgent.Phone = input.Phone ?? string.Empty;
        applicantAgent.Phone2 = input.Phone2 ?? string.Empty;
        applicantAgent.Email = input.Email ?? string.Empty;
        applicantAgent.Title = input.Title ?? string.Empty;

        if (applicantAgent.Id == Guid.Empty)
        {
            return await applicantAgentRepository.InsertAsync(applicantAgent);
        }

        return await applicantAgentRepository.UpdateAsync(applicantAgent);
    }

    [Obsolete("Use ApplicationApplicantAppService.UpdatePartialApplicantInfoAsync instead.")]
    [Authorize(UnitySelector.Applicant.UpdatePolicy)]
    public async Task<GrantApplicationDto> UpdateProjectApplicantInfoAsync(Guid id, CreateUpdateApplicantInfoDto input)
    {
        var application = await applicationRepository.GetAsync(id);

        var applicant = await applicantRepository
            .FirstOrDefaultAsync(a => a.Id == application.ApplicantId) ?? throw new EntityNotFoundException();

        applicant.OrganizationType = input.OrganizationType ?? "";
        applicant.OrgName = input.OrgName ?? "";
        applicant.OrgNumber = input.OrgNumber ?? "";
        applicant.OrgStatus = input.OrgStatus ?? "";
        applicant.OrganizationSize = input.OrganizationSize ?? "";
        applicant.Sector = input.Sector ?? "";
        applicant.SubSector = input.SubSector ?? "";
        applicant.SectorSubSectorIndustryDesc = input.SectorSubSectorIndustryDesc ?? "";
        applicant.IndigenousOrgInd = input.IndigenousOrgInd ?? "";
        applicant.UnityApplicantId = input.UnityApplicantId ?? "";
        applicant.FiscalDay = input.FiscalDay;
        applicant.FiscalMonth = input.FiscalMonth ?? "";
        applicant.NonRegOrgName = input.NonRegOrgName ?? "";

        _ = await applicantRepository.UpdateAsync(applicant);

        var applicantAgent = await applicantAgentRepository.FirstOrDefaultAsync(agent => agent.ApplicantId == application.ApplicantId);
        if (applicantAgent == null)
        {
            applicantAgent = await applicantAgentRepository.InsertAsync(new ApplicantAgent
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
            applicantAgent = await applicantAgentRepository.UpdateAsync(applicantAgent);
        }

        await UpdateApplicantAddresses(application.Id, input);

        application.SigningAuthorityFullName = input.SigningAuthorityFullName ?? "";
        application.SigningAuthorityTitle = input.SigningAuthorityTitle ?? "";
        application.SigningAuthorityEmail = input.SigningAuthorityEmail ?? "";
        application.SigningAuthorityBusinessPhone = input.SigningAuthorityBusinessPhone ?? "";
        application.SigningAuthorityCellPhone = input.SigningAuthorityCellPhone ?? "";

        await PublishCustomFieldUpdatesAsync(application.Id, FlexConsts.ApplicantInfoUiAnchor, input);

        await applicationRepository.UpdateAsync(application);

        var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(application);

        appDto.ContactFullName = applicantAgent.Name;
        appDto.ContactEmail = applicantAgent.Email;
        appDto.ContactTitle = applicantAgent.Title;
        appDto.ContactBusinessPhone = applicantAgent.Phone;
        appDto.ContactCellPhone = applicantAgent.Phone2;

        return appDto;
    }

    [Authorize(UnitySelector.Applicant.UpdatePolicy)]
    public async Task UpdateMergedApplicantAsync(Guid applicationId, CreateUpdateApplicantInfoDto input)
    {
        var application = await applicationRepository.GetAsync(applicationId);

        var applicant = await applicantRepository
            .FirstOrDefaultAsync(a => a.Id == application.ApplicantId) ?? throw new EntityNotFoundException();

        applicant.OrganizationType = input.OrganizationType ?? "";
        applicant.OrgName = input.OrgName ?? "";
        applicant.OrgNumber = input.OrgNumber ?? "";
        applicant.OrgStatus = input.OrgStatus ?? "";
        applicant.OrganizationSize = input.OrganizationSize ?? "";
        applicant.Sector = input.Sector ?? "";
        applicant.SubSector = input.SubSector ?? "";
        applicant.SectorSubSectorIndustryDesc = input.SectorSubSectorIndustryDesc ?? "";
        applicant.IndigenousOrgInd = input.IndigenousOrgInd ?? "";
        applicant.UnityApplicantId = input.UnityApplicantId ?? "";
        applicant.FiscalDay = input.FiscalDay;
        applicant.FiscalMonth = input.FiscalMonth ?? "";
        applicant.NonRegOrgName = input.NonRegOrgName ?? "";
        applicant.ApplicantName = input.ApplicantName ?? "";

        _ = await applicantRepository.UpdateAsync(applicant);
    }

    protected virtual async Task PublishCustomFieldUpdatesAsync(Guid applicationId,
        string uiAnchor,
        CustomDataFieldDto input)
    {
        if (await FeatureChecker.IsEnabledAsync("Unity.Flex"))
        {
            if (input.CorrelationId != Guid.Empty)
            {
                await LocalEventBus.PublishAsync(new PersistWorksheetIntanceValuesEto()
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

    protected virtual async Task UpdateApplicantAddresses(Guid applicationId, CreateUpdateApplicantInfoDto input)
    {
        List<ApplicantAddress> applicantAddresses = await applicantAddressRepository.FindByApplicantIdAndApplicationIdAsync(input.ApplicantId, applicationId);
        if (applicantAddresses != null)
        {
            await UpsertAddress(input, applicantAddresses, AddressType.MailingAddress, input.ApplicantId, applicationId);
            await UpsertAddress(input, applicantAddresses, AddressType.PhysicalAddress, input.ApplicantId, applicationId);
        }
    }

    protected virtual async Task UpsertAddress(CreateUpdateApplicantInfoDto input, List<ApplicantAddress> applicantAddresses, AddressType applicantAddressType, Guid applicantId, Guid applicationId)
    {
        ApplicantAddress? dbAddress = applicantAddresses.Find(address => address.AddressType == applicantAddressType && address.ApplicationId == applicationId);

        if (dbAddress != null)
        {
            MapApplicantAddress(input, applicantAddressType, dbAddress);
            await applicantAddressRepository.UpdateAsync(dbAddress);
        }
        else
        {
            var newAddress = new ApplicantAddress() { AddressType = applicantAddressType, ApplicantId = applicantId, ApplicationId = applicationId };
            MapApplicantAddress(input, applicantAddressType, newAddress);
            await applicantAddressRepository.InsertAsync(newAddress);
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

    public async Task<ApplicationFormSubmission> GetFormSubmissionByApplicationId(Guid applicationId)
    {
        ApplicationFormSubmission applicationFormSubmission = new();
        var application = await applicationRepository.GetAsync(applicationId, false);
        if (application != null)
        {
            IQueryable<ApplicationFormSubmission> queryableFormSubmissions = await applicationFormSubmissionRepository.GetQueryableAsync();
            if (queryableFormSubmissions != null)
            {
                var dbResult = await queryableFormSubmissions
                    .FirstOrDefaultAsync(a => a.ApplicationId.Equals(applicationId));

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
                var application = await applicationRepository.GetAsync(applicationId, false);
                if (application != null)
                {
                    application.ApplicationStatusId = statusId;
                    await applicationRepository.UpdateAsync(application);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }   

    public async Task<List<GrantApplicationDto>> GetApplicationListAsync(List<Guid> applicationIds)
    {
        var applications = await
            (await applicationRepository.WithDetailsAsync())
            .OrderBy(s => s.Id)
            .Where(s => applicationIds.Contains(s.Id))
            .ToListAsync();

        return ObjectMapper.Map<List<Application>, List<GrantApplicationDto>>(applications);
    }

    public async Task<IList<GrantApplicationDto>> GetApplicationDetailsListAsync(List<Guid> applicationIds)
    {
        var query = from application in await applicationRepository.GetQueryableAsync()
                    join appStatus in await applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                    join applicant in await applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                    join applicationForm in await applicationFormRepository.GetQueryableAsync() on application.ApplicationFormId equals applicationForm.Id
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

        return [.. appDtos];
    }

    public async Task InsertOwnerAsync(Guid applicationId, Guid? assigneeId)
    {
        try
        {
            var application = await applicationRepository.GetAsync(applicationId, false);
            if (application != null)
            {
                application.OwnerId = assigneeId;
                await applicationRepository.UpdateAsync(application);
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
            var application = await applicationRepository.GetAsync(applicationId, false);
            if (application != null)
            {
                application.OwnerId = null;
                await applicationRepository.UpdateAsync(application);
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

                    await applicationManager.SetAssigneesAsync(currentApplicationId, assignees);
                }

                previousApplicationId = currentApplicationId;
            }
        }
    }

    public async Task<ApplicationStatusDto> GetApplicationStatusAsync(Guid id)
    {
        var application = await applicationRepository.GetAsync(id, true);
        return ObjectMapper.Map<ApplicationStatus, ApplicationStatusDto>(await applicationStatusRepository.GetAsync(application.ApplicationStatusId));
    }

    public async Task<Guid?> GetAccountCodingIdFromFormIdAsync(Guid formId)
    {
        ApplicationForm? form = await applicationFormRepository.GetAsync(formId, true);
        if (form == null)
        {
            return null;
        }

        return form.AccountCodingId;
    }

    #region APPLICATION WORKFLOW
    /// <summary>
    /// Fetches the list of actions and their status context for a given application.
    /// </summary>
    /// <param name="applicationId">The application</param>
    /// <returns>A list of application actions with their state machine permitted and authorization status.</returns>
    public async Task<ListResultDto<ApplicationActionDto>> GetActions(Guid applicationId, bool includeInternal = false)
    {
        var actionList = await applicationManager.GetActions(applicationId);
        var application = await applicationRepository.GetAsync(applicationId, true);

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
        var application = await applicationRepository.GetAsync(applicationId, true);
        if (!await AuthorizationService.IsGrantedAsync(application, GetActionAuthorizationRequirement(triggerAction)))
        {
            throw new UnauthorizedAccessException();
        }

        application = await applicationManager.TriggerAction(applicationId, triggerAction);

        await LocalEventBus.PublishAsync(
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

        var applicationsQuery = await applicationRepository.GetQueryableAsync();
        var applicantsQuery = await applicantRepository.GetQueryableAsync();

        var query = from applications in applicationsQuery
                    join applicant in applicantsQuery on applications.ApplicantId equals applicant.Id into applicantGroup
                    from applicant in applicantGroup.DefaultIfEmpty()
                    select new GrantApplicationLiteDto
                    {
                        Id = applications.Id,
                        ProjectName = applications.ProjectName,
                        ReferenceNo = applications.ReferenceNo,
                        ApplicantName = applicant != null ? (applicant.ApplicantName ?? GrantManagerConsts.UnknownValue) : GrantManagerConsts.UnknownValue
                    };

        return await query.ToListAsync();
    }

    private static Dictionary<string, object> ExtractCustomFieldsForWorksheet(dynamic customFields, Guid worksheetId)
    {
        var result = new Dictionary<string, object>();
        var worksheetSuffix = $".{worksheetId}";

        if (customFields is JsonElement jsonElement)
        {
            result = jsonElement.EnumerateObject()
                .Where(property => property.Name.EndsWith(worksheetSuffix))
                .ToDictionary(
                    property => property.Name[..^worksheetSuffix.Length],
                    property => property.Value.ValueKind == JsonValueKind.String ? (object)property.Value.GetString()! : string.Empty
                );
        }

        return result;
    }

    public async Task<string> DismissAIIssueAsync(Guid applicationId, string issueId)
    {
        var application = await applicationRepository.GetAsync(applicationId);

        if (string.IsNullOrEmpty(application.AIAnalysis))
        {
            throw new UserFriendlyException("No AI analysis available for this application.");
        }

        try
        {
            var updatedAnalysis = ModifyDismissedItems(application.AIAnalysis, issueId, isDismiss: true);
            application.AIAnalysis = updatedAnalysis;
            await applicationRepository.UpdateAsync(application);
            return updatedAnalysis;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error dismissing AI issue {IssueId} for application {ApplicationId}", issueId, applicationId);
            throw new UserFriendlyException("Failed to dismiss the AI issue. Please try again.");
        }
    }

    public async Task<string> RestoreAIIssueAsync(Guid applicationId, string issueId)
    {
        var application = await applicationRepository.GetAsync(applicationId);

        if (string.IsNullOrEmpty(application.AIAnalysis))
        {
            throw new UserFriendlyException("No AI analysis available for this application.");
        }

        try
        {
            var updatedAnalysis = ModifyDismissedItems(application.AIAnalysis, issueId, isDismiss: false);
            application.AIAnalysis = updatedAnalysis;
            await applicationRepository.UpdateAsync(application);
            return updatedAnalysis;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error restoring AI issue {IssueId} for application {ApplicationId}", issueId, applicationId);
            throw new UserFriendlyException("Failed to restore the AI issue. Please try again.");
        }
    }

    private static string ModifyDismissedItems(string analysisJson, string issueId, bool isDismiss)
    {
        using var jsonDoc = JsonDocument.Parse(analysisJson);
        using var memoryStream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            var dismissedItems = new HashSet<string>();
            if (jsonDoc.RootElement.TryGetProperty("dismissed_items", out var dismissedArray))
            {
                foreach (var item in dismissedArray.EnumerateArray())
                {
                    var itemValue = item.GetString();
                    if (!string.IsNullOrWhiteSpace(itemValue))
                    {
                        dismissedItems.Add(itemValue);
                    }
                }
            }

            // Modify the dismissed items set
            if (isDismiss && !string.IsNullOrWhiteSpace(issueId))
            {
                dismissedItems.Add(issueId);
            }
            else if (!isDismiss)
            {
                dismissedItems.Remove(issueId);
            }

            // Write all properties
            foreach (var property in jsonDoc.RootElement.EnumerateObject())
            {
                if (property.Name != "dismissed_items")
                {
                    property.WriteTo(writer);
                }
            }

            // Write updated dismissed_items array
            writer.WritePropertyName("dismissed_items");
            writer.WriteStartArray();
            foreach (var id in dismissedItems)
            {
                writer.WriteStringValue(id);
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
    }
}
