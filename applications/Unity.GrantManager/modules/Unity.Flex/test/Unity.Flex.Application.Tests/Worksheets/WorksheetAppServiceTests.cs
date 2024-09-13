using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Unity.Flex.Worksheets
{
    public class WorksheetAppServiceTests : FlexApplicationTestBase
    {
        private readonly IWorksheetAppService _worksheetAppService;
        private readonly IWorksheetRepository _worksheetRepository;
        private readonly IWorksheetLinkRepository _worksheetLinkRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public WorksheetAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _worksheetAppService = GetRequiredService<IWorksheetAppService>();
            _worksheetRepository = GetRequiredService<IWorksheetRepository>();
            _worksheetLinkRepository = GetRequiredService<IWorksheetLinkRepository>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetWorksheet()
        {
            // Arrange           
            var worksheetDb = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Get", "Get Me"), true);

            // Act
            var worksheet = await _worksheetAppService.GetAsync(worksheetDb.Id);

            // Assert
            worksheet.ShouldNotBeNull();
            worksheet.Title.ShouldBe("Get Me");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task EditWorksheet()
        {
            // Arrange           
            var worksheetDb = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Edit", "Edit Me"), true);

            // Act
            var worksheet = await _worksheetAppService.EditAsync(worksheetDb.Id, new EditWorksheetDto() { Title = "Okay, sure" });

            // Assert
            var editedWorksheet = await _worksheetAppService.GetAsync(worksheet.Id);

            editedWorksheet.ShouldNotBeNull();
            editedWorksheet.Title.ShouldBe("Okay, sure");
        }


        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetWorksheetListByCorrelation_ShouldBeEmptyList()
        {
            // Arrange           
            var correlationId = Guid.NewGuid();
            var correlationProvider = "UnitTest";

            // Act
            var result = await _worksheetAppService.GetListByCorrelationAsync(correlationId, correlationProvider);

            // Assert
            result.Count.ShouldBe(0);
        }


        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetWorksheetListByCorrelation_ShouldHaveOne()
        {
            // Arrange           
            var correlationId = Guid.NewGuid();
            var correlationProvider = "UnitTest";
            var anchor = "UnitTest";

            var worksheet = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Ws", "Ws"), true);
            _ = await _worksheetLinkRepository.InsertAsync(new WorksheetLink(Guid.NewGuid(), worksheet.Id, correlationId, correlationProvider, anchor), true);

            // Act
            var result = await _worksheetAppService.GetListByCorrelationAsync(correlationId, correlationProvider);

            // Assert
            result.Count.ShouldBe(1);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetWorksheetListByCorrelationAnchor_ShouldBeNull()
        {
            // Arrange           
            var correlationId = Guid.NewGuid();
            var correlationProvider = "UnitTest";
            var anchor = "Unit Test";

            // Act
            var result = await _worksheetAppService.GetByCorrelationAnchorAsync(correlationId, correlationProvider, anchor);

            // Assert
            result.ShouldBeNull();
        }


        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetWorksheetListByCorrelationAnchor_ShouldNotBeNull()
        {
            // Arrange           
            var correlationId = Guid.NewGuid();
            var correlationProvider = "UnitTest";
            var anchor = "UnitTest";

            var worksheet = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Ws", "Ws"), true);
            _ = await _worksheetLinkRepository.InsertAsync(new WorksheetLink(Guid.NewGuid(), worksheet.Id, correlationId, correlationProvider, anchor), true);

            // Act
            var result = await _worksheetAppService.GetByCorrelationAnchorAsync(correlationId, correlationProvider, anchor);

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task DeleteWorksheet()
        {
            // Arrange           
            var worksheetDb = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Delete", "Delete Me"), true);

            // Act
            var worksheet = await _worksheetAppService.GetAsync(worksheetDb.Id);
            await _worksheetAppService.DeleteAsync(worksheet.Id);

            // Assert
            _ = _worksheetAppService.GetAsync(worksheet.Id).ShouldThrowAsync<EntityNotFoundException>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Exists_ShouldBeTrue()
        {
            // Arrange           
            var worksheetDb = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Exists", "I Exist"), true);

            // Act
            var exists = await _worksheetAppService.ExistsAsync(worksheetDb.Id);

            // Assert
            exists.ShouldBeTrue();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Exists_ShouldBeFalse()
        {
            // Arrange           
            _ = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Exists", "I Exist"), true);

            // Act
            var exists = await _worksheetAppService.ExistsAsync(Guid.NewGuid());

            // Assert
            exists.ShouldBeFalse();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task PublishWorksheet()
        {
            // Arrange           
            var worksheet = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Publish", "PublishMe"), true);

            // Act
            _ = await _worksheetAppService.PublishAsync(worksheet.Id);

            // Assert
            var worksheetPublished = await _worksheetRepository.GetAsync(worksheet.Id);
            worksheetPublished.Published.ShouldBeTrue();
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

        // Section: 1, 2, 3, 4, 5
        // 1, 2, 1, 4, 5
        // 3, 1, 2, 4, 5

        [Theory]
        [Trait("Category", "Integration")]
        [InlineData(2, 0, "Section 3", "Section 1", "Section 2", "Section 4", "Section 5")]
        [InlineData(3, 0, "Section 4", "Section 1", "Section 2", "Section 3", "Section 5")]
        [InlineData(1, 4, "Section 1", "Section 3", "Section 4", "Section 5", "Section 2")]
        [InlineData(1, 1, "Section 1", "Section 2", "Section 3", "Section 4", "Section 5")]
        [InlineData(2, 3, "Section 1", "Section 2", "Section 4", "Section 3", "Section 5")]
        [InlineData(3, 2, "Section 1", "Section 2", "Section 4", "Section 3", "Section 5")]
        [InlineData(4, 0, "Section 5", "Section 1", "Section 2", "Section 3", "Section 4")]
        [InlineData(0, 4, "Section 2", "Section 3", "Section 4", "Section 5", "Section 1")]
        public async Task ResequenceWorksheetSections(uint srcIndx, 
            uint targetIndx, 
            string newSequence1Name,
            string newSequence2Name,
            string newSequence3Name,
            string newSequence4Name,
            string newSequence5Name)
        {
            // Arrange
            var name = "resequenceme-v1";
            var title = "Resequence Me";
            var section1Name = "Section 1";
            var section2Name = "Section 2";
            var section3Name = "Section 3";
            var section4Name = "Section 4";
            var section5Name = "Section 5";

            // Setup worksheets to reqsequence (order 1, 2, 3, 4, 5 matching names)
            using var uow = _unitOfWorkManager.Begin();
            var newWorksheet = new Worksheet(Guid.NewGuid(), name, title);
            var section1 = new WorksheetSection(Guid.NewGuid(), section1Name).SetOrder(1);            
            var section2 = new WorksheetSection(Guid.NewGuid(), section2Name).SetOrder(2);            
            var section3 = new WorksheetSection(Guid.NewGuid(), section3Name).SetOrder(3);
            var section4 = new WorksheetSection(Guid.NewGuid(), section4Name).SetOrder(4);
            var section5 = new WorksheetSection(Guid.NewGuid(), section5Name).SetOrder(5);            
            newWorksheet.Sections.Add(section1);
            newWorksheet.Sections.Add(section2);
            newWorksheet.Sections.Add(section3);
            newWorksheet.Sections.Add(section4);
            newWorksheet.Sections.Add(section5);
            await _worksheetRepository.InsertAsync(newWorksheet, true);
            await uow.SaveChangesAsync();

            // section 1 = index 0
            // section 2 = index 1
            // section 3 = index 2
            // section 4 = index 3
            // section 5 = index 4

            // Act
            await _worksheetAppService.ResequenceSectionsAsync(newWorksheet.Id, srcIndx, targetIndx);

            // Assert            
            var resequencedWorksheet = await _worksheetRepository.GetAsync(newWorksheet.Id, true);
            var sections = resequencedWorksheet.Sections.OrderBy(s => s.Order).ToList();
            sections[0].Name.ShouldBe(newSequence1Name);
            sections[1].Name.ShouldBe(newSequence2Name);
            sections[2].Name.ShouldBe(newSequence3Name);
            sections[3].Name.ShouldBe(newSequence4Name);
            sections[4].Name.ShouldBe(newSequence5Name);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CreateWorksheetSection_DuplicateName_ShouldFail()
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
        public async Task CreateWorksheetSection_ExceptionForPublishedWorksheet()
        {
            // Arrange
            var worksheet = new Worksheet(Guid.NewGuid(),
                "Section Test",
                "Section Test");

            var sectionTest = await _worksheetRepository.InsertAsync(worksheet, true);

            await _worksheetAppService.PublishAsync(sectionTest.Id);

            // Act and Assert
            _ = await _worksheetAppService.CreateSectionAsync(sectionTest.Id, new CreateSectionDto()
            {
                Name = "Section1"
            })
            .ShouldThrowAsync<UserFriendlyException>();

        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetWorksheetList()
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

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CloneWorksheet()
        {
            // Arrange           
            var name = "cloneme-v1";
            var title = "Clone Me";
            var sectionName = "Cloned Section";

            using var uow = _unitOfWorkManager.Begin();
            var wsToClone = new Worksheet(Guid.NewGuid(), name, title);
            var sectionToClone = new WorksheetSection(Guid.NewGuid(), sectionName);
            wsToClone.Sections.Add(sectionToClone);
            await _worksheetRepository.InsertAsync(wsToClone, true);
            await uow.SaveChangesAsync();

            wsToClone.Sections[0].AddField(new CustomField(Guid.NewGuid(), "key", wsToClone.Name, "label", CustomFieldType.Text, null));
            await uow.SaveChangesAsync();

            wsToClone = await _worksheetRepository.GetAsync(wsToClone.Id);

            // Act
            var cloned = await _worksheetAppService.CloneAsync(wsToClone.Id);

            // Assert
            var clonedDb = await _worksheetRepository.GetAsync(cloned.Id);
            clonedDb.ShouldNotBeNull();
            clonedDb.Sections.Count.ShouldBe(1);
            clonedDb.Sections[0].Fields.Count.ShouldBe(1);
            clonedDb.Sections[0].Fields[0].Label.ShouldBe("label");
        }
    }
}
