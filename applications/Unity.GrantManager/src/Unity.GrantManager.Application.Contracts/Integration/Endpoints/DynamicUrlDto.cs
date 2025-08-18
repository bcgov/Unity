using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Integrations
{
    public class DynamicUrlDto : AuditedEntityDto<Guid>
    {
        public string KeyName { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}
