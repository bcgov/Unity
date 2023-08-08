using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Unity.GrantManager.Models;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Controllers
{
    [ApiController]
    [Route("api/intakeSubmission")]
    public class IntakeSubmissionController : AbpControllerBase
    {

        [HttpPost]
        public dynamic PostIntakeSubmission([FromBody] IntakeSubmission res)
        {
            // TODO: Save intake submission data in db once the data model is finalized
            return res;
        }
    }
}

