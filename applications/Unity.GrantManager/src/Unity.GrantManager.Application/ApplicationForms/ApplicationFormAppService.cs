using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Integration.Chefs;
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
        private readonly IFormsApiService _formsApiService;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
        private readonly IApplicationFormVersionRepository _applicationFormVersionRepository;
        private readonly IRepository<ApplicationForm, Guid> _applicationFormRepository;
        public ApplicationFormAppService(IRepository<ApplicationForm, Guid> repository,
            IStringEncryptionService stringEncryptionService,
            IApplicationFormVersionAppService applicationFormVersionAppService,
            IApplicationFormVersionRepository applicationFormVersionRepository,
            IFormsApiService formsApiService)
            : base(repository)
        {
            _stringEncryptionService = stringEncryptionService;
            _applicationFormVersionAppService = applicationFormVersionAppService;
            _formsApiService = formsApiService;
            _applicationFormVersionRepository = applicationFormVersionRepository;
            _applicationFormRepository = repository;
        }

        public override async Task<ApplicationFormDto> CreateAsync(CreateUpdateApplicationFormDto input)
        {
            input.ApiKey = _stringEncryptionService.Encrypt(input.ApiKey);
            ApplicationFormDto applicationFormDto = await base.CreateAsync(input);
            return await InitializeFormVersion(applicationFormDto.Id, input);
        }

        public override async Task<ApplicationFormDto> UpdateAsync(Guid id, CreateUpdateApplicationFormDto input)
        {
            var existingForm = await Repository.GetAsync(id);
            input.ApiKey = _stringEncryptionService.Encrypt(input.ApiKey);

            bool hasFormGuidChanged = existingForm.ChefsApplicationFormGuid != input.ChefsApplicationFormGuid;
            bool hasFormApiKeyChanged = existingForm.ApiKey != input.ApiKey;

            // Only initialize form version if changes are made to form connection details
            if (hasFormGuidChanged || hasFormApiKeyChanged)
            {
                return await InitializeFormVersion(id, input);
            }
            else
            {
                return await base.UpdateAsync(id, input);
            }
        }

        private async Task<ApplicationFormDto> InitializeFormVersion(Guid id, CreateUpdateApplicationFormDto input)
        {
            var applicationFormDto = new ApplicationFormDto();
            try
            {
                if (input.ChefsApplicationFormGuid != null && input.ApiKey != null)
                {
                    dynamic form = await _formsApiService.GetForm(Guid.Parse(input.ChefsApplicationFormGuid), input.ChefsApplicationFormGuid.ToString(), input.ApiKey);
                    if (form != null)
                    {
                        JObject formObject = JObject.Parse(form.ToString());
                        var formName = formObject.SelectToken("name");
                        if (formName != null)
                        {
                            input.ApplicationFormName = formName.ToString();
                            applicationFormDto = await base.UpdateAsync(id, input);
                        }
                        bool initializePublishedOnly = false;
                        await _applicationFormVersionAppService.InitializePublishedFormVersion(form, id, initializePublishedOnly);
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
            dto.ApiToken = _stringEncryptionService.Decrypt(dto.ApiToken);
            return dto;
        }

        public async Task<IList<ApplicationFormVersionDto>> GetPublishedVersionsAsync(Guid id)
        {
            IQueryable<ApplicationFormVersion> queryableFormVersions = _applicationFormVersionRepository.GetQueryableAsync().Result;
            var formVersions = queryableFormVersions.Where(c => c.ApplicationFormId.Equals(id) && c.Published.Equals(true)).ToList();
            return await Task.FromResult<IList<ApplicationFormVersionDto>>(ObjectMapper.Map<List<ApplicationFormVersion>, List<ApplicationFormVersionDto>>(formVersions.OrderByDescending(s => s.Version).ToList()));
        }

        public async Task<IList<ApplicationFormVersionDto>> GetVersionsAsync(Guid id)
        {
            IQueryable<ApplicationFormVersion> queryableFormVersions = _applicationFormVersionRepository.GetQueryableAsync().Result;
            var formVersions = queryableFormVersions.Where(c => c.ApplicationFormId.Equals(id)).ToList();
            return await Task.FromResult<IList<ApplicationFormVersionDto>>(ObjectMapper.Map<List<ApplicationFormVersion>, List<ApplicationFormVersionDto>>(formVersions.OrderByDescending(s => s.Version).ToList()));
        }

        public async Task SaveApplicationFormScoresheet(FormScoresheetDto dto)
        {
            var appForm = await _applicationFormRepository.GetAsync(dto.ApplicationFormId);
            appForm.ScoresheetId = dto.ScoresheetId;
            await _applicationFormRepository.UpdateAsync(appForm);
        }

        public  async Task<ApplicationFormDto> UpdateOtherConfig(Guid id, OtherConfigDto config)
        {
            var appForm = await _applicationFormRepository.GetAsync(id);

            appForm.IsDirectApproval = config.IsDirectApproval;
            appForm.ElectoralDistrictAddressType = config.ElectoralDistrictAddressType;

            await _applicationFormRepository.UpdateAsync(appForm);
            return ObjectMapper.Map<ApplicationForm, ApplicationFormDto>(appForm);
        }
    }
}
