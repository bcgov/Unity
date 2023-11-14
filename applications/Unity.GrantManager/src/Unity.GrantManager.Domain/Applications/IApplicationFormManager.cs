using System.Threading.Tasks;
using Unity.GrantManager.Events;

namespace Unity.GrantManager.Applications
{
    public interface IApplicationFormManager
    {
        public Task<ApplicationForm> InitializeApplicationForm(EventSubscriptionDto eventSubscriptionDto);

        public Task<ApplicationForm> SynchronizePublishedForm(ApplicationForm applicationForm, dynamic formVersion);
    }
}