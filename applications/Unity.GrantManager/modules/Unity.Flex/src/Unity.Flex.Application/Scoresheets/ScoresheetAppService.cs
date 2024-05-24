using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Xml.Linq;
using Unity.Flex.Domain.Enums;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.Uow;
using Volo.Abp.Validation;
using static System.Collections.Specialized.BitVector32;

namespace Unity.Flex.Scoresheets
{ 
    public class ScoresheetAppService : FlexAppService, IScoresheetAppService
    {
        private readonly IScoresheetRepository _scoresheetRepository;
        private readonly IScoresheetSectionRepository _sectionRepository;
        private readonly IQuestionRepository _questionRepository;

        private readonly IUnitOfWorkManager _unitOfWorkManager;

        private readonly static object _sectionLockObject = new();
        private readonly static object _questionLockObject = new();

        public ScoresheetAppService(IUnitOfWorkManager unitOfWorkManager, IScoresheetRepository scoresheetRepository, IScoresheetSectionRepository sectionRepository, IQuestionRepository questionRepository)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _scoresheetRepository = scoresheetRepository;
            _sectionRepository = sectionRepository;
            _questionRepository = questionRepository;
        }

        public async Task<List<ScoresheetDto>> GetListAsync(List<Guid> scoresheetIdsToLoad)
        {
            var result = await _scoresheetRepository.GetListWithChildrenAsync();
            var scoresheets = ObjectMapper.Map<List<Scoresheet>, List<ScoresheetDto>>(result);
            var scoresheetsToLoad = scoresheets
                    .Where(s => scoresheetIdsToLoad.Contains(s.Id))
                    .ToList();
            var scoresheetsToLoadByGroupId = scoresheetsToLoad
                    .ToDictionary(s => s.GroupId, s => s);
            var groupedScoresheets = scoresheets.GroupBy(s => s.GroupId);
            var uniqueScoresheets = groupedScoresheets
                    .Select(g => g.OrderBy(s => s.CreationTime).First())
                    .OrderBy(s => s.CreationTime)
                    .ToList();
            var highestVersionScoresheetsByGroupId = groupedScoresheets
                    .Select(g => g.OrderByDescending(s => s.CreationTime).First())
                    .OrderBy(s => s.CreationTime)
                    .ToList();
            var highestVersionScoresheetToLoad = highestVersionScoresheetsByGroupId
                    .ToDictionary(s => s.GroupId, s => s);
            for (int i = 0; i < uniqueScoresheets.Count; i++)
            {
                if (scoresheetsToLoadByGroupId.TryGetValue(uniqueScoresheets[i].GroupId, out var replacement))
                {
                    uniqueScoresheets[i] = replacement;
                }
                else if(highestVersionScoresheetToLoad.TryGetValue(uniqueScoresheets[i].GroupId, out var highestVersionReplacement))
                {
                    uniqueScoresheets[i] = highestVersionReplacement;
                }
            }
            foreach (var scoresheet in uniqueScoresheets)
            {
                var groupVersions = groupedScoresheets
                    .First(g => g.Key == scoresheet.GroupId)
                    .Select(s => new VersionDto { ScoresheetId = s.Id,Version = s.Version })
                    .ToList();

                scoresheet.GroupVersions = new Collection<VersionDto>(groupVersions);
            }
            return uniqueScoresheets;
        }

