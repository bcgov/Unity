using Unity.GrantManager.Applications;

namespace Unity.GrantManager.Locality.BackgroundJobs
{
    public class DetermineElectoralRetrospectivelyEvent
    {
        public Application? Application { get; internal set; }
        public ApplicationFormVersion? FormVersion { get; internal set; }
    }
}
