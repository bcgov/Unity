using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Integration.Chefs;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

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

        private readonly IApplicationFormVersionRepository _applicationFormVersionRepository;
        private readonly IIntakeFormSubmissionMapper _intakeFormSubmissionMapper;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IFormsApiService _formApiService;
        private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;

        public ApplicationFormVersionAppService(IRepository<ApplicationFormVersion, Guid> repository,
            IIntakeFormSubmissionMapper intakeFormSubmissionMapper,
            IUnitOfWorkManager unitOfWorkManager,
            IFormsApiService formsApiService,
            IApplicationFormVersionRepository applicationFormVersionRepository,
            IApplicationFormSubmissionRepository applicationFormSubmissionRepository)
            : base(repository)
        {
            _applicationFormVersionRepository = applicationFormVersionRepository;
            _intakeFormSubmissionMapper = intakeFormSubmissionMapper;
            _unitOfWorkManager = unitOfWorkManager;
            _formApiService = formsApiService;
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
        }

        public override async Task<ApplicationFormVersionDto> CreateAsync(CreateUpdateApplicationFormVersionDto input)
        {
            return await base.CreateAsync(input);
        }

        public override async Task<ApplicationFormVersionDto> UpdateAsync(Guid id, CreateUpdateApplicationFormVersionDto input)
        {
            return await base.UpdateAsync(id, input);
        }

        public override async Task<ApplicationFormVersionDto> GetAsync(Guid id)
        {
            var dto = await base.GetAsync(id);
            return dto;
        }

        private static JToken? GetFormVersionToken(dynamic chefsForm)
        {
            if (chefsForm == null) return null;
            JObject formObject = JObject.Parse(chefsForm.ToString());
            if (formObject == null) return null;

            JToken? versionsToken = formObject["versions"];
            return versionsToken;
        }

        public async Task<bool> InitializePublishedFormVersion(dynamic chefsForm, Guid applicationFormId, bool initializePublishedOnly)
        {
            if (chefsForm == null) return false;

            try
            {
                JToken? versionsToken = GetFormVersionToken(chefsForm);
                if (versionsToken == null) return false;

                foreach (JToken childToken in versionsToken.Children().Where(t => t.Type == JTokenType.Object))
                {
                    if (TryParsePublished(childToken, out var formVersionId, out var published)
                            && formVersionId != null
                            && await FormVersionDoesNotExist(formVersionId))
                    {
                        ApplicationFormVersionDto? applicationFormVersion = new ApplicationFormVersionDto();
                        if ((initializePublishedOnly && published) || !initializePublishedOnly)
                        {
                            applicationFormVersion = await TryInitializeApplicationFormVersionWithToken(childToken, applicationFormId, formVersionId, published);
                        }

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

        private static bool TryParsePublished(JToken token, out string? formVersionId, out bool published)
        {
            formVersionId = token.Value<string>("id");
            return bool.TryParse(token.Value<string>("published"), out published);
        }

        private async Task<bool> FormVersionDoesNotExist(string formVersionId)
        {
            var applicationFormVersion = await GetApplicationFormVersion(formVersionId);
            return applicationFormVersion == null;
        }

        public async Task<ApplicationFormVersionDto?> TryInitializeApplicationFormVersionWithToken(JToken token, Guid applicationFormId, string formVersionId, bool published)
        {
            try
            {
                string? formId = token.Value<string>("formId");
                int version = token.Value<int>("version");
                return await TryInitializeApplicationFormVersion(formId, version, applicationFormId, formVersionId, published);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Initialization Exception: {Exception}", ex);
            }

            return null;
        }

        public async Task<ApplicationFormVersionDto?> TryInitializeApplicationFormVersion(string? formId, int version, Guid applicationFormId, string formVersionId, bool published)
        {
            try
            {
                if (formId != null)
                {
                    var applicationFormVersion = new ApplicationFormVersion
                    {
                        ApplicationFormId = applicationFormId,
                        ChefsApplicationFormGuid = formId,
                        Version = version,
                        Published = published,
                        ChefsFormVersionGuid = formVersionId
                    };

                    var formVersion = await _formApiService.GetFormDataAsync(formId, formVersionId);
                    applicationFormVersion.AvailableChefsFields = _intakeFormSubmissionMapper.InitializeAvailableFormFields(formVersion);
                    return ObjectMapper.Map<ApplicationFormVersion, ApplicationFormVersionDto>(applicationFormVersion);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Initialization Exception: {Exception}", ex);
            }

            return null;
        }

        private async Task InsertApplicationFormVersion(ApplicationFormVersionDto applicationFormVersionDto)
        {
            ApplicationFormVersion applicationFormVersion = ObjectMapper.Map<ApplicationFormVersionDto, ApplicationFormVersion>(applicationFormVersionDto);
            await _applicationFormVersionRepository.InsertAsync(applicationFormVersion);
        }

        public async Task<string?> GetFormVersionSubmissionMapping(string chefsFormVersionId)
        {
            var applicationFormVersion = (await _applicationFormVersionRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsFormVersionGuid == chefsFormVersionId)
                    .First();
            string? formVersionSubmissionHeaderMapping = null;

            if (applicationFormVersion != null)
            {
                formVersionSubmissionHeaderMapping = applicationFormVersion.SubmissionHeaderMapping;
            }
            return formVersionSubmissionHeaderMapping;
        }

        private async Task<ApplicationFormVersion?> GetApplicationFormVersion(string chefsFormVersionId)
        {
            var applicationFormVersion = (await _applicationFormVersionRepository
            .GetQueryableAsync())
            .Where(s => s.ChefsFormVersionGuid == chefsFormVersionId)
            .FirstOrDefault();

            return applicationFormVersion;
        }

        public async Task<bool> FormVersionExists(string chefsFormVersionId)
        {
            var applicationFormVersion = await GetApplicationFormVersion(chefsFormVersionId);
            return applicationFormVersion != null;
        }

        private async Task<bool> UnPublishFormVersions(Guid applicationFormId, string chefsFormVersionId)
        {
            bool unpublished = false;
            using var uow = _unitOfWorkManager.Begin();
            var applicationFormVersion = (await _applicationFormVersionRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsFormVersionGuid != chefsFormVersionId && s.ApplicationFormId == applicationFormId)
                    .FirstOrDefault();
            if (applicationFormVersion != null)
            {
                applicationFormVersion.Published = false;
                await _applicationFormVersionRepository.UpdateAsync(applicationFormVersion);
                unpublished = true;
            }
            await uow.SaveChangesAsync();
            return unpublished;
        }

        public async Task<ApplicationFormVersionDto> UpdateOrCreateApplicationFormVersion(
            string chefsFormId,
            string chefsFormVersionId,
            Guid applicationFormId,
            dynamic chefsFormVersion)
        {

            var applicationFormVersion = await GetApplicationFormVersion(chefsFormVersionId);
            bool formVersionEsists = true;
            if (applicationFormVersion == null)
            {
                applicationFormVersion = (await _applicationFormVersionRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsApplicationFormGuid == chefsFormId && s.ChefsFormVersionGuid == null)
                    .FirstOrDefault();

                if (applicationFormVersion == null)
                {
                    applicationFormVersion = new ApplicationFormVersion();
                    applicationFormVersion.ApplicationFormId = applicationFormId;
                    applicationFormVersion.ChefsApplicationFormGuid = chefsFormId;
                    formVersionEsists = false;
                }

                applicationFormVersion.ChefsFormVersionGuid = chefsFormVersionId;
            }

            if (chefsFormVersion != null)
            {
                JToken? version = ((JObject)chefsFormVersion).SelectToken("version");
                JToken? published = ((JObject)chefsFormVersion).SelectToken("published");
                applicationFormVersion.AvailableChefsFields = _intakeFormSubmissionMapper.InitializeAvailableFormFields(chefsFormVersion);

                if (version != null)
                {
                    applicationFormVersion.Version = int.Parse(version.ToString());
                }

                if (published != null)
                {
                    bool publishedBool = bool.Parse(published.ToString());
                    if (publishedBool)
                    {
                        // set all to false if the current is being updated to true
                        await UnPublishFormVersions(applicationFormId, chefsFormVersionId);
                    }
                    applicationFormVersion.Published = publishedBool;
                }
            }
            else
            {
                throw new EntityNotFoundException("Application Form Not Registered");
            }

            if (formVersionEsists)
            {
                applicationFormVersion = await _applicationFormVersionRepository.UpdateAsync(applicationFormVersion);
            }
            else
            {
                applicationFormVersion = await _applicationFormVersionRepository.InsertAsync(applicationFormVersion);
            }

            return ObjectMapper.Map<ApplicationFormVersion, ApplicationFormVersionDto>(applicationFormVersion);
        }

        public async Task<ApplicationFormVersionDto?> GetByChefsFormVersionId(Guid chefsFormVersionId)
        {
            var applicationFormVersion = await _applicationFormVersionRepository.GetByChefsFormVersionAsync(chefsFormVersionId);
            if (applicationFormVersion == null) return null;
            return ObjectMapper.Map<ApplicationFormVersion, ApplicationFormVersionDto>(applicationFormVersion);
        }

        public async Task<int> GetFormVersionByApplicationIdAsync(Guid applicationId)
        {
            ApplicationFormSubmission formSubmission = await _applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
            if (formSubmission.FormVersionId == null)
            {
                try
                {
                    JObject? submissionJson = JObject.Parse(formSubmission.Submission);
                    if (submissionJson == null) return 0;
                    JToken? tokenFormVersionId = submissionJson.SelectToken("submission.formVersionId");
                    if (tokenFormVersionId == null) return 0;
                    Guid formVersionId = Guid.Parse(tokenFormVersionId.ToString());
                    formSubmission.FormVersionId = formVersionId;
                    await _applicationFormSubmissionRepository.UpdateAsync(formSubmission);
                    return await GetVersion(formVersionId);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
            else
            {
                return await GetVersion(formSubmission.FormVersionId ?? Guid.Empty);
            }
        }

        public async Task DeleteWorkSheetMappingByFormName(string formName, Guid formVersionId)
        {
            ApplicationFormVersion applicationFormVersion = await _applicationFormVersionRepository.GetAsync(formVersionId);
            if (applicationFormVersion != null && applicationFormVersion.SubmissionHeaderMapping != null)
            {
                string mappingString = applicationFormVersion.SubmissionHeaderMapping;
                // (,\s*\"custom_additionalinfo-v1.*\")
                // remove the fields that match the name
                string pattern = "(,\\s*\\\"" + formName + ".*\")|(\"" + formName + ".*\\\",)";
                applicationFormVersion.SubmissionHeaderMapping = Regex.Replace(mappingString, pattern, "", RegexOptions.None, TimeSpan.FromSeconds(30));
                await _applicationFormVersionRepository.UpdateAsync(applicationFormVersion);
            }
        }

        private async Task<int> GetVersion(Guid formVersionId)
        {
            var formVersion = await _applicationFormVersionRepository.GetByChefsFormVersionAsync(formVersionId);
            return formVersion?.Version ?? 0;
        }
    }
}
