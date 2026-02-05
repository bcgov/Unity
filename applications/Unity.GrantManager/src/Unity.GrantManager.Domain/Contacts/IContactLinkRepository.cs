using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Contacts;

public interface IContactLinkRepository : IRepository<ContactLink, Guid>
{
}
