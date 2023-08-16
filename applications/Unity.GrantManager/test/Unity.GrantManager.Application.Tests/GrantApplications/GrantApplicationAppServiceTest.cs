using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Xunit;
using NSubstitute;

namespace Unity.GrantManager.GrantApplications;

public class GrantApplicationAppServiceTest : GrantManagerApplicationTestBase
{
    public GrantApplicationAppServiceTest()
    {        
    }

    [Fact]
    public async Task Should_Get_All_GrantApplications_Without_Any_Filter()
    {
        // TODO: this need fixing 
        // Arrange
        /*
        var fakeRepo = Substitute.For<IRepository<GrantApplication, Guid>>();
        GrantApplicationAppService _appService = new GrantApplicationAppService(fakeRepo);

        // Act
        var result = await _appService.GetListAsync(new GetApplicationListDto());

        // Assert
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(12);
        result.Items.ShouldContain(application => application.ProjectName == "New Helicopter Fund");
        result.Items.ShouldContain(application => application.ProjectName == "Shoebox");
        */
    }
}
