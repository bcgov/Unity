using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Unity.Notifications.Templates;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.Notifications.Templates;

public class TemplatesServiceTests
{
    [Fact]
    public async Task GetTemplateVariables_ForApplicationType_ReturnsApplicationVariables()
    {
        var variables = new[]
        {
            new TemplateVariable { Name = "Application token", Token = "application_token", TemplateType = TemplateTypes.Application },
            new TemplateVariable { Name = "Applicant token", Token = "applicant_token", TemplateType = TemplateTypes.Applicant }
        };
        var service = CreateService(variables);

        var result = await service.GetTemplateVariables(TemplateTypes.Application);

        result.Select(variable => variable.Token).ShouldBe(["application_token"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Unknown")]
    public async Task GetTemplateVariables_WhenTypeIsMissingOrUnknown_DefaultsToApplicantVariables(string? templateType)
    {
        var variables = new[]
        {
            new TemplateVariable { Name = "Application token", Token = "application_token", TemplateType = TemplateTypes.Application },
            new TemplateVariable { Name = "Applicant token", Token = "applicant_token", TemplateType = TemplateTypes.Applicant }
        };
        var service = CreateService(variables);

        var result = await service.GetTemplateVariables(templateType);

        result.Select(variable => variable.Token).ShouldBe(["applicant_token"]);
    }

    private static TemplateService CreateService(IEnumerable<TemplateVariable> variables)
    {
        var variablesRepository = Substitute.For<ITemplateVariablesRepository>();
        variablesRepository.GetQueryableAsync().Returns(Task.FromResult(variables.AsQueryable()));

        return new TemplateService(
            Substitute.For<ITemplatesRepository>(),
            Substitute.For<ICurrentTenant>(),
            variablesRepository);
    }
}