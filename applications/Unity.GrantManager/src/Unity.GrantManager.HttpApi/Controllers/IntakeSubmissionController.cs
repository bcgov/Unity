using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantPrograms;
using Unity.GrantManager.Models;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Security.Encryption;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Controllers
{
    [ApiController]
    [Route("api/intakeSubmission")]
    public class IntakeSubmissionController : AbpControllerBase
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IApplicationStatusRepository _applicationStatusRepository;
        private readonly IApplicantRepository _applicantRepository;
        private readonly IIntakeRepository _intakeRepository;
        private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
        private readonly RestClient _intakeClient;
        private readonly IStringEncryptionService _stringEncryptionService;

        public IntakeSubmissionController(IApplicationRepository applicationService,
                                              IApplicationStatusRepository applicationStatusRepository,
                                              IApplicationFormRepository applicationFormRepository,
                                              IApplicantRepository applicantRepository,
                                              IIntakeRepository intakeRepository,
                                              IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
                                              RestClient restClient,
                                              IStringEncryptionService stringEncryptionService)
        {
            _applicationRepository = applicationService;
            _applicationStatusRepository = applicationStatusRepository;
            _applicationFormRepository = applicationFormRepository;
            _applicantRepository = applicantRepository;
            _intakeRepository = intakeRepository;
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
            _intakeClient = restClient;
            _stringEncryptionService = stringEncryptionService;
        }

        private async Task<Models.Intake> GetApplicationKeyFields(IntakeSubmission intakeSubmission)
        {
            // TODO: adding to enable the intake of the app forms and linking - this is code is being worked on already
            Models.Intake intake = new Models.Intake();

            if (intakeSubmission == null) throw new AbpValidationException();
            if (intakeSubmission.formId == null) throw new EntityNotFoundException();

            var appForm = (await _applicationFormRepository.GetQueryableAsync()).FirstOrDefault(s => s.ChefsApplicationFormGuid == intakeSubmission.formId) ?? throw new EntityNotFoundException();
            var request = new RestRequest($"/submissions/{intakeSubmission.submissionId}")
            {
                Authenticator = new HttpBasicAuthenticator(intakeSubmission.formId, _stringEncryptionService.Decrypt(appForm.ApiKey))
            };
            var response = await _intakeClient.GetAsync(request);

            if (response != null && response.Content != null)
            {
                string content = response.Content;
                var dynamicObject = JsonConvert.DeserializeObject<dynamic>(content)!;
                if (dynamicObject != null)
                {
                    var submission = dynamicObject.submission;
                    var data = submission.submission.data;

                    var form = dynamicObject.form;
                    var formName = form.name;
                    intake.confirmationId = submission.confirmationId;
                    intake.formName = formName;

                    intake.projectName = data.projectName;
                    intake.applicantName = data.applicantName;
                    intake.sector = data.sector;
                    intake.totalProjectBudget = data.totalProjectBudget;
                    intake.requestedAmount = data.requestedAmount;
                }
            }
            return intake;
        }

        [HttpPost]
        public async Task<dynamic> PostIntakeSubmissionAsync([FromBody] IntakeSubmission intakeSubmission)
        {
            try
            {
                var statusList = await _applicationStatusRepository.GetListAsync();
                var submittedStatus = statusList.Find(x => x.StatusCode == ApplicationStatusConsts.SUBMITTED);
                var intake = await GetApplicationKeyFields(intakeSubmission);
                if (intake != null)
                {
                    GrantPrograms.Intake newIntake = await _intakeRepository.InsertAsync(
                    new GrantPrograms.Intake
                    {
                        IntakeName = intake.formName ?? "{Missing}", // Not sure on how this is mapped 

                    },
                    autoSave: true
                    );

                    // Create applicant by calling to CHEFS - User api
                    Applicant newApplicant = await _applicantRepository.InsertAsync(
                        new Applicant
                        {
                            ApplicantName = intake.applicantName ?? "{Missing}",
                        },
                        autoSave: true
                    );

                    // Created Application Form - save submission id
                    ApplicationForm newApplicationForm = await _applicationFormRepository.InsertAsync(
                        new ApplicationForm
                        {
                            ChefsApplicationFormGuid = intakeSubmission.formId,
                            IntakeId = newIntake.Id,
                            ApplicationFormName = intake.projectName ?? "{Missing}", // This should be the form name?/New - this is filled in by applicant Project Name
                            ChefsCriteriaFormGuid = "3a0d369f-7da5-64a8-e1f7-71f027cfaa0e" // ChefsCriteriaFormGuid // What is this?
                        },
                        autoSave: true
                    );

                    if (submittedStatus != null)
                    {
                        await _applicationRepository.InsertAsync(
                            new Application
                            {
                                ProjectName = intake.projectName ?? "{Missing}", // This should be the form name
                                ApplicationFormId = newApplicationForm.Id,
                                ApplicantId = newApplicant.Id,
                                ApplicationStatusId = submittedStatus.Id,
                                ReferenceNo = intake.confirmationId ?? "{Missing}", // Taken from the CHEF Confirmation ID
                                RequestedAmount = double.Parse(intake.requestedAmount ?? "0")
                            },
                            autoSave: true
                        );

                        await _applicationFormSubmissionRepository.InsertAsync(
                         new ApplicationFormSubmission
                         {
                             OidcSub = "3a0d369f-7ea2-b49d-5c9b-ab141dad52e8", // Not sure on this need to remove FK 
                             ApplicantId = newApplicant.Id,
                             ApplicationFormId = newApplicationForm.Id,
                             ChefsSubmissionGuid = intakeSubmission.submissionId
                         },
                          autoSave: true
                        );

                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return ex.ToString();
            }

            // Create a form submission
            return intakeSubmission;
        }
    }
}
