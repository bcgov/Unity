using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.Events;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Controllers
{
    [ApiController]
    [Route("api/chefs/event")]
    public class EventSubscriptionController : AbpControllerBase
    {
        private readonly IIntakeSubmissionAppService _intakeSubmissionAppService;
        private readonly IChefsEventSubscriptionService _iChefsEventSubscriptionService;        
        private readonly ITenantRepository _tenantRepository;

        public EventSubscriptionController(IIntakeSubmissionAppService intakeSubmissionAppService, 
                                           IChefsEventSubscriptionService iChefsEventSubscriptionService,
                                           ITenantRepository tenantRepository)
        {
            _intakeSubmissionAppService = intakeSubmissionAppService;
            _iChefsEventSubscriptionService = iChefsEventSubscriptionService;
            _tenantRepository = tenantRepository; 
        }

        [HttpPost]
        public async Task<dynamic> PostEventSubscriptionAsync([FromBody] EventSubscription eventSubscription)
        {
            var defaultTenant = await _tenantRepository.FindByNameAsync("Default");

            using (CurrentTenant.Change(defaultTenant.Id, defaultTenant.Name))
            {                
                EventSubscriptionDto eventSubscriptionDto = ObjectMapper.Map<EventSubscription, EventSubscriptionDto>(eventSubscription);
                return eventSubscription.SubscriptionEvent switch
                {
                    ChefsEventTypesConsts.FORM_SUBMITTED => await _intakeSubmissionAppService.CreateIntakeSubmissionAsync(eventSubscriptionDto),
                    ChefsEventTypesConsts.FORM_PUBLISHED => await _iChefsEventSubscriptionService.PublishedFormAsync(eventSubscriptionDto),
                    _ => await _intakeSubmissionAppService.CreateIntakeSubmissionAsync(eventSubscriptionDto),
                };
            }
        }
    }
}
