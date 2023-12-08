using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Intakes.Integration;
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
        private readonly IFormIntService _formIntService;

        public ApplicationFormVersionAppService(IRepository<ApplicationFormVersion, Guid> repository,
            IIntakeFormSubmissionMapper intakeFormSubmissionMapper,
            IUnitOfWorkManager unitOfWorkManager,
            IFormIntService formIntService,
            IApplicationFormVersionRepository applicationFormVersionRepository)
            : base(repository)
        {
            _applicationFormVersionRepository = applicationFormVersionRepository;
            _intakeFormSubmissionMapper = intakeFormSubmissionMapper;
            _unitOfWorkManager = unitOfWorkManager;
            _formIntService = formIntService;
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
        public async Task<bool> InitializePublishedFormVersion(dynamic chefsForm, Guid applicationFormId)
        {
            if (chefsForm == null) return false;

            try
            {
                JObject formObject = JObject.Parse(chefsForm.ToString());
                if (formObject == null) return false;
#pragma warning disable CS8600
                JToken versionsToken = formObject["versions"];
#pragma warning restore CS8600
                if (versionsToken == null) return false;

                foreach (JToken childToken in versionsToken.Children().Where(t => t.Type == JTokenType.Object))
                {
                    if (TryParsePublished(childToken, out var formVersionId, out var published) && 
                        published && (formVersionId != null && await FormVersionDoesNotExist(formVersionId)))
                    {
                        ApplicationFormVersion? applicationFormVersion = await TryInitializeApplicationFormVersion(childToken, applicationFormId, formVersionId);
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
                Logger.LogError("Exception: {ex.Message}", ex.Message);
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

        private async Task<ApplicationFormVersion?> TryInitializeApplicationFormVersion(JToken token, Guid applicationFormId, string formVersionId)
        {
            try
            {
                string? formId = token.Value<string>("formId");
                if (formId != null)
                {
                    int version = token.Value<int>("version");

                    var applicationFormVersion = new ApplicationFormVersion
                    {
                        ApplicationFormId = applicationFormId,
                        ChefsApplicationFormGuid = formId,
                        Version = version,
                        Published = true,
                        ChefsFormVersionGuid = formVersionId
                    };

                    var formVersion = await _formIntService.GetFormDataAsync(formId, formVersionId);
                    applicationFormVersion.AvailableChefsFields = _intakeFormSubmissionMapper.InitializeAvailableFormFields(formVersion);

                    return applicationFormVersion;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Initialization Exception: {ex.Message}", ex.Message);
            }

            return null;
        }

        private async Task InsertApplicationFormVersion(ApplicationFormVersion applicationFormVersion)
        {
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

            return ObjectMapper.Map<ApplicationFormVersion, ApplicationFormVersionDto>((applicationFormVersion));
        }

        public async Task<IList<ApplicationFormVersionDto>> GetPublishedListAsync(Guid applicationFormId)
        {
            IQueryable<ApplicationFormVersion> queryableFormVersions = _applicationFormVersionRepository.GetQueryableAsync().Result;
            var formVersions = queryableFormVersions.Where(c => c.ApplicationFormId.Equals(applicationFormId) && c.Published.Equals(true)).ToList();
            return await Task.FromResult<IList<ApplicationFormVersionDto>>(ObjectMapper.Map<List<ApplicationFormVersion>, List<ApplicationFormVersionDto>>(formVersions.OrderByDescending(s => s.Version).ToList()));
        }

        public async Task<IList<ApplicationFormVersionDto>> GetListAsync(Guid applicationFormId)
        {
            IQueryable<ApplicationFormVersion> queryableFormVersions = _applicationFormVersionRepository.GetQueryableAsync().Result;
            var formVersions = queryableFormVersions.Where(c => c.ApplicationFormId.Equals(applicationFormId)).ToList();
            return await Task.FromResult<IList<ApplicationFormVersionDto>>(ObjectMapper.Map<List<ApplicationFormVersion>, List<ApplicationFormVersionDto>>(formVersions.OrderByDescending(s => s.Version).ToList()));
        }
    }
}
