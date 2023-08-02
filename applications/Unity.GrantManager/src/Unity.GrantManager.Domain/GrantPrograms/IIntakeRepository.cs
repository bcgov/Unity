using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.GrantPrograms;

public interface IIntakeRepository : IBasicRepository<Intake, Guid>
{
}
