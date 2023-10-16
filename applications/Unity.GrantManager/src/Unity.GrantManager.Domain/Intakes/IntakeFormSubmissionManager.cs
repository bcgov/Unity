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
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationStatusRepository _applicationStatusRepository;
        private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
        private readonly IIntakeFormSubmissionMapper _intakeFormSubmissionMapper;

        public IntakeFormSubmissionManager(IUnitOfWorkManager unitOfWorkManager,
            IApplicantRepository applicantRepository,
            IApplicationRepository applicationRepository,
            IApplicationStatusRepository applicationStatusRepository,
            IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
            IIntakeFormSubmissionMapper intakeFormSubmissionMapper)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _applicantRepository = applicantRepository;
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
            return await _applicationRepository.InsertAsync(
                new Application
                {
                    ProjectName = intakeMap.ProjectName ?? "{Project Name}",
                    ApplicantId = (await CreateApplicantAsync(intakeMap.ApplicantName)).Id,
                    ApplicationFormId = applicationForm.Id,
                    ApplicationStatusId = (await _applicationStatusRepository.FirstAsync(s => s.StatusCode == "SUBMITTED")).Id,
                    ReferenceNo = intakeMap.ConfirmationId ?? "{Confirmation ID}",
                    RequestedAmount = double.Parse(intakeMap.RequestedAmount ?? "0"),
                    SubmissionDate = DateTime.Parse(intakeMap.SubmissionDate ?? DateTime.UtcNow.ToString(), CultureInfo.InvariantCulture),
                    City = intakeMap.City ?? "{City}", // To be determined from the applicant
                    EconomicRegion = intakeMap.EconomicRegion ?? "{Region}", // TBD how to calculate this - spacial lookup?
                    TotalProjectBudget = double.Parse(intakeMap.TotalProjectBudget ?? "0"),
                    Sector = intakeMap.Sector ?? "{Sector}" // TBD how to calculate this
                }
            );
        }

        private async Task<Applicant> CreateApplicantAsync(string? applicantName)
        {
            var existingApplicant = (await _applicantRepository.GetQueryableAsync()).FirstOrDefault(s => s.ApplicantName == applicantName);
            if (existingApplicant != null) return existingApplicant;
            else
                return await _applicantRepository.InsertAsync(new Applicant
                {
                    ApplicantName = applicantName ?? "{Applicant Name}",
                });
        }
    }
}
