using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Intakes;

public interface IIntakeRepository : IRepository<Intake, Guid>
{
}
