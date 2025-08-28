using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Integrations.Chefs;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.Events
{
    public class ChefsEventSubscriptionServiceTests : GrantManagerApplicationTestBase
    {
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IChefsEventSubscriptionService _chefsEventSubscriptionService;

        public ChefsEventSubscriptionServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _chefsEventSubscriptionService = GetRequiredService<IChefsEventSubscriptionService>();
            _applicationFormRepository = GetRequiredService<IApplicationFormRepository>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        }

        protected override void AfterAddApplication(IServiceCollection services)
        {
            _currentUser = Substitute.For<ICurrentUser>();

            // Cannot mock arg dynamic with NSubstitute very easily testbench classes below to test out the mapping as needed
            ISubmissionsApiService submissionApiMock = new SubmissionApiServiceMock();
            IFormsApiService formsApiMock = new FormsApiServiceMock();
            IIntakeFormSubmissionMapper intakeSubmissionMapperMock = new IntakeFormSubmissionMapperMock();

            services.AddSingleton(_currentUser);
            services.AddSingleton(submissionApiMock);
            services.AddSingleton(formsApiMock);
            services.AddSingleton(intakeSubmissionMapperMock);
        }

        new private void Login(Guid userId)
        {
            _currentUser?.Id.Returns(userId);
            _currentUser?.IsAuthenticated.Returns(true);
        }


        [Fact]
        [Trait("Category", "Integration")]
        public async Task CreateAsync_Should_Create_IntakeMapping()
        {
            // Arrange
            Login(GrantManagerTestData.User2_UserId);

            using var uow = _unitOfWorkManager.Begin();
            EventSubscriptionDto eventSubscriptionDto = new();
            ApplicationForm? appForm1 = await _applicationFormRepository.FirstOrDefaultAsync(s => s.ApplicationFormName == "Integration Tests Form 1");
            eventSubscriptionDto.FormId = appForm1!.ChefsApplicationFormGuid != null ? Guid.Parse(appForm1.ChefsApplicationFormGuid) : Guid.Parse("ca4eab41-b655-40c8-870b-5d3b0d5b68e6");
            eventSubscriptionDto.SubmissionId = Guid.Parse("dad04994-6d8b-4a40-89eb-a490175ef077");

            // Act
            await _chefsEventSubscriptionService
                .CreateIntakeMappingAsync(eventSubscriptionDto)
                .ShouldThrowAsync<NotImplementedException>();
        }

        public class SubmissionApiServiceMock : ISubmissionsApiService
        {
            public Task<dynamic?> GetSubmissionDataAsync(Guid chefsFormId, Guid submissionId)
            {
                throw new NotImplementedException();
            }
        }

        public class FormsApiServiceMock : IFormsApiService
        {
            public Task<object> GetForm(Guid? formId, string chefsApplicationFormGuid, string encryptedApiKey)
            {
                throw new NotImplementedException();
            }

            public Task<dynamic?> GetFormDataAsync(string chefsFormId, string chefsFormVersionId)
            {
                throw new NotImplementedException();
            }

            public Task<JObject?> GetSubmissionDataAsync(Guid chefsFormId, Guid submissionId)
            {
                throw new NotImplementedException();
            }

            Task<JObject> IFormsApiService.GetForm(Guid? formId, string chefsApplicationFormGuid, string encryptedApiKey)
            {
                throw new NotImplementedException();
            }

            Task<JObject?> IFormsApiService.GetFormDataAsync(string chefsFormId, string chefsFormVersionId)
            {
                throw new NotImplementedException();
            }
        }

        public class IntakeFormSubmissionMapperMock : IIntakeFormSubmissionMapper
        {
            public Dictionary<Guid, string> ExtractSubmissionFiles(dynamic formSubmission)
            {
                throw new NotImplementedException();
            }

            public string InitializeAvailableFormFields(dynamic formVersion)
            {
                throw new NotImplementedException();
            }

            public IntakeMapping MapFormSubmissionFields(ApplicationForm applicationForm, dynamic formSubmission, string? mapFormSubmissionFields)
            {
                throw new NotImplementedException();
            }

            public Task ResyncSubmissionAttachments(Guid applicationId, dynamic formSubmission)
            {
                throw new NotImplementedException();
            }

            public Task SaveChefsFiles(dynamic formSubmission, Guid applicationId)
            {
                throw new NotImplementedException();
            }
        }
    }
}
