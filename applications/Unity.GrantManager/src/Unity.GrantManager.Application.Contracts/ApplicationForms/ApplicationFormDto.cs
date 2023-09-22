using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.ApplicationForms
{
    public class ApplicationFormDto : EntityDto<Guid>
    {
        public Guid IntakeId { get; set; }
        public string ApplicationFormName { get; set; } = string.Empty;
        public string? ApplicationFormDescription { get; set; }
        public string? ChefsApplicationFormGuid { get; set; }
        public string? ChefsCriteriaFormGuid { get; set; }
        public string? ApiKey { get; set; }
    }
}
