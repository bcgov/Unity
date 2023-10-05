
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Events
{
    public interface IChefsEventSubscriptionService : IApplicationService
    {
        Task<EventSubscriptionConfirmationDto> CreateIntakeMappingAsync(EventSubscriptionDto intakeSubmission);
    }
}