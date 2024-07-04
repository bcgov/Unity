using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.Uow;
using Volo.Abp.Validation;

namespace Unity.Flex.Scoresheets
{
    [Authorize]
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

        public async Task<List<ScoresheetDto>> GetListAsync()
        {
            var result = await _scoresheetRepository.GetListWithChildrenAsync();
            return ObjectMapper.Map<List<Scoresheet>, List<ScoresheetDto>>(result);
        }

        public virtual async Task<ScoresheetDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<Scoresheet, ScoresheetDto>(await _scoresheetRepository.GetAsync(id));
        }

        public virtual async Task<PreCloneScoresheetDto> GetPreCloneInformationAsync(Guid id)
        {
            var preCloneScoresheet = ObjectMapper.Map<Scoresheet, PreCloneScoresheetDto>(await _scoresheetRepository.GetAsync(id));
            var highestVersionScoresheet = await _scoresheetRepository.GetHighestVersionAsync(preCloneScoresheet.GroupId) ?? throw new AbpValidationException("Scoresheet not found.");
            preCloneScoresheet.HighestVersion = highestVersionScoresheet.Version;
            return preCloneScoresheet;
        }

        public async Task<ScoresheetDto> CreateAsync(CreateScoresheetDto dto)
        {
            var result = await _scoresheetRepository.InsertAsync(new Scoresheet(Guid.NewGuid(), dto.Name, Guid.NewGuid()));
            return ObjectMapper.Map<Scoresheet, ScoresheetDto>(result);
        }

        public virtual Task<QuestionDto> CreateQuestionInHighestOrderSectionAsync(Guid scoresheetId, CreateQuestionDto dto)
        {
            lock (_questionLockObject)
            {
                ScoresheetSection highestOrderSection = _sectionRepository.GetSectionWithHighestOrderAsync(scoresheetId).Result ?? throw new AbpValidationException("Scoresheet has no section.");
                Question? highestOrderQuestion = _questionRepository.GetQuestionWithHighestOrderAsync(highestOrderSection.Id).Result;
                var order = highestOrderQuestion == null ? 0 : highestOrderQuestion.Order + 1;
                var result = _questionRepository.InsertAsync(new Question(Guid.NewGuid(), dto.Name, dto.Label, (QuestionType)dto.QuestionType, order, dto.Description, highestOrderSection.Id)).Result;
                return Task.FromResult(ObjectMapper.Map<Question, QuestionDto>(result));
            }
        }

        public virtual Task<ScoresheetSectionDto> CreateSectionAsync(Guid scoresheetId, CreateSectionDto dto)
        {
            lock (_sectionLockObject)
            {
                ScoresheetSection? highestOrderSection = _sectionRepository.GetSectionWithHighestOrderAsync(scoresheetId).Result;
                var order = highestOrderSection == null ? 0 : highestOrderSection.Order + 1;
                var result = _sectionRepository.InsertAsync(new ScoresheetSection(Guid.NewGuid(), dto.Name, order, scoresheetId)).Result;
                return Task.FromResult(ObjectMapper.Map<ScoresheetSection, ScoresheetSectionDto>(result));
            }
        }

        public async Task UpdateAsync(Guid scoresheetId, EditScoresheetDto dto)
        {
            var scoresheet = await _scoresheetRepository.GetAsync(scoresheetId);
            scoresheet.Name = dto.Name;
            await _scoresheetRepository.UpdateAsync(scoresheet);
        }

        public async Task CloneScoresheetAsync(Guid scoresheetIdToClone)
        {
            using var unitOfWork = _unitOfWorkManager.Begin();
            
            var originalScoresheet = await _scoresheetRepository.GetWithChildrenAsync(scoresheetIdToClone) ?? throw new AbpValidationException("Scoresheet not found.");
            var highestVersionScoresheet = await _scoresheetRepository.GetHighestVersionAsync(originalScoresheet.GroupId) ?? throw new AbpValidationException("Scoresheet not found.");
            var clonedScoresheet = new Scoresheet(Guid.NewGuid(), originalScoresheet.Name, originalScoresheet.GroupId)
            {
                Version = highestVersionScoresheet.Version + 1,
            };

            foreach (var originalSection in originalScoresheet.Sections)
            {
                var clonedSection = new ScoresheetSection(Guid.NewGuid(), originalSection.Name, originalSection.Order)
                {
                    ScoresheetId = clonedScoresheet.Id,
                };

                foreach (var originalQuestion in originalSection.Fields)
                {
                    var clonedQuestion = new Question(Guid.NewGuid(), originalQuestion.Name, originalQuestion.Label, originalQuestion.Type, originalQuestion.Order, originalQuestion.Description, clonedSection.Id);
                    clonedSection.Fields.Add(clonedQuestion);
                }

                clonedScoresheet.Sections.Add(clonedSection);
            }

            _ = await _scoresheetRepository.InsertAsync(clonedScoresheet);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CompleteAsync();
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

        public async Task<List<ScoresheetDto>> GetAllScoresheetsAsync()
        {
            var result = await _scoresheetRepository.GetListAsync();
            return ObjectMapper.Map<List<Scoresheet>, List<ScoresheetDto>>(result);
        }

        public async Task<List<Guid>> GetNonDeletedNumericQuestionIds(List<Guid> questionIdsToCheck)
        {
            var existingQuestions = await _questionRepository.GetListAsync();
            return existingQuestions.Where(q => questionIdsToCheck.Contains(q.Id) && q.Type == QuestionType.Number).Select(q => q.Id).ToList();

        }
    }
}
