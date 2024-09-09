using Shouldly;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System;
using Volo.Abp.Domain.Repositories;
using Unity.GrantManager.Applications;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications
{
    public class ApplicationContactAppServiceTests : GrantManagerApplicationTestBase
    {
        private readonly IApplicationContactService _applicationContactService;
        private readonly IRepository<Application, Guid> _applicationsRepository;
       
        public ApplicationContactAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _applicationContactService = GetRequiredService<IApplicationContactService>();
            _applicationsRepository = GetRequiredService<IRepository<Application, Guid>>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetListAsync_Should_Return_ApplicationContacts()
        {
            // Arrange
            var application = (await _applicationsRepository.GetListAsync())[0];
          
            var applicationTags = (await _applicationContactService.GetListByApplicationAsync(application.Id)).ToList();
            applicationTags.Count.ShouldBeGreaterThanOrEqualTo(0);
        }

    }
}
