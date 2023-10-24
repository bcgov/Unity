using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Applications;
public class ApplicationActionResultItem
{
    public GrantApplicationAction ApplicationAction { get; set; }
    public bool IsPermitted { get; set; }
    public bool IsInternal { get; set; } = false;
}