        public virtual async Task<ScoresheetDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<Scoresheet, ScoresheetDto>(await _scoresheetRepository.GetAsync(id));
        }

        public async Task<ScoresheetDto> CreateAsync(CreateScoresheetDto dto)
        {
            var result = await _scoresheetRepository.InsertAsync(new Scoresheet(Guid.NewGuid(), dto.Name, Guid.NewGuid()));
            return ObjectMapper.Map<Scoresheet, ScoresheetDto>(result);
        }

        public virtual Task<QuestionDto> CreateQuestionAsync(Guid id, CreateQuestionDto dto)
        {
            lock (_questionLockObject)
            {
                ScoresheetSection highestOrderSection = _sectionRepository.GetSectionWithHighestOrderAsync(dto.ScoresheetId).Result ?? throw new AbpValidationException("Scoresheet has no section.");
                Question? highestOrderQuestion = _questionRepository.GetQuestionWithHighestOrderAsync(highestOrderSection.Id).Result;
                var order = highestOrderQuestion == null ? 0 : highestOrderQuestion.Order + 1;
                var result = _questionRepository.InsertAsync(new Question(Guid.NewGuid(), dto.Name, dto.Label, Domain.Enums.QuestionType.Number, order, dto.Description, highestOrderSection.Id)).Result;
                return Task.FromResult(ObjectMapper.Map<Question, QuestionDto>(result));
            }
        }

        public virtual Task<ScoresheetSectionDto> CreateSectionAsync(Guid id, CreateSectionDto dto)
        {
            lock (_sectionLockObject)
            {
                ScoresheetSection? highestOrderSection = _sectionRepository.GetSectionWithHighestOrderAsync(dto.ScoresheetId).Result;
                var order = highestOrderSection == null ? 0 : highestOrderSection.Order + 1;
                var result = _sectionRepository.InsertAsync(new ScoresheetSection(Guid.NewGuid(), dto.Name, order, dto.ScoresheetId)).Result;
                return Task.FromResult(ObjectMapper.Map<ScoresheetSection, ScoresheetSectionDto>(result));
            }
        }

        public async Task UpdateAsync(EditScoresheetDto dto)
        {
            if(dto.ActionType.Contains("Current Version"))
            {
                await UpdateScoresheetOnCurrentVersions(dto);
            }
            else if(dto.ActionType.Contains("New Version"))
            {
                await UpdateScoresheetOnCurrentVersions(dto);
                await CreateNewVersion(dto);
            }
            else
            {
                throw new AbpValidationException("Invalid ActionType for Edit Scoresheet.");
            }
            
        }

        private async Task CreateNewVersion(EditScoresheetDto dto)
        {
            using var unitOfWork = _unitOfWorkManager.Begin();
            var originalScoresheet = await _scoresheetRepository.GetWithChildrenAsync(dto.ScoresheetId) ?? throw new AbpValidationException("Scoresheet not found.");
            var highestVersionScoresheet = await _scoresheetRepository.GetHighestVersionAsync(dto.GroupId) ?? throw new AbpValidationException("Scoresheet not found.");
            var clonedScoresheet = new Scoresheet(Guid.NewGuid(), dto.Name, dto.GroupId)
            {
                Version = highestVersionScoresheet.Version + 1,
                Sections = []
            };

            foreach (var originalSection in originalScoresheet.Sections)
            {
                var clonedSection = new ScoresheetSection(Guid.NewGuid(), originalSection.Name, originalSection.Order)
                {
                    ScoresheetId = clonedScoresheet.Id,
                    Fields = []
                };

                foreach (var originalQuestion in originalSection.Fields)
                {
                    var clonedQuestion = new Question(Guid.NewGuid(), originalQuestion.Name, originalQuestion.Label, originalQuestion.Type, originalQuestion.Order, originalQuestion.Description, clonedSection.Id);
                    clonedSection.Fields.Add(clonedQuestion);
                }

                clonedScoresheet.Sections.Add(clonedSection);
            }

            await _scoresheetRepository.InsertAsync(clonedScoresheet);
            await unitOfWork.CompleteAsync();
        }

        private async Task UpdateScoresheetOnCurrentVersions(EditScoresheetDto dto)
        {
            var scoresheets = await _scoresheetRepository.GetScoresheetsByGroupId(dto.GroupId);
            foreach(var scoresheet in scoresheets)
            {
                scoresheet.Name = dto.Name;
                await _scoresheetRepository.UpdateAsync(scoresheet);
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            await _scoresheetRepository.DeleteAsync(id);
        }

        public async Task SaveOrder(List<ScoresheetItemDto> dto)
        {
            uint sectionOrder = 0;
            uint questionOrder = 0;
            ScoresheetSection? currentSection = null;
            foreach (var item in dto)
            {
                if (item.Type == "section")
                {
                    var section = await _sectionRepository.GetAsync(item.Id) ?? throw new AbpValidationException("SectionId not found.");
                    section.Order = sectionOrder;
                    sectionOrder++;
                    questionOrder = 0;
                    await _sectionRepository.UpdateAsync(section);
                    currentSection = section;
                }
                else if (item.Type == "question")
                {
                    var question = await _questionRepository.GetAsync(item.Id) ?? throw new AbpValidationException("QuestionId not found.");
                    question.Order = questionOrder;
                    questionOrder++;
                    if (currentSection != null)
                    {
                        question.SectionId = currentSection.Id;
                    }
                    await _questionRepository.UpdateAsync(question);
                }
                else
                {
                    throw new AbpValidationException("Invalid type!");
                }
            }
        }
    }
}
