using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Reporting.Configuration;
using Unity.Reporting.Configuration.FieldsProviders;
using Unity.Reporting.Domain.Configuration;
using Xunit;

namespace Unity.GrantManager.Web.Pages.ApplicationForms;

public class ConsolidatedFormVersionLimitTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(580)]
    public async Task Metadata_Should_Fetch_Only_Ten_Newest_Versions_And_Preserve_Ascending_Field_Order(int count)
    {
        var versions = CreateVersions(count);
        var (provider, metadata, formId) = CreateProvider(versions);

        var result = await provider.GetFieldsMetadataAsync(formId);

        var expected = versions.OrderByDescending(version => version.Version).Take(10)
            .OrderBy(version => version.Version).ToList();
        AssertFetchedVersions(metadata, expected);
        result.Fields.Select(field => field.Path).ShouldBe(expected.Select(version => $"field{version.Version}"));
        result.Metadata.ShouldNotBeNull().Info.Keys.OrderBy(key => key).ShouldBe(
            expected.Select(version => $"formversion_{version.Id}").OrderBy(key => key));
    }

    [Fact]
    public async Task ChangeDetection_Should_Fetch_Only_Ten_Newest_Versions()
    {
        var versions = CreateVersions(580);
        var (provider, metadata, formId) = CreateProvider(versions);
        var current = versions.Where(version => version.Version > 570).ToList();

        var changes = await provider.DetectChangesAsync(formId, SavedMapping(current));

        changes.ShouldBeNull();
        AssertFetchedVersions(metadata, current.OrderBy(version => version.Version).ToList());
    }

    [Fact]
    public async Task ChangeDetection_Should_Report_Versions_Entering_And_Leaving_Current_Window()
    {
        var versions = CreateVersions(581);
        var (provider, metadata, formId) = CreateProvider(versions);
        var previouslySelected = versions.Where(version => version.Version > 570 && version.Version <= 580);

        var changes = await provider.DetectChangesAsync(formId, SavedMapping(previouslySelected));

        changes.ShouldBe("Version added: v581; Version removed: v571");
        AssertFetchedVersions(metadata, versions.Where(version => version.Version > 571)
            .OrderBy(version => version.Version).ToList());
    }

    private static List<ApplicationFormVersionDto> CreateVersions(int count) =>
        Enumerable.Range(1, count).Select(number => new ApplicationFormVersionDto
        {
            Id = Guid.NewGuid(),
            Version = number
        }).OrderBy(version => version.Version % 17).ToList();

    private static (ConsolidatedFormVersionFieldsProvider Provider, IFormMetadataService Metadata, Guid FormId)
        CreateProvider(List<ApplicationFormVersionDto> versions)
    {
        var formId = Guid.NewGuid();
        var forms = Substitute.For<IApplicationFormAppService>();
        forms.GetVersionsAsync(formId).Returns(versions);
        var metadata = Substitute.For<IFormMetadataService>();
        var byId = versions.ToDictionary(version => version.Id);
        metadata.GetFormComponentMetaDataAsync(Arg.Any<Guid>()).Returns(call =>
        {
            var version = byId[call.Arg<Guid>()];
            return new FormComponentMetaDataDto
            {
                Components =
                [
                    new FormComponentMetaDataItemDto
                    {
                        Key = $"field{version.Version}",
                        Path = $"field{version.Version}",
                        Label = $"Version {version.Version}",
                        Type = "textfield"
                    }
                ]
            };
        });
        return (new ConsolidatedFormVersionFieldsProvider(forms, metadata), metadata, formId);
    }

    private static ReportColumnsMap SavedMapping(IEnumerable<ApplicationFormVersionDto> versions) =>
        new()
        {
            Mapping = JsonSerializer.Serialize(new Unity.Reporting.Domain.Configuration.Mapping
            {
                Metadata = new MapMetadata
                {
                    Info = versions.ToDictionary(version => $"formversion_{version.Id}", version => $"v{version.Version}")
                }
            })
        };

    private static void AssertFetchedVersions(IFormMetadataService metadata, IList<ApplicationFormVersionDto> expected)
    {
        var requestedIds = metadata.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IFormMetadataService.GetFormComponentMetaDataAsync))
            .Select(call => (Guid)call.GetArguments()[0]!);
        requestedIds.ShouldBe(expected.Select(version => version.Id));
    }
}
