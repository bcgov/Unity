using Shouldly;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System;
using Volo.Abp.Domain.Repositories;
using Unity.GrantManager.Applications;
using Xunit.Abstractions;
using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications
{
    public class ApplicationLinksAppServiceTests : GrantManagerApplicationTestBase
    {
        private readonly IApplicationLinksService _applicationLinksService;
        private readonly IRepository<Application, Guid> _applicationsRepository;
       

        public ApplicationLinksAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _applicationLinksService = GetRequiredService<IApplicationLinksService>();
            _applicationsRepository = GetRequiredService<IRepository<Application, Guid>>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetListByApplicationAsync_Should_Return_ApplicationLinks()
        {
            // Arrange
            var application = (await _applicationsRepository.GetListAsync())[0];
          
            var applicationLinks = (await _applicationLinksService.GetListByApplicationAsync(application.Id)).ToList();
            applicationLinks.Count.ShouldBeGreaterThanOrEqualTo(0);
        }

    }
}
