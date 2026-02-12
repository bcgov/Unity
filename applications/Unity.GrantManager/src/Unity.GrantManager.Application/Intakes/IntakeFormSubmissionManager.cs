using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Intakes.Events;
using Unity.GrantManager.Intakes.Mapping;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Intakes
{
    [RemoteService(IsEnabled = false)]
    public class IntakeFormSubmissionManager(IUnitOfWorkManager _unitOfWorkManager,
                                             IApplicantAppService applicantService,
                                             IApplicationRepository _applicationRepository,
                                             IApplicationStatusRepository _applicationStatusRepository,
                                             IApplicationFormSubmissionRepository _applicationFormSubmissionRepository,
                                             IIntakeFormSubmissionMapper _intakeFormSubmissionMapper,
                                             IApplicationFormVersionRepository _applicationFormVersionRepository,
                                             CustomFieldsIntakeSubmissionMapper _customFieldsIntakeSubmissionMapper,
                                             ILocalEventBus localEventBus,
                                             ISequenceRepository _sequenceRepository) : DomainService, IIntakeFormSubmissionManager
    {
        public async Task<string?> GetApplicationFormVersionMapping(string chefsFormVersionId)
        {
            var applicationFormVersion = (await _applicationFormVersionRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsFormVersionGuid == chefsFormVersionId)
                    .FirstOrDefault();

            string? formVersionSubmissionHeaderMapping = null;

            if (applicationFormVersion != null)
            {
                formVersionSubmissionHeaderMapping = applicationFormVersion.SubmissionHeaderMapping;
            }

            return formVersionSubmissionHeaderMapping;
        }

        public async Task<Guid> ProcessFormSubmissionAsync(ApplicationForm applicationForm, dynamic formSubmission)
        {
            string? formVersionId = formSubmission.submission.formVersionId;
            string? formVersionSubmissionHeaderMapping = await GetApplicationFormVersionMapping(formVersionId);
            IntakeMapping intakeMap = _intakeFormSubmissionMapper.MapFormSubmissionFields(applicationForm, formSubmission, formVersionSubmissionHeaderMapping);
            intakeMap.SubmissionId = formSubmission.submission.id;
            intakeMap.SubmissionDate = formSubmission.submission.updatedAt;
            intakeMap.ConfirmationId = formSubmission.submission.confirmationId;
            using var uow = _unitOfWorkManager.Begin();
            var application = await CreateNewApplicationAsync(intakeMap, applicationForm);
            await _intakeFormSubmissionMapper.SaveChefsFiles(formSubmission, application.Id);

            JObject submission = JObject.Parse(formSubmission.ToString());
            JToken? dataNode = submission.SelectToken("submission");

            var newSubmission = new ApplicationFormSubmission
            {
                OidcSub = IntakeSubmissionHelper.ExtractOidcSub(formSubmission.submission),
                ApplicantId = application.ApplicantId,
                ApplicationFormId = applicationForm.Id,
                ChefsSubmissionGuid = intakeMap.SubmissionId ?? $"{Guid.Empty}",
                ApplicationId = application.Id,
                Submission = dataNode?.ToString() ?? string.Empty                
            };

            _ = await _applicationFormSubmissionRepository.InsertAsync(newSubmission);

            ApplicationFormVersion? localFormVersion = await _applicationFormVersionRepository.GetByChefsFormVersionAsync(Guid.Parse(formVersionId));

            await _customFieldsIntakeSubmissionMapper.MapAndPersistCustomFields(application.Id,
                localFormVersion?.Id ?? Guid.Empty,
                formSubmission,
            formVersionSubmissionHeaderMapping);

            newSubmission.ApplicationFormVersionId = localFormVersion?.Id;
            newSubmission.FormVersionId = string.IsNullOrWhiteSpace(localFormVersion?.ChefsFormVersionGuid) ? null : Guid.Parse(localFormVersion.ChefsFormVersionGuid);

            // Extend any further processing of the application here through local event bus and handlers
            await localEventBus.PublishAsync(new ApplicationProcessEvent
            {
                Application = application,
                FormVersion = localFormVersion,
                ApplicationFormSubmission = newSubmission,
                RawSubmission = formSubmission
            });

            await uow.SaveChangesAsync();

            return application.Id;
        }

        #region Unity Application ID Generation

        private async Task<string?> GenerateUnityApplicationIdAsync(
            ApplicationForm applicationForm,
            string? referenceNo)
        {
            // Return null if no configuration
            if (string.IsNullOrWhiteSpace(applicationForm.Prefix) || 
                applicationForm.SuffixType == null)
            {
                return null;
            }
            
            try
            {
                return applicationForm.SuffixType switch
                {
                    SuffixConfigType.SequentialNumber => 
                        await GenerateSequentialIdAsync(applicationForm.Prefix),
                    SuffixConfigType.SubmissionNumber => 
                        GenerateSubmissionId(applicationForm.Prefix, referenceNo),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                // Log error but don't throw - graceful degradation
                Logger.LogError(ex, 
                    "Failed to generate Unity Application ID. " +
                    "Prefix: {Prefix}, SuffixType: {SuffixType}",
                    applicationForm.Prefix, applicationForm.SuffixType);
                return null;
            }
        }
        
        private async Task<string> GenerateSequentialIdAsync(string prefix)
        {
            // Get next sequence number from repository
            var sequenceNumber = await _sequenceRepository.GetNextSequenceNumberAsync(prefix);
            
            // Auto-expand digits (minimum 5, expands as needed)
            // Examples: 00001, 00002, ..., 99999, 100000, 100001, ...
            var digits = Math.Max(5, sequenceNumber.ToString().Length);
            return $"{prefix}{sequenceNumber.ToString($"D{digits}")}";
        }
        
        private string? GenerateSubmissionId(string prefix, string? referenceNo)
        {
            if (string.IsNullOrWhiteSpace(referenceNo))
            {
                Logger.LogWarning(
                    "Cannot generate Unity ID for submission type: ReferenceNo is empty");
                return null;
            }
            
            // Simple concatenation for submission type
            return $"{prefix}{referenceNo}";
        }

        #endregion

        private async Task<Application> CreateNewApplicationAsync(IntakeMapping intakeMap,
            ApplicationForm applicationForm)
        {            
            var submittedStatus = await _applicationStatusRepository.FirstAsync(s => s.StatusCode.Equals(GrantApplicationState.SUBMITTED));
            
            // Generate Unity Application ID with graceful degradation
            string? unityApplicationId = null;
            try
            {
                unityApplicationId = await GenerateUnityApplicationIdAsync(applicationForm, intakeMap.ConfirmationId);
                    
                if (!string.IsNullOrEmpty(unityApplicationId))
                {
                    Logger.LogInformation(
                        "Generated Unity Application ID: {UnityId} for form {FormId}", 
                        unityApplicationId, applicationForm.Id);
                }
            }
            catch (Exception ex)
            {
                // Log error but continue - application creation is priority
                Logger.LogError(ex, 
                    "Failed to generate Unity Application ID for form {FormId}. " +
                    "Application will be created without Unity ID.", 
                    applicationForm.Id);
            }
            
            var application = await _applicationRepository.InsertAsync(
                new Application
                {
                    ProjectName = MappingUtil.ResolveAndTruncateField(255, string.Empty, intakeMap.ProjectName),                    
                    ApplicationFormId = applicationForm.Id,
                    ApplicationStatusId = submittedStatus.Id,                    
                    ReferenceNo = intakeMap.ConfirmationId ?? string.Empty,
                    UnityApplicationId = unityApplicationId,
                    Acquisition = intakeMap.Acquisition,
                    Forestry = intakeMap.Forestry,
                    ForestryFocus = intakeMap.ForestryFocus,
                    City = intakeMap.PhysicalCity, // To be determined from the applicant
                    EconomicRegion = intakeMap.EconomicRegion,
                    CommunityPopulation = MappingUtil.ConvertToIntFromString(intakeMap.CommunityPopulation),
                    RequestedAmount = MappingUtil.ConvertToDecimalFromStringDefaultZero(intakeMap.RequestedAmount),
                    SubmissionDate = MappingUtil.ConvertDateTimeFromStringDefaultNow(intakeMap.SubmissionDate),
                    ProjectStartDate = MappingUtil.ConvertDateTimeNullableFromString(intakeMap.ProjectStartDate),
                    ProjectEndDate = MappingUtil.ConvertDateTimeNullableFromString(intakeMap.ProjectEndDate),
                    TotalProjectBudget = MappingUtil.ConvertToDecimalFromStringDefaultZero(intakeMap.TotalProjectBudget),
                    Community = intakeMap.Community,
                    ElectoralDistrict = intakeMap.ElectoralDistrict,
                    ApplicantElectoralDistrict = intakeMap.ApplicantElectoralDistrict,
                    RegionalDistrict = intakeMap.RegionalDistrict,
                    SigningAuthorityFullName = intakeMap.SigningAuthorityFullName,
                    SigningAuthorityTitle = intakeMap.SigningAuthorityTitle,
                    SigningAuthorityEmail = intakeMap.SigningAuthorityEmail,
                    SigningAuthorityBusinessPhone = intakeMap.SigningAuthorityBusinessPhone,
                    SigningAuthorityCellPhone = intakeMap.SigningAuthorityCellPhone,
                    Place = intakeMap.Place,
                    RiskRanking = intakeMap.RiskRanking,
                    ProjectSummary = intakeMap.ProjectSummary,
                }
            );

            var applicant = await applicantService.CreateOrRetrieveApplicantAsync(intakeMap, application.Id);
            if (applicant != null)
            {
                application.ApplicantId = applicant.Id;
                application = await _applicationRepository.UpdateAsync(application);
            }            

            ApplicantAgentDto applicantAgentDto = new()
            {
                Applicant = applicant!,
                Application = application,
                IntakeMap = intakeMap
            };

            await applicantService.CreateApplicantAgentAsync(applicantAgentDto);

            try
            {
                await applicantService.RelateDefaultSupplierAsync(applicantAgentDto);
            }
            catch (Exception ex)
            {
                // This was failing with SSL Certificate errors, so we're catching the exception and logging it
                string MessageException = ex.Message;
                Logger.LogError(ex, "Error Relating Default Supplier {MessageException}", MessageException);
            }

            return application;
        }

        public async Task ResyncSubmissionAttachments(Guid applicationId)
        {
            var query = from applicationFormSubmission in await _applicationFormSubmissionRepository.GetQueryableAsync()
                        where applicationFormSubmission.ApplicationId == applicationId
                        select applicationFormSubmission;
            ApplicationFormSubmission? applicationFormSubmissionData = await AsyncExecuter.FirstOrDefaultAsync(query);
            if (applicationFormSubmissionData == null) return;
            var formSubmission = JsonConvert.DeserializeObject<dynamic>(applicationFormSubmissionData.Submission)!;
            await _intakeFormSubmissionMapper.ResyncSubmissionAttachments(applicationId, formSubmission);
        }
    }
}
