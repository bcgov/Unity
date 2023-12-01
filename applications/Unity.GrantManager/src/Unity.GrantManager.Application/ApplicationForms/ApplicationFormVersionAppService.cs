using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.ApplicationForms
{    
    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public class ApplicationFormVersionAppService :
    CrudAppService<
        ApplicationFormVersion,
        ApplicationFormVersionDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateApplicationFormVersionDto>,
        IApplicationFormVersionAppService
    {

        private IApplicationFormVersionRepository _applicationFormVersionRepository;

        public ApplicationFormVersionAppService(IRepository<ApplicationFormVersion, Guid> repository, 
            IApplicationFormVersionRepository applicationFormVersionRepository)
            : base(repository)
        {
            _applicationFormVersionRepository = applicationFormVersionRepository;
        }

        public override async Task<ApplicationFormVersionDto> CreateAsync(CreateUpdateApplicationFormVersionDto input)
        {
            return await base.CreateAsync(input);
        }

        public override async Task<ApplicationFormVersionDto> UpdateAsync(Guid id, CreateUpdateApplicationFormVersionDto input)
        {
            return await  base.UpdateAsync(id, input);
        }

        public override async Task<ApplicationFormVersionDto> GetAsync(Guid id)
        {            
            var dto = await base.GetAsync(id);
            return dto;
        }

        public async Task<IList<ApplicationFormVersionDto>> GetListAsync(Guid applicationFormId)
        {
            IQueryable<ApplicationFormVersion> queryableFormVersions = _applicationFormVersionRepository.GetQueryableAsync().Result;
            var formVersions = queryableFormVersions.Where(c => c.ApplicationFormId.Equals(applicationFormId)).ToList();
            return await Task.FromResult<IList<ApplicationFormVersionDto>>(ObjectMapper.Map<List<ApplicationFormVersion>, List<ApplicationFormVersionDto>>(formVersions.OrderByDescending(s => s.Version).ToList()));
        }
    }
}
