using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicationContactAppService), typeof(IApplicationContactService))]
public class ApplicationContactAppService : CrudAppService<
            ApplicationContact,
            ApplicationContactDto,
            Guid>, IApplicationContactService
{
    private readonly IApplicationContactRepository _applicationContactRepository;
    public ApplicationContactAppService(IRepository<ApplicationContact, Guid> repository,
        IApplicationContactRepository applicationContactRepository) : base(repository)
    {
        _applicationContactRepository = applicationContactRepository;
    }
    
    public async Task<List<ApplicationContactDto>> GetListByApplicationAsync(Guid applicationId)
    {
        var contacts = await _applicationContactRepository.GetListAsync(c => c.ApplicationId == applicationId);

        return ObjectMapper.Map<List<ApplicationContact>, List<ApplicationContactDto>>(contacts.OrderBy(c => c.ContactType).ToList());
    }
}
