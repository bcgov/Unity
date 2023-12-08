using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Locality;

[Serializable]
public class SubSectorDto : EntityDto<Guid>
{
    public string SubSectorName { get; set; } = string.Empty;

    public string SubSectorCode { get; set; } = string.Empty;

    public Guid SectorId { get; set; }

}
