using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.Settings;
using Unity.Flex.Domain.Utils;
using Unity.Flex.Reporting.FieldGenerators;
using Unity.Flex.Scoresheets.Enums;
using Unity.Modules.Shared.Features;
using Volo.Abp;
using Volo.Abp.Features;
using Volo.Abp.Uow;
using Volo.Abp.Validation;

namespace Unity.Flex.Scoresheets
{
    [Authorize]
    public partial class ScoresheetAppService(IUnitOfWorkManager unitOfWorkManager,
        IScoresheetRepository scoresheetRepository,
        IScoresheetSectionRepository sectionRepository,
        IQuestionRepository questionRepository,
        IReportingFieldsGeneratorService<Scoresheet> reportingFieldsGeneratorService,
        IFeatureChecker featureChecker) : FlexAppService, IScoresheetAppService
    {
        private readonly static object _sectionLockObject = new();
        private readonly static object _questionLockObject = new();

        public async Task<List<ScoresheetDto>> GetListAsync()
        {
            var result = await scoresheetRepository.GetListWithChildrenAsync();
            return ObjectMapper.Map<List<Scoresheet>, List<ScoresheetDto>>(result);
        }

        public virtual async Task<ScoresheetDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<Scoresheet, ScoresheetDto>(await scoresheetRepository.GetAsync(id));
        }

        public async Task<ScoresheetDto> CreateAsync(CreateScoresheetDto dto)
        {
            var existingScoresheet = await scoresheetRepository.GetByNameAsync(dto.Name, false);

            if (existingScoresheet != null)
            {
                throw new UserFriendlyException("Scoresheet names must be unique");
            }
            var result = await scoresheetRepository.InsertAsync(new Scoresheet(Guid.NewGuid(), dto.Title, dto.Name));
            return ObjectMapper.Map<Scoresheet, ScoresheetDto>(result);
        }

        public virtual async Task<QuestionDto> CreateQuestionInHighestOrderSectionAsync(Guid scoresheetId, CreateQuestionDto dto)
        {
            await ValidateChangeableScoresheet(scoresheetId);

            lock (_questionLockObject)
            {
                ScoresheetSection highestOrderSection = sectionRepository.GetSectionWithHighestOrderAsync(scoresheetId, true).Result ?? throw new AbpValidationException("Scoresheet has no section.");
                uint highestOrder = (highestOrderSection.Fields != null && highestOrderSection.Fields.Count > 0) ? highestOrderSection.Fields.Max(q => q.Order) : 0;
                var order = highestOrder + 1;
                var newQuestion = new Question(Guid.NewGuid(), dto.Name.Trim(), dto.Label, (QuestionType)dto.QuestionType, order, dto.Description, highestOrderSection.Id, dto.Definition);
                highestOrderSection.AddQuestion(newQuestion);
                _ = sectionRepository.UpdateAsync(highestOrderSection).Result;
                return ObjectMapper.Map<Question, QuestionDto>(newQuestion);
            }
        }

        public virtual async Task<ScoresheetSectionDto> CreateSectionAsync(Guid scoresheetId, CreateSectionDto dto)
        {
            await ValidateChangeableScoresheet(scoresheetId);

            lock (_sectionLockObject)
            {
                ScoresheetSection? highestOrderSection = sectionRepository.GetSectionWithHighestOrderAsync(scoresheetId, true).Result;
                var order = highestOrderSection == null ? 0 : highestOrderSection.Order + 1;
                var scoresheet = scoresheetRepository.GetAsync(scoresheetId, true).Result;
                ScoresheetSection newSection = new(Guid.NewGuid(), dto.Name.Trim(), order);
                _ = scoresheet.AddSection(newSection);
                _ = scoresheetRepository.UpdateAsync(scoresheet).Result;
                return ObjectMapper.Map<ScoresheetSection, ScoresheetSectionDto>(newSection);
            }
        }

        public async Task UpdateAsync(Guid scoresheetId, EditScoresheetDto dto)
        {
            var scoresheet = await scoresheetRepository.GetAsync(scoresheetId);
            scoresheet.Title = dto.Title;
            await scoresheetRepository.UpdateAsync(scoresheet);
        }

