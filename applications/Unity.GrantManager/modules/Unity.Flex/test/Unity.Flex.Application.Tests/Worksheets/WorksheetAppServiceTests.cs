using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace Unity.Flex.Worksheets
{
    public class WorksheetAppServiceTests : FlexApplicationTestBase
    {
        private readonly IWorksheetAppService _worksheetAppService;
        private readonly IWorksheetRepository _worksheetRepository;

        public WorksheetAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _worksheetAppService = GetRequiredService<IWorksheetAppService>();
            _worksheetRepository = GetRequiredService<IWorksheetRepository>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CreateWorksheet()
        {
            // Arrange           
            var title = "New Worksheet";
            var name = "New Worksheet";
            var sanitizedName = "newworksheet";

            // Act
            var applicationDto = await _worksheetAppService.CreateAsync(new CreateWorksheetDto()
            {
                Name = name,
                Sections = [],
                Title = title
            });

            // Assert
            var worksheet = await _worksheetRepository.GetAsync(applicationDto.Id);
            worksheet.Title.ShouldBe(title);
            worksheet.Name.ShouldBe(sanitizedName);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CreateWorksheet_DuplicateName_ShouldFail()
        {
            // Arrange           
            var title = "Duplicate Name Test";
            var name = "Duplicate Name Test";
            _ = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(),
               name,
               title),
               true);

            // Act & Assert
            await _worksheetAppService.CreateAsync(new CreateWorksheetDto()
            {
                Name = name,
                Sections = [],
                Title = title
            })
            .ShouldThrowAsync<UserFriendlyException>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CreateWorksheetSection()
        {
            // Arrange
            var sectionTest = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(),
                "Section Test",
                "Section Test"),
                true);

            // Act
            _ = await _worksheetAppService.CreateSectionAsync(sectionTest.Id, new CreateSectionDto()
            {
                Name = "Section1"
            });

            // Assert
            var worksheet = await _worksheetRepository.GetAsync(sectionTest.Id, true);
            worksheet.Sections.Count.ShouldBe(1);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CreateWorksheetSection_DuplcateName_ShouldFail()
        {
            // Arrange           
            var title = "Duplicate Section Test";
            var name = "Duplicate Section Test";
            var sectionName = "Duplicate";
            var newDupSection = new Worksheet(Guid.NewGuid(),
               name,
               title);

            newDupSection.Sections.Add(new WorksheetSection(Guid.NewGuid(), sectionName));
            await _worksheetRepository.InsertAsync(newDupSection, true);

            // Act & Assert
            _ = await _worksheetAppService.CreateSectionAsync(newDupSection.Id, new CreateSectionDto()
            {
                Name = "Duplicate"
            })
            .ShouldThrowAsync<UserFriendlyException>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetList()
        {
            // Arrange           
            var title1 = "Title 1";
            var name1 = "Name 1";
            var title2 = "Title 2";
            var name2 = "Name 2";
            var title3 = "Title 3";
            var name3 = "Name 3";
            _ = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(),
               name1,
               title1),
               true);
            _ = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(),
               name2,
               title2),
               true);
            _ = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(),
               name3,
               title3),
               true);

            // Act
            var worksheets = _ = await _worksheetAppService.GetListAsync();

            // Assert
            worksheets.Count.ShouldBe(3);
        }
    }
}
