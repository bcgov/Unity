using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class ApplicationSectorDto : EntityDto<Guid>
{
    public string SectorName { get; set; } = string.Empty;

    public string SectorCode { get; set; } = string.Empty;

    public List<ApplicationSubSectorDto>? SubSectors { get; set; }

}
