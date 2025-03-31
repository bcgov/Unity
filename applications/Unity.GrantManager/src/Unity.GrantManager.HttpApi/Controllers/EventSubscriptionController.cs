using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.Events;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.TenantManagement;
using Unity.GrantManager.Controllers.Auth.FormSubmission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Controllers
{
    [ApiController]
    [Route("api/chefs/event")]
    [AllowAnonymous]
    public class EventSubscriptionController(IIntakeSubmissionAppService intakeSubmissionAppService,
                                       IChefsEventSubscriptionService iChefsEventSubscriptionService,
                                       ITenantRepository tenantRepository,
                                       IBackgroundJobManager backgroundJobManager,
                                       ICurrentTenant currentTenant) : AbpControllerBase
    {
        [HttpPost]
        [ServiceFilter(typeof(FormsApiTokenAuthFilter))]
        public async Task<dynamic> PostEventSubscriptionAsync([FromBody] EventSubscription eventSubscription)
        {
            if (CurrentTenant.Id == null)
            {
                var defaultTenant = await tenantRepository.FindByNameAsync(GrantManagerConsts.NormalizedDefaultTenantName);
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

            Logger.LogInformation("Handling Intake Event Of Type: {Type}", eventSubscription.SubscriptionEvent?.ToString() ?? "UNDEFINED");

            if (eventSubscription.SubscriptionEvent == ChefsEventTypesConsts.FORM_SUBMITTED)
            {
                var confirmationId = Guid.NewGuid();

                var args = new IntakeSubmissionBackgroundJobArgs
                {
                    EventSubscriptionDto = eventSubscriptionDto,
                    TenantId = currentTenant.Id,
                    ConfirmationId = confirmationId
                };

                await backgroundJobManager.EnqueueAsync(args);

                // Return a response immediately
                return new EventSubscriptionConfirmationDto() { ConfirmationId = confirmationId, ExceptionMessage = "Processing.." };
            }

            // Not specifcifiying the event type will default to creating an intake submission
            // This processing type will also be inline and not queued

            return eventSubscription.SubscriptionEvent switch
            {
                ChefsEventTypesConsts.FORM_PUBLISHED => await iChefsEventSubscriptionService.PublishedFormAsync(eventSubscriptionDto),
                ChefsEventTypesConsts.FORM_DRAFT_PUBLISHED => await iChefsEventSubscriptionService.PublishedFormAsync(eventSubscriptionDto),
                _ => await intakeSubmissionAppService.CreateIntakeSubmissionAsync(eventSubscriptionDto),
            };
        }
    }
}
