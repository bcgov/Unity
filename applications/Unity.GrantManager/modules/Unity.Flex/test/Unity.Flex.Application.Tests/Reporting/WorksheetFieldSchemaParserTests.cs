using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Reporting.Configuration;
using Unity.Flex.Worksheets;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Unity.Flex.Reporting
{
    public class WorksheetFieldSchemaParserTests : FlexApplicationTestBase
    {
        private readonly IWorksheetRepository _worksheetRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public WorksheetFieldSchemaParserTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _worksheetRepository = GetRequiredService<IWorksheetRepository>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        }

        [Fact]
        public async Task ParseDataGridField_WithDynamicTrueAndColumns_ShouldReturnDynamicPlaceholderAndColumns()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();
            
            var worksheet = new Worksheet(Guid.NewGuid(), "TestWorksheet", "Test Worksheet");
            var section = new WorksheetSection(Guid.NewGuid(), "TestSection");
            worksheet.Sections.Add(section);
            
            await _worksheetRepository.InsertAsync(worksheet, true);
            await uow.SaveChangesAsync();
            
            var field = new CustomField(Guid.NewGuid(), "testDataGrid", "TestWorksheet", "Test DataGrid", 
                CustomFieldType.DataGrid, 
                @"{""dynamic"": true, ""columns"": [{""name"": ""column1"", ""type"": ""Text""}, {""name"": ""column2"", ""type"": ""Numeric""}], ""summaryOption"": ""None""}");
            section.AddField(field);
            await uow.SaveChangesAsync();
            
            worksheet = await _worksheetRepository.GetAsync(worksheet.Id);

            // Act
            var result = WorksheetFieldSchemaParser.ParseField(field, worksheet);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(3); // 1 dynamic placeholder + 2 defined columns

            var dynamicComponent = result.FirstOrDefault(c => c.Key == "dynamic_columns");
            dynamicComponent.ShouldNotBeNull();
            dynamicComponent.Type.ShouldBe("Dynamic");

            var column1Component = result.FirstOrDefault(c => c.Key == "column1");
            column1Component.ShouldNotBeNull();
            column1Component.Type.ShouldBe("Text");

            var column2Component = result.FirstOrDefault(c => c.Key == "column2");
            column2Component.ShouldNotBeNull();
            column2Component.Type.ShouldBe("Numeric");
        }

        [Fact]
        public async Task ParseDataGridField_WithDynamicFalseAndColumns_ShouldReturnOnlyColumns()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();
            
            var worksheet = new Worksheet(Guid.NewGuid(), "TestWorksheet", "Test Worksheet");
            var section = new WorksheetSection(Guid.NewGuid(), "TestSection");
            worksheet.Sections.Add(section);
            
            await _worksheetRepository.InsertAsync(worksheet, true);
            await uow.SaveChangesAsync();
            
            var field = new CustomField(Guid.NewGuid(), "testDataGrid", "TestWorksheet", "Test DataGrid", 
                CustomFieldType.DataGrid, 
                @"{""dynamic"": false, ""columns"": [{""name"": ""column1"", ""type"": ""Text""}, {""name"": ""column2"", ""type"": ""Currency""}], ""summaryOption"": ""None""}");
            section.AddField(field);
            await uow.SaveChangesAsync();
            
            worksheet = await _worksheetRepository.GetAsync(worksheet.Id);

            // Act
            var result = WorksheetFieldSchemaParser.ParseField(field, worksheet);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2); // Only the 2 defined columns, no dynamic placeholder

            result.ShouldNotContain(c => c.Key == "dynamic_columns");

            var column1Component = result.FirstOrDefault(c => c.Key == "column1");
            column1Component.ShouldNotBeNull();
            column1Component.Type.ShouldBe("Text");

            var column2Component = result.FirstOrDefault(c => c.Key == "column2");
            column2Component.ShouldNotBeNull();
            column2Component.Type.ShouldBe("Currency");
        }

        [Fact]
        public async Task ParseDataGridField_WithDynamicFalseAndNoColumns_ShouldReturnSimpleComponent()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();
            
            var worksheet = new Worksheet(Guid.NewGuid(), "TestWorksheet", "Test Worksheet");
            var section = new WorksheetSection(Guid.NewGuid(), "TestSection");
            worksheet.Sections.Add(section);
            
            await _worksheetRepository.InsertAsync(worksheet, true);
            await uow.SaveChangesAsync();
            
            var field = new CustomField(Guid.NewGuid(), "testDataGrid", "TestWorksheet", "Test DataGrid", 
                CustomFieldType.DataGrid, 
                @"{""dynamic"": false, ""columns"": [], ""summaryOption"": ""None""}");
            section.AddField(field);
            await uow.SaveChangesAsync();
            
            worksheet = await _worksheetRepository.GetAsync(worksheet.Id);

            // Act
            var result = WorksheetFieldSchemaParser.ParseField(field, worksheet);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            
            var component = result.First();
            component.Id.ShouldBe(field.Id.ToString());
            component.Key.ShouldBe("testDataGrid");
            component.Type.ShouldBe("DataGrid");
        }

        [Fact]
        public async Task ParseField_WithNonDataGridType_ShouldReturnSimpleComponent()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();
            
            var worksheet = new Worksheet(Guid.NewGuid(), "TestWorksheet", "Test Worksheet");
            var section = new WorksheetSection(Guid.NewGuid(), "TestSection");
            worksheet.Sections.Add(section);
            
            await _worksheetRepository.InsertAsync(worksheet, true);
            await uow.SaveChangesAsync();
            
            var field = new CustomField(Guid.NewGuid(), "testTextField", "TestWorksheet", "Test Text Field", 
                CustomFieldType.Text, "{}");
            section.AddField(field);
            await uow.SaveChangesAsync();
            
            worksheet = await _worksheetRepository.GetAsync(worksheet.Id);

            // Act
            var result = WorksheetFieldSchemaParser.ParseField(field, worksheet);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            
            var component = result.First();
            component.Id.ShouldBe(field.Id.ToString());
            component.Key.ShouldBe("testTextField");
            component.Type.ShouldBe("Text");
        }
    }
}