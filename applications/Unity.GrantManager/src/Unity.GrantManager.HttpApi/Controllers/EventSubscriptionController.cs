using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.Events;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.TenantManagement;
using Unity.GrantManager.Controllers.Auth.FormSubmission;
using Microsoft.AspNetCore.Authorization;

namespace Unity.GrantManager.Controllers
{
    [ApiController]
    [Route("api/chefs/event")]
    [AllowAnonymous]
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
        [ServiceFilter(typeof(FormsApiTokenAuthFilter))]
        public async Task<dynamic> PostEventSubscriptionAsync([FromBody] EventSubscription eventSubscription)
        {
            if (CurrentTenant.Id == null)
            {
                var defaultTenant = await _tenantRepository.FindByNameAsync(GrantManagerConsts.NormalizedDefaultTenantName);
                using (CurrentTenant.Change(defaultTenant.Id, defaultTenant.Name))
                {
                    return await HandleIntakeEventAsync(eventSubscription);
                }
            }
            else return await HandleIntakeEventAsync(eventSubscription);
        }

        [HttpPost]
        [Route("{__tenant}")]
        [ServiceFilter(typeof(FormsApiTokenAuthFilter))]
        public async Task<dynamic> PostEventSubscriptionTenantAsync([FromBody] EventSubscription eventSubscription)
        {
            return await HandleIntakeEventAsync(eventSubscription);
        }

        private async Task<dynamic> HandleIntakeEventAsync(EventSubscription eventSubscription)
        {
            EventSubscriptionDto eventSubscriptionDto = ObjectMapper.Map<EventSubscription, EventSubscriptionDto>(eventSubscription);
            return eventSubscription.SubscriptionEvent switch
            {
                ChefsEventTypesConsts.FORM_SUBMITTED => await _intakeSubmissionAppService.CreateIntakeSubmissionAsync(eventSubscriptionDto),
                ChefsEventTypesConsts.FORM_PUBLISHED => await _iChefsEventSubscriptionService.PublishedFormAsync(eventSubscriptionDto),
                ChefsEventTypesConsts.FORM_DRAFT_PUBLISHED => await _iChefsEventSubscriptionService.PublishedFormAsync(eventSubscriptionDto),
                _ => await _intakeSubmissionAppService.CreateIntakeSubmissionAsync(eventSubscriptionDto),
            };
        }
    }
}