        public async Task CloneScoresheetAsync(Guid scoresheetIdToClone)
        {
            using var unitOfWork = unitOfWorkManager.Begin();

            var originalScoresheet = await scoresheetRepository.GetWithChildrenAsync(scoresheetIdToClone) ?? throw new AbpValidationException("Scoresheet not found.");
            var versionSplit = SheetParserFunctions.SplitSheetNameAndVersion(originalScoresheet.Name);
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

            _ = await scoresheetRepository.InsertAsync(clonedScoresheet);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CompleteAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            await ValidateChangeableScoresheet(id);
            await scoresheetRepository.DeleteAsync(id);
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
                    var section = await sectionRepository.GetAsync(item.Id) ?? throw new AbpValidationException("SectionId not found.");
                    section.Order = sectionOrder;
                    sectionOrder++;
                    questionOrder = 0;
                    await sectionRepository.UpdateAsync(section);
                    currentSection = section;
                }
                else if (item.Type == "question")
                {
                    var question = await questionRepository.GetAsync(item.Id) ?? throw new AbpValidationException("QuestionId not found.");
                    question.Order = questionOrder;
                    questionOrder++;
                    if (currentSection != null)
                    {
                        question.SectionId = currentSection.Id;
                    }
                    await questionRepository.UpdateAsync(question);
                }
                else
                {
                    throw new AbpValidationException("Invalid type!");
                }
            }
        }

        public async Task<List<ScoresheetDto>> GetAllPublishedScoresheetsAsync()
        {
            var result = await scoresheetRepository.GetPublishedListAsync();
            return ObjectMapper.Map<List<Scoresheet>, List<ScoresheetDto>>(result);
        }

        public async Task<List<Guid>> GetNumericQuestionIdsAsync(List<Guid> questionIdsToCheck)
        {
            var existingQuestions = await questionRepository.GetListAsync();
            return existingQuestions.Where(q => questionIdsToCheck.Contains(q.Id) && q.Type == QuestionType.Number).Select(q => q.Id).ToList();
        }

        public async Task ValidateChangeableScoresheet(Guid scoresheetId)
        {
            var scoresheet = await scoresheetRepository.GetAsync(scoresheetId);
            if (scoresheet.Published)
            {
                throw new UserFriendlyException("Cannot change scoresheet.  Scoresheet is already published.");
            }
        }

        public async Task PublishScoresheetAsync(Guid id)
        {
            var scoresheet = await scoresheetRepository.GetAsync(id);

            scoresheet.Published = true;

            if (await featureChecker.IsEnabledAsync(FeatureConsts.Reporting))
            {
                scoresheet = reportingFieldsGeneratorService.GenerateAndSet(scoresheet);
            }

            await scoresheetRepository.UpdateAsync(scoresheet);
        }

        public async Task<List<QuestionDto>> GetYesNoQuestionsAsync(List<Guid> questionIdsToCheck)
        {
            var result = await GetQuestionsAsync(questionIdsToCheck, QuestionType.YesNo);
            return ObjectMapper.Map<List<Question>, List<QuestionDto>>(result);
        }

        private async Task<List<Question>> GetQuestionsAsync(List<Guid> questionIdsToCheck, QuestionType type)
        {
            var existingQuestions = await questionRepository.GetListAsync();
            var result = existingQuestions.Where(q => questionIdsToCheck.Contains(q.Id) && q.Type == type).ToList();
            return result;
        }

        public async Task<ExportScoresheetDto> ExportScoresheet(Guid scoresheetId)
        {
            var worksheet = await scoresheetRepository.GetAsync(scoresheetId, true);
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

            string? name;

            var scoresheets = await scoresheetRepository.GetByNameStartsWithAsync(SheetParserFunctions.RemoveTrailingNumbers(scoresheet.Name));
            uint maxVersion = 0;
            uint newVersion = 0;

            if (scoresheets.Count > 0)
            {
                maxVersion = scoresheets.Max(s => s.Version);
                newVersion = maxVersion + 1;
            }
            else
            {
                newVersion = scoresheet.Version;
            }

            name = scoresheet.Name.Replace($"-v{scoresheet.Version}", $"-v{newVersion}");

            var newScoresheet = new Scoresheet(Guid.NewGuid(), scoresheet.Title, name)
            {
                Version = newVersion
            };

            foreach (var section in scoresheet.Sections)
            {
                var clonedSection = new ScoresheetSection(Guid.NewGuid(), section.Name, section.Order);

                foreach (var question in section.Fields.OrderBy(s => s.Order))
                {
                    var clonedQuestion = new Question(Guid.NewGuid(), question.Name, question.Label, question.Type, question.Order, question.Description, question.Definition);
                    clonedSection.CloneQuestion(clonedQuestion);
                }
                newScoresheet.CloneSection(clonedSection);
            }

            newScoresheet.Published = false;

            await scoresheetRepository.InsertAsync(newScoresheet);
        }

        public async Task<List<QuestionDto>> GetSelectListQuestionsAsync(List<Guid> questionIdsToCheck)
        {
            var result = await GetQuestionsAsync(questionIdsToCheck, QuestionType.SelectList);
            return ObjectMapper.Map<List<Question>, List<QuestionDto>>(result);
        }

        public async Task SaveScoresheetOrder(List<Guid> scoresheetIds)
        {
            uint index = 0;
            foreach (Guid id in scoresheetIds)
            {
                var scoresheet = await scoresheetRepository.GetAsync(id);
                scoresheet.Order = index++;
                await scoresheetRepository.UpdateAsync(scoresheet);
            }
        }
    }
}
