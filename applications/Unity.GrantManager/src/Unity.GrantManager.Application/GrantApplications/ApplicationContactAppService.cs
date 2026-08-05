using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
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
        GetPolicyName = GrantApplicationPermissions.ApplicantInfo.Read;
        CreatePolicyName = GrantApplicationPermissions.ApplicantInfo.AddAdditionalContact;
        UpdatePolicyName = GrantApplicationPermissions.ApplicantInfo.UpdateAdditionalContact;
        DeletePolicyName = GrantApplicationPermissions.ApplicantInfo.DeleteAdditionalContact;
    }

    // The widget lists contacts via GetListByApplicationAsync
    [RemoteService(false)]
    public override Task<PagedResultDto<ApplicationContactDto>> GetListAsync(PagedAndSortedResultRequestDto input) => base.GetListAsync(input);

    [Authorize(GrantApplicationPermissions.ApplicantInfo.Read)]
    public async Task<List<ApplicationContactDto>> GetListByApplicationAsync(Guid applicationId)
    {
        var contacts = await _applicationContactRepository.GetListAsync(c => c.ApplicationId == applicationId);

        return ObjectMapper.Map<List<ApplicationContact>, List<ApplicationContactDto>>(contacts.OrderBy(c => c.ContactType).ToList());
    }
}
