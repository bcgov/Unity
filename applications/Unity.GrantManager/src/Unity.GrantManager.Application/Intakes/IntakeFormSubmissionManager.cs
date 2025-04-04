using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Intakes.Mapping;
using Unity.GrantManager.Reporting.DataGenerators;
using Unity.Modules.Shared.Features;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Features;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Intakes
{
    public class IntakeFormSubmissionManager(IUnitOfWorkManager _unitOfWorkManager,
                                             IApplicantAppService applicantService,
                                             IApplicationRepository _applicationRepository,
                                             IApplicationStatusRepository _applicationStatusRepository,
                                             IApplicationFormSubmissionRepository _applicationFormSubmissionRepository,
                                             IIntakeFormSubmissionMapper _intakeFormSubmissionMapper,
                                             IApplicationFormVersionRepository _applicationFormVersionRepository,
                                             CustomFieldsIntakeSubmissionMapper _customFieldsIntakeSubmissionMapper,
                                             IReportingDataGenerator _reportingDataGenerator,
                                             IFeatureChecker featureChecker) : DomainService, IIntakeFormSubmissionManager
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

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

            var newSubmission = new ApplicationFormSubmission
            {
                OidcSub = Guid.Empty.ToString(),
                ApplicantId = application.ApplicantId,
                ApplicationFormId = applicationForm.Id,
                ChefsSubmissionGuid = intakeMap.SubmissionId ?? $"{Guid.Empty}",
                ApplicationId = application.Id,
                Submission = ChefsFormIOReplacement.ReplaceAdvancedFormIoControls(formSubmission)
            };

            _ = await _applicationFormSubmissionRepository.InsertAsync(newSubmission);

            var localFormVersion = await _applicationFormVersionRepository.GetByChefsFormVersionAsync(Guid.Parse(formVersionId));
            await _customFieldsIntakeSubmissionMapper.MapAndPersistCustomFields(application.Id,
                localFormVersion?.Id ?? Guid.Empty,
                formSubmission,
            formVersionSubmissionHeaderMapping);

            if (await featureChecker.IsEnabledAsync(FeatureConsts.Reporting))
            {
                newSubmission.ReportData = _reportingDataGenerator.Generate(formSubmission, localFormVersion?.ReportKeys, newSubmission.Id);
            }

            newSubmission.ApplicationFormVersionId = localFormVersion?.Id;

            await uow.SaveChangesAsync();

            return application.Id;
        }

        private async Task<Application> CreateNewApplicationAsync(IntakeMapping intakeMap,
            ApplicationForm applicationForm)
        {
            var applicant = await applicantService.CreateOrRetrieveApplicantAsync(intakeMap);
            var submittedStatus = await _applicationStatusRepository.FirstAsync(s => s.StatusCode.Equals(GrantApplicationState.SUBMITTED));
            var application = await _applicationRepository.InsertAsync(
                new Application
                {
                    ProjectName = MappingUtil.ResolveAndTruncateField(255, string.Empty, intakeMap.ProjectName),
                    ApplicantId = applicant.Id,
                    ApplicationFormId = applicationForm.Id,
                    ApplicationStatusId = submittedStatus.Id,
                    ReferenceNo = intakeMap.ConfirmationId ?? string.Empty,
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
            ApplicantAgentDto applicantAgentDto = new ApplicantAgentDto
            {
                Applicant = applicant,
                Application = application,
                IntakeMap = intakeMap
            };
            
            await applicantService.CreateApplicantAgentAsync(applicantAgentDto);

            try {
                await applicantService.RelateDefaultSupplierAsync(applicantAgentDto);
            } catch (Exception ex) {
                // This was failing with SSL Certificate errors, so we're catching the exception and logging it
                string MessageException = ex.Message;
                logger.LogError(ex, "Error Relating Default Supplier {MessageException}", MessageException);
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
