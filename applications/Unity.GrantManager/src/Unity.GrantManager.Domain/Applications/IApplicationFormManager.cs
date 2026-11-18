namespace Unity.GrantManager.Applications
{
    public interface IApplicationFormManager
    {
        public ApplicationForm SynchronizePublishedForm(ApplicationForm applicationForm, dynamic formVersion, dynamic form);
    }
}
