using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class ApplicationTagsDto : EntityDto<Guid>
{
    public Guid ApplicationId { get; set; }
    public string Text { get; set; } = string.Empty;

}
