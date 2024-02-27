using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicationContactAppService), typeof(IApplicationContactService))]
public class ApplicationContactAppService : ApplicationService, IApplicationContactService
{
    private readonly IApplicationContactRepository _applicationContactRepository;
    public ApplicationContactAppService(IApplicationContactRepository repository)
    {
        _applicationContactRepository = repository;
    }

    public async Task<IList<ApplicationContactDto>> GetListAsync()
    {        
        var contacts = await _applicationContactRepository.GetListAsync();

        return ObjectMapper.Map<List<ApplicationContact>, List<ApplicationContactDto>>(contacts.OrderBy(c => c.ContactType).ToList());
    }

    public async Task<ApplicationContactDto> CreateAsync(ApplicationContactDto input)
    {
        var newContact = await _applicationContactRepository.InsertAsync(ObjectMapper.Map<ApplicationContactDto, ApplicationContact>(input), autoSave: true);
        return ObjectMapper.Map<ApplicationContact, ApplicationContactDto>(newContact);
    }
}
