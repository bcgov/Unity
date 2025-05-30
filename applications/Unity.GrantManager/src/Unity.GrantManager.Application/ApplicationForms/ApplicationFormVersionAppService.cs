using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Integration.Chefs;
using Unity.GrantManager.Reporting.FieldGenerators;
using Unity.Modules.Shared.Features;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Uow;
using Unity.GrantManager.Intakes.Mapping;

namespace Unity.GrantManager.ApplicationForms
{
    public class ApplicationFormVersionAppService :
        CrudAppService<
            ApplicationFormVersion,
            ApplicationFormVersionDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateApplicationFormVersionDto>,
        IApplicationFormVersionAppService
    {
        private readonly IApplicationFormVersionRepository _formVersionRepository;
        private readonly IIntakeFormSubmissionMapper _formSubmissionMapper;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IFormsApiService _formsApiService;
        private readonly IApplicationFormSubmissionRepository _formSubmissionRepository;
        private readonly IReportingFieldsGeneratorService _reportingFieldsGeneratorService;
        private readonly IFeatureChecker _featureChecker;

        public ApplicationFormVersionAppService(
            IRepository<ApplicationFormVersion, Guid> repository,
            IIntakeFormSubmissionMapper formSubmissionMapper,
            IUnitOfWorkManager unitOfWorkManager,
            IFormsApiService formsApiService,
            IApplicationFormVersionRepository formVersionRepository,
            IApplicationFormSubmissionRepository formSubmissionRepository,
            IReportingFieldsGeneratorService reportingFieldsGeneratorService,
            IFeatureChecker featureChecker)
            : base(repository)
        {
            _formVersionRepository = formVersionRepository;
            _formSubmissionMapper = formSubmissionMapper;
            _unitOfWorkManager = unitOfWorkManager;
            _formsApiService = formsApiService;
            _formSubmissionRepository = formSubmissionRepository;
            _reportingFieldsGeneratorService = reportingFieldsGeneratorService;
            _featureChecker = featureChecker;
        }

        public override async Task<ApplicationFormVersionDto> CreateAsync(CreateUpdateApplicationFormVersionDto input) =>
            await base.CreateAsync(input);

        public override async Task<ApplicationFormVersionDto> UpdateAsync(Guid id, CreateUpdateApplicationFormVersionDto input) =>
            await base.UpdateAsync(id, input);

        public override async Task<ApplicationFormVersionDto> GetAsync(Guid id) =>
            await base.GetAsync(id);

        public async Task<bool> InitializePublishedFormVersion(dynamic chefsForm, Guid applicationFormId, bool initializePublishedOnly)
        {
            if (chefsForm == null) return false;

            try
            {
                var versionsToken = GetFormVersionToken(chefsForm);
                if (versionsToken == null) return false;

                var childTokens = ((IEnumerable<JToken>)versionsToken.Children()).Where(t => t.Type == JTokenType.Object);
                foreach (var childToken in childTokens)
                {
                    if (TryParsePublished(childToken, out string? formVersionId, out bool published) &&
                        formVersionId != null &&
                        await FormVersionDoesNotExist(formVersionId) &&
                        (!initializePublishedOnly || published))
                    {
                        var applicationFormVersion = await TryInitializeApplicationFormVersionWithToken(childToken, applicationFormId, formVersionId, published);
                        if (applicationFormVersion != null)
                        {
                            await InsertApplicationFormVersion(applicationFormVersion);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception: {Exception}", ex);
            }

            return false;
        }

        private static JToken? GetFormVersionToken(dynamic chefsForm) =>
            chefsForm == null ? null : JObject.Parse(chefsForm.ToString())?["versions"];

        private static bool TryParsePublished(JToken token, out string? formVersionId, out bool published)
        {
            formVersionId = token.Value<string>("id");
            return bool.TryParse(token.Value<string>("published"), out published);
        }

        private async Task<bool> FormVersionDoesNotExist(string formVersionId) =>
            await GetApplicationFormVersion(formVersionId) == null;

        public async Task<ApplicationFormVersionDto?> TryInitializeApplicationFormVersionWithToken(JToken token, Guid applicationFormId, string formVersionId, bool published)
        {
            try
            {
                var formId = token.Value<string>("formId");
                var version = token.Value<int>("version");
                return await TryInitializeApplicationFormVersion(formId, version, applicationFormId, formVersionId, published);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Initialization Exception: {Exception}", ex);
                return null;
            }
        }

        public async Task<ApplicationFormVersionDto?> TryInitializeApplicationFormVersion(string? formId, int version, Guid applicationFormId, string formVersionId, bool published)
        {
            if (formId == null) return null;

            try
            {
                var applicationFormVersion = new ApplicationFormVersion
                {
                    ApplicationFormId = applicationFormId,
                    ChefsApplicationFormGuid = formId,
                    Version = version,
                    Published = published,
                    ChefsFormVersionGuid = formVersionId
                };

                var formVersion = await _formsApiService.GetFormDataAsync(formId, formVersionId);
                applicationFormVersion.AvailableChefsFields = _formSubmissionMapper.InitializeAvailableFormFields(formVersion);

                if (formVersion is JObject formVersionObject)
                {
                    var schema = formVersionObject.SelectToken("schema")?.ToString() ?? string.Empty;
                    applicationFormVersion.FormSchema = ChefsFormIOReplacement.ReplaceAdvancedFormIoControls(schema);
                }

                return ObjectMapper.Map<ApplicationFormVersion, ApplicationFormVersionDto>(applicationFormVersion);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Initialization Exception: {Exception}", ex);
                return null;
            }
        }

        private async Task InsertApplicationFormVersion(ApplicationFormVersionDto applicationFormVersionDto)
        {
            var applicationFormVersion = ObjectMapper.Map<ApplicationFormVersionDto, ApplicationFormVersion>(applicationFormVersionDto);
            await _formVersionRepository.InsertAsync(applicationFormVersion);
        }

        public async Task<string?> GetFormVersionSubmissionMapping(string chefsFormVersionId)
        {
            var applicationFormVersion = (await _formVersionRepository.GetQueryableAsync())
                .FirstOrDefault(s => s.ChefsFormVersionGuid == chefsFormVersionId);

            return applicationFormVersion?.SubmissionHeaderMapping;
        }

        private async Task<ApplicationFormVersion?> GetApplicationFormVersion(string chefsFormVersionId) =>
            (await _formVersionRepository.GetQueryableAsync())
                .FirstOrDefault(s => s.ChefsFormVersionGuid == chefsFormVersionId);

        public async Task<bool> FormVersionExists(string chefsFormVersionId) =>
            await GetApplicationFormVersion(chefsFormVersionId) != null;

        private async Task<bool> UnPublishFormVersions(Guid applicationFormId, string chefsFormVersionId)
        {
            using var uow = _unitOfWorkManager.Begin();
            var applicationFormVersion = (await _formVersionRepository.GetQueryableAsync())
                .FirstOrDefault(s => s.ChefsFormVersionGuid != chefsFormVersionId && s.ApplicationFormId == applicationFormId);

            if (applicationFormVersion != null)
            {
                applicationFormVersion.Published = false;
                await _formVersionRepository.UpdateAsync(applicationFormVersion);
                await uow.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<ApplicationFormVersionDto> UpdateOrCreateApplicationFormVersion(
            string chefsFormId,
            string chefsFormVersionId,
            Guid applicationFormId,
            dynamic chefsFormVersion)
        {
            var applicationFormVersion = await GetOrCreateApplicationFormVersion(chefsFormId, chefsFormVersionId, applicationFormId);
            await UpdateApplicationFormVersionFields(applicationFormVersion, chefsFormVersion, applicationFormId, chefsFormVersionId);

            if (await _featureChecker.IsEnabledAsync(FeatureConsts.Reporting) &&
                string.IsNullOrEmpty(applicationFormVersion.ReportViewName))
            {
                await _reportingFieldsGeneratorService.GenerateAndSetAsync(applicationFormVersion);
            }

            return ObjectMapper.Map<ApplicationFormVersion, ApplicationFormVersionDto>(applicationFormVersion);
        }

        private async Task<ApplicationFormVersion> GetOrCreateApplicationFormVersion(string chefsFormId, string chefsFormVersionId, Guid applicationFormId)
        {
            var applicationFormVersion = await GetApplicationFormVersion(chefsFormVersionId) ??
                                         (await _formVersionRepository.GetQueryableAsync())
                                             .FirstOrDefault(s => s.ChefsApplicationFormGuid == chefsFormId && s.ChefsFormVersionGuid == null) ??
                                         new ApplicationFormVersion
                                         {
                                             ApplicationFormId = applicationFormId,
                                             ChefsApplicationFormGuid = chefsFormId
                                         };

            applicationFormVersion.ChefsFormVersionGuid = chefsFormVersionId;
            return applicationFormVersion;
        }

        private async Task UpdateApplicationFormVersionFields(ApplicationFormVersion applicationFormVersion, dynamic chefsFormVersion, Guid applicationFormId, string chefsFormVersionId)
        {
            if (chefsFormVersion == null)
                throw new EntityNotFoundException("Application Form Not Registered");

            var version = ((JObject)chefsFormVersion).SelectToken("version")?.ToString();
            var published = ((JObject)chefsFormVersion).SelectToken("published")?.ToString();
            var schema = ((JObject)chefsFormVersion).SelectToken("schema")?.ToString();

            applicationFormVersion.AvailableChefsFields = _formSubmissionMapper.InitializeAvailableFormFields(chefsFormVersion);
            applicationFormVersion.FormSchema = schema != null ? ChefsFormIOReplacement.ReplaceAdvancedFormIoControls(schema) ?? string.Empty : string.Empty;

            if (version != null)
                applicationFormVersion.Version = int.Parse(version);

            if (published != null && bool.TryParse(published, out var isPublished))
            {
                if (isPublished)
                    await UnPublishFormVersions(applicationFormId, chefsFormVersionId);

                applicationFormVersion.Published = isPublished;
            }

            if (applicationFormVersion.Id == Guid.Empty)
                await _formVersionRepository.InsertAsync(applicationFormVersion, true);
            else
                await _formVersionRepository.UpdateAsync(applicationFormVersion, true);
        }

        public async Task<ApplicationFormVersionDto?> GetByChefsFormVersionId(Guid chefsFormVersionId)
        {
            var applicationFormVersion = await _formVersionRepository.GetByChefsFormVersionAsync(chefsFormVersionId);
            return applicationFormVersion == null ? null : ObjectMapper.Map<ApplicationFormVersion, ApplicationFormVersionDto>(applicationFormVersion);
        }

        public async Task<int> GetFormVersionByApplicationIdAsync(Guid applicationId)
        {
            var formSubmission = await _formSubmissionRepository.GetByApplicationAsync(applicationId);
            if (formSubmission.FormVersionId == null)
            {
                try
                {
                    var submissionJson = JObject.Parse(formSubmission.Submission);
                    var tokenFormVersionId = submissionJson?.SelectToken("submission.formVersionId")?.ToString();
                    if (tokenFormVersionId == null) return 0;

                    var formVersionId = Guid.Parse(tokenFormVersionId);
                    formSubmission.FormVersionId = formVersionId;
                    await _formSubmissionRepository.UpdateAsync(formSubmission);
                    return await GetVersion(formVersionId);
                }
                catch
                {
                    return 0;
                }
            }

            return await GetVersion(formSubmission.FormVersionId ?? Guid.Empty);
        }

        public async Task DeleteWorkSheetMappingByFormName(string formName, Guid formVersionId)
        {
            var applicationFormVersion = await _formVersionRepository.GetAsync(formVersionId);
            if (applicationFormVersion?.SubmissionHeaderMapping == null) return;

            var pattern = $"(,\\s*\\\"{formName}.*\\\")|(\\\"{formName}.*\\\",)";
            applicationFormVersion.SubmissionHeaderMapping = Regex.Replace(applicationFormVersion.SubmissionHeaderMapping, pattern, "", RegexOptions.None, TimeSpan.FromSeconds(30));
            await _formVersionRepository.UpdateAsync(applicationFormVersion);
        }

        private async Task<int> GetVersion(Guid formVersionId)
        {
            var formVersion = await _formVersionRepository.GetByChefsFormVersionAsync(formVersionId);
            return formVersion?.Version ?? 0;
        }
    }
}
