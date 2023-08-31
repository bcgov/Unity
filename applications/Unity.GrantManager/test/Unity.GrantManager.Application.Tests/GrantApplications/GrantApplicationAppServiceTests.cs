using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Unity.GrantManager.GrantApplications;

public class GrantApplicationAppServiceTests : GrantManagerApplicationTestBase
{
    private readonly IGrantApplicationAppService _grantApplicationAppService;

    public GrantApplicationAppServiceTests()
    {
        _grantApplicationAppService = GetRequiredService<IGrantApplicationAppService>();            
    }

    protected override IServiceCollection CreateServiceCollection()
    {
        var serviceCollection = base.CreateServiceCollection();
        serviceCollection.AddTransient<IGrantApplicationAppService>();
        return serviceCollection;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Add_Integration_Test_Application()
    {        
        // Act
        var grantApplications = await _grantApplicationAppService.GetListAsync(new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto() { MaxResultCount = 100 });

        // Assert
        grantApplications.Items.Any(s => s.ProjectName == "Application For Integration Test Funding").ShouldBeTrue();        
    }    
}
