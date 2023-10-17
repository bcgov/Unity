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
            switch (eventSubscriptionDto.SubscriptionEvent) 
            {
                case ChefsEventTypesConsts.FORM_SUBMITTED:
                    return await _intakeSubmissionAppService.CreateIntakeSubmissionAsync(eventSubscriptionDto);
                case  ChefsEventTypesConsts.FORM_PUBLISHED:
                    return await _iChefsEventSubscriptionService.CreateIntakeMappingAsync(eventSubscriptionDto);
                default:
                    return await _intakeSubmissionAppService.CreateIntakeSubmissionAsync(eventSubscriptionDto);
            }
        }
    }
}
