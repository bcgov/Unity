using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Domain.AccountCodings;

public interface IAccountCodingRepository : IRepository<AccountCoding, Guid>
{

}
