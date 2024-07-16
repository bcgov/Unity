using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp;
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

        public async Task<ScoresheetDto> CreateAsync(CreateScoresheetDto dto)
        {
            var result = await _scoresheetRepository.InsertAsync(new Scoresheet(Guid.NewGuid(), dto.Title, dto.Name));
            return ObjectMapper.Map<Scoresheet, ScoresheetDto>(result);
        }

        public virtual async Task<QuestionDto> CreateQuestionInHighestOrderSectionAsync(Guid scoresheetId, CreateQuestionDto dto)
        {
            await ValidateChangeableScoresheet(scoresheetId);

            lock (_questionLockObject)
            {
                ScoresheetSection highestOrderSection = _sectionRepository.GetSectionWithHighestOrderAsync(scoresheetId).Result ?? throw new AbpValidationException("Scoresheet has no section.");
                Question? highestOrderQuestion = _questionRepository.GetQuestionWithHighestOrderAsync(highestOrderSection.Id).Result;
                var order = highestOrderQuestion == null ? 0 : highestOrderQuestion.Order + 1;
                var result = _questionRepository.InsertAsync(new Question(Guid.NewGuid(), dto.Name, dto.Label, (QuestionType)dto.QuestionType, order, dto.Description, highestOrderSection.Id, dto.Definition)).Result;
                return ObjectMapper.Map<Question, QuestionDto>(result);
            }
        }

        public virtual async Task<ScoresheetSectionDto> CreateSectionAsync(Guid scoresheetId, CreateSectionDto dto)
        {
            await ValidateChangeableScoresheet(scoresheetId);

            lock (_sectionLockObject)
            {
                ScoresheetSection? highestOrderSection = _sectionRepository.GetSectionWithHighestOrderAsync(scoresheetId).Result;
                var order = highestOrderSection == null ? 0 : highestOrderSection.Order + 1;
                var result = _sectionRepository.InsertAsync(new ScoresheetSection(Guid.NewGuid(), dto.Name, order, scoresheetId)).Result;
                return ObjectMapper.Map<ScoresheetSection, ScoresheetSectionDto>(result);
            }
        }

        public async Task UpdateAsync(Guid scoresheetId, EditScoresheetDto dto)
        {
            var scoresheet = await _scoresheetRepository.GetAsync(scoresheetId);
            scoresheet.Title = dto.Title;
            await _scoresheetRepository.UpdateAsync(scoresheet);
        }

        public async Task CloneScoresheetAsync(Guid scoresheetIdToClone)
        {
            using var unitOfWork = _unitOfWorkManager.Begin();
            
            var originalScoresheet = await _scoresheetRepository.GetWithChildrenAsync(scoresheetIdToClone) ?? throw new AbpValidationException("Scoresheet not found.");
            var versionSplit = originalScoresheet.Name.Split('-');
            var clonedScoresheet = new Scoresheet(Guid.NewGuid(), originalScoresheet.Title, $"{versionSplit[0]}-v{originalScoresheet.Version + 1}")
            {
                Version = originalScoresheet.Version + 1,
            };

            foreach (var originalSection in originalScoresheet.Sections)
            {
                var clonedSection = new ScoresheetSection(Guid.NewGuid(), originalSection.Name, originalSection.Order)
                {
                    ScoresheetId = clonedScoresheet.Id,
                };

                foreach (var originalQuestion in originalSection.Fields)
                {
                    var clonedQuestion = new Question(Guid.NewGuid(), originalQuestion.Name, originalQuestion.Label, originalQuestion.Type, originalQuestion.Order, originalQuestion.Description, clonedSection.Id, originalQuestion.Definition);
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
            await ValidateChangeableScoresheet(id);
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

        public async Task<List<ScoresheetDto>> GetAllPublishedScoresheetsAsync()
        {
            var result = await _scoresheetRepository.GetPublishedListAsync();
            return ObjectMapper.Map<List<Scoresheet>, List<ScoresheetDto>>(result);
        }

        public async Task<List<Guid>> GetNonDeletedNumericQuestionIds(List<Guid> questionIdsToCheck)
        {
            var existingQuestions = await _questionRepository.GetListAsync();
            return existingQuestions.Where(q => questionIdsToCheck.Contains(q.Id) && q.Type == QuestionType.Number).Select(q => q.Id).ToList();

        }

        public async Task ValidateChangeableScoresheet(Guid scoresheetId)
        {
            var scoresheet = await _scoresheetRepository.GetAsync(scoresheetId);
            if (scoresheet.Published)
            {
                throw new UserFriendlyException("Cannot change scoresheet.  Scoresheet is already published.");
            }
        }

        public async Task PublishScoresheetAsync(Guid id)
        {
            var scoresheet = await _scoresheetRepository.GetAsync(id);
            scoresheet.Published = true;
            await _scoresheetRepository.UpdateAsync(scoresheet);
        }
    }
}
