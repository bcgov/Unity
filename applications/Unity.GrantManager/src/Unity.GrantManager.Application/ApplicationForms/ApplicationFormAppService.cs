using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Integrations.Chefs;
using Unity.GrantManager.Permissions;
using Unity.Payments.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Encryption;

namespace Unity.GrantManager.ApplicationForms;

[Authorize]
public class ApplicationFormAppService(IRepository<ApplicationForm, Guid> repository,
    IStringEncryptionService stringEncryptionService,
    IApplicationFormVersionAppService applicationFormVersionAppService,
    IApplicationFormVersionRepository applicationFormVersionRepository,
    IGrantApplicationAppService applicationService,
    IFormsApiService formsApiService) :
CrudAppService<
    ApplicationForm,
    ApplicationFormDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateApplicationFormDto>(repository),
    IApplicationFormAppService
{
    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public override async Task<ApplicationFormDto> CreateAsync(CreateUpdateApplicationFormDto input)
    {
        input.ApiKey = stringEncryptionService.Encrypt(input.ApiKey);
        ApplicationFormDto applicationFormDto = await base.CreateAsync(input);
        return await InitializeFormVersion(applicationFormDto.Id, input);
    }

    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public override async Task<ApplicationFormDto> UpdateAsync(Guid id, CreateUpdateApplicationFormDto input)
    {
        var existingForm = await Repository.GetAsync(id);
        input.ApiKey = stringEncryptionService.Encrypt(input.ApiKey);

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

    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    private async Task<ApplicationFormDto> InitializeFormVersion(Guid id, CreateUpdateApplicationFormDto input)
    {
        var applicationFormDto = new ApplicationFormDto();
        try
        {
            if (input.ChefsApplicationFormGuid != null && input.ApiKey != null)
            {
                dynamic form = await formsApiService.GetForm(Guid.Parse(input.ChefsApplicationFormGuid), input.ChefsApplicationFormGuid.ToString(), input.ApiKey);
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
                    await applicationFormVersionAppService.InitializePublishedFormVersion(form, id, initializePublishedOnly);
                }
            }
            return applicationFormDto;
        }
        catch (Exception ex)
        {
            throw new UserFriendlyException("Exception: " + ex.Message + "\n\r Please check the CHEFS Form ID and CHEFS Form API Key");
        }

    }

    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public override async Task<ApplicationFormDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        dto.ApiKey = stringEncryptionService.Decrypt(dto.ApiKey);
        dto.ApiToken = stringEncryptionService.Decrypt(dto.ApiToken);
        return dto;
    }

    [Authorize]
    public async Task<AddressType> GetElectoralDistrictAddressTypeAsync(Guid id)
    {
        var applicationFormDto = await base.GetAsync(id);
        return applicationFormDto?.ElectoralDistrictAddressType ?? ApplicationForm.GetDefaultElectoralDistrictAddressType();
    }

    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public async Task<IList<ApplicationFormVersionDto>> GetPublishedVersionsAsync(Guid id)
    {
        IQueryable<ApplicationFormVersion> queryableFormVersions = applicationFormVersionRepository.GetQueryableAsync().Result;
        var formVersions = queryableFormVersions.Where(c => c.ApplicationFormId.Equals(id) && c.Published.Equals(true)).ToList();
        return await Task.FromResult<IList<ApplicationFormVersionDto>>(ObjectMapper.Map<List<ApplicationFormVersion>, List<ApplicationFormVersionDto>>([.. formVersions.OrderByDescending(s => s.Version)]));
    }

    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public async Task<IList<ApplicationFormVersionDto>> GetVersionsAsync(Guid id)
    {
        IQueryable<ApplicationFormVersion> queryableFormVersions = applicationFormVersionRepository.GetQueryableAsync().Result;
        var formVersions = queryableFormVersions.Where(c => c.ApplicationFormId.Equals(id)).ToList();
        return await Task.FromResult<IList<ApplicationFormVersionDto>>(ObjectMapper.Map<List<ApplicationFormVersion>, List<ApplicationFormVersionDto>>([.. formVersions.OrderByDescending(s => s.Version)]));
    }

    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public async Task SaveApplicationFormScoresheet(FormScoresheetDto dto)
    {
        var appForm = await repository.GetAsync(dto.ApplicationFormId);
        appForm.ScoresheetId = dto.ScoresheetId;
        await repository.UpdateAsync(appForm);
    }

    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public async Task PatchOtherConfig(Guid id, OtherConfigDto config)
    {
        var form = await repository.GetAsync(id);

        form.IsDirectApproval = config.IsDirectApproval;
        form.ElectoralDistrictAddressType = config.ElectoralDistrictAddressType;

        await repository.UpdateAsync(form);
    }

    [Authorize(PaymentsPermissions.Payments.EditFormPaymentConfiguration)]
    public async Task<bool> GetFormPreventPaymentStatusByApplicationId(Guid applicationId)
    {
        // Get the payment threshold for the application
        GrantApplicationDto grantApplicationDto = await applicationService.GetAsync(applicationId);
        Guid formId = grantApplicationDto.ApplicationForm.Id;
        ApplicationForm appForm = await repository.GetAsync(formId);     
        return appForm.PreventPayment;
    }

    public async Task SavePaymentConfiguration(FormPaymentConfigurationDto dto)
    {
        ApplicationForm appForm = await repository.GetAsync(dto.ApplicationFormId);
        appForm.AccountCodingId = dto.AccountCodingId;
        appForm.Payable = dto.Payable;
        appForm.PreventPayment = dto.PreventPayment;
        appForm.PaymentApprovalThreshold = dto.PaymentApprovalThreshold;
        await repository.UpdateAsync(appForm);
    }
    
    public async Task<decimal?> GetFormPaymentApprovalThresholdByApplicationIdAsync(Guid applicationId)
    {
        // Get the payment threshold for the application
        GrantApplicationDto application = await applicationService.GetAsync(applicationId);
        Guid formId = application.ApplicationForm.Id;
        ApplicationForm appForm = await repository.GetAsync(formId);     
        return appForm.PaymentApprovalThreshold;
    }
}
