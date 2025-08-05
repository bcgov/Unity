using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Notifications.EmailGroups
{
    public class EmailGroupDto :EntityDto<Guid>
    {
        
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

    }
}
