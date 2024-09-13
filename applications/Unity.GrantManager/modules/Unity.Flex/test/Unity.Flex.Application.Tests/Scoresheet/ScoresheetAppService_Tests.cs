using Shouldly;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets;
using Unity.Flex.Worksheets.Values;
using Xunit;
using Xunit.Abstractions;

namespace Unity.Flex.Scoresheet
{
    public class ScoresheetAppService_Tests : FlexApplicationTestBase
    {
        private readonly IQuestionAppService _questionAppService;
        private readonly IScoresheetAppService _scoresheetAppService;
        private readonly ISectionAppService _sectionAppService;
        private readonly IScoresheetRepository _scoresheetRepository;
        public ScoresheetAppService_Tests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _scoresheetRepository = GetRequiredService<IScoresheetRepository>();
            _sectionAppService = GetRequiredService<ISectionAppService>();
            _scoresheetAppService = GetRequiredService<IScoresheetAppService>();
            _questionAppService = GetRequiredService<IQuestionAppService>();
        }

        [Fact]
        public async Task CreateScoresheet()
        {
            // Arrange
            var scoresheetTitle = "Test Scoresheet";
            var scoresheet = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto { Title = scoresheetTitle });

            // Act
            var scoresheetFromRepo = await _scoresheetRepository.GetAsync(scoresheet.Id);

            // Assert
            scoresheetFromRepo.ShouldNotBeNull();
            scoresheetFromRepo.Id.ShouldBeEquivalentTo(scoresheet.Id);
        }

        [Fact]
        public async Task CreateQuestion()
        {
            // Arrange
            var scoresheet = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto { Title = "Test Scoresheet"});
            _ = await _scoresheetAppService.CreateSectionAsync(scoresheet.Id, new CreateSectionDto { Name = "Test Section" });
            var questionName = "Test Question";
            var questionLabel = "Test Label";
            var description = "Test Description";
            string definition = "{\"min\":2,\"max\":6}";

            // Act
            var question = await _scoresheetAppService.CreateQuestionInHighestOrderSectionAsync(scoresheet.Id, new CreateQuestionDto { Name = questionName, Label = questionLabel, Description = description, QuestionType = 1, Definition = JsonSerializer.Deserialize<NumericValue>(definition)?.Value });

            // Assert
            question.ShouldNotBeNull();
            question.Name.ShouldBeEquivalentTo(questionName);
            question.Label.ShouldBeEquivalentTo(questionLabel);
        }

        [Fact]
        public async Task CreateSection()
        {
            // Arrange
            var scoresheet = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto { Title = "Test Scoresheet" });
            var sectionName = "Test Section";

            // Act
            var section = await _scoresheetAppService.CreateSectionAsync(scoresheet.Id, new CreateSectionDto { Name = sectionName });
        
            // Assert
            section.ShouldNotBeNull();
            section.Name.ShouldBeEquivalentTo(sectionName);
        }

        [Fact]
        public async Task UpdateSection()
        {
            // Arrange
            var scoresheet = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto { Title = "Test Scoresheet" });
            var sectionName = "Test Section";
            var section = await _scoresheetAppService.CreateSectionAsync(scoresheet.Id, new CreateSectionDto { Name = sectionName });
            var newSectionName = "New Test Section";

            // Act
            var updatedSection = await _sectionAppService.UpdateAsync(section.Id, new EditSectionDto { Name = newSectionName });

            // Assert
            updatedSection.ShouldNotBeNull();
            updatedSection.Id.ShouldBeEquivalentTo(section.Id);
            updatedSection.Name.ShouldBeEquivalentTo(newSectionName);
        }

        [Fact]
        public async Task UpdateQuestion()
        {
            // Arrange
            var scoresheet = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto { Title = "Test Scoresheet" });
            _ = await _scoresheetAppService.CreateSectionAsync(scoresheet.Id, new CreateSectionDto { Name = "Test Section" });
            var question = await _scoresheetAppService.CreateQuestionInHighestOrderSectionAsync(scoresheet.Id, new CreateQuestionDto { Name = "Test Question", Label = "Test Label", Description = "Test Description", QuestionType = 1, Definition = JsonSerializer.Deserialize<NumericValue>("{\"min\":0,\"max\":3}")?.Value });
            var updatedQuestionName = "Updated Test Question";
            var updatedQuestionLabel = "Updated Test Label";
            var updatedDescription = "Updated Test Description";
            string updatedDefinition = "{\"min\":2,\"max\":6}";

            // Act
            var updatedQuestion = await _questionAppService.UpdateAsync(question.Id, new EditQuestionDto { Name = updatedQuestionName, Label = updatedQuestionLabel, Description = updatedDescription, QuestionType = 1, Definition = JsonSerializer.Deserialize<NumericValue>(updatedDefinition)?.Value });

            // Assert
            updatedQuestion.ShouldNotBeNull();
            updatedQuestion.Id.ShouldBeEquivalentTo(question.Id);
            updatedQuestion.Name.ShouldBeEquivalentTo(updatedQuestionName);
            updatedQuestion.Label.ShouldBeEquivalentTo(updatedQuestionLabel);
        }

        [Fact]
        public async Task UpdateScoresheet()
        {
            // Arrange
            var scoresheet = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto { Title = "Scoresheet" });
            var scoresheetTitle = "Updated Test Scoresheet";

            // Act
            await _scoresheetAppService.UpdateAsync(scoresheet.Id, new EditScoresheetDto { Title = scoresheetTitle });
            var updatedScoresheet = await _scoresheetRepository.GetAsync(scoresheet.Id);

            // Assert
            updatedScoresheet.ShouldNotBeNull();
            updatedScoresheet.Id.ShouldBeEquivalentTo(scoresheet.Id);
            updatedScoresheet.Title.ShouldBeEquivalentTo(scoresheetTitle);
        }
    }
}
