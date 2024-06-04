using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Intakes
{
    public class IntakeFormSubmissionManager : DomainService, IIntakeFormSubmissionManager
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IApplicantRepository _applicantRepository;
        private readonly IApplicantAgentRepository _applicantAgentRepository;
        private readonly IApplicantAddressRepository _addressRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationStatusRepository _applicationStatusRepository;
        private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
        private readonly IIntakeFormSubmissionMapper _intakeFormSubmissionMapper;
        private readonly IApplicationFormVersionRepository _applicationFormVersionRepository;
        private readonly CustomFieldsIntakeSubmissionMapper _customFieldsIntakeSubmissionMapper;

        public IntakeFormSubmissionManager(IUnitOfWorkManager unitOfWorkManager,
            IApplicantRepository applicantRepository,
            IApplicantAgentRepository applicantAgentRepository,
            IApplicantAddressRepository addressRepository,
            IApplicationRepository applicationRepository,
            IApplicationStatusRepository applicationStatusRepository,
            IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
            IIntakeFormSubmissionMapper intakeFormSubmissionMapper,
            IApplicationFormVersionRepository applicationFormVersionRepository,
            CustomFieldsIntakeSubmissionMapper customFieldsIntakeSubmissionMapper)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _applicantRepository = applicantRepository;
            _applicantAgentRepository = applicantAgentRepository;
            _addressRepository = addressRepository;
            _applicationRepository = applicationRepository;
            _applicationStatusRepository = applicationStatusRepository;
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
            _intakeFormSubmissionMapper = intakeFormSubmissionMapper;
            _applicationFormVersionRepository = applicationFormVersionRepository;
            _customFieldsIntakeSubmissionMapper = customFieldsIntakeSubmissionMapper;
        }

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
            _intakeFormSubmissionMapper.SaveChefsFiles(formSubmission, application.Id);

            var applicationFormSubmission = await _applicationFormSubmissionRepository.InsertAsync(
            new ApplicationFormSubmission
            {
                OidcSub = Guid.Empty.ToString(),
                ApplicantId = application.ApplicantId,
                ApplicationFormId = applicationForm.Id,
                ChefsSubmissionGuid = intakeMap.SubmissionId ?? $"{Guid.Empty}",
                ApplicationId = application.Id,
                Submission = formSubmission.ToString()
            });

            await _customFieldsIntakeSubmissionMapper.MapAndPersistCustomFields(application.Id, formSubmission, formVersionSubmissionHeaderMapping);
            
            await uow.SaveChangesAsync();

            return applicationFormSubmission.Id;
        }

        private async Task<Application> CreateNewApplicationAsync(IntakeMapping intakeMap,
            ApplicationForm applicationForm)
        {
            var applicant = await CreateApplicantAsync(intakeMap);
            var submittedStatus = await _applicationStatusRepository.FirstAsync(s => s.StatusCode.Equals(GrantApplicationState.SUBMITTED));
            var application = await _applicationRepository.InsertAsync(
                new Application
                {
                    ProjectName = ResolveAndTruncateField(255, "{ProjectName}", intakeMap.ProjectName),
                    ApplicantId = applicant.Id,
                    ApplicationFormId = applicationForm.Id,
                    ApplicationStatusId = submittedStatus.Id,
                    ReferenceNo = intakeMap.ConfirmationId ?? "{Confirmation ID}",
                    Acquisition = intakeMap.Acquisition ?? null,
                    Forestry = intakeMap.Forestry ?? null,
                    ForestryFocus = intakeMap.ForestryFocus ?? null,
                    City = intakeMap.PhysicalCity ?? "{City}", // To be determined from the applicant
                    EconomicRegion = intakeMap.EconomicRegion ?? "{Region}",
                    CommunityPopulation = ConvertToIntFromString(intakeMap.CommunityPopulation),
                    RequestedAmount = ConvertToDecimalFromStringDefaultZero(intakeMap.RequestedAmount),
                    SubmissionDate = ConvertDateTimeFromStringDefaultNow(intakeMap.SubmissionDate),
                    ProjectStartDate = ConvertDateTimeNullableFromString(intakeMap.ProjectStartDate),
                    ProjectEndDate = ConvertDateTimeNullableFromString(intakeMap.ProjectEndDate),
                    TotalProjectBudget = ConvertToDecimalFromStringDefaultZero(intakeMap.TotalProjectBudget),
                    Community = intakeMap.Community ?? "{Community}",
                    ElectoralDistrict = intakeMap.ElectoralDistrict ?? "{ElectoralDistrict}",
                    RegionalDistrict = intakeMap.RegionalDistrict ?? "{RegionalDistrict}",
                    SigningAuthorityFullName = intakeMap.SigningAuthorityFullName ?? "{SigningAuthorityFullName}",
                    SigningAuthorityTitle = intakeMap.SigningAuthorityTitle ?? "{SigningAuthorityTitle}",
                    SigningAuthorityEmail = intakeMap.SigningAuthorityEmail ?? "{SigningAuthorityEmail}",
                    SigningAuthorityBusinessPhone = intakeMap.SigningAuthorityBusinessPhone ?? "{SigningAuthorityBusinessPhone}",
                    SigningAuthorityCellPhone = intakeMap.SigningAuthorityCellPhone ?? "{SigningAuthorityCellPhone}",
                    Place = intakeMap.Place ?? "{Place}"
                }
            );
            await CreateApplicantAgentAsync(intakeMap, applicant, application);
            return application;
        }

        private string ResolveAndTruncateField(int maxLength, string defaultFieldName, string? valueString)
        {
            string fieldValue = defaultFieldName;

            if (!string.IsNullOrEmpty(valueString) && valueString.Length > maxLength)
            {
                Logger.LogWarning("Truncation: {fieldName} has been truncated! - Max length: {length}", defaultFieldName, maxLength);
                fieldValue = valueString.Substring(0, maxLength);
            }
            else if (!string.IsNullOrEmpty(valueString))
            {
                fieldValue = valueString.Trim();
            }

            return fieldValue;
        }

        private int? ConvertToIntFromString(string? intString)
        {
            if (int.TryParse(intString, out int intParse))
            {
                return intParse;
            }

            return null;
        }

        private decimal ConvertToDecimalFromStringDefaultZero(string? decimalString)
        {
            decimal decimalValue;
            if (decimal.TryParse(decimalString, out decimal decimalParse))
            {
                decimalValue = decimalParse;
            }
            else
            {
                decimalValue = Convert.ToDecimal("0");
            }
            return decimalValue;
        }

        private DateTime? ConvertDateTimeNullableFromString(string? dateTime)
        {
            DateTime? dateTimeValue = null;

            if (DateTime.TryParse(dateTime, out DateTime testDateTimeParse))
            {
                dateTimeValue = testDateTimeParse;
            }

            return dateTimeValue;
        }

        private DateTime ConvertDateTimeFromStringDefaultNow(string? dateTime)
        {
            DateTime dateTimeValue;
            DateTime.TryParse(dateTime, out dateTimeValue);

            if (string.IsNullOrEmpty(dateTime))
            {
                dateTimeValue = DateTime.Parse(DateTime.UtcNow.ToString("u"));
            }

            return dateTimeValue;
        }

        private async Task<Applicant> CreateApplicantAsync(IntakeMapping intakeMap)
        {
            var applicant = await _applicantRepository.InsertAsync(new Applicant
            {
                ApplicantName = ResolveAndTruncateField(600, "{ApplicantName}", intakeMap.ApplicantName),
                NonRegisteredBusinessName = intakeMap.NonRegisteredBusinessName ?? "{NonRegisteredBusinessName}",
                OrgName = intakeMap.OrgName ?? "{OrgName}",
                OrgNumber = intakeMap.OrgNumber ?? "{OrgNumber}",
                OrganizationType = intakeMap.OrganizationType ?? "{OrganizationType}",
                Sector = intakeMap.Sector ?? "{Sector}",
                SubSector = intakeMap.SubSector ?? "{SubSector}",
                SectorSubSectorIndustryDesc = intakeMap.SectorSubSectorIndustryDesc ?? "{SectorSubSectorIndustryDesc}",
                ApproxNumberOfEmployees = intakeMap.ApproxNumberOfEmployees ?? "{ApproxNumberOfEmployees}",
                IndigenousOrgInd = intakeMap.IndigenousOrgInd ?? "N",
            });

            await CreateApplicantAddressesAsync(intakeMap, applicant);

            return applicant;
        }

        private async Task<ApplicantAgent> CreateApplicantAgentAsync(IntakeMapping intakeMap, Applicant applicant, Application application)
        {
            var applicantAgent = new ApplicantAgent();
            if (!string.IsNullOrEmpty(intakeMap.ContactName) || !string.IsNullOrEmpty(intakeMap.ContactPhone) || !string.IsNullOrEmpty(intakeMap.ContactPhone2)
                || !string.IsNullOrEmpty(intakeMap.ContactEmail) || !string.IsNullOrEmpty(intakeMap.ContactTitle))
            {

                applicantAgent = await _applicantAgentRepository.InsertAsync(new ApplicantAgent
                {
                    ApplicantId = applicant.Id,
                    ApplicationId = application.Id,
                    Name = intakeMap.ContactName ?? "{ContactName}",
                    Phone = intakeMap.ContactPhone ?? "{ContactPhone}",
                    Phone2 = intakeMap.ContactPhone2 ?? "{ContactPhone2}",
                    Email = intakeMap.ContactEmail ?? "{ContactEmail}",
                    Title = intakeMap.ContactTitle ?? "{ContactTitle}",
                });
            }

            return applicantAgent;
        }

        private async Task CreateApplicantAddressesAsync(IntakeMapping intakeMap, Applicant applicant)
        {
            if (!intakeMap.PhysicalStreet.IsNullOrEmpty()
                || !intakeMap.PhysicalStreet2.IsNullOrEmpty())
            {
                await _addressRepository.InsertAsync(new ApplicantAddress
                {
                    ApplicantId = applicant.Id,
                    City = intakeMap.PhysicalCity,
                    Country = intakeMap.PhysicalCountry,
                    Province = intakeMap.PhysicalProvince,
                    Postal = intakeMap.PhysicalPostal,
                    Street = intakeMap.PhysicalStreet,
                    Street2 = intakeMap.PhysicalStreet2,
                    Unit = intakeMap.PhysicalUnit,
                    AddressType = AddressType.PhysicalAddress
                });
            }

            if (!intakeMap.MailingStreet.IsNullOrEmpty()
                || !intakeMap.MailingStreet2.IsNullOrEmpty())
            {
                await _addressRepository.InsertAsync(new ApplicantAddress
                {
                    ApplicantId = applicant.Id,
                    City = intakeMap.MailingCity,
                    Country = intakeMap.MailingCountry,
                    Province = intakeMap.MailingProvince,
                    Postal = intakeMap.MailingPostal,
                    Street = intakeMap.MailingStreet,
                    Street2 = intakeMap.MailingStreet2,
                    Unit = intakeMap.MailingUnit,
                    AddressType = AddressType.MailingAddress
                });
            }
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
