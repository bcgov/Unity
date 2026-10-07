using System;
using Shouldly;
using Unity.Flex.Domain.Worksheets;
using Xunit;

namespace Unity.Flex.Worksheets;

public class WorksheetAvailabilityFilterTests
{
    [Fact]
    public void Published_filter_excludes_archived_worksheets()
    {
        var worksheet = CreateWorksheet(isArchived: true);

        WorksheetAvailabilityFilter.Matches(worksheet, null, WorksheetAvailabilityStatus.Published)
            .ShouldBeFalse();
    }

    [Fact]
    public void All_filter_includes_published_archived_worksheets()
    {
        var worksheet = CreateWorksheet(isArchived: true);

        WorksheetAvailabilityFilter.Matches(worksheet, null, WorksheetAvailabilityStatus.All)
            .ShouldBeTrue();
    }

    [Fact]
    public void Archived_filter_excludes_published_non_archived_worksheets()
    {
        var worksheet = CreateWorksheet(isArchived: false);

        WorksheetAvailabilityFilter.Matches(worksheet, null, WorksheetAvailabilityStatus.Archived)
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData("Funding", true)]
    [InlineData("funding-v1", true)]
    [InlineData("unrelated", false)]
    public void Search_matches_title_or_technical_name(string searchText, bool expected)
    {
        var worksheet = CreateWorksheet(title: "Funding Application", name: "funding-v1");

        WorksheetAvailabilityFilter.Matches(worksheet, searchText, WorksheetAvailabilityStatus.All)
            .ShouldBe(expected);
    }

    [Fact]
    public void Unpublished_worksheets_are_never_available()
    {
        var worksheet = CreateWorksheet(isPublished: false, isArchived: true);

        WorksheetAvailabilityFilter.Matches(worksheet, null, WorksheetAvailabilityStatus.All)
            .ShouldBeFalse();
        WorksheetAvailabilityFilter.Matches(worksheet, null, WorksheetAvailabilityStatus.Archived)
            .ShouldBeFalse();
    }

    [Fact]
    public void Worksheet_mapper_preserves_archived_status()
    {
        var worksheet = new Worksheet(Guid.NewGuid(), "technical-name", "Worksheet title")
            .SetPublished(true)
            .SetArchived(true);

        var result = new WorksheetToWorksheetBasicDtoMapper().Map(worksheet);

        result.IsArchived.ShouldBeTrue();
        result.Published.ShouldBeTrue();
    }

    private static WorksheetBasicDto CreateWorksheet(
        string title = "Worksheet",
        string name = "worksheet",
        bool isPublished = true,
        bool isArchived = false)
    {
        return new WorksheetBasicDto
        {
            Title = title,
            Name = name,
            Published = isPublished,
            IsArchived = isArchived
        };
    }
}