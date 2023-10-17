using System.Threading.Tasks;
using Unity.GrantManager.Events;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Intakes
{
    public interface IIntakeSubmissionAppService : IApplicationService
    {
        Task<EventSubscriptionConfirmationDto> CreateIntakeSubmissionAsync(EventSubscriptionDto eventSubscriptionDto);
    }
}
