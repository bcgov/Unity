using System.Threading.Tasks;
using System;
using Unity.Flex.Domain.Worksheets;
using Xunit.Abstractions;
using Xunit;
using Shouldly;
using Volo.Abp.Uow;
using Volo.Abp.Domain.Entities;

namespace Unity.Flex.Worksheets
{
    public class CustomFieldAppServiceTests : FlexApplicationTestBase
    {
        private readonly ICustomFieldAppService _customFieldAppService;
        private readonly IWorksheetRepository _worksheetRepository;
        private readonly ICustomFieldRepository _customFieldRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public CustomFieldAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _customFieldAppService = GetRequiredService<ICustomFieldAppService>();
            _worksheetRepository = GetRequiredService<IWorksheetRepository>();
            _customFieldRepository = GetRequiredService<ICustomFieldRepository>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetCustomField()
        {
            // Arrange            
            var customFieldId = await CreateCustomField("key", "label");

            // Act
            var customField = await _customFieldAppService.GetAsync(customFieldId);

            // Assert
            customField.ShouldNotBeNull();
            customField.Key.ShouldBe("key");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task EditCustomField()
        {
            // Arrange                      
            var customFieldId = await CreateCustomField("key", "label");

            // Act
            await _customFieldAppService.EditAsync(customFieldId, new EditCustomFieldDto()
            {
                Key = "editKeyU",
                Type = CustomFieldType.Numeric,
                Label = "editLabelU",
                Definition = null
            });

            // Assert
            var customField = await _customFieldRepository.GetAsync(customFieldId);
            customField.ShouldNotBeNull();
            customField.Key.ShouldBe("editKeyU");
            customField.Label.ShouldBe("editLabelU");
            customField.Type.ShouldBe(CustomFieldType.Numeric);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task DeleteCustomField()
        {
            // Arrange                                  
            var customFieldId = await CreateCustomField("key", "label");

            // Act
            await _customFieldAppService.DeleteAsync(customFieldId);

            // Assert
            await _customFieldRepository.GetAsync(customFieldId).ShouldThrowAsync<EntityNotFoundException>();
        }

        private async Task<Guid> CreateCustomField(string key, string label)
        {
            var customFieldId = Guid.NewGuid();
            var uow = _unitOfWorkManager.Begin();
            var newWorksheet = new Worksheet(Guid.NewGuid(), "unittest-v1", "Title");
            var section1 = new WorksheetSection(Guid.NewGuid(), "section1").SetOrder(1);
            newWorksheet.Sections.Add(section1);
            section1.Fields.Add(new CustomField(customFieldId, key, "unittest-v1", label, CustomFieldType.Text, null));
            await _worksheetRepository.InsertAsync(newWorksheet, true);
            await uow.SaveChangesAsync();
            return customFieldId;
        }
    }
}
