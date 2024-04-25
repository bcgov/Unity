using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class ApplicationLinksDto : EntityDto<Guid>
{
    public Guid ApplicationId { get; set; }
    public Guid LinkedApplicationId { get; set; }

}
