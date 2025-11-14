using Shouldly;
using System;
using System.Linq;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Reporting.Configuration;
using Unity.Flex.Scoresheets.Enums;
using Unity.Flex.Worksheets.Definitions;
using Xunit;
using Xunit.Abstractions;
using ScoresheetEntity = Unity.Flex.Domain.Scoresheets.Scoresheet;

namespace Unity.Flex.Tests.Reporting
{
    public class ScoresheetFieldSchemaParserTests : FlexApplicationTestBase
    {
        public ScoresheetFieldSchemaParserTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void ParseScoresheet_WithNullScoresheet_ReturnsEmptyList()
        {
            // Arrange
            ScoresheetEntity scoresheet = null;

            // Act
            var result = ScoresheetFieldSchemaParser.ParseScoresheet(scoresheet);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(0);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void ParseScoresheet_WithEmptySections_ReturnsEmptyList()
        {
            // Arrange
            var scoresheet = new ScoresheetEntity(Guid.NewGuid(), "Test Scoresheet", "test_scoresheet");

            // Act
            var result = ScoresheetFieldSchemaParser.ParseScoresheet(scoresheet);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(0);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void ParseScoresheet_WithSimpleQuestions_ReturnsCorrectComponents()
        {
            // Arrange
            var scoresheet = new ScoresheetEntity(Guid.NewGuid(), "Test Scoresheet", "test_scoresheet");
            var section = new ScoresheetSection(Guid.NewGuid(), "Test Section", 1);
            
            // Add simple questions using the string definition constructor to avoid DefinitionResolver issues
            var numberQuestion = new Question(Guid.NewGuid(), "age", "Age", QuestionType.Number, 1, "Enter your age", "{\"min\": 0, \"max\": 150, \"required\": false}");
            var textQuestion = new Question(Guid.NewGuid(), "name", "Full Name", QuestionType.Text, 2, "Enter your name", "{\"required\": true, \"maxLength\": 100, \"minLength\": 1}");
            var yesNoQuestion = new Question(Guid.NewGuid(), "eligible", "Are you eligible?", QuestionType.YesNo, 3, "Select yes or no", "{\"yes_value\": 10, \"no_value\": 0, \"required\": false}");
            var textAreaQuestion = new Question(Guid.NewGuid(), "comments", "Comments", QuestionType.TextArea, 4, "Additional comments", "{\"rows\": 5, \"required\": false, \"maxLength\": 500, \"minLength\": 0}");

            // Manually set the section ID and add to scoresheet first, then add questions
            scoresheet.AddSection(section);
            numberQuestion.SectionId = section.Id;
            textQuestion.SectionId = section.Id;
            yesNoQuestion.SectionId = section.Id;
            textAreaQuestion.SectionId = section.Id;
            
            section.Fields.Add(numberQuestion);
            section.Fields.Add(textQuestion);
            section.Fields.Add(yesNoQuestion);
            section.Fields.Add(textAreaQuestion);

            // Act
            var result = ScoresheetFieldSchemaParser.ParseScoresheet(scoresheet);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(4);

            // Check number question
            var numberComponent = result.FirstOrDefault(c => c.Key == "age");
            numberComponent.ShouldNotBeNull();
            numberComponent.Type.ShouldBe("Number");
            numberComponent.Label.ShouldBe("Age");
            numberComponent.Path.ShouldBe("test_scoresheet->Test_Section->age");
            numberComponent.TypePath.ShouldBe("scoresheet->section->number");
            numberComponent.DataPath.ShouldBe("age");

            // Check text question
            var textComponent = result.FirstOrDefault(c => c.Key == "name");
            textComponent.ShouldNotBeNull();
            textComponent.Type.ShouldBe("Text");
            textComponent.Label.ShouldBe("Full Name");

            // Check yes/no question
            var yesNoComponent = result.FirstOrDefault(c => c.Key == "eligible");
            yesNoComponent.ShouldNotBeNull();
            yesNoComponent.Type.ShouldBe("YesNo");

            // Check textarea question
            var textAreaComponent = result.FirstOrDefault(c => c.Key == "comments");
            textAreaComponent.ShouldNotBeNull();
            textAreaComponent.Type.ShouldBe("TextArea");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void ParseScoresheet_WithSelectListQuestion_ReturnsSingleComponent()
        {
            // Arrange
            var scoresheet = new ScoresheetEntity(Guid.NewGuid(), "Test Scoresheet", "test_scoresheet");
            var section = new ScoresheetSection(Guid.NewGuid(), "Test Section", 1);
            
            // Create a select list question definition matching the example from the user
            var selectListDefinition = new QuestionSelectListDefinition
            {
                Options = new System.Collections.Generic.List<QuestionSelectListOption>
                {
                    new QuestionSelectListOption { Key = "key1", Value = "Option1", NumericValue = 5 },
                    new QuestionSelectListOption { Key = "key2", Value = "Option2", NumericValue = 10 },
                    new QuestionSelectListOption { Key = "key3", Value = "Option3", NumericValue = 15 }
                },
                Required = false
            };

            var selectListDefinitionJson = System.Text.Json.JsonSerializer.Serialize(selectListDefinition);
            var selectListQuestion = new Question(Guid.NewGuid(), "rating", "Rating", QuestionType.SelectList, 1, "Select a rating", selectListDefinitionJson);
            
            scoresheet.AddSection(section);
            selectListQuestion.SectionId = section.Id;
            section.Fields.Add(selectListQuestion);

            // Act
            var result = ScoresheetFieldSchemaParser.ParseScoresheet(scoresheet);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1); // Should return 1 component for the SelectList question, not individual options

            // Check that the SelectList question is represented as a single component
            var selectListComponent = result.FirstOrDefault(c => c.Key == "rating");
            selectListComponent.ShouldNotBeNull();
            selectListComponent.Type.ShouldBe("SelectList");
            selectListComponent.Label.ShouldBe("Rating");
            selectListComponent.Path.ShouldBe("test_scoresheet->Test_Section->rating");
            selectListComponent.TypePath.ShouldBe("scoresheet->section->selectlist");
            selectListComponent.DataPath.ShouldBe("rating");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void ParseScoresheet_WithMultipleSectionsAndQuestions_ReturnsAllComponents()
        {
            // Arrange
            var scoresheet = new ScoresheetEntity(Guid.NewGuid(), "Test Scoresheet", "test_scoresheet");
            
            // Section 1
            var section1 = new ScoresheetSection(Guid.NewGuid(), "Personal Info", 1);
            var nameQuestion = new Question(Guid.NewGuid(), "full_name", "Full Name", QuestionType.Text, 1, "Enter full name", "{\"required\": true, \"maxLength\": 100, \"minLength\": 1}");
            scoresheet.AddSection(section1);
            nameQuestion.SectionId = section1.Id;
            section1.Fields.Add(nameQuestion);
            
            // Section 2
            var section2 = new ScoresheetSection(Guid.NewGuid(), "Assessment", 2);
            var ageQuestion = new Question(Guid.NewGuid(), "age", "Age", QuestionType.Number, 1, "Enter age", "{\"min\": 0, \"max\": 150, \"required\": false}");
            scoresheet.AddSection(section2);
            ageQuestion.SectionId = section2.Id;
            section2.Fields.Add(ageQuestion);

            // Act
            var result = ScoresheetFieldSchemaParser.ParseScoresheet(scoresheet);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);

            var nameComponent = result.FirstOrDefault(c => c.Key == "full_name");
            nameComponent.ShouldNotBeNull();
            nameComponent.Path.ShouldBe("test_scoresheet->Personal_Info->full_name");

            var ageComponent = result.FirstOrDefault(c => c.Key == "age");
            ageComponent.ShouldNotBeNull();
            ageComponent.Path.ShouldBe("test_scoresheet->Assessment->age");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void ParseScoresheet_WithInvalidSelectListDefinition_ReturnsSimpleComponent()
        {
            // Arrange
            var scoresheet = new ScoresheetEntity(Guid.NewGuid(), "Test Scoresheet", "test_scoresheet");
            var section = new ScoresheetSection(Guid.NewGuid(), "Test Section", 1);
            
            // Create a select list question with invalid JSON definition
            var selectListQuestion = new Question(Guid.NewGuid(), "rating", "Rating", QuestionType.SelectList, 1, "Select a rating", "invalid json");
            
            scoresheet.AddSection(section);
            selectListQuestion.SectionId = section.Id;
            section.Fields.Add(selectListQuestion);

            // Act
            var result = ScoresheetFieldSchemaParser.ParseScoresheet(scoresheet);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1); // Should return 1 simple component when JSON parsing fails

            var component = result.FirstOrDefault();
            component.ShouldNotBeNull();
            component.Key.ShouldBe("rating");
            component.Type.ShouldBe("SelectList"); // Should fall back to simple component
        }
    }
}