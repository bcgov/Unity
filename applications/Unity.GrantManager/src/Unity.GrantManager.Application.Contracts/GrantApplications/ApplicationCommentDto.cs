using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications
{
    public class ApplicationCommentDto : EntityDto<Guid>
    {
        public string Comment { get; set; } = string.Empty;
    }
}
