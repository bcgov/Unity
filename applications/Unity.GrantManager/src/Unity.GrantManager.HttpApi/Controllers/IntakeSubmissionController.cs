using Microsoft.AspNetCore.Mvc;
using Unity.GrantManager.Models;
using Volo.Abp.AspNetCore.Mvc;
using Unity.GrantManager.Applications;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantPrograms;
using RestSharp;
using Newtonsoft.Json;

namespace Unity.GrantManager.Controllers
{
    [ApiController]
    [Route("api/intakeSubmission")]
    public class IntakeSubmissionController : AbpControllerBase
    {
        private IApplicationRepository _applicationRepository;
        private IApplicationFormRepository _applicationFormRepository;
        private IApplicationStatusRepository _applicationStatusRepository;
        private IApplicantRepository _applicantRepository;
        private IIntakeRepository _intakeRepository;
        private IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
        private readonly RestClient _intakeClient;

        public IntakeSubmissionController(IApplicationRepository applicationService,
                                              IApplicationStatusRepository applicationStatusRepository,
                                              IApplicationFormRepository applicationFormRepository,
                                              IApplicantRepository applicantRepository,
                                              IIntakeRepository intakeRepository,
                                              IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
                                              RestClient restClient)
        {
            _applicationRepository = applicationService;
            _applicationStatusRepository = applicationStatusRepository;
            _applicationFormRepository = applicationFormRepository;
            _applicantRepository = applicantRepository;
            _intakeRepository = intakeRepository;
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
            _intakeClient = restClient;
        }

        private async Task<Models.Intake> GetApplicationKeyFields(IntakeSubmission intakeSubmission)
        {
            Models.Intake intake = new Models.Intake();
            var request = new RestRequest($"/submissions/{intakeSubmission.submissionId}");
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
                        IntakeName = intake.formName, // Not sure on how this is mapped 

                    },
                    autoSave: true
                    );

                    // Create applicant by calling to CHEFS - User api
                    Applicant newApplicant = await _applicantRepository.InsertAsync(
                        new Applicant
                        {
                            ApplicantName = intake.applicantName, 
                        },
                        autoSave: true
                    );

                    // Created Application Form - save submission id
                    ApplicationForm newApplicationForm = await _applicationFormRepository.InsertAsync(
                        new ApplicationForm
                        {
                            ChefsApplicationFormGuid = intakeSubmission.formId,
                            IntakeId = newIntake.Id,
                            ApplicationFormName = intake.projectName, // This should be the form name?/New - this is filled in by applicant Project Name
                            ChefsCriteriaFormGuid = "3a0d369f-7da5-64a8-e1f7-71f027cfaa0e" // ChefsCriteriaFormGuid // What is this?
                        },
                        autoSave: true
                    );

                    if (submittedStatus != null)
                    {
                        Application newApplication = await _applicationRepository.InsertAsync(
                            new Application
                            {
                                ProjectName = intake.projectName, // This should be the form name
                                ApplicationFormId = newApplicationForm.Id,
                                ApplicantId = newApplicant.Id,
                                ApplicationStatusId = submittedStatus.Id,
                                ReferenceNo = intake.confirmationId, // Taken from the CHEF Confirmation ID
                                RequestedAmount = Double.Parse(intake.requestedAmount)
                            },
                            autoSave: true
                        );

                        ApplicationFormSubmission applicationFormSubmission = await _applicationFormSubmissionRepository.InsertAsync(
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
