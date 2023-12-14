using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Locality;

public interface IRegionalDistrictRepository  : IRepository<RegionalDistrict, Guid>
{
}

