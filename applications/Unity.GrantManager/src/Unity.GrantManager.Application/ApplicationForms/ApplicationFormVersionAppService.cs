using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
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

        public ApplicationFormVersionAppService(IRepository<ApplicationFormVersion, Guid> repository,
            IIntakeFormSubmissionMapper intakeFormSubmissionMapper,
            IUnitOfWorkManager unitOfWorkManager,
            IApplicationFormVersionRepository applicationFormVersionRepository)
            : base(repository)
        {
            _applicationFormVersionRepository = applicationFormVersionRepository;
            _intakeFormSubmissionMapper = intakeFormSubmissionMapper;
            _unitOfWorkManager = unitOfWorkManager;
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

        public async Task<string?> GetFormVersionSubmissionMapping(string chefsFormVersionId) {
            var applicationFormVersion = (await _applicationFormVersionRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsFormVersionGuid == chefsFormVersionId)
                    .First();
            string? formVersionSubmissionHeaderMapping = null;
            
            if(applicationFormVersion != null) {
                formVersionSubmissionHeaderMapping = applicationFormVersion.SubmissionHeaderMapping;
            }
            return formVersionSubmissionHeaderMapping;
        }

        public async Task<bool> FormVersionExists(string chefsFormVersionId) {
            var applicationFormVersion = (await _applicationFormVersionRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsFormVersionGuid == chefsFormVersionId)
                    .First();

            return applicationFormVersion != null;
        }

        private async Task<bool> UnPublishFormVersions(string applicationFormId, string chefsFormVersionId) {
            bool unpublished = false;
            using var uow = _unitOfWorkManager.Begin();
            var applicationFormVersion = (await _applicationFormVersionRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsFormVersionGuid != chefsFormVersionId && s.ChefsApplicationFormGuid == applicationFormId)
                    .FirstOrDefault();
            if(applicationFormVersion != null)
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
            dynamic chefsFormVersion) {

            var applicationFormVersion = (await _applicationFormVersionRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsFormVersionGuid == chefsFormVersionId)
                    .FirstOrDefault();
            
            bool formVersionEsists = true;

            if(applicationFormVersion == null) {
                applicationFormVersion = (await _applicationFormVersionRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsApplicationFormGuid == chefsFormId && s.ChefsFormVersionGuid == null)
                    .FirstOrDefault();

                if(applicationFormVersion == null)
                {
                    applicationFormVersion = new ApplicationFormVersion();
                    applicationFormVersion.ApplicationFormId = applicationFormId;
                    applicationFormVersion.ChefsApplicationFormGuid = chefsFormId;
                    formVersionEsists = false;
                }

                applicationFormVersion.ChefsFormVersionGuid = chefsFormVersionId;
            }

            if(chefsFormVersion != null)
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
                    if(publishedBool) {
                        // set others to false if the current is being updated to true
                        await UnPublishFormVersions(applicationFormId.ToString(), chefsFormVersionId);
                    }
                    applicationFormVersion.Published = publishedBool;
                }
            } else {
                throw new EntityNotFoundException("Application Form Not Registered");
            }

            if (formVersionEsists)
            {
                applicationFormVersion = await _applicationFormVersionRepository.UpdateAsync(applicationFormVersion);
            } else {
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
