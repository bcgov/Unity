﻿using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets;
using Xunit;

namespace Unity.Flex.Scoresheet
{
    public class ScoresheetAppService_Tests : FlexApplicationTestBase
    {
        private readonly IQuestionAppService _questionAppService;
        private readonly IScoresheetAppService _scoresheetAppService;
        private readonly ISectionAppService _sectionAppService;
        private readonly IScoresheetRepository _scoresheetRepository;
        public ScoresheetAppService_Tests() 
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
            var scoresheetName = "Test Scoresheet";
            var scoresheet = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto { Name = scoresheetName });

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
            var scoresheet = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto { Name = "Test Scoresheet"});
            _ = await _scoresheetAppService.CreateSectionAsync(scoresheet.Id, new CreateSectionDto { Name = "Test Section" });
            var questionName = "Test Question";
            var questionLabel = "Test Label";

            // Act
            var question = await _scoresheetAppService.CreateQuestionInHighestOrderSectionAsync(scoresheet.Id, new CreateQuestionDto { Name = questionName, Label = questionLabel });

            // Assert
            question.ShouldNotBeNull();
            question.Name.ShouldBeEquivalentTo(questionName);
            question.Label.ShouldBeEquivalentTo(questionLabel);
        }

        [Fact]
        public async Task CreateSection()
        {
            // Arrange
            var scoresheet = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto { Name = "Test Scoresheet" });
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
            var scoresheet = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto { Name = "Test Scoresheet" });
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
            var scoresheet = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto { Name = "Test Scoresheet" });
            _ = await _scoresheetAppService.CreateSectionAsync(scoresheet.Id, new CreateSectionDto { Name = "Test Section" });
            var question = await _scoresheetAppService.CreateQuestionInHighestOrderSectionAsync(scoresheet.Id, new CreateQuestionDto { Name = "Test Question", Label = "Test Label" });
            var questionName = "Updated Test Question";
            var questionLabel = "Updated Test Label";

            // Act
            var updatedQuestion = await _questionAppService.UpdateAsync(question.Id, new EditQuestionDto { Name = questionName, Label = questionLabel });

            // Assert
            updatedQuestion.ShouldNotBeNull();
            updatedQuestion.Id.ShouldBeEquivalentTo(question.Id);
            updatedQuestion.Name.ShouldBeEquivalentTo(questionName);
            updatedQuestion.Label.ShouldBeEquivalentTo(questionLabel);
        }

        [Fact]
        public async Task UpdateScoresheet()
        {
            // Arrange
            var scoresheet = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto { Name = "Scoresheet" });
            var scoresheetName = "Updated Test Scoresheet";

            // Act
            await _scoresheetAppService.UpdateAllAsync(scoresheet.GroupId, new EditScoresheetsDto { Name = scoresheetName });
            var updatedScoresheet = await _scoresheetRepository.GetAsync(scoresheet.Id);

            // Assert
            updatedScoresheet.ShouldNotBeNull();
            updatedScoresheet.Id.ShouldBeEquivalentTo(scoresheet.Id);
            updatedScoresheet.Name.ShouldBeEquivalentTo(scoresheetName);
        }
    }
}