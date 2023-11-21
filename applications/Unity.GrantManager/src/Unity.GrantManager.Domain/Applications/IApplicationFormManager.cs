using System.Threading.Tasks;
using Unity.GrantManager.Events;

namespace Unity.GrantManager.Applications
{
    public interface IApplicationFormManager
    {
        public Task<ApplicationForm> InitializeApplicationForm(EventSubscription eventSubscription);

        public ApplicationForm SynchronizePublishedForm(ApplicationForm applicationForm, dynamic formVersion, dynamic form);
    }
}