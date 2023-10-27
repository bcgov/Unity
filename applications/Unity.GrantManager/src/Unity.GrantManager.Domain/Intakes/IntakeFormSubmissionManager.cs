using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
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
        private readonly IAddressRepository _addressRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationStatusRepository _applicationStatusRepository;
        private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
        private readonly IIntakeFormSubmissionMapper _intakeFormSubmissionMapper;

        public IntakeFormSubmissionManager(IUnitOfWorkManager unitOfWorkManager,
            IApplicantRepository applicantRepository,
            IApplicantAgentRepository applicantAgentRepository,
            IAddressRepository addressRepository,
            IApplicationRepository applicationRepository,
            IApplicationStatusRepository applicationStatusRepository,
            IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
            IIntakeFormSubmissionMapper intakeFormSubmissionMapper)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _applicantRepository = applicantRepository;
            _applicantAgentRepository = applicantAgentRepository;
            _addressRepository = addressRepository;
            _applicationRepository = applicationRepository;
            _applicationStatusRepository = applicationStatusRepository;
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
            _intakeFormSubmissionMapper = intakeFormSubmissionMapper;
        }

        public async Task<Guid> ProcessFormSubmissionAsync(ApplicationForm applicationForm, dynamic formSubmission)
        {
            IntakeMapping intakeMap = _intakeFormSubmissionMapper.MapFormSubmissionFields(applicationForm, formSubmission);
            intakeMap.SubmissionId = formSubmission.submission.id;
            intakeMap.SubmissionDate = formSubmission.submission.date;
            intakeMap.ConfirmationId = formSubmission.submission.confirmationId;
            using var uow = _unitOfWorkManager.Begin();
            var application = await CreateNewApplicationAsync(intakeMap, applicationForm);
            var applicationFormSubmission = await _applicationFormSubmissionRepository.InsertAsync(
            new ApplicationFormSubmission
            {
                OidcSub = Guid.Empty.ToString(),
                ApplicantId = application.ApplicantId,
                ApplicationFormId = applicationForm.Id,
                ChefsSubmissionGuid = intakeMap.SubmissionId ?? $"{Guid.Empty}"
            });
            await uow.SaveChangesAsync();
            return applicationFormSubmission.Id;
        }

        private async Task<Application> CreateNewApplicationAsync(IntakeMapping intakeMap,
            ApplicationForm applicationForm)
        {
            var applicant = await CreateApplicantAsync(intakeMap);
            var application = await _applicationRepository.InsertAsync(
                new Application
                {
                    ProjectName = intakeMap.ProjectName ?? "{Project Name}",
                    ApplicantId = applicant.Id,
                    ApplicationFormId = applicationForm.Id,
                    ApplicationStatusId = (await _applicationStatusRepository.FirstAsync(s => s.StatusCode == "SUBMITTED")).Id,
                    ReferenceNo = intakeMap.ConfirmationId ?? "{Confirmation ID}",
                    RequestedAmount = double.Parse(intakeMap.RequestedAmount ?? "0"),
                    SubmissionDate = DateTime.Parse(intakeMap.SubmissionDate ?? DateTime.UtcNow.ToString(), CultureInfo.InvariantCulture),
                    City = intakeMap.PhysicalCity ?? "{City}", // To be determined from the applicant
                    EconomicRegion = intakeMap.EconomicRegion ?? "{Region}", // TBD how to calculate this - spacial lookup?
                    TotalProjectBudget = double.Parse(intakeMap.TotalProjectBudget ?? "0"),
                    Sector = intakeMap.Sector ?? "{Sector}" // TBD how to calculate this
                }                
            );   
            await CreateApplicantAgentAsync(intakeMap, applicant, application);
            return application;
        }

        private async Task<Applicant> CreateApplicantAsync(IntakeMapping intakeMap)
        {
            var applicant = (await _applicantRepository.GetQueryableAsync()).FirstOrDefault(s => s.ApplicantName == intakeMap.ApplicantName);
            if (applicant == null) {

                applicant = await _applicantRepository.InsertAsync(new Applicant
                {
                    ApplicantName = intakeMap.ApplicantName ?? "{ApplicantName}",
                    NonRegisteredBusinessName = intakeMap.NonRegisteredBusinessName ?? "{NonRegisteredBusinessName}",
                    OrgName = intakeMap.OrgName ?? "{OrgName}",
                    OrgNumber = intakeMap.OrgNumber ?? "{OrgNumber}",
                    OrganizationType = intakeMap.OrganizationType ?? "{OrganizationType}",
                    Sector = intakeMap.Sector ?? "{Sector}",
                    SubSector = intakeMap.SubSector ?? "{SubSector}",
                    ApproxNumberOfEmployees = intakeMap.ApproxNumberOfEmployees ?? "{ApproxNumberOfEmployees}",
                    Community = intakeMap.Community ?? "{Community}",
                    IndigenousOrgInd =  intakeMap.IndigenousOrgInd ?? "N",
                    ElectoralDistrict = intakeMap.ElectoralDistrict ?? "{ElectoralDistrict}",
                    EconomicRegion = intakeMap.EconomicRegion ?? "{Region}", 
                });

                await CreateApplicantAddressAsync(intakeMap, applicant);
            }

           return applicant;
        }

        private async Task<ApplicantAgent> CreateApplicantAgentAsync(IntakeMapping intakeMap, Applicant applicant, Application application)
        {
            var applicantAgent = new ApplicantAgent();
            if (intakeMap.ContactName != null) {

                applicantAgent = await _applicantAgentRepository.InsertAsync(new ApplicantAgent
                {
                    ApplicantId = applicant.Id,
                    ApplicationId = application.Id,
                    Name = intakeMap.ContactName ?? "{ContactName}",
                    Phone = intakeMap.ContactPhone ?? "{ContactPhone}",
                    Phone2 = intakeMap.ContactPhone2 ?? "{ContactPhone2}",
                    Email = intakeMap.ContactEmail ?? "{ContactEmail}",
                    Title = intakeMap.ContactTitle ?? "{ContactTitle}"
                });
            }

           return applicantAgent;
        }

        private async Task<Address> CreateApplicantAddressAsync(IntakeMapping intakeMap, Applicant applicant)
        {
            var address = new Address();
            if(!intakeMap.PhysicalStreet.IsNullOrEmpty()) {
                address = await _addressRepository.InsertAsync(new Address
                {
                    ApplicantId = applicant.Id,
                    City = intakeMap.PhysicalCity ?? "{PhysicalCity}",
                    Country = intakeMap.PhysicalProvince ?? "{PhysicalProvince}",
                    Province = intakeMap.PhysicalCountry ?? "{PhysicalCountry}",
                    Postal = intakeMap.PhysicalPostal ?? "{PhysicalPostal}",
                    Street = intakeMap.PhysicalStreet ?? "{PhysicalStreet}",
                    Street2 = intakeMap.PhysicalStreet2 ?? "{PhysicalStreet2}",
                    Unit = intakeMap.PhysicalUnit ?? "{PhysicalUnit}",
                });

            }
            return address;
        }
    }
}
