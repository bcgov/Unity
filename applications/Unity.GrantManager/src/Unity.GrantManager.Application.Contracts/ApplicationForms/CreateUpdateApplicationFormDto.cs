using System;

namespace Unity.GrantManager.Forms
{
    public class CreateUpdateApplicationFormDto
    {
        public Guid IntakeId { get; set; }
        public string ApplicationFormName { get; set; } = string.Empty;
        public string? ApplicationFormDescription { get; set; }
        public string? ChefsApplicationFormGuid { get; set; }
        public string? ChefsCriteriaFormGuid { get; set; }
        public string? ApiKey { get; set; }
    }
}
