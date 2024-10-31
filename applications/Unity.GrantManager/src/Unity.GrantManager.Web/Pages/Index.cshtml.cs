using System;

namespace Unity.GrantManager.Web.Pages;

public class IndexModel : GrantManagerPageModel
{
    public Guid InstanceId { get; set; } = StartupUtils.InstanceId;
    public DateTime StartupTime { get; set; } = StartupUtils.StartupTime;

    public void OnGet()
    {
        //Placeholder. Nothing to do here yet.
    }
}
