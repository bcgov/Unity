using Microsoft.AspNetCore.Authorization;
using System;
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

    public async Task<List<ApplicationContactDto>> GetListAsync(Guid applicationId)
    {
        var contacts = await _applicationContactRepository.GetListAsync(c => c.ApplicationId == applicationId);

        return ObjectMapper.Map<List<ApplicationContact>, List<ApplicationContactDto>>(contacts.OrderBy(c => c.ContactType).ToList());
    }

    public async Task<ApplicationContactDto> GetAsync(Guid id)
    {
        var contact = await _applicationContactRepository.GetAsync(id);
        return ObjectMapper.Map<ApplicationContact, ApplicationContactDto>(contact);
    }

    public async Task<ApplicationContactDto> CreateAsync(ApplicationContactDto input)
    {
        var newContact = await _applicationContactRepository.InsertAsync(ObjectMapper.Map<ApplicationContactDto, ApplicationContact>(input), autoSave: true);
        return ObjectMapper.Map<ApplicationContact, ApplicationContactDto>(newContact);
    }

    public async Task<ApplicationContactDto> UpdateAsync(ApplicationContactDto input)
    {
        var newContact = await _applicationContactRepository.UpdateAsync(ObjectMapper.Map<ApplicationContactDto, ApplicationContact>(input), autoSave: true);
        return ObjectMapper.Map<ApplicationContact, ApplicationContactDto>(newContact);
    }
}
