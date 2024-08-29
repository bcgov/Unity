using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.Settings;
using Volo.Abp;
using Volo.Abp.Uow;
using Volo.Abp.Validation;

namespace Unity.Flex.Scoresheets
{
    [Authorize]
    public partial class ScoresheetAppService : FlexAppService, IScoresheetAppService
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
            var existingScoresheet = await _scoresheetRepository.GetByNameAsync(dto.Name, false);

            if (existingScoresheet != null)
            {
                throw new UserFriendlyException("Scoresheet names must be unique");
            }
            var result = await _scoresheetRepository.InsertAsync(new Scoresheet(Guid.NewGuid(), dto.Title, dto.Name));
            return ObjectMapper.Map<Scoresheet, ScoresheetDto>(result);
        }

        public virtual async Task<QuestionDto> CreateQuestionInHighestOrderSectionAsync(Guid scoresheetId, CreateQuestionDto dto)
        {
            await ValidateChangeableScoresheet(scoresheetId);

            lock (_questionLockObject)
            {
                var questionName = dto.Name.Trim();
                if(_sectionRepository.HasQuestionWithNameAsync(scoresheetId, questionName).Result)
                {
                    throw new UserFriendlyException("Question names should be unique");
                }
                ScoresheetSection highestOrderSection = _sectionRepository.GetSectionWithHighestOrderAsync(scoresheetId).Result ?? throw new AbpValidationException("Scoresheet has no section.");
                Question? highestOrderQuestion = _questionRepository.GetQuestionWithHighestOrderAsync(highestOrderSection.Id).Result;
                var order = highestOrderQuestion == null ? 0 : highestOrderQuestion.Order + 1;
                var result = _questionRepository.InsertAsync(new Question(Guid.NewGuid(), questionName, dto.Label, (QuestionType)dto.QuestionType, order, dto.Description, highestOrderSection.Id, dto.Definition)).Result;
                return ObjectMapper.Map<Question, QuestionDto>(result);
            }
        }

        public virtual async Task<ScoresheetSectionDto> CreateSectionAsync(Guid scoresheetId, CreateSectionDto dto)
        {
            await ValidateChangeableScoresheet(scoresheetId);

            lock (_sectionLockObject)
            {
                var sectionName = dto.Name.Trim();
                if(_sectionRepository.HasSectionWithNameAsync(scoresheetId, sectionName).Result)
                {
                    throw new UserFriendlyException("Section names must be unique");
                }
                ScoresheetSection? highestOrderSection = _sectionRepository.GetSectionWithHighestOrderAsync(scoresheetId).Result;
                var order = highestOrderSection == null ? 0 : highestOrderSection.Order + 1;
                var result = _sectionRepository.InsertAsync(new ScoresheetSection(Guid.NewGuid(), sectionName, order, scoresheetId)).Result;
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
                    var clonedQuestion = new Question(Guid.NewGuid(), originalQuestion.Name, originalQuestion.Label, originalQuestion.Type, originalQuestion.Order, originalQuestion.Description, originalQuestion.Definition)
                    {
                        SectionId = clonedSection.Id
                    };
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

        public async Task<List<QuestionDto>> GetNonDeletedYesNoQuestions(List<Guid> questionIdsToCheck)
        {
            var existingQuestions = await _questionRepository.GetListAsync();
            var result = existingQuestions.Where(q => questionIdsToCheck.Contains(q.Id) && q.Type == QuestionType.YesNo).ToList();
            return ObjectMapper.Map<List<Question>, List<QuestionDto>>(result);
        }

        public async Task<ExportScoresheetDto> ExportScoresheet(Guid scoresheetId)
        {
            var worksheet = await _scoresheetRepository.GetAsync(scoresheetId, true);
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new ScoresheetContractResolver()
            };
            var json = JsonConvert.SerializeObject(worksheet, settings);
            var byteArray = System.Text.Encoding.UTF8.GetBytes(json);

            return new ExportScoresheetDto { Content = byteArray, ContentType = "application/json", Name = "worksheet_" + worksheet.Title + "_" + worksheet.Name + ".json" };
        }

        public async Task ImportScoresheetAsync(ScoresheetImportDto scoresheetImportDto)
        {
            if (scoresheetImportDto.Content == null || scoresheetImportDto.Content.Length == 0)
            {
                throw new UserFriendlyException("No file content provided.");
            }

            var json = scoresheetImportDto.Content;
            var scoresheet = JsonConvert.DeserializeObject<Scoresheet>(json, new JsonSerializerSettings
            {
                ContractResolver = new PrivateSetterContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            }) ?? throw new UserFriendlyException("Invalid JSON content.");
            var name = scoresheet.Title.ToLower().Trim() + "-v1";
            _ = scoresheet.SetName(NameRegex().Replace(name, ""));
            scoresheet.Published = false;
            await _scoresheetRepository.InsertAsync(scoresheet);
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex NameRegex();

    }
}
