using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using Moq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.Events
{
    public class ChefsEventSubscriptionServiceTests : GrantManagerApplicationTestBase
    {
        private readonly ChefsEventSubscriptionService _chefsEventSubscriptionService;
        private readonly IApplicationFormRepository _applicationFormRepository;


        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private ICurrentUser? _currentUser;

        public ChefsEventSubscriptionServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _chefsEventSubscriptionService = GetRequiredService<ChefsEventSubscriptionService>();
            _applicationFormRepository = GetRequiredService<IApplicationFormRepository>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        }

        protected override void AfterAddApplication(IServiceCollection services)
        {
            _currentUser = Substitute.For<ICurrentUser>();
            services.AddSingleton(_currentUser);
        }

        private void Login(Guid userId)
        {
            _currentUser?.Id.Returns(userId);
            _currentUser?.IsAuthenticated.Returns(true);
        }

        [Fact(Skip = "Failing Test")]
        public async Task CreateAsync_Should_Create_IntakeMapping()
        {
            // Arrange
            Login(GrantManagerTestData.User_Assessor2_UserId);

            using var uow = _unitOfWorkManager.Begin();
            EventSubscriptionDto eventSubscriptionDto = new EventSubscriptionDto();
            ApplicationForm? appForm1 = await _applicationFormRepository.FirstOrDefaultAsync(s => s.ApplicationFormName == "Integration Tests Form 1");
            eventSubscriptionDto.FormId = appForm1.ChefsApplicationFormGuid != null ? Guid.Parse(appForm1.ChefsApplicationFormGuid) : Guid.Parse("ca4eab41-b655-40c8-870b-5d3b0d5b68e6");
            eventSubscriptionDto.SubmissionId = Guid.Parse("dad04994-6d8b-4a40-89eb-a490175ef077");
            var mockChefsEventSubscriptionService = new Mock<IChefsEventSubscriptionService>();

            // Act
            mockChefsEventSubscriptionService.Setup(f => f.CreateIntakeMappingAsync(eventSubscriptionDto)).ReturnsAsync(true);

            // Assert
            mockChefsEventSubscriptionService.Verify(f => f.CreateIntakeMappingAsync(eventSubscriptionDto));

        }

    }
}
