using System;
using System.Collections.Generic;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationTags  : AuditedAggregateRoot<Guid>
{
   
    public Guid? ApplicationId { get; set; } 
    public string Text { get; set; } = string.Empty;


}