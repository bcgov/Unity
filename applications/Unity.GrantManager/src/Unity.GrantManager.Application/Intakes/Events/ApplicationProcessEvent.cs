using Unity.GrantManager.Applications;

namespace Unity.GrantManager.Intakes.Events
{
    public class ApplicationProcessEvent
    {
        public Application? Application { get; set; }
        public ApplicationFormVersion? FormVersion { get; internal set; }
        public ApplicationFormSubmission? ApplicationFormSubmission { get; internal set; }
        public dynamic? RawSubmission { get; internal set; }

        // As this expands, turn this into a flags enum for control over which event handlers to run
        public bool? OnlyLocationRetrofill { get; set; } = false;
    }
}
