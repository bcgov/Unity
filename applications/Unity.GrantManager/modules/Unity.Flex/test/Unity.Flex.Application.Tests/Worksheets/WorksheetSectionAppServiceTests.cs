using System.Threading.Tasks;
using System;
using Unity.Flex.Domain.Worksheets;
using Xunit;
using Xunit.Abstractions;
using Shouldly;
using Volo.Abp.Domain.Entities;
using System.Linq;
using Volo.Abp.Uow;

namespace Unity.Flex.Worksheets
{
    public class WorksheetSectionAppServiceTests : FlexApplicationTestBase
    {
        private readonly IWorksheetSectionAppService _worksheetSectionAppService;
        private readonly IWorksheetRepository _worksheetRepository;
        private readonly IWorksheetSectionRepository _worksheetSectionRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public WorksheetSectionAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _worksheetSectionAppService = GetRequiredService<IWorksheetSectionAppService>();
            _worksheetRepository = GetRequiredService<IWorksheetRepository>();
            _worksheetSectionRepository = GetRequiredService<IWorksheetSectionRepository>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetWorksheetSection()
        {
            // Arrange           
            var title = "Section Test";
            var name = "Section Test";
            var sectionName = "Section Test";
            var newWorkSheet = new Worksheet(Guid.NewGuid(), name, title);

            var sectionId = Guid.NewGuid();
            newWorkSheet.Sections.Add(new WorksheetSection(sectionId, sectionName));
            await _worksheetRepository.InsertAsync(newWorkSheet, true);

            // Act
            var worksheetSection = await _worksheetSectionAppService.GetAsync(sectionId);

            // Assert
            worksheetSection.ShouldNotBeNull();
            worksheetSection.Name.ShouldBe(sectionName);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task DeleteWorksheetSection()
        {
            // Arrange           
            var title = "Delete Section Test";
            var name = "Delete Section Test";
            var sectionName = "Delete Section Test";
            var newWorkSheet = new Worksheet(Guid.NewGuid(), name, title);

            var sectionId = Guid.NewGuid();
            newWorkSheet.Sections.Add(new WorksheetSection(sectionId, sectionName));
            await _worksheetRepository.InsertAsync(newWorkSheet, true);

            // Act
            await _worksheetSectionAppService.DeleteAsync(sectionId);

            // Assert
            _ = await _worksheetSectionRepository.GetAsync(sectionId).ShouldThrowAsync<EntityNotFoundException>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task EditWorksheetSection()
        {
            // Arrange           
            var title = "Edit Section Test";
            var name = "Edit Section Test";
            var sectionName = "Edit Section Test";

            var newWorkSheet = new Worksheet(Guid.NewGuid(), name, title);
            var sectionId = Guid.NewGuid();
            newWorkSheet.Sections.Add(new WorksheetSection(sectionId, sectionName));
            await _worksheetRepository.InsertAsync(newWorkSheet, true);

            // Act
            await _worksheetSectionAppService.EditAsync(sectionId, new EditSectionDto() { Name = "Updated" });

            // Assert
            var section = await _worksheetSectionRepository.GetAsync(sectionId);
            section.ShouldNotBeNull();
            section!.Name.ShouldBe("Updated");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task EditWorksheetSectionCustomField()
        {
            // Arrange           
            var title = "Create Field Test";
            var name = "Create Field Test";
            var sectionName = "Section Field Test";

            var newWorkSheet = new Worksheet(Guid.NewGuid(), name, title);
            var sectionId = Guid.NewGuid();
            newWorkSheet.Sections.Add(new WorksheetSection(sectionId, sectionName));
            await _worksheetRepository.InsertAsync(newWorkSheet, true);

            // Act
            await _worksheetSectionAppService.CreateCustomFieldAsync(sectionId,
                new CreateCustomFieldDto()
                {
                    Key = "Key",
                    Label = "Label",
                    Type = CustomFieldType.Text,
                    Definition = null
                });

            // Assert
            var worksheet = await _worksheetRepository.GetAsync(newWorkSheet.Id, true);
            worksheet.ShouldNotBeNull();
            worksheet.Sections.Count.ShouldBe(1);
            worksheet.Sections[0].Fields.Count.ShouldBe(1);
            worksheet.Sections[0].Fields[0].Key.ShouldBe("Key");
            worksheet.Sections[0].Fields[0].Label.ShouldBe("Label");
        }

        [Theory]
        [Trait("Category", "Integration")]
        [InlineData(2, 0, "Field3", "Field1", "Field2", "Field4", "Field5")]
        [InlineData(3, 0, "Field4", "Field1", "Field2", "Field3", "Field5")]
        [InlineData(1, 4, "Field1", "Field3", "Field4", "Field5", "Field2")]
        [InlineData(1, 1, "Field1", "Field2", "Field3", "Field4", "Field5")]
        [InlineData(2, 3, "Field1", "Field2", "Field4", "Field3", "Field5")]
        [InlineData(3, 2, "Field1", "Field2", "Field4", "Field3", "Field5")]
        [InlineData(4, 0, "Field5", "Field1", "Field2", "Field3", "Field4")]
        [InlineData(0, 4, "Field2", "Field3", "Field4", "Field5", "Field1")]        
        public async Task ResequenceWorksheetSectionFields(uint srcIndx,
            uint targetIndx,
            string newSequence1Key,
            string newSequence2Key,
            string newSequence3Key,
            string newSequence4Key,
            string newSequence5Key)
        {
            // Arrange           
            var title = "Resequence Field Test";
            var name = "resequence-v1";
            var sectionName = "Section1";

            using var uow = _unitOfWorkManager.Begin();
            var newWorkSheet = new Worksheet(Guid.NewGuid(), name, title);
            var sectionId = Guid.NewGuid();

            // Setup fields to reqsequence (order 1, 2, 3 matching names)
            newWorkSheet.Sections.Add(new WorksheetSection(sectionId, sectionName));
            newWorkSheet.Sections[0].Fields.Add(new CustomField(Guid.NewGuid(), "Field1", name, "Field 1", CustomFieldType.Text, null).SetOrder(1));
            newWorkSheet.Sections[0].Fields.Add(new CustomField(Guid.NewGuid(), "Field2", name, "Field 2", CustomFieldType.Text, null).SetOrder(2));
            newWorkSheet.Sections[0].Fields.Add(new CustomField(Guid.NewGuid(), "Field3", name, "Field 3", CustomFieldType.Text, null).SetOrder(3));
            newWorkSheet.Sections[0].Fields.Add(new CustomField(Guid.NewGuid(), "Field4", name, "Field 4", CustomFieldType.Text, null).SetOrder(4));
            newWorkSheet.Sections[0].Fields.Add(new CustomField(Guid.NewGuid(), "Field5", name, "Field 5", CustomFieldType.Text, null).SetOrder(5));

            await _worksheetRepository.InsertAsync(newWorkSheet, true);
            await uow.SaveChangesAsync();

            // Act
            await _worksheetSectionAppService.ResequenceCustomFieldsAsync(sectionId, srcIndx, targetIndx);

            // Field 1 = index 0
            // Field 2 = index 1
            // Field 3 = index 2
            // Field 4 = index 3
            // Field 5 = index 4

            // Assert
            // expect Field 3 (indx 2) to move to start (indx 0), so order of fields should be field 3, field 1, field 2
            var worksheet = await _worksheetRepository.GetAsync(newWorkSheet.Id, true);
            worksheet.ShouldNotBeNull();
            worksheet.Sections.Count.ShouldBe(1);
            var fields = worksheet.Sections[0].Fields.OrderBy(s => s.Order).ToList();            
            fields[0].Key.ShouldBe(newSequence1Key);
            fields[1].Key.ShouldBe(newSequence2Key);
            fields[2].Key.ShouldBe(newSequence3Key);
            fields[3].Key.ShouldBe(newSequence4Key);
            fields[4].Key.ShouldBe(newSequence5Key);
        }
    }
}
