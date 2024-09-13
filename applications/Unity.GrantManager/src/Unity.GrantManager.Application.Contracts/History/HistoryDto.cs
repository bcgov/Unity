using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.History
{
    public class HistoryDto : EntityDto<Guid>
    {
        public string OriginalValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string PropertyTypeFullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;        
        public DateTime ChangeTime { get; set; }
        public Guid UserId { get; set; }
    }
}
