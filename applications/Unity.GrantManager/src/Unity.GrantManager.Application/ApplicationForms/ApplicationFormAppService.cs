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
public class ApplicationFormAppService 
    : CrudAppService<
        ApplicationForm,
        ApplicationFormDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateApplicationFormDto>,
      IApplicationFormAppService
{
    private readonly IStringEncryptionService _stringEncryptionService;
    private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
    private readonly IApplicationFormVersionRepository _applicationFormVersionRepository;
    private readonly IGrantApplicationAppService _applicationService;
    private readonly IFormsApiService _formsApiService;
    private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;

    public ApplicationFormAppService(
        IRepository<ApplicationForm, Guid> repository,
        IStringEncryptionService stringEncryptionService,
        IApplicationFormVersionAppService applicationFormVersionAppService,
        IApplicationFormVersionRepository applicationFormVersionRepository,
        IGrantApplicationAppService applicationService,
        IFormsApiService formsApiService,
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository)
        : base(repository)
    {
        _stringEncryptionService = stringEncryptionService;
        _applicationFormVersionAppService = applicationFormVersionAppService;
        _applicationFormVersionRepository = applicationFormVersionRepository;
        _applicationService = applicationService;
        _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
        _formsApiService = formsApiService;
    }

    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public override async Task<ApplicationFormDto> CreateAsync(CreateUpdateApplicationFormDto input)
    {
        input.ApiKey = _stringEncryptionService.Encrypt(input.ApiKey);
        var applicationFormDto = await base.CreateAsync(input);
        return await InitializeFormVersion(applicationFormDto.Id, input);
    }

    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public override async Task<ApplicationFormDto> UpdateAsync(Guid id, CreateUpdateApplicationFormDto input)
    {
        var existingForm = await Repository.GetAsync(id);
        input.ApiKey = _stringEncryptionService.Encrypt(input.ApiKey);

        bool hasFormGuidChanged = existingForm.ChefsApplicationFormGuid != input.ChefsApplicationFormGuid;
        bool hasFormApiKeyChanged = existingForm.ApiKey != input.ApiKey;

        if (hasFormGuidChanged || hasFormApiKeyChanged)
        {
            return await InitializeFormVersion(id, input);
        }

        return await base.UpdateAsync(id, input);
    }

    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    private async Task<ApplicationFormDto> InitializeFormVersion(Guid id, CreateUpdateApplicationFormDto input)
    {
        var applicationFormDto = new ApplicationFormDto();

        try
        {
            if (!string.IsNullOrWhiteSpace(input.ChefsApplicationFormGuid) && !string.IsNullOrWhiteSpace(input.ApiKey))
            {
                var form = await _formsApiService.GetForm(
                    Guid.Parse(input.ChefsApplicationFormGuid),
                    input.ChefsApplicationFormGuid,
                    input.ApiKey);

                if (form is JObject formObject)
                {
                    var formName = formObject.SelectToken("name")?.ToString();
                    if (!string.IsNullOrWhiteSpace(formName))
                    {
                        input.ApplicationFormName = formName;
                        applicationFormDto = await base.UpdateAsync(id, input);
                    }

                    await _applicationFormVersionAppService.InitializePublishedFormVersion(formObject, id, initializePublishedOnly: false);
                }
            }

            return applicationFormDto;
        }
        catch (Exception ex)
        {
            throw new UserFriendlyException(
                "Error initializing CHEFS form. Please check the CHEFS: Form ID and API Key.",
                innerException: ex
            );
        }
    }

    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public override async Task<ApplicationFormDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        dto.ApiKey = _stringEncryptionService.Decrypt(dto.ApiKey);
        dto.ApiToken = _stringEncryptionService.Decrypt(dto.ApiToken);
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
        var queryableFormVersions = await _applicationFormVersionRepository.GetQueryableAsync();
        var formVersions = queryableFormVersions
            .Where(c => c.ApplicationFormId == id && c.Published)
            .OrderByDescending(s => s.Version)
            .ToList();

        return ObjectMapper.Map<List<ApplicationFormVersion>, List<ApplicationFormVersionDto>>(formVersions);
    }

    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public async Task<IList<ApplicationFormVersionDto>> GetVersionsAsync(Guid id)
    {
        var queryableFormVersions = await _applicationFormVersionRepository.GetQueryableAsync();
        var formVersions = queryableFormVersions
            .Where(c => c.ApplicationFormId == id)
            .OrderByDescending(s => s.Version)
            .ToList();

        return ObjectMapper.Map<List<ApplicationFormVersion>, List<ApplicationFormVersionDto>>(formVersions);
    }

    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public async Task SaveApplicationFormScoresheet(FormScoresheetDto dto)
    {
        var appForm = await Repository.GetAsync(dto.ApplicationFormId);
        appForm.ScoresheetId = dto.ScoresheetId;
        await Repository.UpdateAsync(appForm);
    }

    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public async Task PatchOtherConfig(Guid id, OtherConfigDto config)
    {
        var form = await Repository.GetAsync(id);
        form.IsDirectApproval = config.IsDirectApproval;
        form.ElectoralDistrictAddressType = config.ElectoralDistrictAddressType;
        form.Prefix = config.Prefix;
        form.SuffixType = config.SuffixType;
        await Repository.UpdateAsync(form);
    }

    [Authorize(PaymentsPermissions.Payments.EditFormPaymentConfiguration)]
    public async Task<bool> GetFormPreventPaymentStatusByApplicationId(Guid applicationId)
    {
        var grantApplicationDto = await _applicationService.GetAsync(applicationId);
        var formId = grantApplicationDto.ApplicationForm.Id;
        var appForm = await Repository.GetAsync(formId);
        return appForm.PreventPayment;
    }

    [Authorize(PaymentsPermissions.Payments.EditFormPaymentConfiguration)]
    public async Task SavePaymentConfiguration(FormPaymentConfigurationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var appForm = await Repository.GetAsync(dto.ApplicationFormId);

        if (dto.Payable)
        {
            if (!dto.FormHierarchy.HasValue || dto.FormHierarchy == FormHierarchyType.None)
            {
                throw new BusinessException(GrantManagerDomainErrorCodes.PayableFormRequiresHierarchy);
            }

            if (dto.FormHierarchy == FormHierarchyType.Child)
            {
                if (!dto.ParentFormId.HasValue)
                {
                    throw new BusinessException(GrantManagerDomainErrorCodes.ChildFormRequiresParentForm);
                }

                if (!dto.ParentFormVersionId.HasValue)
                {
                    throw new BusinessException(GrantManagerDomainErrorCodes.ChildFormRequiresParentFormVersion);
                }

                if (dto.ParentFormId.Value == appForm.Id)
                {
                    throw new BusinessException(GrantManagerDomainErrorCodes.ChildFormCannotReferenceSelf);
                }

                var parentForm = await Repository.FindAsync(dto.ParentFormId.Value);
                if (parentForm is null)
                {
                    throw new BusinessException(GrantManagerDomainErrorCodes.ChildFormRequiresParentForm);
                }

                var parentFormVersion = await _applicationFormVersionRepository.FindAsync(dto.ParentFormVersionId.Value);
                if (parentFormVersion is null || parentFormVersion.ApplicationFormId != parentForm.Id)
                {
                    throw new BusinessException(GrantManagerDomainErrorCodes.ParentFormVersionMismatch);
                }
            }
        }

        appForm.AccountCodingId = dto.AccountCodingId;
        appForm.Payable = dto.Payable;
        appForm.PreventPayment = dto.PreventPayment;
        appForm.PaymentApprovalThreshold = dto.PaymentApprovalThreshold;
        await Repository.UpdateAsync(appForm);
    }

    [Authorize(PaymentsPermissions.Payments.EditFormPaymentConfiguration)]
    public async Task<PagedResultDto<ParentFormLookupDto>> GetParentFormLookupAsync(ParentFormLookupRequestDto input)
    {
        input ??= new ParentFormLookupRequestDto();

        var maxResultCount = input.MaxResultCount > 0 ? input.MaxResultCount : 20;
        var skipCount = input.SkipCount > 0 ? input.SkipCount : 0;
        var normalizedFilter = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();

        var formsQueryable = await Repository.GetQueryableAsync();
        var versionsQueryable = await _applicationFormVersionRepository.GetQueryableAsync();

        var query =
            from form in formsQueryable
            join version in versionsQueryable on form.Id equals version.ApplicationFormId
            where version.Published
            select new { form, version };

        if (input.ExcludeFormId.HasValue)
        {
            query = query.Where(x => x.form.Id != input.ExcludeFormId.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedFilter))
        {
            var loweredFilter = normalizedFilter.ToLowerInvariant();
            query = query.Where(x =>
                x.form.ApplicationFormName != null &&
                x.form.ApplicationFormName.ToLower().Contains(loweredFilter));
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query
                .OrderBy(x => x.form.ApplicationFormName)
                .ThenByDescending(x => x.version.Version)
                .Skip(skipCount)
                .Take(maxResultCount)
        );

        var results = items.Select(x => new ParentFormLookupDto
        {
            ApplicationFormId = x.form.Id,
            ApplicationFormVersionId = x.version.Id,
            ApplicationFormName = x.form.ApplicationFormName ?? string.Empty,
            Version = x.version.Version
        }).ToList();

        return new PagedResultDto<ParentFormLookupDto>(totalCount, results);
    }

    public async Task<decimal?> GetFormPaymentApprovalThresholdByApplicationIdAsync(Guid applicationId)
    {
        var application = await _applicationService.GetAsync(applicationId);
        var formId = application.ApplicationForm.Id;
        var appForm = await Repository.GetAsync(formId);
        return appForm.PaymentApprovalThreshold;
    }

    public async Task<ApplicationFormDetailsDto> GetFormDetailsByApplicationIdAsync(Guid applicationId)
    {
        var formDetails = await _applicationFormSubmissionRepository.GetFormDetailsByApplicationIdAsync(applicationId);
        
        return new ApplicationFormDetailsDto
        {
            ApplicationId = formDetails.ApplicationId,
            ApplicationFormId = formDetails.ApplicationFormId,
            ApplicationFormName = formDetails.ApplicationFormName,
            ApplicationFormDescription = formDetails.ApplicationFormDescription,
            ApplicationFormCategory = formDetails.ApplicationFormCategory,
            ApplicationFormVersionId = formDetails.ApplicationFormVersionId,
            ApplicationFormVersion = formDetails.ApplicationFormVersion
        };
    }
}
