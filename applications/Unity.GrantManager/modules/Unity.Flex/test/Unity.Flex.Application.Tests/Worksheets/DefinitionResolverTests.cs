using System.Text.Json;
using Shouldly;
using Unity.Flex;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Definitions;
using Xunit;

namespace Unity.Flex.Application.Tests.Worksheets;

public class DefinitionResolverTests
{
    [Fact]
    public void Resolve_Should_Preserve_JsonObject_When_Definition_Is_JsonElement()
    {
        using var document = JsonDocument.Parse("""{"required":true,"maxLength":100}""");

        var definition = DefinitionResolver.Resolve(CustomFieldType.Text, document.RootElement);

        definition.ShouldBe("""{"required":true,"maxLength":100}""");
        definition.ConvertDefinition(CustomFieldType.Text)!.Required.ShouldBeTrue();
    }
}
