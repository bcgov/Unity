using System;
using Unity.GrantManager.GlobalTag;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class ApplicationTagsDto : EntityDto<Guid>
{

    public Guid ApplicationId { get; set; }

    public  TagDto? Tag  { get; set; }

}
