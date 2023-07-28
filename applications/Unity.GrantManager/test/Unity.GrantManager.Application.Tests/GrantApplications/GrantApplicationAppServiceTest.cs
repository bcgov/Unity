using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Unity.GrantManager.GrantApplications;

public class GrantApplicationAppServiceTest : GrantManagerApplicationTestBase
{
    private readonly GrantApplicationAppService _appService;

    public GrantApplicationAppServiceTest()
    {
        _appService = GetRequiredService<GrantApplicationAppService>();
    }

    [Fact]
    public async Task Should_Get_All_GrantApplications_Without_Any_Filter()
    {
        var result = await _appService.GetListAsync(new GetApplicationListDto());
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
        result.Items.ShouldContain(application => application.ApplicationName == "Application For Space Farms Grant");
        result.Items.ShouldContain(application => application.ApplicationName == "Application For BizBusiness Fund");
    }
}
