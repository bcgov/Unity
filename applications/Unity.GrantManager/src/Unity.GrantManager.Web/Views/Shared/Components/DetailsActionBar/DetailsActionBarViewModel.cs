using System;

namespace Unity.GrantManager.Web.Views.Shared.Components.DetailsActionBar;

public class DetailsActionBarViewModel
{
    public Guid ApplicationId { get; set; }
    public bool ExternalStatusVisibility { get; set; }
    public bool CanUpdateExternalStatusVisibility { get; set; }
}
