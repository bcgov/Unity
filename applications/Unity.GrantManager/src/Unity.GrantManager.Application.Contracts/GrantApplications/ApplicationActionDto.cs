using System;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class ApplicationActionDto
{
    public GrantApplicationAction ApplicationAction { get; set; }
    public bool IsPermitted { get; set; }
    public bool IsAuthorized { get; set; } = false;
}
