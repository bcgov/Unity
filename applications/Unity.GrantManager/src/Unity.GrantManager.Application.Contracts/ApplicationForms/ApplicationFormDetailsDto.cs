using System;

namespace Unity.GrantManager.ApplicationForms;

public class ApplicationFormDetailsDto
{
    public Guid ApplicationId { get; set; }
    public Guid ApplicationFormId { get; set; }
    public string ApplicationFormName { get; set; } = string.Empty;
    public string ApplicationFormDescription { get; set; } = string.Empty;
    public string ApplicationFormCategory { get; set; } = string.Empty;
    public Guid ApplicationFormVersionId { get; set; }
    public int ApplicationFormVersion { get; set; }
}
