using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Locale;

public interface IElectoralDistrictRepository : IRepository<ElectoralDistrict, Guid>
{
}

