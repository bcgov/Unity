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
            bool initializePublishedFormVersion = false;
            if (chefsForm != null)
            {
                JObject formObject = JObject.Parse(chefsForm.ToString());
                if (formObject != null)
                {
                    dynamic? versions = ((JObject)formObject!).SelectToken("versions");
                    if (versions != null)
                    {
                        foreach (JToken? childToken in versions.Children())
                        {
                            if (childToken != null && childToken.Type == JTokenType.Object)
                            {
                                dynamic? published = childToken["published"];
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                                if (published != null && bool.Parse(published.ToString()))
                                {
                                    dynamic? formVersionId = childToken["id"];
                                    if (formVersionId != null)
                                    {
                                        var applicationFormVersion = await GetApplicationFormVersion(formVersionId.ToString());
                                        if (applicationFormVersion == null)
                                        {
                                            string formId = childToken["formId"].ToString();
                                            dynamic? version = childToken["version"];
                                            applicationFormVersion = new ApplicationFormVersion();
                                            applicationFormVersion.ApplicationFormId = applicationFormId;
                                            applicationFormVersion.ChefsApplicationFormGuid = formId;
                                            applicationFormVersion.Version = int.Parse(version.ToString());
                                            applicationFormVersion.Published = bool.Parse(published.ToString());
                                            applicationFormVersion.ChefsFormVersionGuid = formVersionId.ToString();
                                            var formVersion = await _formIntService.GetFormDataAsync(formId, formVersionId.ToString());
                                            applicationFormVersion.AvailableChefsFields = _intakeFormSubmissionMapper.InitializeAvailableFormFields(formVersion);
                                            await _applicationFormVersionRepository.InsertAsync(applicationFormVersion);
                                            initializePublishedFormVersion = true;
                                        }
                                    }
                                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                            }
                        }
                    }
                }

            }

            return initializePublishedFormVersion;
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
