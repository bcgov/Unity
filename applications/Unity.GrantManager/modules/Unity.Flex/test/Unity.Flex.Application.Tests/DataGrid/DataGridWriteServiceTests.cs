using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Web.Pages.Flex;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Values;
using Unity.Flex.WorksheetInstances;
using Xunit;

namespace Unity.Flex.DataGrid;

public class DataGridWriteServiceTests
{
    private readonly ICustomFieldAppService _customFieldAppService;
    private readonly ICustomFieldValueAppService _customFieldValueAppService;
    private readonly IWorksheetInstanceAppService _worksheetInstanceAppService;
    private readonly DataGridWriteService _service;

    public DataGridWriteServiceTests()
    {
        _customFieldAppService = Substitute.For<ICustomFieldAppService>();
        _customFieldValueAppService = Substitute.For<ICustomFieldValueAppService>();
        _worksheetInstanceAppService = Substitute.For<IWorksheetInstanceAppService>();
        _service = new DataGridWriteService(_customFieldAppService, _customFieldValueAppService, _worksheetInstanceAppService);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteRowAsync_RemovesCorrectRow()
    {
        // Arrange
        var valueId = Guid.NewGuid();
        var worksheetInstanceId = Guid.NewGuid();

        SetupValueWithRows(valueId, ["row0", "row1", "row2"]);

        string? capturedJson = null;
        _customFieldValueAppService
            .ExplicitSetAsync(valueId, Arg.Do<string>(j => capturedJson = j))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteRowAsync(valueId, 1, worksheetInstanceId);

        // Assert
        var rows = DeserializeRows(capturedJson);
        rows.Count.ShouldBe(2);
        rows[0].Cells[0].Value.ShouldBe("row0");
        rows[1].Cells[0].Value.ShouldBe("row2");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteRowAsync_LastRow_LeavesEmptyRowsArray()
    {
        // Arrange
        var valueId = Guid.NewGuid();
        var worksheetInstanceId = Guid.NewGuid();

        SetupValueWithRows(valueId, ["row0"]);

        string? capturedJson = null;
        _customFieldValueAppService
            .ExplicitSetAsync(valueId, Arg.Do<string>(j => capturedJson = j))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteRowAsync(valueId, 0, worksheetInstanceId);

        // Assert
        var rows = DeserializeRows(capturedJson);
        rows.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteRowAsync_OutOfBoundsRow_DoesNotPersist()
    {
        // Arrange
        var valueId = Guid.NewGuid();
        var worksheetInstanceId = Guid.NewGuid();

        SetupValueWithRows(valueId, ["row0"]);

        // Act
        await _service.DeleteRowAsync(valueId, 5, worksheetInstanceId);

        // Assert
        await _customFieldValueAppService
            .DidNotReceive()
            .ExplicitSetAsync(Arg.Any<Guid>(), Arg.Any<string>());
    }

    private void SetupValueWithRows(Guid valueId, IEnumerable<string> rowValues)
    {
        var rows = rowValues.Select(v => new DataGridRow
        {
            Cells = [new DataGridRowCell("col", v)]
        }).ToList();

        var json = JsonSerializer.Serialize(new DataGridValue(new DataGridRowsValue(rows)));

        _customFieldValueAppService
            .GetAsync(valueId)
            .Returns(new CustomFieldValueDto { Id = valueId, CurrentValue = json });
    }

    private static List<DataGridRow> DeserializeRows(string? json)
    {
        json.ShouldNotBeNull();
        var grid = JsonSerializer.Deserialize<DataGridValue>(json!);
        var rowsJson = grid!.Value!.ToString()!;
        return JsonSerializer.Deserialize<DataGridRowsValue>(rowsJson)!.Rows;
    }
}
