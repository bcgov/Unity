using System;
using System.Collections.Generic;
using Unity.GrantManager.GlobalTag;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class AssignApplicationTagsDto : EntityDto<Guid>
{

    public Guid ApplicationId { get; set; }

    public List<TagDto>? Tags  { get; set; }

}
