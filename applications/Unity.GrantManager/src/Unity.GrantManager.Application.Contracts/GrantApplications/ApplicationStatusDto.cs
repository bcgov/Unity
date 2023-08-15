using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class ApplicationStatusDto : EntityDto<Guid>
{
    public string ExternalStatus { get; set; }

    public string InternalStatus { get; set; }

    public string StatusCode { get; set; }
}
