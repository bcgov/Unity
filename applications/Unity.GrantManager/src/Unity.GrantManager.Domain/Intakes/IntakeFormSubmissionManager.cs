using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
                Submission = ReplaceAdvancedFormIoControls(formSubmission)
            });

            await _customFieldsIntakeSubmissionMapper.MapAndPersistCustomFields(application.Id, application.ApplicationFormId, formSubmission, formVersionSubmissionHeaderMapping);
            
            await uow.SaveChangesAsync();

            return applicationFormSubmission.Id;
        }

        private static string ReplaceAdvancedFormIoControls(dynamic formSubmission)
        {
            string formSubmissionStr = formSubmission.ToString();
            if (!string.IsNullOrEmpty(formSubmissionStr))
            {
                Dictionary<string, string> subPatterns = new Dictionary<string, string>();
                subPatterns.Add(@"\borgbook\b", "select");
                subPatterns.Add(@"\bsimpleaddressadvanced\b", "address");
                subPatterns.Add(@"\bsimplebuttonadvanced\b", "button");
                subPatterns.Add(@"\bsimplecheckboxadvanced\b", "checkbox");
                subPatterns.Add(@"\bsimplecurrencyadvanced\b", "currency");
                subPatterns.Add(@"\bsimpledatetimeadvanced\b", "datetime");
                subPatterns.Add(@"\bsimpledayadvanced\b", "day");
                subPatterns.Add(@"\bsimpleemailadvanced\b", "email");
                subPatterns.Add(@"\bsimplenumberadvanced\b", "number");
                subPatterns.Add(@"\bsimplepasswordadvanced\b", "password");
                subPatterns.Add(@"\bsimplephonenumberadvanced\b", "phoneNumber");
                subPatterns.Add(@"\bsimpleradioadvanced\b", "radio");
                subPatterns.Add(@"\bsimpleselectadvanced\b", "select");
                subPatterns.Add(@"\bsimpleselectboxesadvanced\b", "selectboxes");
                subPatterns.Add(@"\bsimplesignatureadvanced\b", "signature");
                subPatterns.Add(@"\bsimplesurveyadvanced\b", "survey");
                subPatterns.Add(@"\bsimpletagsadvanced\b", "tags");
                subPatterns.Add(@"\bsimpletextareaadvanced\b", "textarea");
                subPatterns.Add(@"\bsimpletextfieldadvanced\b", "textfield");
                subPatterns.Add(@"\bsimpletimeadvanced\b", "time");
                subPatterns.Add(@"\bsimpleurladvanced\b", "url");

                // Regular components
                subPatterns.Add(@"\bsimplebcaddress\b", "address");
                subPatterns.Add(@"\bbcaddress\b", "address");
                subPatterns.Add(@"\bsimplebtnreset\b", "button");
                subPatterns.Add(@"\bsimplebtnsubmit\b", "button");
                subPatterns.Add(@"\bsimplecheckboxes\b", "selectboxes");
                subPatterns.Add(@"\bsimplecheckbox\b", "checkbox");
                subPatterns.Add(@"\bsimplecols2\b", "columns");
                subPatterns.Add(@"\bsimplecols3\b", "columns");
                subPatterns.Add(@"\bsimplecols4\b", "columns");
                subPatterns.Add(@"\bsimplecontent\b", "content");
                subPatterns.Add(@"\bsimpledatetime\b", "datetime");
                subPatterns.Add(@"\bsimpleday\b", "day");
                subPatterns.Add(@"\bsimpleemail\b", "email");
                subPatterns.Add(@"\bsimplefile\b", "file");
                subPatterns.Add(@"\bsimpleheading\b", "header");
                subPatterns.Add(@"\bsimplefieldset\b", "fieldset");
                subPatterns.Add(@"\bsimplenumber\b", "number");
                subPatterns.Add(@"\bsimplepanel", "panel");
                subPatterns.Add(@"\bsimpleparagraph\b", "textarea");
                subPatterns.Add(@"\bsimplephonenumber\b", "phoneNumber");
                subPatterns.Add(@"\bsimpleradios\b", "radio");
                subPatterns.Add(@"\bsimpleselect\b", "select");
                subPatterns.Add(@"\bsimpletabs\b", "tabs");
                subPatterns.Add(@"\bsimpletextarea\b", "textarea");
                subPatterns.Add(@"\bsimpletextfield\b", "textfield");
                subPatterns.Add(@"\bsimpletime\b", "time");

                string replacedString = formSubmissionStr;
   
                //find the replacement
                foreach (var subPattern in subPatterns)
                {
                    string patternKey = subPattern.Key;
                    string replace = subPattern.Value;
                    replacedString = Regex.Replace(replacedString, patternKey, replace);
                }
                
                formSubmissionStr = replacedString;
            }
            return formSubmissionStr;
        }

        private async Task<Application> CreateNewApplicationAsync(IntakeMapping intakeMap,
            ApplicationForm applicationForm)
        {
            var applicant = await CreateApplicantAsync(intakeMap);
            var submittedStatus = await _applicationStatusRepository.FirstAsync(s => s.StatusCode.Equals(GrantApplicationState.SUBMITTED));
            var application = await _applicationRepository.InsertAsync(
                new Application
                {
                    ProjectName = ResolveAndTruncateField(255, string.Empty, intakeMap.ProjectName),
                    ApplicantId = applicant.Id,
                    ApplicationFormId = applicationForm.Id,
                    ApplicationStatusId = submittedStatus.Id,
                    ReferenceNo = intakeMap.ConfirmationId ?? string.Empty,
                    Acquisition = intakeMap.Acquisition,
                    Forestry = intakeMap.Forestry,
                    ForestryFocus = intakeMap.ForestryFocus,
                    City = intakeMap.PhysicalCity, // To be determined from the applicant
                    EconomicRegion = intakeMap.EconomicRegion,
                    CommunityPopulation = ConvertToIntFromString(intakeMap.CommunityPopulation),
                    RequestedAmount = ConvertToDecimalFromStringDefaultZero(intakeMap.RequestedAmount),
                    SubmissionDate = ConvertDateTimeFromStringDefaultNow(intakeMap.SubmissionDate),
                    ProjectStartDate = ConvertDateTimeNullableFromString(intakeMap.ProjectStartDate),
                    ProjectEndDate = ConvertDateTimeNullableFromString(intakeMap.ProjectEndDate),
                    TotalProjectBudget = ConvertToDecimalFromStringDefaultZero(intakeMap.TotalProjectBudget),
                    Community = intakeMap.Community,
                    ElectoralDistrict = intakeMap.ElectoralDistrict,
                    RegionalDistrict = intakeMap.RegionalDistrict,
                    SigningAuthorityFullName = intakeMap.SigningAuthorityFullName,
                    SigningAuthorityTitle = intakeMap.SigningAuthorityTitle,
                    SigningAuthorityEmail = intakeMap.SigningAuthorityEmail,
                    SigningAuthorityBusinessPhone = intakeMap.SigningAuthorityBusinessPhone,
                    SigningAuthorityCellPhone = intakeMap.SigningAuthorityCellPhone,
                    Place = intakeMap.Place
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
                ApplicantName = ResolveAndTruncateField(600, string.Empty, intakeMap.ApplicantName),
                NonRegisteredBusinessName = intakeMap.NonRegisteredBusinessName,
                OrgName = intakeMap.OrgName,
                OrgNumber = intakeMap.OrgNumber,
                OrganizationType = intakeMap.OrganizationType,
                Sector = intakeMap.Sector,
                SubSector = intakeMap.SubSector,
                SectorSubSectorIndustryDesc = intakeMap.SectorSubSectorIndustryDesc,
                ApproxNumberOfEmployees = intakeMap.ApproxNumberOfEmployees,
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
                    Name = intakeMap.ContactName ?? string.Empty,
                    Phone = intakeMap.ContactPhone ?? string.Empty,
                    Phone2 = intakeMap.ContactPhone2 ?? string.Empty,
                    Email = intakeMap.ContactEmail ?? string.Empty,
                    Title = intakeMap.ContactTitle ?? string.Empty,
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
