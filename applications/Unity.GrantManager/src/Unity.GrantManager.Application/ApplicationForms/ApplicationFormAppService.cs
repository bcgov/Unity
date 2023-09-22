using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Encryption;

namespace Unity.GrantManager.ApplicationForms
{    
    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public class ApplicationFormAppService :
    CrudAppService<
        ApplicationForm,
        ApplicationFormDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateApplicationFormDto>,
        IApplicationFormAppService
    {
        private readonly IStringEncryptionService _stringEncryptionService;

        public ApplicationFormAppService(IRepository<ApplicationForm, Guid> repository, 
            IStringEncryptionService stringEncryptionService)
            : base(repository)
        {
            _stringEncryptionService = stringEncryptionService;
        }

        public override async Task<ApplicationFormDto> CreateAsync(CreateUpdateApplicationFormDto input)
        {
            input.ApiKey = _stringEncryptionService.Encrypt(input.ApiKey);
            return await base.CreateAsync(input);
        }

        public override async Task<ApplicationFormDto> UpdateAsync(Guid id, CreateUpdateApplicationFormDto input)
        {
            input.ApiKey = _stringEncryptionService.Encrypt(input.ApiKey);
            return await  base.UpdateAsync(id, input);
        }

        public override async Task<ApplicationFormDto> GetAsync(Guid id)
        {            
            var dto = await base.GetAsync(id);
            dto.ApiKey = _stringEncryptionService.Decrypt(dto.ApiKey);
            return dto;
        }
    }
}
