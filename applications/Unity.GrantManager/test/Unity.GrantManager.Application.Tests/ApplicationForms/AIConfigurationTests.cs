using Shouldly;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.ApplicationForms;

/// <summary>
/// Tests for form-level AI configuration (AB#32446).
/// Verifies that <see cref="IApplicationFormAppService.PatchAiConfig"/> correctly
/// persists the two AI toggles — AutomaticallyGenerateAIAnalysis and
/// ManuallyInitiateAIAnalysis — on the ApplicationForm entity.
/// These flags act as the form-level gate: even when a tenant has AI enabled,
/// each form can independently opt in or out.
/// </summary>
public class AIConfigurationTests : GrantManagerApplicationTestBase
{
    private readonly IApplicationFormAppService _formAppService;
    private readonly IApplicationFormRepository _formRepository;

    public AIConfigurationTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _formAppService = GetRequiredService<IApplicationFormAppService>();
        _formRepository = GetRequiredService<IApplicationFormRepository>();
    }

    [Fact]
    public async Task Should_PersistAIConfig_When_PatchAiConfigCalled()
    {
        // Arrange - both toggles enabled via the service
        // Act
        await _formAppService.PatchAiConfig(GrantManagerTestData.ApplicationForm1_Id, new AIConfigDto
        {
            AutomaticallyGenerateAIAnalysis = true,
            ManuallyInitiateAIAnalysis = true
        });

        // Assert - values are saved to the entity in the database
        var form = await _formRepository.GetAsync(GrantManagerTestData.ApplicationForm1_Id);
        form.AutomaticallyGenerateAIAnalysis.ShouldBeTrue();
        form.ManuallyInitiateAIAnalysis.ShouldBeTrue();
    }
}
