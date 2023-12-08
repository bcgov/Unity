using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Locality;

[Serializable]
public class SectorDto : EntityDto<Guid>
{
    public string SectorName { get; set; } = string.Empty;

    public string SectorCode { get; set; } = string.Empty;

    public List<SubSectorDto>? SubSectors { get; set; }

}
