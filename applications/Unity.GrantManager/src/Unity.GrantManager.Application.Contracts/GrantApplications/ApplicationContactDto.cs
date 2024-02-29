using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications
{
    public class ApplicationContactDto : EntityDto<Guid>
    {
        public Guid ApplicationId { get; set; }
        public string ContactType { get; set; } = string.Empty;
        public string ContactFullName { get; set; } = string.Empty;
        public string? ContactTitle { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactMobilePhone { get; set; }
        public string? ContactWorkPhone { get; set; }
    }
}
