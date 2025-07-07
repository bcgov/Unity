using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GlobalTag;

[Serializable]
public class TagDto  : EntityDto<Guid>
{
   
    public string Name { get; set; } = string.Empty;

}
