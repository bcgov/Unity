using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications
{
    public class GrantApplicationDto : AuditedEntityDto<Guid>
    {
        public string Name { get; set; }
    }
}
