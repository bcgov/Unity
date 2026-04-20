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

        [Fact]
        public async Task ParseDataGridField_DynamicWithFormSchema_ShouldExtractColumnsFromChefsSchema()
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
                @"{""dynamic"": true, ""columns"": [], ""summaryOption"": ""None""}");
            section.AddField(field);
            await uow.SaveChangesAsync();

            worksheet = await _worksheetRepository.GetAsync(worksheet.Id);

            // The header mapping maps "field.Name.DataGrid" -> the CHEFS datagrid key
            var submissionHeaderMapping = $@"{{""{field.Name}.DataGrid"": ""chefsDataGrid1""}}";

            var formSchema = @"{
                ""components"": [
                    {
                        ""key"": ""chefsDataGrid1"",
                        ""type"": ""datagrid"",
                        ""components"": [
                            { ""key"": ""firstName"", ""label"": ""First Name"", ""type"": ""textfield"" },
                            { ""key"": ""amount"", ""label"": ""Amount"", ""type"": ""number"" }
                        ]
                    }
                ]
            }";

            // Act
            var result = WorksheetFieldSchemaParser.ParseField(field, worksheet, formSchema, submissionHeaderMapping);

            // Assert — should extract columns from CHEFS schema, no dynamic placeholder
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);
            result.ShouldNotContain(c => c.Key == "dynamic_columns");

            var firstNameCol = result.FirstOrDefault(c => c.Key == "firstName");
            firstNameCol.ShouldNotBeNull();
            firstNameCol.Label.ShouldBe("First Name");
            firstNameCol.Type.ShouldBe("Text");

            var amountCol = result.FirstOrDefault(c => c.Key == "amount");
            amountCol.ShouldNotBeNull();
            amountCol.Label.ShouldBe("Amount");
            amountCol.Type.ShouldBe("Numeric");
        }

        [Fact]
        public async Task ParseDataGridField_DynamicWithFormSchema_ShouldMergeDefinedColumnsWithChefsExtracted()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();

            var worksheet = new Worksheet(Guid.NewGuid(), "TestWorksheet", "Test Worksheet");
            var section = new WorksheetSection(Guid.NewGuid(), "TestSection");
            worksheet.Sections.Add(section);

            await _worksheetRepository.InsertAsync(worksheet, true);
            await uow.SaveChangesAsync();

            // Definition has both dynamic=true AND static columns defined (mixed grid scenario)
            var field = new CustomField(Guid.NewGuid(), "testDataGrid", "TestWorksheet", "Test DataGrid",
                CustomFieldType.DataGrid,
                @"{""dynamic"": true, ""columns"": [{""name"": ""staticCol"", ""type"": ""Text""}], ""summaryOption"": ""None""}");
            section.AddField(field);
            await uow.SaveChangesAsync();

            worksheet = await _worksheetRepository.GetAsync(worksheet.Id);

            var submissionHeaderMapping = $@"{{""{field.Name}.DataGrid"": ""chefsGrid""}}";

            var formSchema = @"{
                ""components"": [
                    {
                        ""key"": ""chefsGrid"",
                        ""type"": ""datagrid"",
                        ""components"": [
                            { ""key"": ""dynamicCol"", ""label"": ""Dynamic Column"", ""type"": ""textfield"" }
                        ]
                    }
                ]
            }";

            // Act
            var result = WorksheetFieldSchemaParser.ParseField(field, worksheet, formSchema, submissionHeaderMapping);

            // Assert — CHEFS only returns dynamic columns; statically-defined columns must still be emitted
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);
            result.ShouldNotContain(c => c.Key == "dynamic_columns");

            var dynamicCol = result.FirstOrDefault(c => c.Key == "dynamicCol");
            dynamicCol.ShouldNotBeNull();
            dynamicCol.Label.ShouldBe("Dynamic Column");
            dynamicCol.Type.ShouldBe("Text");

            var staticCol = result.FirstOrDefault(c => c.Key == "staticCol");
            staticCol.ShouldNotBeNull();
            staticCol.Type.ShouldBe("Text");
        }

        [Fact]
        public async Task ParseDataGridField_DynamicWithNoHeaderMapping_ShouldFallBackToPlaceholder()
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
                @"{""dynamic"": true, ""columns"": [{""name"": ""col1"", ""type"": ""Text""}], ""summaryOption"": ""None""}");
            section.AddField(field);
            await uow.SaveChangesAsync();

            worksheet = await _worksheetRepository.GetAsync(worksheet.Id);

            // Header mapping does NOT contain an entry for this field
            var submissionHeaderMapping = @"{""unrelated_key.DataGrid"": ""someGrid""}";
            var formSchema = @"{ ""components"": [] }";

            // Act
            var result = WorksheetFieldSchemaParser.ParseField(field, worksheet, formSchema, submissionHeaderMapping);

            // Assert — no matching header mapping, so dynamic placeholder + static columns
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2); // 1 dynamic placeholder + 1 defined column

            result.ShouldContain(c => c.Key == "dynamic_columns");
            result.ShouldContain(c => c.Key == "col1");
        }

        [Fact]
        public async Task ParseDataGridField_DynamicOnlyWithNoHeaderMapping_ShouldReturnOnlyPlaceholder()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();

            var worksheet = new Worksheet(Guid.NewGuid(), "TestWorksheet", "Test Worksheet");
            var section = new WorksheetSection(Guid.NewGuid(), "TestSection");
            worksheet.Sections.Add(section);

            await _worksheetRepository.InsertAsync(worksheet, true);
            await uow.SaveChangesAsync();

            // Purely dynamic grid: dynamic=true and no static columns defined
            var field = new CustomField(Guid.NewGuid(), "testDataGrid", "TestWorksheet", "Test DataGrid",
                CustomFieldType.DataGrid,
                @"{""dynamic"": true, ""columns"": [], ""summaryOption"": ""None""}");
            section.AddField(field);
            await uow.SaveChangesAsync();

            worksheet = await _worksheetRepository.GetAsync(worksheet.Id);

            // Header mapping does NOT contain an entry for this field, so CHEFS extraction cannot resolve
            var submissionHeaderMapping = @"{""unrelated_key.DataGrid"": ""someGrid""}";
            var formSchema = @"{ ""components"": [] }";

            // Act
            var result = WorksheetFieldSchemaParser.ParseField(field, worksheet, formSchema, submissionHeaderMapping);

            // Assert — dynamic-only with no CHEFS resolution should yield exactly the placeholder
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);

            var placeholder = result.First();
            placeholder.Key.ShouldBe("dynamic_columns");
            placeholder.Type.ShouldBe("Dynamic");
        }

        [Fact]
        public async Task ParseDataGridField_DynamicWithNestedFormSchema_ShouldFindDataGridInPanel()
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
                @"{""dynamic"": true, ""columns"": [], ""summaryOption"": ""None""}");
            section.AddField(field);
            await uow.SaveChangesAsync();

            worksheet = await _worksheetRepository.GetAsync(worksheet.Id);

            var submissionHeaderMapping = $@"{{""{field.Name}.DataGrid"": ""nestedGrid""}}";

            // DataGrid is nested inside a panel component
            var formSchema = @"{
                ""components"": [
                    {
                        ""key"": ""panel1"",
                        ""type"": ""panel"",
                        ""components"": [
                            {
                                ""key"": ""nestedGrid"",
                                ""type"": ""datagrid"",
                                ""components"": [
                                    { ""key"": ""nestedCol"", ""label"": ""Nested Column"", ""type"": ""textarea"" }
                                ]
                            }
                        ]
                    }
                ]
            }";

            // Act
            var result = WorksheetFieldSchemaParser.ParseField(field, worksheet, formSchema, submissionHeaderMapping);

            // Assert — should find the datagrid inside the panel via recursive search
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result.ShouldNotContain(c => c.Key == "dynamic_columns");

            var col = result.First();
            col.Key.ShouldBe("nestedCol");
            col.Label.ShouldBe("Nested Column");
            col.Type.ShouldBe("Text"); // textarea maps to text
        }

        [Fact]
        public async Task ParseDataGridField_DynamicWithLayoutColumns_ShouldFindDataGridInLayoutColumn()
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
                @"{""dynamic"": true, ""columns"": [], ""summaryOption"": ""None""}");
            section.AddField(field);
            await uow.SaveChangesAsync();

            worksheet = await _worksheetRepository.GetAsync(worksheet.Id);

            var submissionHeaderMapping = $@"{{""{field.Name}.DataGrid"": ""layoutGrid""}}";

            // DataGrid is inside a layout "columns" element
            var formSchema = @"{
                ""components"": [
                    {
                        ""key"": ""layout1"",
                        ""type"": ""columns"",
                        ""columns"": [
                            {
                                ""components"": [
                                    {
                                        ""key"": ""layoutGrid"",
                                        ""type"": ""datagrid"",
                                        ""components"": [
                                            { ""key"": ""layoutCol"", ""label"": ""Layout Column"", ""type"": ""currency"" }
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }";

            // Act
            var result = WorksheetFieldSchemaParser.ParseField(field, worksheet, formSchema, submissionHeaderMapping);

            // Assert — should find the datagrid inside layout columns
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result.ShouldNotContain(c => c.Key == "dynamic_columns");

            var col = result.First();
            col.Key.ShouldBe("layoutCol");
            col.Label.ShouldBe("Layout Column");
            col.Type.ShouldBe("Currency");
        }

        [Fact]
        public async Task ParseDataGridField_DynamicWithInvalidFormSchema_ShouldFallBackToPlaceholder()
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
                @"{""dynamic"": true, ""columns"": [], ""summaryOption"": ""None""}");
            section.AddField(field);
            await uow.SaveChangesAsync();

            worksheet = await _worksheetRepository.GetAsync(worksheet.Id);

            var submissionHeaderMapping = $@"{{""{field.Name}.DataGrid"": ""someGrid""}}";
            var formSchema = "not valid json {{{";

            // Act
            var result = WorksheetFieldSchemaParser.ParseField(field, worksheet, formSchema, submissionHeaderMapping);

            // Assert — invalid form schema should fall back to dynamic placeholder
            result.ShouldNotBeNull();
            result.ShouldContain(c => c.Key == "dynamic_columns");
            result.First(c => c.Key == "dynamic_columns").Type.ShouldBe("Dynamic");
        }

        [Fact]
        public async Task ParseDataGridField_DynamicWithFormSchemaKeyMismatch_ShouldFallBackToPlaceholder()
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
                @"{""dynamic"": true, ""columns"": [], ""summaryOption"": ""None""}");
            section.AddField(field);
            await uow.SaveChangesAsync();

            worksheet = await _worksheetRepository.GetAsync(worksheet.Id);

            // Header mapping points to a key that does NOT exist in the form schema
            var submissionHeaderMapping = $@"{{""{field.Name}.DataGrid"": ""nonExistentGrid""}}";
            var formSchema = @"{
                ""components"": [
                    {
                        ""key"": ""differentGrid"",
                        ""type"": ""datagrid"",
                        ""components"": [
                            { ""key"": ""col1"", ""label"": ""Col 1"", ""type"": ""textfield"" }
                        ]
                    }
                ]
            }";

            // Act
            var result = WorksheetFieldSchemaParser.ParseField(field, worksheet, formSchema, submissionHeaderMapping);

            // Assert — key mismatch means no columns extracted, should fall back to placeholder
            result.ShouldNotBeNull();
            result.ShouldContain(c => c.Key == "dynamic_columns");
        }
    }
}