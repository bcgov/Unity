using Microsoft.AspNetCore.Mvc;
using Unity.GrantManager.Models;
using Volo.Abp.AspNetCore.Mvc;
using Unity.GrantManager.Applications;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Controllers
{
    [ApiController]
    [Route("api/intakeSubmission")]
    public class IntakeSubmissionController : AbpControllerBase
    {


        private IApplicationRepository _applicationRepository;
        private IApplicationStatusRepository _applicationStatusRepository;

        public IntakeSubmissionController(IApplicationRepository applicationService, IApplicationStatusRepository applicationStatusRepository)
        {
            _applicationRepository = applicationService;
            _applicationStatusRepository = applicationStatusRepository;
        }


        [HttpPost]
        public async Task<dynamic> PostIntakeSubmissionAsync([FromBody] IntakeSubmission res)
        {
            try
            {
                var statusList = await _applicationStatusRepository.GetListAsync();
                var submittedStatus = statusList.Find(x => x.StatusCode == ApplicationStatusConsts.SUBMITTED);

                if (submittedStatus != null)
                {
                    Application newApplication = await _applicationRepository.InsertAsync(
                        new Application
                        {
                            ProjectName = "Ministry of Teleportation",
                            ApplicationFormId = Guid.Parse("3a0d369f-7da5-64a8-e1f7-71f027cfaa0e"),
                            ApplicantId = Guid.Parse("3a0d369f-7de3-90a9-e494-363e4ba93447"),
                            ApplicationStatusId = submittedStatus.Id,
                            ReferenceNo = "ABC123",
                        },
                        autoSave: true
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return ex.ToString();
            }

            // Create a form submission
            return res;
        }
    }
}
