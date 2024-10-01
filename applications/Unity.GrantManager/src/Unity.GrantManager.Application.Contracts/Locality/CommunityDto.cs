using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Locality
{
    [Serializable]
    public class CommunityDto : EntityDto<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string RegionalDistrictCode { get; set; } = string.Empty;
    }
}
