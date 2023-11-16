using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.Events;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Controllers
{
    [ApiController]
    [Route("api/chefs/event")]
    public class EventSubscriptionController : AbpControllerBase
    {
        private readonly IIntakeSubmissionAppService _intakeSubmissionAppService;
        private readonly IChefsEventSubscriptionService _iChefsEventSubscriptionService;

        public EventSubscriptionController(IIntakeSubmissionAppService intakeSubmissionAppService, 
                                           IChefsEventSubscriptionService iChefsEventSubscriptionService)
        {
            _intakeSubmissionAppService = intakeSubmissionAppService;
            _iChefsEventSubscriptionService = iChefsEventSubscriptionService;
        }

        [HttpPost]
        public async Task<dynamic> PostEventSubscriptionAsync([FromBody] EventSubscriptionDto eventSubscriptionDto)
        {
            return eventSubscriptionDto.SubscriptionEvent switch
            {
                ChefsEventTypesConsts.FORM_SUBMITTED => await _intakeSubmissionAppService.CreateIntakeSubmissionAsync(eventSubscriptionDto),
                ChefsEventTypesConsts.FORM_PUBLISHED => await _iChefsEventSubscriptionService.PublishedFormAsync(eventSubscriptionDto),
                _ => await _intakeSubmissionAppService.CreateIntakeSubmissionAsync(eventSubscriptionDto),
            };
        }
    }
}
