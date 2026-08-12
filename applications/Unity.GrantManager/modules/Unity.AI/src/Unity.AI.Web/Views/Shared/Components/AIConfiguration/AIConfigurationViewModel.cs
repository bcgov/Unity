using System;

namespace Unity.AI.Web.Views.Shared.Components.AIConfiguration;

public class AIConfigurationViewModel
{
    public Guid ApplicationFormId { get; set; }

    public bool ShowAutomatic { get; set; }

    public bool ShowManual { get; set; }

    public bool AutomaticallyGenerateAIAnalysis { get; set; }

    public bool ManuallyInitiateAIAnalysis { get; set; }
}
