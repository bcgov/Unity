using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes.Integration;
using Unity.GrantManager.Permissions;
using Volo.Abp;
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
        private readonly IFormIntService _formIntService;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;

        public ApplicationFormAppService(IRepository<ApplicationForm, Guid> repository,
            IStringEncryptionService stringEncryptionService,
            IApplicationFormVersionAppService applicationFormVersionAppService,
            IFormIntService formIntService)
            : base(repository)
        {
            _stringEncryptionService = stringEncryptionService;
            _applicationFormVersionAppService = applicationFormVersionAppService;
            _formIntService = formIntService;
        }

        public override async Task<ApplicationFormDto> CreateAsync(CreateUpdateApplicationFormDto input)
        {
            input.ApiKey = _stringEncryptionService.Encrypt(input.ApiKey);
            ApplicationFormDto applicationFormDto = await base.CreateAsync(input);            
            return await InitializeFormVersion(applicationFormDto.Id, input);
        }

        public override async Task<ApplicationFormDto> UpdateAsync(Guid id, CreateUpdateApplicationFormDto input)
        {
            input.ApiKey = _stringEncryptionService.Encrypt(input.ApiKey);                        
            return await InitializeFormVersion(id, input); 
        }

        private async Task<ApplicationFormDto> InitializeFormVersion(Guid id, CreateUpdateApplicationFormDto input)
        {
            var applicationFormDto = new ApplicationFormDto();
            try
            {
                if (input.ChefsApplicationFormGuid != null && input.ApiKey != null)
                {
                    dynamic form = await _formIntService.GetForm(Guid.Parse(input.ChefsApplicationFormGuid), input.ChefsApplicationFormGuid.ToString(), input.ApiKey);
                    if (form != null)
                    {
                        JObject formObject = JObject.Parse(form.ToString());
                        var formName = formObject.SelectToken("name");
                        if (formName != null)
                        {
                            input.ApplicationFormName = formName.ToString();
                            applicationFormDto = await base.UpdateAsync(id, input);
                        }

                        await _applicationFormVersionAppService.InitializePublishedFormVersion(form, id);
                    }
                }
                return applicationFormDto;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException("Exception: " + ex.Message + "\n\r Please check the CHEFS Form ID and CHEFS Form API Key");
            }

        }

        public override async Task<ApplicationFormDto> GetAsync(Guid id)
        {
            var dto = await base.GetAsync(id);
            dto.ApiKey = _stringEncryptionService.Decrypt(dto.ApiKey);
            return dto;
        }
    }
}
